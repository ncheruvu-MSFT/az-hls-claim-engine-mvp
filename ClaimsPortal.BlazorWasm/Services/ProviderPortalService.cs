using System.Net.Http.Json;

namespace ClaimsPortal.BlazorWasm.Services;

public class ProviderPortalService
{
    private readonly HttpClient _http;
    private readonly string _apiBaseUrl;

    public ProviderPortalService(HttpClient http, IConfiguration configuration)
    {
        _http = http;
        _apiBaseUrl = configuration["ApiBaseUrl"] ?? "http://localhost:7071/api";
    }

    public async Task<ProviderSearchResponse> SearchProvidersAsync(ProviderSearchRequest request)
    {
        try
        {
            var response = await _http.PostAsJsonAsync($"{_apiBaseUrl}/providers/search", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ProviderSearchResponse>() ?? new();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error searching providers: {ex.Message}");
            return new ProviderSearchResponse();
        }
    }

    public async Task<Provider?> GetProviderByNPIAsync(string npi)
    {
        try
        {
            return await _http.GetFromJsonAsync<Provider>($"{_apiBaseUrl}/providers/{npi}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting provider: {ex.Message}");
            return null;
        }
    }

    public async Task<Provider?> AddProviderAsync(Provider provider)
    {
        try
        {
            var response = await _http.PostAsJsonAsync($"{_apiBaseUrl}/providers", provider);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Provider>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error adding provider: {ex.Message}");
            return null;
        }
    }

    public async Task<Provider?> UpdateProviderAsync(string npi, Provider provider)
    {
        try
        {
            var response = await _http.PutAsJsonAsync($"{_apiBaseUrl}/providers/{npi}", provider);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Provider>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating provider: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> UpdateProviderTierAsync(string npi, string networkTier)
    {
        try
        {
            var tierUpdate = new Dictionary<string, string> { { "networkTier", networkTier } };
            var response = await _http.PutAsJsonAsync($"{_apiBaseUrl}/providers/{npi}/tier", tierUpdate);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating provider tier: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> RemoveProviderAsync(string npi)
    {
        try
        {
            var response = await _http.DeleteAsync($"{_apiBaseUrl}/providers/{npi}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error removing provider: {ex.Message}");
            return false;
        }
    }

    public async Task<List<ProviderContract>> GetContractsAsync(string? providerNpi = null, string? status = null)
    {
        try
        {
            var queryParams = new List<string>();
            if (!string.IsNullOrEmpty(providerNpi))
                queryParams.Add($"providerNpi={providerNpi}");
            if (!string.IsNullOrEmpty(status))
                queryParams.Add($"status={status}");

            var query = queryParams.Any() ? "?" + string.Join("&", queryParams) : "";
            return await _http.GetFromJsonAsync<List<ProviderContract>>($"{_apiBaseUrl}/contracts{query}") ?? new();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting contracts: {ex.Message}");
            return new();
        }
    }

    public async Task<ProviderContract?> GetContractByIdAsync(string contractId)
    {
        try
        {
            return await _http.GetFromJsonAsync<ProviderContract>($"{_apiBaseUrl}/contracts/{contractId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting contract: {ex.Message}");
            return null;
        }
    }

    public async Task<ProviderContract?> CreateContractAsync(ProviderContract contract)
    {
        try
        {
            var response = await _http.PostAsJsonAsync($"{_apiBaseUrl}/contracts", contract);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ProviderContract>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating contract: {ex.Message}");
            return null;
        }
    }

    public async Task<ProviderContract?> UpdateContractAsync(string contractId, ProviderContract contract)
    {
        try
        {
            var response = await _http.PutAsJsonAsync($"{_apiBaseUrl}/contracts/{contractId}", contract);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ProviderContract>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating contract: {ex.Message}");
            return null;
        }
    }

    public async Task<List<NetworkTier>> GetNetworkTiersAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<List<NetworkTier>>($"{_apiBaseUrl}/network-tiers") ?? new();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting network tiers: {ex.Message}");
            return new();
        }
    }
}

// Models matching the API
public class ProviderSearchRequest
{
    public string? SearchTerm { get; set; }
    public string? Specialty { get; set; }
    public string? NetworkTier { get; set; }
    public string? ContractStatus { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    public bool? AcceptingNewPatients { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}

public class ProviderSearchResponse
{
    public List<Provider> Providers { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class Provider
{
    public string ProviderId { get; set; } = string.Empty;
    public string Npi { get; set; } = string.Empty;
    public string TaxId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ProviderType { get; set; } = string.Empty;
    public List<string> Specialties { get; set; } = new();
    public List<string> TaxonomyCodes { get; set; } = new();
    public ProviderAddress Address { get; set; } = new();
    public List<string> NetworkIds { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public decimal QualityScore { get; set; }
    public bool AcceptingNewPatients { get; set; } = true;
    public DateTime CredentialingDate { get; set; }
    public DateTime? CredentialExpirationDate { get; set; }
}

public class ProviderAddress
{
    public string Line1 { get; set; } = string.Empty;
    public string Line2 { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string Country { get; set; } = "US";
}

public class ProviderContract
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ContractNumber { get; set; } = string.Empty;
    public string ProviderNPI { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public string PayerId { get; set; } = string.Empty;
    public string PayerName { get; set; } = string.Empty;
    public string ContractType { get; set; } = string.Empty;
    public string NetworkTier { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime EffectiveDate { get; set; }
    public DateTime ExpirationDate { get; set; }
    public DateTime? TerminationDate { get; set; }
    public ReimbursementModel Reimbursement { get; set; } = new();
    public List<string> CoveredServices { get; set; } = new();
    public List<string> ExcludedServices { get; set; } = new();
    public Dictionary<string, decimal> FeeSchedule { get; set; } = new();
    public bool RequiresPriorAuth { get; set; }
    public string CredentialingStatus { get; set; } = string.Empty;
    public DateTime CredentialingDate { get; set; }
    public DateTime? RecredentialingDate { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string ModifiedBy { get; set; } = string.Empty;
}

public class ReimbursementModel
{
    public string Type { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public string RateType { get; set; } = string.Empty;
    public decimal PercentOfMedicare { get; set; }
    public decimal PMPMRate { get; set; }
    public bool HasQualityBonus { get; set; }
    public decimal QualityBonusPercent { get; set; }
    public Dictionary<string, decimal> CPTRates { get; set; } = new();
}

public class NetworkTier
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string TierName { get; set; } = string.Empty;
    public string TierLabel { get; set; } = string.Empty;
    public int MemberCopay { get; set; }
    public int MemberCoinsurance { get; set; }
    public decimal ProviderReimbursementPercent { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<string> BenefitRestrictions { get; set; } = new();
    public bool RequiresReferral { get; set; }
    public int ProviderCount { get; set; }
    public List<string> AssociatedPlans { get; set; } = new();
}
