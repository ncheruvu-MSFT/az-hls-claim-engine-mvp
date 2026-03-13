namespace ClaimsPortal.BlazorWasm.Models;

public class BenefitPlan
{
    public string PlanId { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public string PlanType { get; set; } = string.Empty; // PPO, HMO, EPO, POS
    public decimal Deductible { get; set; }
    public decimal OutOfPocketMax { get; set; }
    public decimal CoinsuranceRate { get; set; } // e.g., 0.20 for 20%
    public int Coinsurance { get; set; } // Percentage 0-100
    public decimal Copay { get; set; }
    public int PrimaryCopay { get; set; }
    public int SpecialistCopay { get; set; }
    public List<string> CoveredServices { get; set; } = new();
    public List<NetworkProvider> InNetworkProviders { get; set; } = new();
    public bool RequiresPriorAuth { get; set; }
    public DateTime EffectiveDate { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime? TerminationDate { get; set; }
}

public class NetworkProvider
{
    public string NPI { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Specialty { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Zip { get; set; } = string.Empty;
}

public class ClaimValidationRequest
{
    public string PatientId { get; set; } = string.Empty;
    public string PlanId { get; set; } = string.Empty;
    public DateTime ServiceDate { get; set; }
    public string ServiceCode { get; set; } = string.Empty; // CPT/HCPCS code
    public decimal BilledAmount { get; set; }
    public string ProviderId { get; set; } = string.Empty;
    public decimal YearToDateSpending { get; set; }
}

public class ClaimValidationResult
{
    public bool IsApproved { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public decimal PatientResponsibility { get; set; }
    public decimal InsurancePayment { get; set; }
    public string Explanation { get; set; } = string.Empty;
}

public class EligibilityRequest
{
    public string PatientId { get; set; } = string.Empty;
    public string PlanId { get; set; } = string.Empty;
}

public class EligibilityResult
{
    public bool IsEligible { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public decimal YearToDateDeductible { get; set; }
    public decimal RemainingDeductible { get; set; }
    public decimal YearToDateOutOfPocket { get; set; }
    public decimal RemainingOutOfPocket { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class BenefitConfigRequest
{
    public string NaturalLanguageQuery { get; set; } = string.Empty;
    public string Context { get; set; } = string.Empty; // Current page/configuration context
}
