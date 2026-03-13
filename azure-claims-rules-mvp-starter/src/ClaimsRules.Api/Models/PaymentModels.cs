namespace ClaimsRules.Api.Models;

/// <summary>
/// Detailed payment calculation with line-item breakdown
/// Maps to FHIR ClaimResponse resource
/// </summary>
public class PaymentCalculation
{
    public string ClaimId { get; set; } = string.Empty;
    public string MemberId { get; set; } = string.Empty;
    public string PlanId { get; set; } = string.Empty;
    
    // Overall claim totals
    public decimal TotalBilled { get; set; }
    public decimal TotalAllowed { get; set; }
    public decimal TotalPlanPaid { get; set; }
    public decimal TotalPatientResponsibility { get; set; }
    public decimal TotalAdjustment { get; set; }
    
    // Line-item details
    public List<PaymentLineItem> LineItems { get; set; } = new();
    
    // Accumulator updates
    public decimal DeductibleApplied { get; set; }
    public decimal CoinsuranceApplied { get; set; }
    public decimal CopayApplied { get; set; }
    
    public DateTime CalculationDate { get; set; } = DateTime.UtcNow;
    public string CalculationStatus { get; set; } = "Calculated"; // Calculated, Approved, Denied, Pending
    
    public List<string> Messages { get; set; } = new();
}

/// <summary>
/// Individual service line payment detail
/// </summary>
public class PaymentLineItem
{
    public int LineNumber { get; set; }
    public string ServiceCode { get; set; } = string.Empty; // CPT/HCPCS code
    public string ServiceDescription { get; set; } = string.Empty;
    public DateTime ServiceDate { get; set; }
    public string ProviderId { get; set; } = string.Empty;
    public string PlaceOfService { get; set; } = "11"; // Office = 11, Hospital = 21
    
    // Financial breakdown
    public decimal Quantity { get; set; } = 1;
    public decimal BilledAmount { get; set; }
    public decimal AllowedAmount { get; set; }
    public decimal Adjustment { get; set; } // Billed - Allowed
    
    // Patient responsibility components
    public decimal Deductible { get; set; }
    public decimal Coinsurance { get; set; }
    public decimal Copay { get; set; }
    public decimal TotalPatientCost { get; set; }
    
    // Plan payment
    public decimal PlanPaid { get; set; }
    
    // Network impact
    public string NetworkStatus { get; set; } = string.Empty; // "In-Network", "Out-of-Network"
    public decimal NetworkCostMultiplier { get; set; } = 1.0m;
    
    // Adjustment codes
    public List<AdjustmentCode> Adjustments { get; set; } = new();
    
    public bool IsBundled { get; set; }
    public string? BundledIntoLineNumber { get; set; }
}

/// <summary>
/// CARC/RARC adjustment codes for claim adjustments
/// </summary>
public class AdjustmentCode
{
    public string GroupCode { get; set; } = string.Empty; // CO=Contractual, PR=Patient Responsibility, OA=Other
    public string ReasonCode { get; set; } = string.Empty; // CARC code (e.g., "45" = charge exceeds fee schedule)
    public string RemarkCode { get; set; } = string.Empty; // RARC code for additional detail
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Fee schedule for contracted rates
/// </summary>
public class FeeSchedule
{
    public string PlanId { get; set; } = string.Empty;
    public string ServiceCode { get; set; } = string.Empty;
    public string PlaceOfService { get; set; } = "11";
    public decimal AllowedAmount { get; set; }
    public DateTime EffectiveDate { get; set; }
    public DateTime? EndDate { get; set; }
}

/// <summary>
/// Claim service detail for payment calculation
/// </summary>
public class ClaimService
{
    public int LineNumber { get; set; }
    public string ProcedureCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime ServiceDate { get; set; }
    public decimal Quantity { get; set; } = 1;
    public decimal ChargeAmount { get; set; }
    public string ProviderId { get; set; } = string.Empty;
    public string PlaceOfService { get; set; } = "11";
    public List<string> Modifiers { get; set; } = new();
    public string DiagnosisPointer { get; set; } = string.Empty;
}

/// <summary>
/// Claim for payment calculation
/// </summary>
public class Claim
{
    public string ClaimId { get; set; } = string.Empty;
    public string MemberId { get; set; } = string.Empty;
    public string PlanId { get; set; } = string.Empty;
    public string ProviderId { get; set; } = string.Empty;
    public DateTime ReceivedDate { get; set; }
    public List<ClaimService> Services { get; set; } = new();
    public List<string> DiagnosisCodes { get; set; } = new();
    
    public string PrimaryServiceCode => Services.FirstOrDefault()?.ProcedureCode ?? "";
    public string PrimaryDiagnosis => DiagnosisCodes.FirstOrDefault() ?? "";
    public DateTime ServiceDate => Services.FirstOrDefault()?.ServiceDate ?? DateTime.Today;
    public decimal TotalCharges => Services.Sum(s => s.ChargeAmount * s.Quantity);
    public bool IsEmergency { get; set; }
    public bool IsOutOfNetwork { get; set; }
}
