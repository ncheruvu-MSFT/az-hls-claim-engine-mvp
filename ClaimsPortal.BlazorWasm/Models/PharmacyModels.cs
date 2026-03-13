namespace ClaimsPortal.BlazorWasm.Models;

/// <summary>
/// Pharmacy benefit configuration for a plan
/// </summary>
public class PharmacyBenefit
{
    public string PlanId { get; set; } = string.Empty;
    public string FormularyId { get; set; } = string.Empty;
    public string FormularyName { get; set; } = string.Empty;
    
    // Annual limits
    public decimal AnnualDrugDeductible { get; set; }
    public decimal AnnualDrugOOPMax { get; set; }
    
    // Copays by tier
    public decimal Tier1Copay { get; set; } // Generic
    public decimal Tier2Copay { get; set; } // Preferred Brand
    public decimal Tier3Copay { get; set; } // Non-Preferred Brand
    public decimal Tier4Copay { get; set; } // Specialty
    public decimal Tier5Copay { get; set; } // Preferred Specialty
    
    // Coinsurance (instead of copay)
    public decimal Tier1Coinsurance { get; set; }
    public decimal Tier2Coinsurance { get; set; }
    public decimal Tier3Coinsurance { get; set; }
    public decimal Tier4Coinsurance { get; set; }
    public decimal Tier5Coinsurance { get; set; }
    
    // Medicare Part D specific
    public bool IsPartDPlan { get; set; }
    public decimal InitialCoverageLimit { get; set; } // $5,030 in 2026
    public decimal CatastrophicThreshold { get; set; } // $8,000 in 2026
    public bool HasCoverageGap { get; set; } // "Donut hole"
    
    // Mail order
    public bool MailOrderAvailable { get; set; }
    public int MailOrderDaysSupply { get; set; } = 90;
    public decimal MailOrderCopayMultiplier { get; set; } = 2.0m; // 90-day = 2x 30-day
    
    // Prior authorization
    public bool RequiresPriorAuth { get; set; }
    public List<string> PriorAuthDrugs { get; set; } = new();
    
    // Step therapy
    public bool RequiresStepTherapy { get; set; }
    public List<StepTherapyRule> StepTherapyRules { get; set; } = new();
}

/// <summary>
/// Pharmacy accumulator tracking
/// </summary>
public class PharmacyAccumulator
{
    public string MemberId { get; set; } = string.Empty;
    public string PlanId { get; set; } = string.Empty;
    public int Year { get; set; }
    
    // Deductible
    public decimal DrugDeductible { get; set; }
    public decimal DrugDeductibleMet { get; set; }
    public decimal DrugDeductibleRemaining => Math.Max(0, DrugDeductible - DrugDeductibleMet);
    
    // Out-of-pocket
    public decimal DrugOOPMax { get; set; }
    public decimal DrugOOPMet { get; set; }
    public decimal DrugOOPRemaining => Math.Max(0, DrugOOPMax - DrugOOPMet);
    
    // Part D specific
    public decimal TrueOutOfPocket { get; set; } // TrOOP for Part D
    public string CoveragePhase { get; set; } = "Deductible"; // Deductible, Initial, Gap, Catastrophic
    public decimal TotalDrugSpend { get; set; }
    
    // By tier
    public int Tier1RxCount { get; set; }
    public int Tier2RxCount { get; set; }
    public int Tier3RxCount { get; set; }
    public int Tier4RxCount { get; set; }
    public int Tier5RxCount { get; set; }
    
    public decimal Tier1Spend { get; set; }
    public decimal Tier2Spend { get; set; }
    public decimal Tier3Spend { get; set; }
    public decimal Tier4Spend { get; set; }
    public decimal Tier5Spend { get; set; }
    
    public DateTime LastUpdated { get; set; } = DateTime.Now;
}

/// <summary>
/// Formulary drug list
/// </summary>
public class Formulary
{
    public string FormularyId { get; set; } = string.Empty;
    public string FormularyName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public DateTime EffectiveDate { get; set; }
    public DateTime? EndDate { get; set; }
    
    public List<FormularyDrug> Drugs { get; set; } = new();
    
    public int TotalDrugs => Drugs.Count;
    public int Tier1Count => Drugs.Count(d => d.Tier == 1);
    public int Tier2Count => Drugs.Count(d => d.Tier == 2);
    public int Tier3Count => Drugs.Count(d => d.Tier == 3);
    public int Tier4Count => Drugs.Count(d => d.Tier == 4);
    public int Tier5Count => Drugs.Count(d => d.Tier == 5);
}

/// <summary>
/// Individual drug in formulary
/// </summary>
public class FormularyDrug
{
    public string NDC { get; set; } = string.Empty; // 11-digit National Drug Code
    public string DrugName { get; set; } = string.Empty;
    public string GenericName { get; set; } = string.Empty;
    public string Strength { get; set; } = string.Empty;
    public string DosageForm { get; set; } = string.Empty; // Tablet, Capsule, Liquid, etc.
    
    public int Tier { get; set; } // 1=Generic, 2=Preferred Brand, 3=Non-Preferred, 4=Specialty, 5=Preferred Specialty
    public string TierName => GetTierName(Tier);
    
    public bool IsGeneric { get; set; }
    public bool IsBrand { get; set; }
    public bool IsSpecialty { get; set; }
    
    // Pricing
    public decimal AWP { get; set; } // Average Wholesale Price
    public decimal WAC { get; set; } // Wholesale Acquisition Cost
    public decimal PlanCost { get; set; } // What plan pays
    
    // Restrictions
    public bool RequiresPriorAuth { get; set; }
    public bool RequiresStepTherapy { get; set; }
    public bool HasQuantityLimit { get; set; }
    public int? MaxQuantity { get; set; }
    public int? MaxDaysSupply { get; set; }
    
    // Clinical
    public List<string> TherapeuticClasses { get; set; } = new();
    public List<string> Indications { get; set; } = new();
    public List<string> AlternativeDrugs { get; set; } = new(); // For step therapy
    
    public DateTime LastPriceUpdate { get; set; } = DateTime.Now;
    
    private string GetTierName(int tier) => tier switch
    {
        1 => "Generic",
        2 => "Preferred Brand",
        3 => "Non-Preferred Brand",
        4 => "Specialty",
        5 => "Preferred Specialty",
        _ => "Unknown"
    };
}

/// <summary>
/// Step therapy rule - must try Drug A before Drug B covered
/// </summary>
public class StepTherapyRule
{
    public string RuleId { get; set; } = string.Empty;
    public string TargetDrug { get; set; } = string.Empty; // Drug that requires step
    public List<string> RequiredDrugs { get; set; } = new(); // Must try these first
    public int MinimumDaysSupply { get; set; } = 30;
    public bool AllowOverride { get; set; } = true;
}

/// <summary>
/// Pharmacy claim
/// </summary>
public class PharmacyClaim
{
    public string ClaimId { get; set; } = string.Empty;
    public string MemberId { get; set; } = string.Empty;
    public string PlanId { get; set; } = string.Empty;
    
    public string NDC { get; set; } = string.Empty;
    public string DrugName { get; set; } = string.Empty;
    public string PrescriberNPI { get; set; } = string.Empty;
    public string PharmacyNPI { get; set; } = string.Empty;
    
    public DateTime FillDate { get; set; }
    public DateTime DateWritten { get; set; }
    
    public int Quantity { get; set; }
    public int DaysSupply { get; set; }
    public int RefillNumber { get; set; }
    
    public decimal IngredientCost { get; set; }
    public decimal DispensingFee { get; set; }
    public decimal TotalCost => IngredientCost + DispensingFee;
    
    public decimal PlanPaid { get; set; }
    public decimal MemberCopay { get; set; }
    
    public string Status { get; set; } = "Pending"; // Pending, Approved, Denied
    public List<string> DenialReasons { get; set; } = new();
}
