namespace ClaimsPortal.BlazorWasm.Models;

/// <summary>
/// Complete Medicare Advantage plan configuration with medical, dental, and pharmacy benefits
/// </summary>
public class MedicareAdvantagePlan
{
    // Plan identification
    public string PlanId { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public string ContractNumber { get; set; } = string.Empty; // H1234
    public string PlanBenefitPackage { get; set; } = string.Empty; // 001, 002, etc.
    public string PlanType { get; set; } = string.Empty; // HMO, PPO, PFFS, SNP
    
    // Service area
    public List<string> Counties { get; set; } = new();
    public List<string> States { get; set; } = new();
    
    // Star rating
    public decimal StarRating { get; set; }
    
    // Premium and cost sharing
    public decimal MonthlyPremium { get; set; }
    public decimal PartBPremiumReduction { get; set; } // Give-back
    
    // Medical benefits
    public BenefitPlan MedicalBenefit { get; set; } = new();
    
    // Dental benefits
    public DentalBenefit DentalBenefit { get; set; } = new();
    
    // Pharmacy benefits (Part D)
    public PharmacyBenefit PharmacyBenefit { get; set; } = new();
    
    // Additional benefits (SSBCI)
    public bool IncludesVisionBenefit { get; set; }
    public bool IncludesHearingBenefit { get; set; }
    public bool IncludesOTCAllowance { get; set; }
    public decimal OTCAllowance { get; set; }
    public bool IncludesTransportation { get; set; }
    public bool IncludesMealsBenefit { get; set; }
    public bool IncludesFitnessProgram { get; set; }
    
    // Network
    public string NetworkId { get; set; } = string.Empty;
    public string NetworkName { get; set; } = string.Empty;
    
    public DateTime EffectiveDate { get; set; }
    public DateTime? TerminationDate { get; set; }
}

/// <summary>
/// Sample Medicare Advantage plan - Northridge Medicare Prime HMO
/// </summary>
public static class SampleMedicarePlan
{
    public static MedicareAdvantagePlan GetNorthridgeMedicarePrime()
    {
        return new MedicareAdvantagePlan
        {
            PlanId = "MA-H1234-001",
            PlanName = "Northridge Medicare Prime HMO",
            ContractNumber = "H1234",
            PlanBenefitPackage = "001",
            PlanType = "HMO",
            Counties = new List<string> { "Los Angeles", "Orange", "San Diego", "Riverside", "San Bernardino" },
            States = new List<string> { "CA" },
            StarRating = 4.5m,
            MonthlyPremium = 0.00m, // $0 premium plan
            PartBPremiumReduction = 50.00m, // $50/month Part B give-back
            
            // Medical benefits
            MedicalBenefit = new BenefitPlan
            {
                PlanId = "MA-H1234-001",
                PlanName = "Northridge Medicare Prime HMO - Medical",
                PlanType = "HMO",
                Deductible = 0, // No medical deductible
                OutOfPocketMax = 4900, // 2026 CMS limit: $8,850
                CoinsuranceRate = 0.20m,
                Copay = 0, // $0 PCP copay
                RequiresPriorAuth = true,
                EffectiveDate = new DateTime(2026, 1, 1),
                TerminationDate = new DateTime(2026, 12, 31)
            },
            
            // Dental benefits
            DentalBenefit = new DentalBenefit
            {
                PlanId = "MA-H1234-001",
                DentalNetwork = "DentaMax Preferred",
                
                // Annual maximum
                AnnualDentalMax = 2000,
                AnnualDeductible = 50,
                DeductibleWaivedForPreventive = true,
                
                // Coinsurance
                PreventiveCoinsurance = 0, // 100% covered
                BasicCoinsurance = 20, // 80% covered
                MajorCoinsurance = 50, // 50% covered
                OrthodontiaCoinsurance = 50,
                
                // Orthodontia
                OrthodontiaCovered = true,
                OrthodontiaLifetimeMax = 1500,
                OrthodontiaAgeLimit = 19,
                
                // Frequency
                CleaningsPerYear = 2,
                ExamsPerYear = 2,
                XRaysPerYear = 1,
                
                // Waiting periods
                BasicServicesWaitingPeriod = 0, // No waiting
                MajorServicesWaitingPeriod = 0,
                OrthodontiaWaitingPeriod = 12,
                
                MissingToothClause = false // Cover missing teeth
            },
            
            // Pharmacy benefits (Part D)
            PharmacyBenefit = new PharmacyBenefit
            {
                PlanId = "MA-H1234-001",
                FormularyId = "FORM-2026-PREF",
                FormularyName = "Northridge Preferred Formulary 2026",
                
                // Annual limits
                AnnualDrugDeductible = 0, // No deductible
                AnnualDrugOOPMax = 2000, // 2026 CMS limit: $2,000
                
                // Copays (30-day supply)
                Tier1Copay = 0, // Preferred Generic - $0
                Tier2Copay = 10, // Generic - $10
                Tier3Copay = 47, // Preferred Brand - $47
                Tier4Copay = 100, // Non-Preferred Brand - $100
                Tier5Copay = 33, // Specialty - 33% coinsurance
                
                // Coinsurance
                Tier1Coinsurance = 0,
                Tier2Coinsurance = 0,
                Tier3Coinsurance = 0,
                Tier4Coinsurance = 0,
                Tier5Coinsurance = 33, // 33% for specialty
                
                // Part D specific
                IsPartDPlan = true,
                InitialCoverageLimit = 5030, // 2026 limit
                CatastrophicThreshold = 8000, // 2026 threshold
                HasCoverageGap = false, // Gap closed in 2025
                
                // Mail order
                MailOrderAvailable = true,
                MailOrderDaysSupply = 90,
                MailOrderCopayMultiplier = 2.0m, // 90-day = 2x 30-day copay
                
                // Prior auth
                RequiresPriorAuth = true,
                PriorAuthDrugs = new List<string>
                {
                    "Specialty drugs",
                    "Brand drugs with generic available",
                    "High-cost biologics"
                },
                
                // Step therapy
                RequiresStepTherapy = true,
                StepTherapyRules = new List<StepTherapyRule>
                {
                    new()
                    {
                        RuleId = "STEP-PPI",
                        TargetDrug = "Nexium 40mg",
                        RequiredDrugs = new List<string> { "Omeprazole 20mg", "Pantoprazole 40mg" },
                        MinimumDaysSupply = 30,
                        AllowOverride = true
                    },
                    new()
                    {
                        RuleId = "STEP-STATIN",
                        TargetDrug = "Crestor 20mg",
                        RequiredDrugs = new List<string> { "Atorvastatin 40mg", "Simvastatin 40mg" },
                        MinimumDaysSupply = 90,
                        AllowOverride = true
                    }
                }
            },
            
            // Additional benefits
            IncludesVisionBenefit = true,
            IncludesHearingBenefit = true,
            IncludesOTCAllowance = true,
            OTCAllowance = 100, // $100/quarter
            IncludesTransportation = true,
            IncludesMealsBenefit = true,
            IncludesFitnessProgram = true,
            
            // Network
            NetworkId = "NET-CA-001",
            NetworkName = "Northridge Preferred Provider Network",
            
            EffectiveDate = new DateTime(2026, 1, 1),
            TerminationDate = new DateTime(2026, 12, 31)
        };
    }
    
    /// <summary>
    /// Sample formulary for the plan
    /// </summary>
    public static Formulary GetSampleFormulary()
    {
        return new Formulary
        {
            FormularyId = "FORM-2026-PREF",
            FormularyName = "Northridge Preferred Formulary 2026",
            Version = "2026.1",
            EffectiveDate = new DateTime(2026, 1, 1),
            
            Drugs = new List<FormularyDrug>
            {
                // Tier 1 - Preferred Generic
                new()
                {
                    NDC = "00093-0058-01",
                    DrugName = "Metformin 500mg Tablet",
                    GenericName = "Metformin HCl",
                    Strength = "500mg",
                    DosageForm = "Tablet",
                    Tier = 1,
                    IsGeneric = true,
                    AWP = 12.50m,
                    PlanCost = 5.00m,
                    RequiresPriorAuth = false,
                    TherapeuticClasses = new List<string> { "Antidiabetic", "Biguanide" }
                },
                new()
                {
                    NDC = "00378-0781-93",
                    DrugName = "Lisinopril 10mg Tablet",
                    GenericName = "Lisinopril",
                    Strength = "10mg",
                    DosageForm = "Tablet",
                    Tier = 1,
                    IsGeneric = true,
                    AWP = 8.75m,
                    PlanCost = 3.50m,
                    RequiresPriorAuth = false,
                    TherapeuticClasses = new List<string> { "ACE Inhibitor", "Antihypertensive" }
                },
                new()
                {
                    NDC = "00093-7347-01",
                    DrugName = "Atorvastatin 40mg Tablet",
                    GenericName = "Atorvastatin Calcium",
                    Strength = "40mg",
                    DosageForm = "Tablet",
                    Tier = 1,
                    IsGeneric = true,
                    AWP = 15.00m,
                    PlanCost = 6.00m,
                    RequiresPriorAuth = false,
                    TherapeuticClasses = new List<string> { "Statin", "Lipid-Lowering" }
                },
                
                // Tier 2 - Generic
                new()
                {
                    NDC = "00143-9537-01",
                    DrugName = "Omeprazole 20mg Capsule",
                    GenericName = "Omeprazole",
                    Strength = "20mg",
                    DosageForm = "Capsule",
                    Tier = 2,
                    IsGeneric = true,
                    AWP = 25.00m,
                    PlanCost = 10.00m,
                    RequiresPriorAuth = false,
                    TherapeuticClasses = new List<string> { "PPI", "GI" }
                },
                
                // Tier 3 - Preferred Brand
                new()
                {
                    NDC = "00310-0710-39",
                    DrugName = "Januvia 100mg Tablet",
                    GenericName = "Sitagliptin",
                    Strength = "100mg",
                    DosageForm = "Tablet",
                    Tier = 3,
                    IsBrand = true,
                    AWP = 550.00m,
                    PlanCost = 450.00m,
                    RequiresPriorAuth = true,
                    RequiresStepTherapy = true,
                    AlternativeDrugs = new List<string> { "Metformin 1000mg" },
                    TherapeuticClasses = new List<string> { "DPP-4 Inhibitor", "Antidiabetic" }
                },
                new()
                {
                    NDC = "00186-0084-28",
                    DrugName = "Eliquis 5mg Tablet",
                    GenericName = "Apixaban",
                    Strength = "5mg",
                    DosageForm = "Tablet",
                    Tier = 3,
                    IsBrand = true,
                    AWP = 580.00m,
                    PlanCost = 475.00m,
                    RequiresPriorAuth = true,
                    TherapeuticClasses = new List<string> { "Anticoagulant", "DOAC" }
                },
                
                // Tier 4 - Non-Preferred Brand
                new()
                {
                    NDC = "00088-2228-47",
                    DrugName = "Nexium 40mg Capsule",
                    GenericName = "Esomeprazole",
                    Strength = "40mg",
                    DosageForm = "Capsule",
                    Tier = 4,
                    IsBrand = true,
                    AWP = 285.00m,
                    PlanCost = 225.00m,
                    RequiresPriorAuth = true,
                    RequiresStepTherapy = true,
                    AlternativeDrugs = new List<string> { "Omeprazole 20mg", "Pantoprazole 40mg" },
                    TherapeuticClasses = new List<string> { "PPI", "GI" }
                },
                
                // Tier 5 - Specialty
                new()
                {
                    NDC = "59676-0580-02",
                    DrugName = "Humira 40mg Pen",
                    GenericName = "Adalimumab",
                    Strength = "40mg/0.8mL",
                    DosageForm = "Injection",
                    Tier = 5,
                    IsBrand = true,
                    IsSpecialty = true,
                    AWP = 7850.00m,
                    PlanCost = 6500.00m,
                    RequiresPriorAuth = true,
                    HasQuantityLimit = true,
                    MaxQuantity = 2,
                    MaxDaysSupply = 30,
                    TherapeuticClasses = new List<string> { "TNF Inhibitor", "Immunosuppressant" },
                    Indications = new List<string> { "Rheumatoid Arthritis", "Crohn's Disease", "Psoriasis" }
                },
                new()
                {
                    NDC = "50090-5106-01",
                    DrugName = "Enbrel 50mg Syringe",
                    GenericName = "Etanercept",
                    Strength = "50mg/mL",
                    DosageForm = "Injection",
                    Tier = 5,
                    IsBrand = true,
                    IsSpecialty = true,
                    AWP = 6900.00m,
                    PlanCost = 5750.00m,
                    RequiresPriorAuth = true,
                    HasQuantityLimit = true,
                    MaxQuantity = 4,
                    MaxDaysSupply = 30,
                    TherapeuticClasses = new List<string> { "TNF Inhibitor", "Immunosuppressant" }
                }
            }
        };
    }
}
