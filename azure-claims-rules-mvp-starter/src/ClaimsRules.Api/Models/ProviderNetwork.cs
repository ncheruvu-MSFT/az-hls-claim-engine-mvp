namespace ClaimsRules.Api.Models;

/// <summary>
/// Provider network with tier-based reimbursement rates
/// Maps to FHIR Organization resource
/// </summary>
public class ProviderNetwork
{
    public string NetworkId { get; set; } = string.Empty;
    public string NetworkName { get; set; } = string.Empty;
    public string NetworkType { get; set; } = string.Empty; // "Tier1", "Tier2", "Preferred", "Standard"
    public decimal ReimbursementRate { get; set; } = 1.0m; // 100% = 1.0, 80% = 0.8
    public List<string> ProviderIds { get; set; } = new();
    public List<string> CoveredStates { get; set; } = new();
    public DateTime EffectiveDate { get; set; }
    public DateTime? TerminationDate { get; set; }
}

/// <summary>
/// Healthcare provider with credentials and network affiliations
/// Maps to FHIR Practitioner/PractitionerRole and Organization resources
/// </summary>
public class Provider
{
    public string ProviderId { get; set; } = string.Empty;
    public string Npi { get; set; } = string.Empty; // National Provider Identifier (10 digits)
    public string TaxId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ProviderType { get; set; } = string.Empty; // "Hospital", "Physician", "Lab", "Facility"
    public List<string> Specialties { get; set; } = new();
    public List<string> TaxonomyCodes { get; set; } = new();
    public ProviderAddress Address { get; set; } = new();
    public List<string> NetworkIds { get; set; } = new(); // Networks this provider belongs to
    public bool IsActive { get; set; } = true;
    public decimal QualityScore { get; set; } = 0; // 0-100 scale
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

/// <summary>
/// Links benefit plans to provider networks with tier-specific cost sharing
/// </summary>
public class PlanNetworkMapping
{
    public string PlanId { get; set; } = string.Empty;
    public string NetworkId { get; set; } = string.Empty;
    public string NetworkTier { get; set; } = string.Empty; // "In-Network", "Preferred", "Out-of-Network"
    public decimal CostShareMultiplier { get; set; } = 1.0m; // 1.0 = standard, 1.5 = 50% higher, 2.0 = double
    public bool RequiresPriorAuth { get; set; } = false;
    public bool RequiresReferral { get; set; } = false; // HMO requirement
}

/// <summary>
/// Result of network validation with cost impact
/// </summary>
public class NetworkValidationResult
{
    public bool IsValid { get; set; }
    public string NetworkStatus { get; set; } = string.Empty; // "In-Network", "Preferred", "Out-of-Network", "Not Found"
    public string NetworkId { get; set; } = string.Empty;
    public string NetworkName { get; set; } = string.Empty;
    public decimal CostShareMultiplier { get; set; } = 1.0m;
    public decimal ReimbursementRate { get; set; } = 1.0m;
    public bool RequiresPriorAuth { get; set; }
    public bool RequiresReferral { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    
    // Provider details
    public string ProviderId { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public string ProviderNpi { get; set; } = string.Empty;
    public string ProviderType { get; set; } = string.Empty;
    public bool ProviderIsActive { get; set; }
    public bool CredentialsValid { get; set; }
}

/// <summary>
/// Provider contract with reimbursement terms
/// </summary>
public class ProviderContract
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ContractNumber { get; set; } = string.Empty;
    public string ProviderNPI { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public string PayerId { get; set; } = string.Empty;
    public string PayerName { get; set; } = string.Empty;
    public string ContractType { get; set; } = string.Empty; // Individual, Group, IPA
    public string NetworkTier { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // Active, Pending, Terminated
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
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
    public string ModifiedBy { get; set; } = string.Empty;
}

/// <summary>
/// Reimbursement model details
/// </summary>
public class ReimbursementModel
{
    public string Type { get; set; } = string.Empty; // FFS, Capitation, BundledPayment, ValueBased
    public decimal Rate { get; set; }
    public string RateType { get; set; } = string.Empty; // PercentOfMedicare, FlatFee, PMPM
    public decimal PercentOfMedicare { get; set; }
    public decimal PMPMRate { get; set; }
    public bool HasQualityBonus { get; set; }
    public decimal QualityBonusPercent { get; set; }
    public Dictionary<string, decimal> CPTRates { get; set; } = new();
}

/// <summary>
/// Network tier definition
/// </summary>
public class NetworkTier
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string TierName { get; set; } = string.Empty; // Tier1, Tier2, Tier3
    public string TierLabel { get; set; } = string.Empty; // Premium, Standard, Basic
    public int MemberCopay { get; set; }
    public int MemberCoinsurance { get; set; }
    public decimal ProviderReimbursementPercent { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<string> BenefitRestrictions { get; set; } = new();
    public bool RequiresReferral { get; set; }
    public int ProviderCount { get; set; }
    public List<string> AssociatedPlans { get; set; } = new();
}

/// <summary>
/// Provider search request
/// </summary>
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

/// <summary>
/// Provider search response with pagination
/// </summary>
public class ProviderSearchResponse
{
    public List<Provider> Providers { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}
