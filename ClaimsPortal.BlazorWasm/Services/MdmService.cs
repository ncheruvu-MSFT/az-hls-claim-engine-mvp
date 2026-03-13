using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ClaimsPortal.BlazorWasm.Models;

namespace ClaimsPortal.BlazorWasm.Services;

/// <summary>
/// MDM Service - handles all MDM API operations
/// </summary>
public class MdmService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiBaseUrl;

    public MdmService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _apiBaseUrl = "https://funcclaimstest001ncv.azurewebsites.net/api";
    }

    // ========== Metrics and Dashboard ==========
    
    public async Task<MdmMetricsModel?> GetMetricsAsync(string resourceType = "Patient")
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<MdmMetricsModel>($"{_apiBaseUrl}/mdm/metrics?resourceType={resourceType}");
        }
        catch
        {
            return GetMockMetrics();
        }
    }

    // ========== Review Queue ==========
    
    public async Task<List<ReviewQueueItemModel>> GetReviewQueueAsync(int limit = 50)
    {
        try
        {
            var items = await _httpClient.GetFromJsonAsync<List<ReviewQueueItemModel>>($"{_apiBaseUrl}/mdm/review-queue?limit={limit}");
            return items ?? new List<ReviewQueueItemModel>();
        }
        catch
        {
            return GetMockReviewQueue();
        }
    }

    public async Task<MdmLinkModel?> ApproveMatchAsync(string linkId, string userName)
    {
        try
        {
            var request = new { matchResult = "MATCH" };
            _httpClient.DefaultRequestHeaders.Remove("X-User-Name");
            _httpClient.DefaultRequestHeaders.Add("X-User-Name", userName);
            
            var response = await _httpClient.PutAsJsonAsync($"{_apiBaseUrl}/mdm/link/{linkId}", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<MdmLinkModel>();
        }
        catch
        {
            return null;
        }
    }

    public async Task<MdmLinkModel?> RejectMatchAsync(string linkId, string userName)
    {
        try
        {
            var request = new { matchResult = "NO_MATCH" };
            _httpClient.DefaultRequestHeaders.Remove("X-User-Name");
            _httpClient.DefaultRequestHeaders.Add("X-User-Name", userName);
            
            var response = await _httpClient.PutAsJsonAsync($"{_apiBaseUrl}/mdm/link/{linkId}", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<MdmLinkModel>();
        }
        catch
        {
            return null;
        }
    }

    // ========== Golden Resources ==========
    
    public async Task<GoldenResourceModel?> GetGoldenResourceAsync(string goldenResourceId)
    {
        try
        {
            // Get links for this golden resource
            var links = await GetGoldenResourceLinksAsync(goldenResourceId);
            
            // Mock golden resource (in real scenario, query FHIR)
            return new GoldenResourceModel
            {
                Id = goldenResourceId,
                ResourceType = "Patient",
                FhirResourceId = $"Patient/{goldenResourceId}",
                LinkedSourceCount = links.Count,
                IsActive = true,
                Created = DateTime.UtcNow.AddDays(-30),
                Updated = DateTime.UtcNow,
                Data = new Dictionary<string, object>
                {
                    { "name", "Golden Patient Record" },
                    { "birthDate", "1975-05-15" },
                    { "gender", "female" }
                }
            };
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<MdmLinkModel>> GetGoldenResourceLinksAsync(string goldenResourceId)
    {
        try
        {
            var links = await _httpClient.GetFromJsonAsync<List<MdmLinkModel>>($"{_apiBaseUrl}/mdm/golden-resource/{goldenResourceId}/links");
            return links ?? new List<MdmLinkModel>();
        }
        catch
        {
            return new List<MdmLinkModel>();
        }
    }

    public async Task<GoldenResourceModel?> MergeGoldenResourcesAsync(string fromId, string toId, string userName)
    {
        try
        {
            var request = new
            {
                fromGoldenResourceId = fromId,
                toGoldenResourceId = toId
            };
            
            _httpClient.DefaultRequestHeaders.Remove("X-User-Name");
            _httpClient.DefaultRequestHeaders.Add("X-User-Name", userName);
            
            var response = await _httpClient.PostAsJsonAsync($"{_apiBaseUrl}/mdm/golden-resource/$merge", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<GoldenResourceModel>();
        }
        catch
        {
            return null;
        }
    }

    // ========== Configuration ==========
    
    public async Task<MdmConfigModel?> GetConfigurationAsync(string resourceType)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<MdmConfigModel>($"{_apiBaseUrl}/mdm/config/{resourceType}");
        }
        catch
        {
            return GetMockConfiguration(resourceType);
        }
    }

    public async Task<MdmConfigModel?> UpdateConfigurationAsync(MdmConfigModel config, string userName)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Remove("X-User-Name");
            _httpClient.DefaultRequestHeaders.Add("X-User-Name", userName);
            
            var response = await _httpClient.PutAsJsonAsync($"{_apiBaseUrl}/mdm/config/{config.ResourceType}", config);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<MdmConfigModel>();
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<AlgorithmMetadata>> GetAlgorithmsAsync()
    {
        try
        {
            var algorithms = await _httpClient.GetFromJsonAsync<List<AlgorithmMetadata>>($"{_apiBaseUrl}/mdm/config/algorithms");
            return algorithms ?? GetMockAlgorithms();
        }
        catch
        {
            return GetMockAlgorithms();
        }
    }

    public async Task<Dictionary<string, object>?> TestRulesAsync(string resourceType, Dictionary<string, object> source, Dictionary<string, object> target)
    {
        try
        {
            var request = new { source, target };
            var response = await _httpClient.PostAsJsonAsync($"{_apiBaseUrl}/mdm/config/{resourceType}/test", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        }
        catch
        {
            return null;
        }
    }

    // ========== Patient Match Operation ==========
    
    public async Task<List<MatchCandidateModel>> FindMatchesAsync(Dictionary<string, object> patientData, int count = 10)
    {
        try
        {
            var request = new
            {
                resourceType = "Patient",
                resource = patientData,
                count
            };
            
            var response = await _httpClient.PostAsJsonAsync($"{_apiBaseUrl}/mdm/Patient/$match", request);
            response.EnsureSuccessStatusCode();
            
            var matchResponse = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
            if (matchResponse != null && matchResponse.TryGetValue("candidates", out var candidatesObj))
            {
                var json = JsonSerializer.Serialize(candidatesObj);
                return JsonSerializer.Deserialize<List<MatchCandidateModel>>(json) ?? new List<MatchCandidateModel>();
            }
            
            return new List<MatchCandidateModel>();
        }
        catch
        {
            return new List<MatchCandidateModel>();
        }
    }

    // ========== Mock Data for Development ==========
    
    private MdmMetricsModel GetMockMetrics()
    {
        return new MdmMetricsModel
        {
            ResourceType = "Patient",
            LinkCountsByResult = new Dictionary<string, long>
            {
                { "MATCH", 15234 },
                { "POSSIBLE_MATCH", 487 },
                { "NO_MATCH", 123 }
            },
            AutoLinksCount = 15234,
            ManualLinksCount = 610,
            TotalGoldenResources = 14532,
            ActiveGoldenResources = 14501,
            MergedGoldenResources = 31,
            PendingReviewCount = 487,
            ReviewedTodayCount = 23,
            AverageReviewTime = 4.5
        };
    }

    private List<ReviewQueueItemModel> GetMockReviewQueue()
    {
        return new List<ReviewQueueItemModel>
        {
            new()
            {
                Id = "queue-1",
                LinkId = "link-1",
                GoldenResourceId = "golden-123",
                SourceResourceId = "patient-456",
                ResourceType = "Patient",
                Status = "PENDING",
                Created = DateTime.UtcNow.AddHours(-2),
                MatchScore = 0.75,
                Priority = 10,
                MatchDetails = new Dictionary<string, object>
                {
                    { "SSN", 1.0 },
                    { "FamilyName", 0.9 },
                    { "BirthDate", 1.0 }
                },
                GoldenResourceData = new Dictionary<string, object>
                {
                    { "name", "Jennifer Smith" },
                    { "birthDate", "1985-03-15" },
                    { "gender", "female" },
                    { "ssn", "***-**-1234" }
                },
                SourceResourceData = new Dictionary<string, object>
                {
                    { "name", "Jennifer Smyth" },
                    { "birthDate", "1985-03-15" },
                    { "gender", "female" },
                    { "ssn", "***-**-1234" }
                }
            },
            new()
            {
                Id = "queue-2",
                LinkId = "link-2",
                GoldenResourceId = "golden-789",
                SourceResourceId = "patient-101",
                ResourceType = "Patient",
                Status = "PENDING",
                Created = DateTime.UtcNow.AddHours(-5),
                MatchScore = 0.68,
                Priority = 6,
                MatchDetails = new Dictionary<string, object>
                {
                    { "FamilyName", 0.85 },
                    { "GivenName", 0.8 },
                    { "BirthDate", 0.5 }
                },
                GoldenResourceData = new Dictionary<string, object>
                {
                    { "name", "Michael Johnson" },
                    { "birthDate", "1972-08-22" },
                    { "gender", "male" }
                },
                SourceResourceData = new Dictionary<string, object>
                {
                    { "name", "Michael Jonson" },
                    { "birthDate", "1972-08-25" },
                    { "gender", "male" }
                }
            }
        };
    }

    private MdmConfigModel GetMockConfiguration(string resourceType)
    {
        return new MdmConfigModel
        {
            Id = $"config-{resourceType}",
            ResourceType = resourceType,
            Enabled = true,
            MatchThreshold = 0.8,
            PossibleMatchThreshold = 0.6,
            MatchRules = new List<MatchRuleModel>
            {
                new() { Name = "SSN", FieldPath = "identifier[?(@.system=='http://hl7.org/fhir/sid/us-ssn')].value", Algorithm = "EXACT", Weight = 5.0, Required = false },
                new() { Name = "MRN", FieldPath = "identifier[0].value", Algorithm = "EXACT", Weight = 4.0, Required = false },
                new() { Name = "FamilyName", FieldPath = "name[0].family", Algorithm = "PHONETIC", Weight = 2.0, Required = true },
                new() { Name = "GivenName", FieldPath = "name[0].given[0]", Algorithm = "PHONETIC", Weight = 2.0, Required = true },
                new() { Name = "BirthDate", FieldPath = "birthDate", Algorithm = "EXACT", Weight = 3.0, Required = true },
                new() { Name = "Gender", FieldPath = "gender", Algorithm = "EXACT", Weight = 1.0, Required = false }
            },
            Created = DateTime.UtcNow.AddMonths(-1),
            Updated = DateTime.UtcNow.AddDays(-2),
            UpdatedBy = "admin@claimsiq.com"
        };
    }

    private List<AlgorithmMetadata> GetMockAlgorithms()
    {
        return new List<AlgorithmMetadata>
        {
            new() { Name = "EXACT", Description = "Exact string match - values must be identical", Parameters = new Dictionary<string, string>() },
            new() { Name = "NORMALIZED", Description = "Case-insensitive match with whitespace trimming", Parameters = new Dictionary<string, string>() },
            new() { Name = "PHONETIC", Description = "Soundex phonetic matching - matches words that sound alike", Parameters = new Dictionary<string, string>() },
            new() { Name = "FUZZY", Description = "Levenshtein distance fuzzy matching - tolerates typos", Parameters = new Dictionary<string, string> { { "threshold", "Minimum similarity score (0.0-1.0)" } } },
            new() { Name = "DATE_RANGE", Description = "Date matching within allowed range", Parameters = new Dictionary<string, string> { { "allowedDays", "Maximum days difference allowed" } } },
            new() { Name = "NUMERIC_RANGE", Description = "Numeric matching within allowed range", Parameters = new Dictionary<string, string> { { "allowedRange", "Maximum numeric difference allowed" } } },
            new() { Name = "SUBSTRING", Description = "Substring containment matching", Parameters = new Dictionary<string, string>() }
        };
    }
}
