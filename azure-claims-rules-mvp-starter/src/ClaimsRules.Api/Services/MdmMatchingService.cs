using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ClaimsRules.Api.Models;

namespace ClaimsRules.Api.Services;

/// <summary>
/// MDM Matching Service - implements various matching algorithms
/// </summary>
public class MdmMatchingService
{
    private readonly CosmosAudit _cosmosAudit;
    private readonly FhirClient _fhirClient;

    public MdmMatchingService(CosmosAudit cosmosAudit, FhirClient fhirClient)
    {
        _cosmosAudit = cosmosAudit;
        _fhirClient = fhirClient;
    }

    /// <summary>
    /// Find matching golden resources for a source resource
    /// </summary>
    public async Task<List<MatchCandidate>> FindMatchesAsync(
        string resourceType,
        Dictionary<string, object> sourceResource,
        MdmConfiguration config)
    {
        var candidates = new List<MatchCandidate>();

        // Get all golden resources for this resource type
        var goldenResources = await GetGoldenResourcesAsync(resourceType);

        foreach (var golden in goldenResources)
        {
            var score = CalculateMatchScore(sourceResource, golden.Data, config.MatchRules);
            var matchDetails = new Dictionary<string, object>();

            if (score >= config.MatchThreshold)
            {
                candidates.Add(new MatchCandidate
                {
                    GoldenResourceId = golden.Id,
                    FhirResourceId = golden.FhirResourceId,
                    Score = score,
                    MatchResult = MdmMatchResult.MATCH,
                    MatchDetails = matchDetails,
                    Resource = golden.Data
                });
            }
            else if (score >= config.PossibleMatchThreshold)
            {
                candidates.Add(new MatchCandidate
                {
                    GoldenResourceId = golden.Id,
                    FhirResourceId = golden.FhirResourceId,
                    Score = score,
                    MatchResult = MdmMatchResult.POSSIBLE_MATCH,
                    MatchDetails = matchDetails,
                    Resource = golden.Data
                });
            }
        }

        return candidates.OrderByDescending(c => c.Score).ToList();
    }

    /// <summary>
    /// Calculate match score between source and golden resource
    /// </summary>
    public double CalculateMatchScore(
        Dictionary<string, object> source,
        Dictionary<string, object> golden,
        List<MatchRule> rules)
    {
        double totalWeight = 0;
        double weightedScore = 0;

        foreach (var rule in rules)
        {
            var sourceValue = ExtractValue(source, rule.FieldPath);
            var goldenValue = ExtractValue(golden, rule.FieldPath);

            if (sourceValue == null || goldenValue == null)
            {
                if (rule.Required)
                    return 0; // Required field missing
                continue;
            }

            var fieldScore = CompareValues(sourceValue, goldenValue, rule);
            weightedScore += fieldScore * rule.Weight;
            totalWeight += rule.Weight;
        }

        return totalWeight > 0 ? weightedScore / totalWeight : 0;
    }

    /// <summary>
    /// Compare two values using specified algorithm
    /// </summary>
    private double CompareValues(object value1, object value2, MatchRule rule)
    {
        var str1 = value1?.ToString() ?? string.Empty;
        var str2 = value2?.ToString() ?? string.Empty;

        return rule.Algorithm switch
        {
            MatchAlgorithm.EXACT => ExactMatch(str1, str2),
            MatchAlgorithm.NORMALIZED => NormalizedMatch(str1, str2),
            MatchAlgorithm.PHONETIC => PhoneticMatch(str1, str2),
            MatchAlgorithm.FUZZY => FuzzyMatch(str1, str2, rule.Parameters),
            MatchAlgorithm.DATE_RANGE => DateRangeMatch(str1, str2, rule.Parameters),
            MatchAlgorithm.NUMERIC_RANGE => NumericRangeMatch(str1, str2, rule.Parameters),
            MatchAlgorithm.SUBSTRING => SubstringMatch(str1, str2),
            _ => 0
        };
    }

    /// <summary>
    /// Exact string match
    /// </summary>
    private double ExactMatch(string str1, string str2)
    {
        return str1 == str2 ? 1.0 : 0.0;
    }

    /// <summary>
    /// Normalized match (case-insensitive, trimmed)
    /// </summary>
    private double NormalizedMatch(string str1, string str2)
    {
        var normalized1 = str1.Trim().ToLowerInvariant();
        var normalized2 = str2.Trim().ToLowerInvariant();
        return normalized1 == normalized2 ? 1.0 : 0.0;
    }

    /// <summary>
    /// Phonetic match using Soundex algorithm
    /// </summary>
    private double PhoneticMatch(string str1, string str2)
    {
        var soundex1 = Soundex(str1);
        var soundex2 = Soundex(str2);
        return soundex1 == soundex2 ? 0.9 : 0.0; // Slightly lower confidence than exact
    }

    /// <summary>
    /// Soundex algorithm implementation
    /// </summary>
    private string Soundex(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "0000";

        input = input.ToUpperInvariant();
        var result = input[0].ToString();

        var dict = new Dictionary<char, char>
        {
            {'B', '1'}, {'F', '1'}, {'P', '1'}, {'V', '1'},
            {'C', '2'}, {'G', '2'}, {'J', '2'}, {'K', '2'}, {'Q', '2'}, {'S', '2'}, {'X', '2'}, {'Z', '2'},
            {'D', '3'}, {'T', '3'},
            {'L', '4'},
            {'M', '5'}, {'N', '5'},
            {'R', '6'}
        };

        char? lastCode = null;
        foreach (var c in input.Skip(1))
        {
            if (dict.TryGetValue(c, out var code))
            {
                if (code != lastCode && result.Length < 4)
                    result += code;
                lastCode = code;
            }
            else
            {
                lastCode = null;
            }
        }

        return result.PadRight(4, '0');
    }

    /// <summary>
    /// Fuzzy match using Levenshtein distance
    /// </summary>
    private double FuzzyMatch(string str1, string str2, Dictionary<string, object> parameters)
    {
        var distance = LevenshteinDistance(str1, str2);
        var maxLength = Math.Max(str1.Length, str2.Length);

        if (maxLength == 0)
            return 1.0;

        var similarity = 1.0 - ((double)distance / maxLength);

        // Apply threshold from parameters if specified
        if (parameters.TryGetValue("threshold", out var thresholdObj) && thresholdObj is double threshold)
        {
            return similarity >= threshold ? similarity : 0.0;
        }

        return similarity;
    }

    /// <summary>
    /// Levenshtein distance algorithm
    /// </summary>
    private int LevenshteinDistance(string str1, string str2)
    {
        var n = str1.Length;
        var m = str2.Length;
        var d = new int[n + 1, m + 1];

        if (n == 0) return m;
        if (m == 0) return n;

        for (int i = 0; i <= n; i++)
            d[i, 0] = i;
        for (int j = 0; j <= m; j++)
            d[0, j] = j;

        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                var cost = (str2[j - 1] == str1[i - 1]) ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }

        return d[n, m];
    }

    /// <summary>
    /// Date range match
    /// </summary>
    private double DateRangeMatch(string str1, string str2, Dictionary<string, object> parameters)
    {
        if (!DateTime.TryParse(str1, out var date1) || !DateTime.TryParse(str2, out var date2))
            return 0.0;

        var daysDiff = Math.Abs((date1 - date2).TotalDays);

        // Get allowed range from parameters (default: 1 day)
        var allowedDays = 1;
        if (parameters.TryGetValue("allowedDays", out var daysObj) && daysObj is int days)
            allowedDays = days;

        return daysDiff <= allowedDays ? 1.0 : 0.0;
    }

    /// <summary>
    /// Numeric range match
    /// </summary>
    private double NumericRangeMatch(string str1, string str2, Dictionary<string, object> parameters)
    {
        if (!double.TryParse(str1, out var num1) || !double.TryParse(str2, out var num2))
            return 0.0;

        var diff = Math.Abs(num1 - num2);

        // Get allowed range from parameters (default: 0)
        var allowedRange = 0.0;
        if (parameters.TryGetValue("allowedRange", out var rangeObj) && rangeObj is double range)
            allowedRange = range;

        return diff <= allowedRange ? 1.0 : 0.0;
    }

    /// <summary>
    /// Substring match
    /// </summary>
    private double SubstringMatch(string str1, string str2)
    {
        var normalized1 = str1.Trim().ToLowerInvariant();
        var normalized2 = str2.Trim().ToLowerInvariant();
        
        if (normalized1.Contains(normalized2) || normalized2.Contains(normalized1))
            return 0.7; // Lower confidence for substring matches
        
        return 0.0;
    }

    /// <summary>
    /// Extract value from nested object using JSONPath-like syntax
    /// </summary>
    private object? ExtractValue(Dictionary<string, object> data, string path)
    {
        try
        {
            // Simple path traversal (e.g., "name[0].family")
            var parts = path.Split('.');
            object? current = data;

            foreach (var part in parts)
            {
                if (current == null)
                    return null;

                // Handle array indexing (e.g., "name[0]")
                if (part.Contains('[') && part.Contains(']'))
                {
                    var propName = part.Substring(0, part.IndexOf('['));
                    var indexStr = part.Substring(part.IndexOf('[') + 1, part.IndexOf(']') - part.IndexOf('[') - 1);

                    if (current is Dictionary<string, object> dict && dict.TryGetValue(propName, out var arrayValue))
                    {
                        if (arrayValue is JsonElement jsonArray && jsonArray.ValueKind == JsonValueKind.Array)
                        {
                            if (int.TryParse(indexStr, out var index) && index < jsonArray.GetArrayLength())
                            {
                                current = jsonArray[index];
                                continue;
                            }
                        }
                    }
                    return null;
                }

                // Regular property access
                if (current is Dictionary<string, object> currentDict)
                {
                    if (currentDict.TryGetValue(part, out var value))
                        current = value;
                    else
                        return null;
                }
                else if (current is JsonElement jsonElement)
                {
                    if (jsonElement.TryGetProperty(part, out var property))
                        current = property;
                    else
                        return null;
                }
                else
                {
                    return null;
                }
            }

            // Convert JsonElement to string
            if (current is JsonElement finalElement)
            {
                return finalElement.ValueKind switch
                {
                    JsonValueKind.String => finalElement.GetString(),
                    JsonValueKind.Number => finalElement.GetDouble(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    _ => finalElement.ToString()
                };
            }

            return current;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Get all golden resources for a resource type
    /// </summary>
    private async Task<List<GoldenResource>> GetGoldenResourcesAsync(string resourceType)
    {
        // Query Cosmos DB for golden resources
        var query = $"SELECT * FROM c WHERE c.resourceType = '{resourceType}' AND c.isActive = true";
        var goldenResources = await _cosmosAudit.QueryAsync<GoldenResource>(query);
        return goldenResources.ToList();
    }
}
