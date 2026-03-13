using System.Net;
using System.Text.Json;
using ClaimsRules.Api.Models;
using ClaimsRules.Api.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace ClaimsRules.Api.Functions;

/// <summary>
/// MDM Configuration Management - CRUD operations for matching rules
/// </summary>
public class MdmConfigFn
{
    private readonly ILogger<MdmConfigFn> _logger;
    private readonly MdmLinkService _linkService;

    public MdmConfigFn(ILogger<MdmConfigFn> logger, MdmLinkService linkService)
    {
        _logger = logger;
        _linkService = linkService;
    }

    /// <summary>
    /// Get MDM configuration for a resource type
    /// </summary>
    [Function("GetMdmConfig")]
    public async Task<HttpResponseData> GetMdmConfig(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "mdm/config/{resourceType}")] HttpRequestData req,
        string resourceType)
    {
        try
        {
            var config = await _linkService.GetConfigurationAsync(resourceType);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonSerializer.Serialize(config));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetMdmConfig");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync(JsonSerializer.Serialize(new { error = ex.Message }));
            return errorResponse;
        }
    }

    /// <summary>
    /// Update MDM configuration
    /// </summary>
    [Function("UpdateMdmConfig")]
    public async Task<HttpResponseData> UpdateMdmConfig(
        [HttpTrigger(AuthorizationLevel.Function, "put", Route = "mdm/config/{resourceType}")] HttpRequestData req,
        string resourceType)
    {
        try
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var config = JsonSerializer.Deserialize<MdmConfiguration>(requestBody);

            if (config == null)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Invalid configuration");
                return badResponse;
            }

            // Ensure resource type matches
            config.ResourceType = resourceType;

            var userName = req.Headers.TryGetValues("X-User-Name", out var userValues) ? userValues.First() : "System";
            await _linkService.UpdateConfigurationAsync(config, userName);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonSerializer.Serialize(config));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in UpdateMdmConfig");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync(JsonSerializer.Serialize(new { error = ex.Message }));
            return errorResponse;
        }
    }

    /// <summary>
    /// Test match rules against sample data
    /// </summary>
    [Function("TestMdmRules")]
    public async Task<HttpResponseData> TestMdmRules(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "mdm/config/{resourceType}/test")] HttpRequestData req,
        string resourceType)
    {
        try
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var testRequest = JsonSerializer.Deserialize<Dictionary<string, object>>(requestBody);

            if (testRequest == null || !testRequest.TryGetValue("source", out var sourceObj) || !testRequest.TryGetValue("target", out var targetObj))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Invalid request: source and target are required");
                return badResponse;
            }

            var source = JsonSerializer.Deserialize<Dictionary<string, object>>(sourceObj.ToString()!);
            var target = JsonSerializer.Deserialize<Dictionary<string, object>>(targetObj.ToString()!);

            if (source == null || target == null)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Invalid source or target data");
                return badResponse;
            }

            // Get configuration
            var config = await _linkService.GetConfigurationAsync(resourceType);

            // Create matching service instance
            var matchingService = new MdmMatchingService(null!, null!); // Mock dependencies for testing
            
            // Calculate match score
            var score = matchingService.CalculateMatchScore(source, target, config.MatchRules);

            var matchResult = score >= config.MatchThreshold ? MdmMatchResult.MATCH :
                            score >= config.PossibleMatchThreshold ? MdmMatchResult.POSSIBLE_MATCH :
                            MdmMatchResult.NO_MATCH;

            var testResult = new
            {
                score,
                matchResult = matchResult.ToString(),
                threshold = new
                {
                    match = config.MatchThreshold,
                    possibleMatch = config.PossibleMatchThreshold
                },
                ruleDetails = CalculateRuleDetails(source, target, config.MatchRules, matchingService)
            };

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonSerializer.Serialize(testResult));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in TestMdmRules");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync(JsonSerializer.Serialize(new { error = ex.Message }));
            return errorResponse;
        }
    }

    private Dictionary<string, object> CalculateRuleDetails(
        Dictionary<string, object> source,
        Dictionary<string, object> target,
        List<MatchRule> rules,
        MdmMatchingService matchingService)
    {
        var details = new Dictionary<string, object>();

        foreach (var rule in rules)
        {
            var ruleScore = 0.0;
            try
            {
                // Extract values and compare
                var sourceValue = matchingService.GetType()
                    .GetMethod("ExtractValue", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                    .Invoke(matchingService, new object[] { source, rule.FieldPath });

                var targetValue = matchingService.GetType()
                    .GetMethod("ExtractValue", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                    .Invoke(matchingService, new object[] { target, rule.FieldPath });

                if (sourceValue != null && targetValue != null)
                {
                    ruleScore = matchingService.CalculateMatchScore(
                        new Dictionary<string, object> { { rule.FieldPath, sourceValue } },
                        new Dictionary<string, object> { { rule.FieldPath, targetValue } },
                        new List<MatchRule> { rule });
                }
            }
            catch
            {
                // Rule evaluation failed
            }

            details[rule.Name] = new
            {
                score = ruleScore,
                weight = rule.Weight,
                weightedScore = ruleScore * rule.Weight,
                algorithm = rule.Algorithm.ToString(),
                fieldPath = rule.FieldPath
            };
        }

        return details;
    }

    /// <summary>
    /// Get available matching algorithms
    /// </summary>
    [Function("GetMatchingAlgorithms")]
    public HttpResponseData GetMatchingAlgorithms(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "mdm/config/algorithms")] HttpRequestData req)
    {
        try
        {
            var algorithms = Enum.GetValues<MatchAlgorithm>().Select(a => new
            {
                name = a.ToString(),
                description = GetAlgorithmDescription(a),
                parameters = GetAlgorithmParameters(a)
            }).ToList();

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            response.WriteString(JsonSerializer.Serialize(algorithms));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetMatchingAlgorithms");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            errorResponse.WriteString(JsonSerializer.Serialize(new { error = ex.Message }));
            return errorResponse;
        }
    }

    private string GetAlgorithmDescription(MatchAlgorithm algorithm) => algorithm switch
    {
        MatchAlgorithm.EXACT => "Exact string match - values must be identical",
        MatchAlgorithm.NORMALIZED => "Case-insensitive match with whitespace trimming",
        MatchAlgorithm.PHONETIC => "Soundex phonetic matching - matches words that sound alike",
        MatchAlgorithm.FUZZY => "Levenshtein distance fuzzy matching - tolerates typos and misspellings",
        MatchAlgorithm.DATE_RANGE => "Date matching within allowed range",
        MatchAlgorithm.NUMERIC_RANGE => "Numeric matching within allowed range",
        MatchAlgorithm.SUBSTRING => "Substring containment matching",
        _ => "Unknown algorithm"
    };

    private Dictionary<string, string> GetAlgorithmParameters(MatchAlgorithm algorithm) => algorithm switch
    {
        MatchAlgorithm.FUZZY => new Dictionary<string, string>
        {
            { "threshold", "Minimum similarity score (0.0-1.0)" }
        },
        MatchAlgorithm.DATE_RANGE => new Dictionary<string, string>
        {
            { "allowedDays", "Maximum days difference allowed" }
        },
        MatchAlgorithm.NUMERIC_RANGE => new Dictionary<string, string>
        {
            { "allowedRange", "Maximum numeric difference allowed" }
        },
        _ => new Dictionary<string, string>()
    };
}
