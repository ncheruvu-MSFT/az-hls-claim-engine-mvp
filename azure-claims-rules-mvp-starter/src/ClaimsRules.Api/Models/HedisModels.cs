namespace ClaimsRules.Api.Models;

/// <summary>
/// HEDIS measure result with numerator, denominator, and gap patients
/// </summary>
public class HedisMeasureResult
{
    public required string MeasureId { get; set; } // e.g., BCS, CBP, HBD
    public required string MeasureName { get; set; }
    public required string MeasureCategory { get; set; } // Effectiveness of Care, Prevention & Screening
    public int Denominator { get; set; } // Eligible population count
    public int Numerator { get; set; } // Met criteria count
    public double Rate { get; set; } // Percentage (0-100)
    public int GapCount { get; set; } // Patients with gaps
    public List<HedisGapPatient> GapPatients { get; set; } = new();
    public DateTime CalculatedAt { get; set; }
    public int MeasurementYear { get; set; }
    public string? PlanSegment { get; set; } // e.g., "HMO East", "PPO West"
}

/// <summary>
/// Patient with HEDIS measure gap (needs intervention)
/// </summary>
public class HedisGapPatient
{
    public required string PatientId { get; set; }
    public required string PatientName { get; set; }
    public required string DateOfBirth { get; set; }
    public int Age { get; set; }
    public string? Gender { get; set; }
    public required string MeasureId { get; set; }
    public required string MeasureName { get; set; }
    public string? LastServiceDate { get; set; } // Last time they met criteria (if ever)
    public int DaysSinceLastService { get; set; }
    public string? RecommendedAction { get; set; } // e.g., "Schedule mammogram"
    public string? PrimaryCareProvider { get; set; }
    public string? Phone { get; set; }
    public int Priority { get; set; } // 1-5, higher = more urgent
    public string? RiskLevel { get; set; } // Low, Medium, High
}

/// <summary>
/// HEDIS measure dashboard summary
/// </summary>
public class HedisDashboard
{
    public int MeasurementYear { get; set; }
    public string? PlanSegment { get; set; }
    public double OverallStarRating { get; set; } // 1.0-5.0
    public double ProjectedStarRating { get; set; } // With gap closure
    public int TotalMembers { get; set; }
    public int TotalGaps { get; set; }
    public List<HedisMeasureResult> Measures { get; set; } = new();
    public List<HedisPriorityAction> PriorityActions { get; set; } = new();
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// Priority action for gap closure
/// </summary>
public class HedisPriorityAction
{
    public required string MeasureId { get; set; }
    public required string MeasureName { get; set; }
    public int GapCount { get; set; }
    public double CurrentRate { get; set; }
    public double TargetRate { get; set; }
    public double StarRatingImpact { get; set; } // +0.1, +0.5, etc.
    public string? RecommendedApproach { get; set; } // Outreach strategy
}

/// <summary>
/// HEDIS measure definition with FHIR mapping
/// </summary>
public class HedisMeasureDefinition
{
    public required string MeasureId { get; set; }
    public required string MeasureName { get; set; }
    public required string Description { get; set; }
    public required string Category { get; set; }
    public required string NumeratorDescription { get; set; }
    public required string DenominatorDescription { get; set; }
    public List<string> LoincCodes { get; set; } = new(); // Lab tests
    public List<string> SnomedCodes { get; set; } = new(); // Procedures
    public List<string> CptCodes { get; set; } = new(); // CPT codes
    public List<string> IcdCodes { get; set; } = new(); // Diagnosis codes
    public int LookbackMonths { get; set; } // How far back to search
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public string? Gender { get; set; }
    public List<string> ExclusionCriteria { get; set; } = new();
}

/// <summary>
/// Static class with HEDIS measure definitions
/// </summary>
public static class HedisMeasures
{
    // Effectiveness of Care - Clinical
    public const string BCS = "BCS"; // Breast Cancer Screening
    public const string COL = "COL"; // Colorectal Cancer Screening
    public const string CBP = "CBP"; // Controlling High Blood Pressure
    public const string CDC = "CDC"; // Comprehensive Diabetes Care
    public const string HBD = "HBD"; // HbA1c Control for Patients with Diabetes
    public const string KED = "KED"; // Kidney Health Evaluation for Patients with Diabetes
    public const string EED = "EED"; // Eye Exam for Patients with Diabetes
    public const string SPC = "SPC"; // Statin Therapy for Patients with Cardiovascular Disease
    public const string OMW = "OMW"; // Osteoporosis Management in Women Who Had a Fracture
    public const string PBH = "PBH"; // Persistence of Beta-Blocker Treatment After a Heart Attack

    // Prevention & Screening
    public const string IMA = "IMA"; // Immunizations for Adolescents
    public const string CIS = "CIS"; // Childhood Immunization Status
    public const string W30 = "W30"; // Well-Child Visits in the First 30 Months of Life
    public const string AWC = "AWC"; // Adolescent Well-Care Visits
    public const string ABA = "ABA"; // Adult BMI Assessment

    /// <summary>
    /// Get all measure definitions
    /// </summary>
    public static List<HedisMeasureDefinition> GetAllDefinitions()
    {
        return new List<HedisMeasureDefinition>
        {
            // BCS - Breast Cancer Screening
            new HedisMeasureDefinition
            {
                MeasureId = BCS,
                MeasureName = "Breast Cancer Screening",
                Description = "Percentage of women 50-74 years of age who had a mammogram to screen for breast cancer in the past 27 months",
                Category = "Effectiveness of Care",
                NumeratorDescription = "Mammogram in past 27 months",
                DenominatorDescription = "Women age 50-74",
                LoincCodes = new List<string> { "24604-1", "24605-8", "24606-6", "37768-3", "38070-1" },
                SnomedCodes = new List<string> { "24623002", "71651007", "439324009" },
                CptCodes = new List<string> { "77065", "77066", "77067" },
                LookbackMonths = 27,
                MinAge = 50,
                MaxAge = 74,
                Gender = "female",
                ExclusionCriteria = new List<string> { "Bilateral mastectomy", "History of breast cancer" }
            },

            // COL - Colorectal Cancer Screening
            new HedisMeasureDefinition
            {
                MeasureId = COL,
                MeasureName = "Colorectal Cancer Screening",
                Description = "Percentage of members 45-75 years who had appropriate colorectal cancer screening",
                Category = "Effectiveness of Care",
                NumeratorDescription = "Colonoscopy in past 10 years OR FIT test in past year OR Cologuard in past 3 years",
                DenominatorDescription = "Members age 45-75",
                LoincCodes = new List<string> { "27396-1", "27401-9", "27925-7", "29771-3", "2335-8" },
                SnomedCodes = new List<string> { "73761001", "12350003", "446745002" },
                CptCodes = new List<string> { "45378", "45380", "45381", "45384", "45385", "44388", "44389" },
                LookbackMonths = 120, // 10 years for colonoscopy
                MinAge = 45,
                MaxAge = 75,
                ExclusionCriteria = new List<string> { "Colorectal cancer", "Total colectomy" }
            },

            // CBP - Controlling High Blood Pressure
            new HedisMeasureDefinition
            {
                MeasureId = CBP,
                MeasureName = "Controlling High Blood Pressure",
                Description = "Percentage of members 18-85 years with hypertension whose BP was <140/90 mmHg",
                Category = "Effectiveness of Care",
                NumeratorDescription = "Most recent BP <140/90 mmHg",
                DenominatorDescription = "Members age 18-85 with hypertension diagnosis",
                LoincCodes = new List<string> { "85354-9", "8480-6", "8462-4" }, // BP systolic and diastolic
                IcdCodes = new List<string> { "I10", "I11", "I12", "I13", "I15" }, // Hypertension
                LookbackMonths = 12,
                MinAge = 18,
                MaxAge = 85,
                ExclusionCriteria = new List<string> { "ESRD", "Kidney transplant", "Pregnancy" }
            },

            // HBD - HbA1c Control for Patients with Diabetes
            new HedisMeasureDefinition
            {
                MeasureId = HBD,
                MeasureName = "HbA1c Control for Patients with Diabetes",
                Description = "Percentage of members 18-75 years with diabetes (type 1 and type 2) whose HbA1c was <8.0%",
                Category = "Effectiveness of Care",
                NumeratorDescription = "Most recent HbA1c <8.0%",
                DenominatorDescription = "Members age 18-75 with diabetes",
                LoincCodes = new List<string> { "4548-4", "17856-6", "59261-8" }, // HbA1c
                IcdCodes = new List<string> { "E10", "E11" }, // Diabetes Type 1 and Type 2
                LookbackMonths = 12,
                MinAge = 18,
                MaxAge = 75,
                ExclusionCriteria = new List<string> { "Polycystic ovaries", "Steroid-induced diabetes" }
            },

            // CDC - Comprehensive Diabetes Care (composite)
            new HedisMeasureDefinition
            {
                MeasureId = CDC,
                MeasureName = "Comprehensive Diabetes Care",
                Description = "Percentage of members 18-75 years with diabetes who received all recommended care (HbA1c, eye exam, kidney test, BP control)",
                Category = "Effectiveness of Care",
                NumeratorDescription = "HbA1c test + Eye exam + Kidney test + BP <140/90",
                DenominatorDescription = "Members age 18-75 with diabetes",
                LoincCodes = new List<string> { "4548-4", "2085-9", "32294-1" }, // HbA1c, creatinine, eye exam
                IcdCodes = new List<string> { "E10", "E11" },
                LookbackMonths = 12,
                MinAge = 18,
                MaxAge = 75
            },

            // KED - Kidney Health Evaluation for Patients with Diabetes
            new HedisMeasureDefinition
            {
                MeasureId = KED,
                MeasureName = "Kidney Health Evaluation for Patients with Diabetes",
                Description = "Percentage of members 18-75 years with diabetes who received a kidney health evaluation (uACR or eGFR)",
                Category = "Effectiveness of Care",
                NumeratorDescription = "uACR or eGFR test in measurement year",
                DenominatorDescription = "Members age 18-75 with diabetes",
                LoincCodes = new List<string> { "14959-1", "30000-4", "9318-7", "33914-3" }, // uACR, eGFR
                IcdCodes = new List<string> { "E10", "E11" },
                LookbackMonths = 12,
                MinAge = 18,
                MaxAge = 75
            },

            // EED - Eye Exam for Patients with Diabetes
            new HedisMeasureDefinition
            {
                MeasureId = EED,
                MeasureName = "Eye Exam for Patients with Diabetes",
                Description = "Percentage of members 18-75 years with diabetes who had a retinal eye exam",
                Category = "Effectiveness of Care",
                NumeratorDescription = "Retinal or dilated eye exam in past 2 years",
                DenominatorDescription = "Members age 18-75 with diabetes",
                CptCodes = new List<string> { "92002", "92004", "92012", "92014", "67028", "67030" },
                SnomedCodes = new List<string> { "252779005", "420050004" },
                IcdCodes = new List<string> { "E10", "E11" },
                LookbackMonths = 24,
                MinAge = 18,
                MaxAge = 75
            },

            // SPC - Statin Therapy for Patients with Cardiovascular Disease
            new HedisMeasureDefinition
            {
                MeasureId = SPC,
                MeasureName = "Statin Therapy for Patients with Cardiovascular Disease",
                Description = "Percentage of males 21-75 and females 40-75 years with CVD who received statin therapy",
                Category = "Effectiveness of Care",
                NumeratorDescription = "At least 1 statin medication fill in measurement year",
                DenominatorDescription = "Members with CVD (IVD, ischemic stroke, MI, CABG, PCI)",
                IcdCodes = new List<string> { "I20", "I21", "I22", "I23", "I24", "I25", "I63" }, // CVD
                LookbackMonths = 12,
                MinAge = 21,
                MaxAge = 75
            },

            // OMW - Osteoporosis Management in Women Who Had a Fracture
            new HedisMeasureDefinition
            {
                MeasureId = OMW,
                MeasureName = "Osteoporosis Management in Women Who Had a Fracture",
                Description = "Percentage of women 67-85 years who suffered a fracture and received osteoporosis treatment or screening",
                Category = "Effectiveness of Care",
                NumeratorDescription = "Osteoporosis medication OR DXA scan within 6 months",
                DenominatorDescription = "Women age 67-85 with fracture",
                IcdCodes = new List<string> { "S22", "S32", "S42", "S52", "S62", "S72", "S82", "S92" }, // Fractures
                CptCodes = new List<string> { "77080", "77081" }, // DXA scan
                LookbackMonths = 6,
                MinAge = 67,
                MaxAge = 85,
                Gender = "female"
            },

            // PBH - Persistence of Beta-Blocker Treatment After a Heart Attack
            new HedisMeasureDefinition
            {
                MeasureId = PBH,
                MeasureName = "Persistence of Beta-Blocker Treatment After a Heart Attack",
                Description = "Percentage of members 18+ years who received beta-blocker for at least 180 days after MI",
                Category = "Effectiveness of Care",
                NumeratorDescription = "At least 180 days of beta-blocker therapy",
                DenominatorDescription = "Members age 18+ with MI",
                IcdCodes = new List<string> { "I21", "I22" }, // Myocardial infarction
                LookbackMonths = 12,
                MinAge = 18
            },

            // IMA - Immunizations for Adolescents
            new HedisMeasureDefinition
            {
                MeasureId = IMA,
                MeasureName = "Immunizations for Adolescents",
                Description = "Percentage of adolescents 13 years who had HPV, Tdap, and meningococcal vaccines by their 13th birthday",
                Category = "Prevention & Screening",
                NumeratorDescription = "HPV + Tdap + Meningococcal vaccines",
                DenominatorDescription = "Adolescents turning 13 in measurement year",
                CptCodes = new List<string> { "90460", "90461", "90471", "90472", "90473", "90474" },
                LookbackMonths = 24,
                MinAge = 13,
                MaxAge = 13
            },

            // CIS - Childhood Immunization Status
            new HedisMeasureDefinition
            {
                MeasureId = CIS,
                MeasureName = "Childhood Immunization Status",
                Description = "Percentage of children 2 years who had all required immunizations by their 2nd birthday",
                Category = "Prevention & Screening",
                NumeratorDescription = "DTaP, IPV, MMR, HiB, Hep B, VZV, Pneumococcal vaccines",
                DenominatorDescription = "Children turning 2 in measurement year",
                CptCodes = new List<string> { "90700", "90702", "90707", "90710", "90723" },
                LookbackMonths = 24,
                MinAge = 2,
                MaxAge = 2
            },

            // W30 - Well-Child Visits in the First 30 Months of Life
            new HedisMeasureDefinition
            {
                MeasureId = W30,
                MeasureName = "Well-Child Visits in the First 30 Months of Life",
                Description = "Percentage of children who had the recommended number of well-child visits",
                Category = "Prevention & Screening",
                NumeratorDescription = "6+ visits in first 15 months, 2+ visits age 15-30 months",
                DenominatorDescription = "Children 30 months old",
                CptCodes = new List<string> { "99381", "99382", "99391", "99392" },
                LookbackMonths = 30,
                MinAge = 0,
                MaxAge = 2
            },

            // AWC - Adolescent Well-Care Visits
            new HedisMeasureDefinition
            {
                MeasureId = AWC,
                MeasureName = "Adolescent Well-Care Visits",
                Description = "Percentage of adolescents 12-21 years who had at least one comprehensive well-care visit",
                Category = "Prevention & Screening",
                NumeratorDescription = "At least 1 well-care visit in measurement year",
                DenominatorDescription = "Adolescents age 12-21",
                CptCodes = new List<string> { "99384", "99385", "99386", "99394", "99395", "99396" },
                LookbackMonths = 12,
                MinAge = 12,
                MaxAge = 21
            },

            // ABA - Adult BMI Assessment
            new HedisMeasureDefinition
            {
                MeasureId = ABA,
                MeasureName = "Adult BMI Assessment",
                Description = "Percentage of members 18-74 years who had BMI documented in the past year",
                Category = "Prevention & Screening",
                NumeratorDescription = "BMI documented in measurement year",
                DenominatorDescription = "Members age 18-74",
                LoincCodes = new List<string> { "39156-5" }, // BMI
                LookbackMonths = 12,
                MinAge = 18,
                MaxAge = 74
            }
        };
    }

    /// <summary>
    /// Get measure definition by ID
    /// </summary>
    public static HedisMeasureDefinition? GetDefinition(string measureId)
    {
        return GetAllDefinitions().FirstOrDefault(m => m.MeasureId == measureId);
    }

    /// <summary>
    /// Get emoji icon for measure category
    /// </summary>
    public static string GetCategoryIcon(string category)
    {
        return category switch
        {
            "Effectiveness of Care" => "🏥",
            "Prevention & Screening" => "🩺",
            "Access to Care" => "📅",
            "Utilization" => "📊",
            _ => "📋"
        };
    }

    /// <summary>
    /// Get color for measure rate (for UI)
    /// </summary>
    public static string GetRateColor(double rate)
    {
        return rate switch
        {
            >= 75.0 => "#107c10", // Green
            >= 50.0 => "#ff8c00", // Orange
            _ => "#d13438" // Red
        };
    }

    /// <summary>
    /// Calculate Star Rating impact of measure rate change
    /// Simplified model - actual Star Ratings use complex CMS methodology
    /// </summary>
    public static double CalculateStarRatingImpact(string measureId, double currentRate, double newRate)
    {
        var improvement = newRate - currentRate;
        
        // High-impact measures (weight 3x)
        var highImpact = new[] { CBP, HBD, CDC, BCS };
        
        // Medium-impact measures (weight 2x)
        var mediumImpact = new[] { COL, KED, EED, SPC };
        
        var weight = highImpact.Contains(measureId) ? 3.0 : 
                     mediumImpact.Contains(measureId) ? 2.0 : 1.0;
        
        // Each 10% improvement = 0.05 stars (weighted)
        return (improvement / 10.0) * 0.05 * weight;
    }
}
