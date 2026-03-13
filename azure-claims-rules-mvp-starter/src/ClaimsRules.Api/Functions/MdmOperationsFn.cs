using System.Net;
using System.Text.Json;
using ClaimsRules.Api.Models;
using ClaimsRules.Api.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace ClaimsRules.Api.Functions;

/// <summary>
/// MDM Operations - Patient/$match, Create/Update/Delete links, Metrics
/// </summary>
public class MdmOperationsFn
{
    private readonly ILogger<MdmOperationsFn> _logger;
    private readonly MdmLinkService _linkService;
    private readonly MdmMatchingService _matchingService;
    private readonly CosmosAudit _cosmosAudit;

    public MdmOperationsFn(
        ILogger<MdmOperationsFn> logger,
        MdmLinkService linkService,
        MdmMatchingService matchingService,
        CosmosAudit cosmosAudit)
    {
        _logger = logger;
        _linkService = linkService;
        _matchingService = matchingService;
        _cosmosAudit = cosmosAudit;
    }

    /// <summary>
    /// Patient/$match operation - find matching patients
    /// </summary>
    [Function("PatientMatch")]
    public async Task<HttpResponseData> PatientMatch(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "mdm/Patient/$match")] HttpRequestData req)
    {
        try
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var matchRequest = JsonSerializer.Deserialize<PatientMatchRequest>(requestBody);

            if (matchRequest?.Resource == null)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Invalid request: resource is required");
                return badResponse;
            }

            // Get configuration
            var config = await _linkService.GetConfigurationAsync(matchRequest.ResourceType);

            // Find matches
            var candidates = await _matchingService.FindMatchesAsync(
                matchRequest.ResourceType,
                matchRequest.Resource,
                config);

            // Filter by count
            if (matchRequest.Count.HasValue)
                candidates = candidates.Take(matchRequest.Count.Value).ToList();

            // Filter by match result if requested
            if (matchRequest.OnlyReturnMatches)
                candidates = candidates.Where(c => c.MatchResult == MdmMatchResult.MATCH).ToList();

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonSerializer.Serialize(new MatchResponse
            {
                Candidates = candidates,
                TotalCount = candidates.Count
            }));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in PatientMatch");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync(JsonSerializer.Serialize(new { error = ex.Message }));
            return errorResponse;
        }
    }

    /// <summary>
    /// Create MDM link between golden resource and source
    /// </summary>
    [Function("CreateMdmLink")]
    public async Task<HttpResponseData> CreateMdmLink(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "mdm/link")] HttpRequestData req)
    {
        try
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var linkRequest = JsonSerializer.Deserialize<CreateUpdateLinkRequest>(requestBody);

            if (linkRequest == null || string.IsNullOrEmpty(linkRequest.GoldenResourceId) || string.IsNullOrEmpty(linkRequest.SourceResourceId))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Invalid request: goldenResourceId and sourceResourceId are required");
                return badResponse;
            }

            // Get user from header or default to System
            var userName = req.Headers.TryGetValues("X-User-Name", out var userValues) ? userValues.First() : "System";

            var link = await _linkService.CreateLinkAsync(
                linkRequest.GoldenResourceId,
                linkRequest.SourceResourceId,
                "Patient", // Default to Patient, could be parameter
                linkRequest.MatchResult,
                MdmLinkSource.MANUAL,
                null,
                new Dictionary<string, object> { { "reason", "Manual link creation" } },
                userName);

            var response = req.CreateResponse(HttpStatusCode.Created);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonSerializer.Serialize(link));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CreateMdmLink");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync(JsonSerializer.Serialize(new { error = ex.Message }));
            return errorResponse;
        }
    }

    /// <summary>
    /// Update existing MDM link (e.g., after manual review)
    /// </summary>
    [Function("UpdateMdmLink")]
    public async Task<HttpResponseData> UpdateMdmLink(
        [HttpTrigger(AuthorizationLevel.Function, "put", Route = "mdm/link/{linkId}")] HttpRequestData req,
        string linkId)
    {
        try
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var updateRequest = JsonSerializer.Deserialize<Dictionary<string, object>>(requestBody);

            if (updateRequest == null || !updateRequest.TryGetValue("matchResult", out var matchResultObj))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Invalid request: matchResult is required");
                return badResponse;
            }

            var matchResult = Enum.Parse<MdmMatchResult>(matchResultObj.ToString()!);
            var userName = req.Headers.TryGetValues("X-User-Name", out var userValues) ? userValues.First() : "System";

            var link = await _linkService.UpdateLinkAsync(linkId, matchResult, userName);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonSerializer.Serialize(link));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in UpdateMdmLink");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync(JsonSerializer.Serialize(new { error = ex.Message }));
            return errorResponse;
        }
    }

    /// <summary>
    /// Get MDM link by ID
    /// </summary>
    [Function("GetMdmLink")]
    public async Task<HttpResponseData> GetMdmLink(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "mdm/link/{linkId}")] HttpRequestData req,
        string linkId)
    {
        try
        {
            var link = await _linkService.GetLinkByIdAsync(linkId);

            if (link == null)
            {
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteStringAsync($"Link {linkId} not found");
                return notFoundResponse;
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonSerializer.Serialize(link));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetMdmLink");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync(JsonSerializer.Serialize(new { error = ex.Message }));
            return errorResponse;
        }
    }

    /// <summary>
    /// Get all links for a golden resource
    /// </summary>
    [Function("GetGoldenResourceLinks")]
    public async Task<HttpResponseData> GetGoldenResourceLinks(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "mdm/golden-resource/{goldenResourceId}/links")] HttpRequestData req,
        string goldenResourceId)
    {
        try
        {
            var links = await _linkService.GetLinksByGoldenResourceIdAsync(goldenResourceId);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonSerializer.Serialize(links));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetGoldenResourceLinks");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync(JsonSerializer.Serialize(new { error = ex.Message }));
            return errorResponse;
        }
    }

    /// <summary>
    /// Merge two golden resources
    /// </summary>
    [Function("MergeGoldenResources")]
    public async Task<HttpResponseData> MergeGoldenResources(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "mdm/golden-resource/$merge")] HttpRequestData req)
    {
        try
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var mergeRequest = JsonSerializer.Deserialize<MergeGoldenResourcesRequest>(requestBody);

            if (mergeRequest == null || string.IsNullOrEmpty(mergeRequest.FromGoldenResourceId) || string.IsNullOrEmpty(mergeRequest.ToGoldenResourceId))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Invalid request: fromGoldenResourceId and toGoldenResourceId are required");
                return badResponse;
            }

            var userName = req.Headers.TryGetValues("X-User-Name", out var userValues) ? userValues.First() : "System";

            var mergedGolden = await _linkService.MergeGoldenResourcesAsync(
                mergeRequest.FromGoldenResourceId,
                mergeRequest.ToGoldenResourceId,
                mergeRequest.MergedResourceData,
                userName);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonSerializer.Serialize(mergedGolden));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in MergeGoldenResources");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync(JsonSerializer.Serialize(new { error = ex.Message }));
            return errorResponse;
        }
    }

    /// <summary>
    /// Get manual review queue
    /// </summary>
    [Function("GetReviewQueue")]
    public async Task<HttpResponseData> GetReviewQueue(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "mdm/review-queue")] HttpRequestData req)
    {
        try
        {
            var limitStr = req.Query["limit"] ?? "50";
            var limit = int.Parse(limitStr);

            var queueItems = await _linkService.GetPendingReviewItemsAsync(limit);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonSerializer.Serialize(queueItems));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetReviewQueue");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync(JsonSerializer.Serialize(new { error = ex.Message }));
            return errorResponse;
        }
    }

    /// <summary>
    /// Process source resource for MDM
    /// </summary>
    [Function("ProcessSourceResource")]
    public async Task<HttpResponseData> ProcessSourceResource(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "mdm/process/{resourceType}/{resourceId}")] HttpRequestData req,
        string resourceType,
        string resourceId)
    {
        try
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var resource = JsonSerializer.Deserialize<Dictionary<string, object>>(requestBody);

            if (resource == null)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Invalid request: resource data is required");
                return badResponse;
            }

            var userName = req.Headers.TryGetValues("X-User-Name", out var userValues) ? userValues.First() : "System";

            var link = await _linkService.ProcessSourceResourceAsync(resourceType, resourceId, resource, userName);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonSerializer.Serialize(link));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ProcessSourceResource");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync(JsonSerializer.Serialize(new { error = ex.Message }));
            return errorResponse;
        }
    }

    /// <summary>
    /// Get MDM metrics for dashboard
    /// </summary>
    [Function("GetMdmMetrics")]
    public async Task<HttpResponseData> GetMdmMetrics(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "mdm/metrics")] HttpRequestData req)
    {
        try
        {
            var resourceType = req.Query["resourceType"] ?? "Patient";

            // Calculate metrics from Cosmos DB
            var metrics = await CalculateMetricsAsync(resourceType);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonSerializer.Serialize(metrics));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetMdmMetrics");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync(JsonSerializer.Serialize(new { error = ex.Message }));
            return errorResponse;
        }
    }

    private async Task<MdmMetrics> CalculateMetricsAsync(string resourceType)
    {
        var metrics = new MdmMetrics { ResourceType = resourceType };

        // Query link counts by match result
        var allLinksQuery = $"SELECT c.matchResult, COUNT(1) as count FROM c WHERE c.resourceType = '{resourceType}' AND c.matchResult != null GROUP BY c.matchResult";
        var linkCounts = await _cosmosAudit.QueryAsync<Dictionary<string, object>>(allLinksQuery);
        
        foreach (var item in linkCounts)
        {
            if (item.TryGetValue("matchResult", out var matchResultObj) && item.TryGetValue("count", out var countObj))
            {
                var matchResult = Enum.Parse<MdmMatchResult>(matchResultObj.ToString()!);
                var count = Convert.ToInt64(countObj);
                metrics.LinkCountsByResult[matchResult] = count;
            }
        }

        // Query link counts by source
        var autoLinksQuery = $"SELECT VALUE COUNT(1) FROM c WHERE c.resourceType = '{resourceType}' AND c.linkSource = 'AUTO'";
        var autoLinksResult = await _cosmosAudit.QueryAsync<long>(autoLinksQuery);
        metrics.AutoLinksCount = autoLinksResult.FirstOrDefault();

        var manualLinksQuery = $"SELECT VALUE COUNT(1) FROM c WHERE c.resourceType = '{resourceType}' AND c.linkSource = 'MANUAL'";
        var manualLinksResult = await _cosmosAudit.QueryAsync<long>(manualLinksQuery);
        metrics.ManualLinksCount = manualLinksResult.FirstOrDefault();

        // Query golden resource counts
        var activeGoldenQuery = $"SELECT VALUE COUNT(1) FROM c WHERE c.resourceType = '{resourceType}' AND c.isActive = true";
        var activeGoldenResult = await _cosmosAudit.QueryAsync<long>(activeGoldenQuery);
        metrics.ActiveGoldenResources = activeGoldenResult.FirstOrDefault();

        var mergedGoldenQuery = $"SELECT VALUE COUNT(1) FROM c WHERE c.resourceType = '{resourceType}' AND c.isActive = false";
        var mergedGoldenResult = await _cosmosAudit.QueryAsync<long>(mergedGoldenQuery);
        metrics.MergedGoldenResources = mergedGoldenResult.FirstOrDefault();

        metrics.TotalGoldenResources = metrics.ActiveGoldenResources + metrics.MergedGoldenResources;

        // Query review queue metrics
        var pendingQuery = "SELECT VALUE COUNT(1) FROM c WHERE c.status = 'PENDING' AND c.partitionKey = 'queue'";
        var pendingResult = await _cosmosAudit.QueryAsync<long>(pendingQuery);
        metrics.PendingReviewCount = pendingResult.FirstOrDefault();

        var reviewedTodayQuery = $"SELECT VALUE COUNT(1) FROM c WHERE c.status IN ('APPROVED', 'REJECTED') AND c.reviewedDate >= '{DateTime.UtcNow.Date:o}'";
        var reviewedTodayResult = await _cosmosAudit.QueryAsync<long>(reviewedTodayQuery);
        metrics.ReviewedTodayCount = reviewedTodayResult.FirstOrDefault();

        return metrics;
    }
}
