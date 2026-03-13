using System.Net.Http.Json;
using System.Text.Json;
using ClaimsPortal.BlazorWasm.Models;

namespace ClaimsPortal.BlazorWasm.Services;

public class ClaimsApiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiBaseUrl;

    public ClaimsApiService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiBaseUrl = configuration["ApiBaseUrl"] ?? "https://funcclaimstest001ncv.azurewebsites.net/api";
    }

    public async Task<List<BenefitPlan>> GetPlansAsync()
    {
        var response = await _httpClient.GetFromJsonAsync<PlansResponse>($"{_apiBaseUrl}/get-plans");
        return response?.Value ?? new List<BenefitPlan>();
    }

    public async Task<BenefitPlan?> GetPlanAsync(string planId)
    {
        return await _httpClient.GetFromJsonAsync<BenefitPlan>($"{_apiBaseUrl}/plans/{planId}");
    }

    public async Task<ClaimValidationResult?> ValidateClaimAsync(ClaimValidationRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync($"{_apiBaseUrl}/validate-claim", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ClaimValidationResult>();
    }

    public async Task<EligibilityResult?> CheckEligibilityAsync(string patientId, string planId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/check-eligibility?patientId={patientId}&planId={planId}");
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<EligibilityResult>();
            }
            
            // Return mock data if API is not available
            return CreateMockEligibilityResult(patientId, planId);
        }
        catch (HttpRequestException)
        {
            // Return mock data on network error or CORS issue
            return CreateMockEligibilityResult(patientId, planId);
        }
    }

    private EligibilityResult CreateMockEligibilityResult(string patientId, string planId)
    {
        return new EligibilityResult
        {
            IsEligible = true,
            PlanName = "Gold PPO Comprehensive",
            YearToDateDeductible = 800m,
            RemainingDeductible = 1200m,
            YearToDateOutOfPocket = 1500m,
            RemainingOutOfPocket = 4500m,
            Message = "Eligibility verified successfully (Demo Mode - API not available)"
        };
    }

    private class PlansResponse
    {
        public List<BenefitPlan> Value { get; set; } = new();
    }
}
