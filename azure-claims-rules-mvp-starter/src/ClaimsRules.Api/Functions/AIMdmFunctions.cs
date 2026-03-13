using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using ClaimsRules.Api.Models;
using ClaimsRules.Api.Services;

namespace ClaimsRules.Api.Functions;

/// <summary>
/// Azure Functions for AI-powered Master Data Management (MDM)
/// Provides confidence scoring and learning capabilities using OpenAI
/// </summary>
public class AIMdmFunctions
{
    private readonly AIMatchingService _aiMatchingService;
    private readonly ILogger<AIMdmFunctions> _logger;

    public AIMdmFunctions(
        AIMatchingService aiMatchingService,
        ILogger<AIMdmFunctions> logger)
    {
        _aiMatchingService = aiMatchingService;
        _logger = logger;
    }

    /// <summary>
    /// POST /api/mdm/ai-score
    /// Evaluate patient match confidence using AI
    /// Returns confidence score (0-100) and recommended decision
    /// Cost: ~$0.0004 per evaluation
    /// </summary>
    [Function("AIMdmScore")]
    public async Task<HttpResponseData> ScoreMatch(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "mdm/ai-score")] HttpRequestData req)
    {
        _logger.LogInformation("AI MDM Score: evaluating match confidence");

        try
        {
            // Parse request body
            var body = await req.ReadAsStringAsync();
            var request = JsonSerializer.Deserialize<AIMatchRequest>(body!)
                ?? throw new ArgumentException("Invalid request body");

            // Validate request
            if (request.Record1 == null || request.Record2 == null)
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteAsJsonAsync(new
                {
                    error = "Both Record1 and Record2 are required"
                });
                return badRequest;
            }

            // Evaluate match with AI
            var result = await _aiMatchingService.EvaluateMatchAsync(request);

            // Return response
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(result);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating AI match");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new
            {
                error = "Failed to evaluate match",
                details = ex.Message
            });
            return errorResponse;
        }
    }

    /// <summary>
    /// POST /api/mdm/ai-learn
    /// Provide human reviewer feedback to improve AI model
    /// Stores learning data in Cosmos DB for future model retraining
    /// </summary>
    [Function("AIMdmLearn")]
    public async Task<HttpResponseData> LearnFromReviewer(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "mdm/ai-learn")] HttpRequestData req)
    {
        _logger.LogInformation("AI MDM Learn: processing reviewer feedback");

        try
        {
            // Parse request body
            var body = await req.ReadAsStringAsync();
            var request = JsonSerializer.Deserialize<AILearningRequest>(body!)
                ?? throw new ArgumentException("Invalid request body");

            // Process feedback
            var result = await _aiMatchingService.LearnFromReviewerAsync(request);

            // Return response
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(result);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing AI learning feedback");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new
            {
                error = "Failed to process learning feedback",
                details = ex.Message
            });
            return errorResponse;
        }
    }

    /// <summary>
    /// GET /api/mdm/ai-metrics
    /// Get AI model performance metrics
    /// Returns accuracy, precision, recall, auto-approve rate, and cost
    /// </summary>
    [Function("AIMdmMetrics")]
    public async Task<HttpResponseData> GetMetrics(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "mdm/ai-metrics")] HttpRequestData req)
    {
        _logger.LogInformation("AI MDM Metrics: retrieving model performance");

        try
        {
            var metrics = await _aiMatchingService.GetModelMetricsAsync();

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(metrics);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving AI metrics");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new
            {
                error = "Failed to retrieve metrics",
                details = ex.Message
            });
            return errorResponse;
        }
    }

    /// <summary>
    /// POST /api/mdm/ai-batch-score
    /// Batch evaluate multiple match candidates
    /// Processes up to 100 candidates at once for efficiency
    /// </summary>
    [Function("AIMdmBatchScore")]
    public async Task<HttpResponseData> BatchScoreMatches(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "mdm/ai-batch-score")] HttpRequestData req)
    {
        _logger.LogInformation("AI MDM Batch Score: evaluating multiple matches");

        try
        {
            // Parse request body
            var body = await req.ReadAsStringAsync();
            var requests = JsonSerializer.Deserialize<List<AIMatchRequest>>(body!)
                ?? throw new ArgumentException("Invalid request body");

            // Limit batch size
            if (requests.Count > 100)
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteAsJsonAsync(new
                {
                    error = "Maximum batch size is 100 requests"
                });
                return badRequest;
            }

            // Process all requests
            var results = new List<AIMatchResponse>();
            foreach (var request in requests)
            {
                try
                {
                    var result = await _aiMatchingService.EvaluateMatchAsync(request);
                    results.Add(result);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to evaluate match {MatchId}", request.MatchId);
                    // Continue with next item
                }
            }

            // Return aggregated response
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                totalRequests = requests.Count,
                successfulEvaluations = results.Count,
                failedEvaluations = requests.Count - results.Count,
                totalCost = results.Sum(r => r.EstimatedCost),
                totalTokens = results.Sum(r => r.TokensUsed),
                results
            });
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing batch AI matches");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new
            {
                error = "Failed to process batch",
                details = ex.Message
            });
            return errorResponse;
        }
    }
}
