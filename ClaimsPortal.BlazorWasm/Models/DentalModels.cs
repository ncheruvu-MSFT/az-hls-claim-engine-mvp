namespace ClaimsPortal.BlazorWasm.Models;

/// <summary>
/// Dental benefit configuration for a plan
/// </summary>
public class DentalBenefit
{
    public string PlanId { get; set; } = string.Empty;
    public string DentalNetwork { get; set; } = string.Empty;
    
    // Annual limits
    public decimal AnnualDentalMax { get; set; } // Typical: $1,500-$2,000
    public decimal AnnualDentalMaxUsed { get; set; }
    public decimal AnnualDentalMaxRemaining => Math.Max(0, AnnualDentalMax - AnnualDentalMaxUsed);
    
    public decimal AnnualDeductible { get; set; }
    public bool DeductibleWaivedForPreventive { get; set; } = true;
    
    // Coinsurance by category
    public decimal PreventiveCoinsurance { get; set; } // Class I - Typically 0% (100% covered)
    public decimal BasicCoinsurance { get; set; } // Class II - Typically 20% (80% covered)
    public decimal MajorCoinsurance { get; set; } // Class III - Typically 50% (50% covered)
    public decimal OrthodontiaCoinsurance { get; set; } // Class IV - Typically 50%
    
    // Orthodontia
    public bool OrthodontiaCovered { get; set; }
    public decimal OrthodontiaLifetimeMax { get; set; } // Typical: $1,500-$2,000
    public int OrthodontiaAgeLimit { get; set; } = 19; // Often only for children
    
    // Frequency limitations
    public int CleaningsPerYear { get; set; } = 2;
    public int ExamsPerYear { get; set; } = 2;
    public int XRaysPerYear { get; set; } = 1;
    
    // Waiting periods (in months)
    public int BasicServicesWaitingPeriod { get; set; } = 6;
    public int MajorServicesWaitingPeriod { get; set; } = 12;
    public int OrthodontiaWaitingPeriod { get; set; } = 12;
    
    // Missing tooth clause
    public bool MissingToothClause { get; set; } = true; // Don't cover replacement of missing teeth before enrollment
}

/// <summary>
/// Dental accumulator tracking
/// </summary>
public class DentalAccumulator
{
    public string MemberId { get; set; } = string.Empty;
    public string PlanId { get; set; } = string.Empty;
    public int Year { get; set; }
    
    // Annual maximum
    public decimal AnnualMax { get; set; }
    public decimal AnnualMaxUsed { get; set; }
    public decimal AnnualMaxRemaining => Math.Max(0, AnnualMax - AnnualMaxUsed);
    
    // Deductible
    public decimal Deductible { get; set; }
    public decimal DeductibleMet { get; set; }
    public decimal DeductibleRemaining => Math.Max(0, Deductible - DeductibleMet);
    
    // Orthodontia lifetime max
    public decimal OrthodontiaLifetimeMax { get; set; }
    public decimal OrthodontiaUsed { get; set; }
    public decimal OrthodontiaRemaining => Math.Max(0, OrthodontiaLifetimeMax - OrthodontiaUsed);
    
    // Spend by category
    public decimal PreventiveSpend { get; set; }
    public decimal BasicSpend { get; set; }
    public decimal MajorSpend { get; set; }
    public decimal OrthodontiaSpend { get; set; }
    
    // Service counts (for frequency limits)
    public int CleaningsThisYear { get; set; }
    public int ExamsThisYear { get; set; }
    public int BitewingXRaysThisYear { get; set; }
    public int FullMouthXRaysThisYear { get; set; }
    
    public DateTime LastUpdated { get; set; } = DateTime.Now;
}

/// <summary>
/// Dental fee schedule
/// </summary>
public class DentalFeeSchedule
{
    public string PlanId { get; set; } = string.Empty;
    public string ScheduleName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    
    public List<DentalProcedure> Procedures { get; set; } = new();
}

/// <summary>
/// Dental procedure with CDT code
/// </summary>
public class DentalProcedure
{
    public string CDTCode { get; set; } = string.Empty; // D0120, D1110, etc.
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // Preventive, Basic, Major, Orthodontia
    public string ServiceClass { get; set; } = string.Empty; // Class I, II, III, IV
    
    public decimal UCR { get; set; } // Usual, Customary, and Reasonable fee
    public decimal AllowedAmount { get; set; }
    
    // Frequency limits
    public int? MaxPerYear { get; set; }
    public int? MaxPerLifetime { get; set; }
    public int? MonthsBetweenServices { get; set; }
    
    // Age restrictions
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    
    public bool RequiresPriorAuth { get; set; }
    public bool RequiresPredetermination { get; set; } // For major services >$300
}

/// <summary>
/// Dental claim
/// </summary>
public class DentalClaim
{
    public string ClaimId { get; set; } = string.Empty;
    public string MemberId { get; set; } = string.Empty;
    public string PlanId { get; set; } = string.Empty;
    
    public string DentistNPI { get; set; } = string.Empty;
    public string DentistName { get; set; } = string.Empty;
    
    public DateTime ServiceDate { get; set; }
    
    public List<DentalService> Services { get; set; } = new();
    
    public decimal TotalBilled => Services.Sum(s => s.BilledAmount);
    public decimal TotalAllowed => Services.Sum(s => s.AllowedAmount);
    public decimal TotalPlanPaid => Services.Sum(s => s.PlanPaid);
    public decimal TotalPatientCost => Services.Sum(s => s.PatientCost);
    
    public string Status { get; set; } = "Pending";
    public List<string> DenialReasons { get; set; } = new();
}

/// <summary>
/// Individual dental service line
/// </summary>
public class DentalService
{
    public int LineNumber { get; set; }
    public string CDTCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ToothNumber { get; set; } = string.Empty; // 1-32
    public string ToothSurface { get; set; } = string.Empty; // M, O, D, B, L
    
    public decimal BilledAmount { get; set; }
    public decimal AllowedAmount { get; set; }
    public decimal Deductible { get; set; }
    public decimal Coinsurance { get; set; }
    public decimal PatientCost => Deductible + Coinsurance;
    public decimal PlanPaid => AllowedAmount - PatientCost;
    
    public string Category { get; set; } = string.Empty; // Preventive, Basic, Major
    public bool IsApproved { get; set; }
    public List<string> DenialReasons { get; set; } = new();
}

/// <summary>
/// Common dental CDT codes
/// </summary>
public static class DentalCDTCodes
{
    // Preventive (Class I)
    public const string CLEANING_ADULT = "D1110"; // Adult prophylaxis
    public const string CLEANING_CHILD = "D1120"; // Child prophylaxis
    public const string FLUORIDE = "D1206"; // Topical fluoride
    public const string EXAM_PERIODIC = "D0120"; // Periodic oral evaluation
    public const string EXAM_COMPREHENSIVE = "D0150"; // Comprehensive oral evaluation
    public const string XRAY_BITEWING = "D0274"; // Bitewing - four films
    public const string XRAY_FULL_MOUTH = "D0210"; // Full mouth series
    
    // Basic (Class II)
    public const string FILLING_AMALGAM_1 = "D2140"; // Amalgam - one surface
    public const string FILLING_AMALGAM_2 = "D2150"; // Amalgam - two surfaces
    public const string FILLING_COMPOSITE_1 = "D2330"; // Composite - one surface
    public const string FILLING_COMPOSITE_2 = "D2331"; // Composite - two surfaces
    public const string EXTRACTION_SIMPLE = "D7140"; // Simple extraction
    public const string ROOT_CANAL_ANTERIOR = "D3310"; // Anterior root canal
    
    // Major (Class III)
    public const string CROWN_PORCELAIN = "D2740"; // Porcelain/ceramic crown
    public const string CROWN_FULL_CAST = "D2750"; // Full cast metal crown
    public const string BRIDGE_PONTIC = "D6242"; // Pontic - porcelain fused to metal
    public const string DENTURE_COMPLETE_UPPER = "D5110"; // Complete upper denture
    public const string DENTURE_COMPLETE_LOWER = "D5120"; // Complete lower denture
    public const string IMPLANT_BODY = "D6010"; // Endosteal implant
    
    // Orthodontia (Class IV)
    public const string ORTHO_COMPREHENSIVE = "D8080"; // Comprehensive orthodontic treatment
    public const string ORTHO_INTERCEPTIVE = "D8050"; // Interceptive orthodontic treatment
}
