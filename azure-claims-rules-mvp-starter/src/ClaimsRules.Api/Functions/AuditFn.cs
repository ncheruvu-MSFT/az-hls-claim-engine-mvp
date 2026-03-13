using System.Net;
using System.Text.Json;
using ClaimsRules.Api.Models;
using ClaimsRules.Api.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace ClaimsRules.Api.Functions;

public class AuditFn
{
    private readonly ILogger<AuditFn> _logger;
    private readonly AuditService _auditService;

    public AuditFn(ILogger<AuditFn> logger, AuditService auditService)
    {
        _logger = logger;
        _auditService = auditService;
    }

    [Function("SearchAuditLogs")]
    public async Task<HttpResponseData> SearchAuditLogs(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "audit/search")] HttpRequestData req)
    {
        _logger.LogInformation("Searching audit logs");

        try
        {
            var requestBody = await req.ReadAsStringAsync() ?? string.Empty;
            var searchRequest = JsonSerializer.Deserialize<AuditSearchRequest>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new AuditSearchRequest();

            var result = await _auditService.SearchAuditLogsAsync(searchRequest);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(result);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching audit logs");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = ex.Message });
            return errorResponse;
        }
    }

    [Function("GetContractAuditSummary")]
    public async Task<HttpResponseData> GetContractAuditSummary(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "audit/contract/{contractId}/summary")] HttpRequestData req,
        string contractId)
    {
        _logger.LogInformation($"Getting audit summary for contract: {contractId}");

        try
        {
            var summary = await _auditService.GetContractAuditSummaryAsync(contractId);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(summary);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting audit summary for contract {contractId}");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = ex.Message });
            return errorResponse;
        }
    }

    [Function("ArchiveAuditLogs")]
    public async Task<HttpResponseData> ArchiveAuditLogs(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "audit/archive")] HttpRequestData req)
    {
        _logger.LogInformation("Manual archive trigger received");

        try
        {
            var archivedCount = await _auditService.ArchiveOldLogsAsync();

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new 
            { 
                message = $"Successfully archived {archivedCount} audit logs",
                archivedCount 
            });
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving audit logs");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = ex.Message });
            return errorResponse;
        }
    }

    /// <summary>
    /// Timer trigger to automatically archive old logs monthly (1st of every month at 2 AM UTC)
    /// </summary>
    [Function("ScheduledArchiveAuditLogs")]
    public async Task ScheduledArchiveAuditLogs(
        [TimerTrigger("0 0 2 1 * *")] TimerInfo timerInfo)
    {
        _logger.LogInformation($"Scheduled audit archival started at: {DateTime.UtcNow}");

        try
        {
            var archivedCount = await _auditService.ArchiveOldLogsAsync();
            _logger.LogInformation($"Scheduled archival completed: {archivedCount} logs archived");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in scheduled audit archival");
        }
    }
}
