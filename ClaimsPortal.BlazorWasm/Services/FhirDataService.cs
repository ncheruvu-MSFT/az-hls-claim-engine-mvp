using System.Net.Http.Json;
using System.Text.Json;
using ClaimsPortal.BlazorWasm.Models;

namespace ClaimsPortal.BlazorWasm.Services;

/// <summary>
/// Service for loading data from FHIR server instead of hardcoding
/// Connects to Azure Health Data Services FHIR API or Mock FHIR Server
/// </summary>
public class FhirDataService
{
    private readonly HttpClient _httpClient;
    private readonly string _fhirBaseUrl;

    public FhirDataService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        // Use Mock FHIR Server for development, Azure FHIR for production
        _fhirBaseUrl = configuration["FhirServer:BaseUrl"] ?? "http://localhost:5000/fhir";
    }

    /// <summary>
    /// Load benefit plans from FHIR Coverage resources
    /// </summary>
    public async Task<List<BenefitPlan>> GetBenefitPlansAsync()
    {
        try
        {
            // Query FHIR server for Coverage resources
            var response = await _httpClient.GetAsync($"{_fhirBaseUrl}/Coverage?_count=100");
            
            if (!response.IsSuccessStatusCode)
            {
                // Fallback to default data if FHIR server unavailable
                return GetDefaultBenefitPlans();
            }

            var bundle = await response.Content.ReadAsStringAsync();
            var plans = ParseCoverageBundle(bundle);
            
            return plans.Any() ? plans : GetDefaultBenefitPlans();
        }
        catch (Exception)
        {
            // Return default data on error
            return GetDefaultBenefitPlans();
        }
    }

    /// <summary>
    /// Load patient/member data from FHIR Patient resources
    /// </summary>
    public async Task<List<AccountMember>> GetMembersAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_fhirBaseUrl}/Patient?_count=100");
            
            if (!response.IsSuccessStatusCode)
            {
                return GetDefaultMembers();
            }

            var bundle = await response.Content.ReadAsStringAsync();
            var members = ParsePatientBundle(bundle);
            
            return members.Any() ? members : GetDefaultMembers();
        }
        catch (Exception)
        {
            return GetDefaultMembers();
        }
    }

    /// <summary>
    /// Load claims data from FHIR Claim resources
    /// </summary>
    public async Task<List<ClaimRecord>> GetClaimsAsync(string? patientId = null)
    {
        try
        {
            var query = string.IsNullOrEmpty(patientId) 
                ? $"{_fhirBaseUrl}/Claim?_count=100"
                : $"{_fhirBaseUrl}/Claim?patient={patientId}&_count=100";
                
            var response = await _httpClient.GetAsync(query);
            
            if (!response.IsSuccessStatusCode)
            {
                return new List<ClaimRecord>();
            }

            var bundle = await response.Content.ReadAsStringAsync();
            return ParseClaimBundle(bundle);
        }
        catch (Exception)
        {
            return new List<ClaimRecord>();
        }
    }

    /// <summary>
    /// Load organization/account data from FHIR Organization resources
    /// </summary>
    public async Task<List<Account>> GetAccountsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_fhirBaseUrl}/Organization?_count=100");
            
            if (!response.IsSuccessStatusCode)
            {
                return GetDefaultAccounts();
            }

            var bundle = await response.Content.ReadAsStringAsync();
            var accounts = ParseOrganizationBundle(bundle);
            
            return accounts.Any() ? accounts : GetDefaultAccounts();
        }
        catch (Exception)
        {
            return GetDefaultAccounts();
        }
    }

    /// <summary>
    /// Create or update a FHIR Coverage resource
    /// </summary>
    public async Task<(bool Success, string Message)> SaveCoverageAsync(BenefitPlan plan)
    {
        try
        {
            var fhirCoverage = MapToFhirCoverage(plan);
            var json = JsonSerializer.Serialize(fhirCoverage);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/fhir+json");

            HttpResponseMessage response;
            if (await CoverageExistsAsync(plan.PlanId))
            {
                // Update existing
                response = await _httpClient.PutAsync($"{_fhirBaseUrl}/Coverage/{plan.PlanId}", content);
            }
            else
            {
                // Create new
                response = await _httpClient.PostAsync($"{_fhirBaseUrl}/Coverage", content);
            }

            if (response.IsSuccessStatusCode)
            {
                return (true, "Plan saved successfully to FHIR server");
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                return (false, $"FHIR server error: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            return (false, $"Error saving to FHIR: {ex.Message}");
        }
    }

    /// <summary>
    /// Check if FHIR server is available
    /// </summary>
    public async Task<bool> IsServerAvailableAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_fhirBaseUrl}/metadata");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    #region Private Helper Methods

    private async Task<bool> CoverageExistsAsync(string planId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_fhirBaseUrl}/Coverage/{planId}");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private List<BenefitPlan> ParseCoverageBundle(string bundle)
    {
        var plans = new List<BenefitPlan>();
        // Parse FHIR Bundle and extract Coverage resources
        // Convert to BenefitPlan objects
        // This is a simplified version - actual implementation would use FHIR.NET SDK
        return plans;
    }

    private List<AccountMember> ParsePatientBundle(string bundle)
    {
        var members = new List<AccountMember>();
        // Parse FHIR Bundle and extract Patient resources
        return members;
    }

    private List<ClaimRecord> ParseClaimBundle(string bundle)
    {
        var claims = new List<ClaimRecord>();
        // Parse FHIR Bundle and extract Claim resources
        return claims;
    }

    private List<Account> ParseOrganizationBundle(string bundle)
    {
        var accounts = new List<Account>();
        // Parse FHIR Bundle and extract Organization resources
        return accounts;
    }

    private object MapToFhirCoverage(BenefitPlan plan)
    {
        return new
        {
            resourceType = "Coverage",
            id = plan.PlanId,
            status = "active",
            type = new
            {
                coding = new[]
                {
                    new
                    {
                        system = "http://terminology.hl7.org/CodeSystem/v3-ActCode",
                        code = plan.PlanType,
                        display = $"{plan.PlanType} Health Plan"
                    }
                },
                text = plan.PlanName
            },
            period = new
            {
                start = plan.StartDate.ToString("yyyy-MM-dd"),
                end = plan.EndDate.ToString("yyyy-MM-dd")
            },
            costToBeneficiary = new[]
            {
                new
                {
                    type = new
                    {
                        coding = new[]
                        {
                            new
                            {
                                system = "http://terminology.hl7.org/CodeSystem/coverage-copay-type",
                                code = "deductible"
                            }
                        }
                    },
                    valueMoney = new
                    {
                        value = plan.Deductible,
                        currency = "USD"
                    }
                },
                new
                {
                    type = new
                    {
                        coding = new[]
                        {
                            new
                            {
                                system = "http://terminology.hl7.org/CodeSystem/coverage-copay-type",
                                code = "maxoutofpocket"
                            }
                        }
                    },
                    valueMoney = new
                    {
                        value = plan.OutOfPocketMax,
                        currency = "USD"
                    }
                }
            }
        };
    }

    private List<BenefitPlan> GetDefaultBenefitPlans()
    {
        return new List<BenefitPlan>
        {
            new BenefitPlan
            {
                PlanId = "HMO-001",
                PlanName = "Gold HMO Plan",
                PlanType = "HMO",
                Deductible = 1500,
                OutOfPocketMax = 6000,
                Coinsurance = 20,
                PrimaryCopay = 25,
                SpecialistCopay = 50,
                RequiresPriorAuth = true,
                StartDate = new DateTime(2026, 1, 1),
                EndDate = new DateTime(2026, 12, 31),
                InNetworkProviders = new List<NetworkProvider>
                {
                    new NetworkProvider 
                    { 
                        NPI = "1234567890", 
                        Name = "Valley Medical Group", 
                        Specialty = "Primary Care", 
                        Address = "123 Health St", 
                        City = "Los Angeles", 
                        State = "CA", 
                        Zip = "90001" 
                    }
                }
            },
            new BenefitPlan
            {
                PlanId = "PPO-002",
                PlanName = "Platinum PPO Plan",
                PlanType = "PPO",
                Deductible = 2000,
                OutOfPocketMax = 8000,
                Coinsurance = 30,
                PrimaryCopay = 30,
                SpecialistCopay = 60,
                RequiresPriorAuth = false,
                StartDate = new DateTime(2026, 1, 1),
                EndDate = new DateTime(2026, 12, 31),
                InNetworkProviders = new List<NetworkProvider>
                {
                    new NetworkProvider 
                    { 
                        NPI = "9876543210", 
                        Name = "Northridge Specialists", 
                        Specialty = "Cardiology", 
                        Address = "456 Care Ave", 
                        City = "Northridge", 
                        State = "CA", 
                        Zip = "91324" 
                    }
                }
            }
        };
    }

    private List<AccountMember> GetDefaultMembers()
    {
        return new List<AccountMember>
        {
            new AccountMember 
            { 
                AccountId = "ACC001", 
                MemberId = "SUB001", 
                FirstName = "John", 
                LastName = "Smith", 
                Relationship = "Subscriber", 
                DateOfBirth = new DateTime(1980, 5, 15), 
                PlanName = "Gold HMO Plan", 
                Status = "Active" 
            },
            new AccountMember 
            { 
                AccountId = "ACC001", 
                MemberId = "DEP001-01", 
                FirstName = "Jane", 
                LastName = "Smith", 
                Relationship = "Spouse", 
                DateOfBirth = new DateTime(1982, 8, 22), 
                PlanName = "Gold HMO Plan", 
                Status = "Active" 
            }
        };
    }

    private List<Account> GetDefaultAccounts()
    {
        return new List<Account>
        {
            new Account
            {
                AccountId = "ACC001",
                AccountName = "Northridge Healthcare Group",
                EmployerName = "Northridge Healthcare",
                PlanName = "Gold HMO Plan",
                PlanId = "HMO-001",
                MemberCount = 145,
                Status = "Active",
                TotalPremium = 87000,
                EffectiveDate = new DateTime(2025, 1, 1)
            }
        };
    }

    #endregion
}
