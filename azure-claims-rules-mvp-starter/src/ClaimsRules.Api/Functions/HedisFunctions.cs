using ClaimsRules.Api.Models;
using ClaimsRules.Api.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace ClaimsRules.Api.Functions;

/// <summary>
/// Azure Functions for NCQA HEDIS quality measure calculations
/// </summary>
public class HedisFunctions
{
    private readonly HedisService _hedisService;
    private readonly ILogger<HedisFunctions> _logger;

    public HedisFunctions(HedisService hedisService, ILogger<HedisFunctions> logger)
    {
        _hedisService = hedisService;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/hedis/measures - Get all measure definitions
    /// </summary>
    [Function("GetHedisMeasures")]
    public async Task<HttpResponseData> GetMeasures(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "hedis/measures")] HttpRequestData req)
    {
        try
        {
            _logger.LogInformation("Getting HEDIS measure definitions");
            var measures = HedisMeasures.GetAllDefinitions();
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new { count = measures.Count, measures });
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting measures");
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteAsJsonAsync(new { error = ex.Message });
            return response;
        }
    }

    /// <summary>
    /// GET /api/hedis/dashboard?planSegment={segment}&year={year} - Get full dashboard
    /// </summary>
    [Function("GetHedisDashboard")]
    public async Task<HttpResponseData> GetDashboard(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "hedis/dashboard")] HttpRequestData req)
    {
        try
        {
            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            var planSegment = query["planSegment"] ?? "All Plans";
            var year = int.TryParse(query["year"], out var y) ? y : DateTime.UtcNow.Year;

            _logger.LogInformation("Calculating dashboard for {Segment} {Year}", planSegment, year);
            var dashboard = await _hedisService.CalculateDashboardAsync(planSegment, year);
            
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(dashboard);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating dashboard");
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteAsJsonAsync(new { error = ex.Message });
            return response;
        }
    }

    /// <summary>
    /// GET /api/hedis/measure/{measureId}?planSegment={segment}&year={year} - Calculate specific measure
    /// </summary>
    [Function("CalculateHedisMeasure")]
    public async Task<HttpResponseData> CalculateMeasure(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "hedis/measure/{measureId}")] HttpRequestData req,
        string measureId)
    {
        try
        {
            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            var planSegment = query["planSegment"] ?? "All Plans";
            var year = int.TryParse(query["year"], out var y) ? y : DateTime.UtcNow.Year;

            _logger.LogInformation("Calculating measure {MeasureId}", measureId);
            var result = await _hedisService.CalculateMeasureAsync(measureId, planSegment, year);
            
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(result);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating measure {MeasureId}", measureId);
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteAsJsonAsync(new { error = ex.Message });
            return response;
        }
    }

    /// <summary>
    /// GET /api/hedis/gaps/{measureId}?planSegment={segment}&year={year}&top={n} - Get care gaps
    /// </summary>
    [Function("GetHedisGaps")]
    public async Task<HttpResponseData> GetGaps(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "hedis/gaps/{measureId}")] HttpRequestData req,
        string measureId)
    {
        try
        {
            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            var planSegment = query["planSegment"] ?? "All Plans";
            var year = int.TryParse(query["year"], out var y) ? y : DateTime.UtcNow.Year;
            var top = int.TryParse(query["top"], out var t) ? t : 100;

            _logger.LogInformation("Getting gaps for {MeasureId} (top {Top})", measureId, top);
            var result = await _hedisService.CalculateMeasureAsync(measureId, planSegment, year);
            
            var gaps = result.GapPatients.OrderByDescending(p => p.Priority).Take(top).ToList();
            
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                measureId = result.MeasureId,
                measureName = result.MeasureName,
                totalGaps = result.GapCount,
                returned = gaps.Count,
                rate = result.Rate,
                gaps
            });
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting gaps for {MeasureId}", measureId);
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteAsJsonAsync(new { error = ex.Message });
            return response;
        }
    }
}
