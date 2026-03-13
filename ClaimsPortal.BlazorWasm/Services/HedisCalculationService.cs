using ClaimsPortal.BlazorWasm.Models;

namespace ClaimsPortal.BlazorWasm.Services;

/// <summary>
/// HEDIS quality measure calculation service
/// Calculates HEDIS measures from claims and FHIR data
/// </summary>
public class HedisCalculationService
{
    private readonly DateTime _measurementPeriodStart = new(2025, 1, 1);
    private readonly DateTime _measurementPeriodEnd = new(2025, 12, 31);

    /// <summary>
    /// Get comprehensive HEDIS dashboard
    /// </summary>
    public async Task<HedisDashboard> GetHedisDashboard()
    {
        await Task.Delay(100); // Simulate API call

        var measures = GetAllHedisMeasures();
        var categories = GetCategorySummaries(measures);

        return new HedisDashboard
        {
            MeasurementPeriod = _measurementPeriodEnd,
            TotalMembers = 12450,
            TotalMeasures = measures.Count,
            OverallComplianceRate = measures.Average(m => m.ComplianceRate),
            TotalGapsInCare = measures.Sum(m => m.GapsInCare),
            Categories = categories,
            TopPerformingMeasures = measures.OrderByDescending(m => m.ComplianceRate).Take(5).ToList(),
            ImprovementOpportunities = measures.OrderBy(m => m.ComplianceRate).Take(5).ToList(),
            StarRating = 4.2m,
            StarRatingTrend = "Improving",
            LastRefresh = DateTime.Now
        };
    }

    /// <summary>
    /// Get all HEDIS measures with current performance
    /// </summary>
    public List<HedisMeasure> GetAllHedisMeasures()
    {
        return new List<HedisMeasure>
        {
            // PREVENTION MEASURES
            new()
            {
                MeasureId = "BCS",
                MeasureName = "Breast Cancer Screening",
                MeasureDescription = "Women ages 50-74 who had a mammogram in the past 2 years",
                Category = "Prevention - Cancer Screening",
                Domain = "Clinical Quality",
                EligiblePopulation = 2845,
                Numerator = 2187,
                Denominator = 2845,
                ComplianceRate = 76.87m,
                NationalAverage = 74.2m,
                NcqaTarget50thPercentile = 75.0m,
                NcqaTarget90thPercentile = 82.5m,
                PerformanceLevel = "Average",
                GapsInCare = 658,
                PriorYearRate = 73.5m,
                YearOverYearChange = 3.37m,
                MeasurementPeriodStart = _measurementPeriodStart,
                MeasurementPeriodEnd = _measurementPeriodEnd,
                GapDetails = GenerateGapsForMeasure("BCS", 658)
            },
            new()
            {
                MeasureId = "COL",
                MeasureName = "Colorectal Cancer Screening",
                MeasureDescription = "Adults ages 45-75 who had appropriate colorectal cancer screening",
                Category = "Prevention - Cancer Screening",
                Domain = "Clinical Quality",
                EligiblePopulation = 4123,
                Numerator = 2886,
                Denominator = 4123,
                ComplianceRate = 70.00m,
                NationalAverage = 68.5m,
                NcqaTarget50thPercentile = 70.0m,
                NcqaTarget90thPercentile = 78.0m,
                PerformanceLevel = "Average",
                GapsInCare = 1237,
                PriorYearRate = 67.2m,
                YearOverYearChange = 2.80m,
                MeasurementPeriodStart = _measurementPeriodStart,
                MeasurementPeriodEnd = _measurementPeriodEnd,
                GapDetails = GenerateGapsForMeasure("COL", 1237)
            },
            new()
            {
                MeasureId = "CCS",
                MeasureName = "Cervical Cancer Screening",
                MeasureDescription = "Women ages 21-64 who had cervical cancer screening",
                Category = "Prevention - Cancer Screening",
                Domain = "Clinical Quality",
                EligiblePopulation = 3567,
                Numerator = 2996,
                Denominator = 3567,
                ComplianceRate = 84.00m,
                NationalAverage = 79.8m,
                NcqaTarget50thPercentile = 81.0m,
                NcqaTarget90thPercentile = 88.5m,
                PerformanceLevel = "Above Average",
                GapsInCare = 571,
                PriorYearRate = 81.5m,
                YearOverYearChange = 2.50m,
                MeasurementPeriodStart = _measurementPeriodStart,
                MeasurementPeriodEnd = _measurementPeriodEnd,
                GapDetails = GenerateGapsForMeasure("CCS", 571)
            },
            
            // DIABETES CARE
            new()
            {
                MeasureId = "CDC-HbA1c",
                MeasureName = "Diabetes Care - HbA1c Testing",
                MeasureDescription = "Adults with diabetes who had HbA1c testing",
                Category = "Chronic Care - Diabetes",
                Domain = "Clinical Quality",
                EligiblePopulation = 1456,
                Numerator = 1283,
                Denominator = 1456,
                ComplianceRate = 88.12m,
                NationalAverage = 85.3m,
                NcqaTarget50thPercentile = 87.0m,
                NcqaTarget90thPercentile = 92.0m,
                PerformanceLevel = "Above Average",
                GapsInCare = 173,
                PriorYearRate = 86.5m,
                YearOverYearChange = 1.62m,
                MeasurementPeriodStart = _measurementPeriodStart,
                MeasurementPeriodEnd = _measurementPeriodEnd,
                GapDetails = GenerateGapsForMeasure("CDC-HbA1c", 173)
            },
            new()
            {
                MeasureId = "CDC-Eye",
                MeasureName = "Diabetes Care - Eye Exam",
                MeasureDescription = "Adults with diabetes who had a retinal eye exam",
                Category = "Chronic Care - Diabetes",
                Domain = "Clinical Quality",
                EligiblePopulation = 1456,
                Numerator = 1048,
                Denominator = 1456,
                ComplianceRate = 71.98m,
                NationalAverage = 68.5m,
                NcqaTarget50thPercentile = 70.0m,
                NcqaTarget90thPercentile = 80.0m,
                PerformanceLevel = "Average",
                GapsInCare = 408,
                PriorYearRate = 69.3m,
                YearOverYearChange = 2.68m,
                MeasurementPeriodStart = _measurementPeriodStart,
                MeasurementPeriodEnd = _measurementPeriodEnd,
                GapDetails = GenerateGapsForMeasure("CDC-Eye", 408)
            },
            new()
            {
                MeasureId = "CDC-BP",
                MeasureName = "Diabetes Care - Blood Pressure Control",
                MeasureDescription = "Adults with diabetes whose BP was <140/90 mmHg",
                Category = "Chronic Care - Diabetes",
                Domain = "Clinical Quality",
                EligiblePopulation = 1456,
                Numerator = 1118,
                Denominator = 1456,
                ComplianceRate = 76.79m,
                NationalAverage = 72.5m,
                NcqaTarget50thPercentile = 75.0m,
                NcqaTarget90thPercentile = 82.0m,
                PerformanceLevel = "Above Average",
                GapsInCare = 338,
                PriorYearRate = 74.2m,
                YearOverYearChange = 2.59m,
                MeasurementPeriodStart = _measurementPeriodStart,
                MeasurementPeriodEnd = _measurementPeriodEnd,
                GapDetails = GenerateGapsForMeasure("CDC-BP", 338)
            },
            
            // CARDIOVASCULAR CARE
            new()
            {
                MeasureId = "CBP",
                MeasureName = "Controlling High Blood Pressure",
                MeasureDescription = "Adults 18-85 with hypertension whose BP was <140/90 mmHg",
                Category = "Chronic Care - Cardiovascular",
                Domain = "Clinical Quality",
                EligiblePopulation = 2789,
                Numerator = 2039,
                Denominator = 2789,
                ComplianceRate = 73.11m,
                NationalAverage = 70.8m,
                NcqaTarget50thPercentile = 72.0m,
                NcqaTarget90thPercentile = 80.0m,
                PerformanceLevel = "Above Average",
                GapsInCare = 750,
                PriorYearRate = 71.5m,
                YearOverYearChange = 1.61m,
                MeasurementPeriodStart = _measurementPeriodStart,
                MeasurementPeriodEnd = _measurementPeriodEnd,
                GapDetails = GenerateGapsForMeasure("CBP", 750)
            },
            new()
            {
                MeasureId = "SPC",
                MeasureName = "Statin Therapy for Cardiovascular Disease",
                MeasureDescription = "Adults 21-75 with clinical CVD who received statin therapy",
                Category = "Chronic Care - Cardiovascular",
                Domain = "Clinical Quality",
                EligiblePopulation = 987,
                Numerator = 821,
                Denominator = 987,
                ComplianceRate = 83.18m,
                NationalAverage = 78.5m,
                NcqaTarget50thPercentile = 80.0m,
                NcqaTarget90thPercentile = 88.0m,
                PerformanceLevel = "Above Average",
                GapsInCare = 166,
                PriorYearRate = 80.2m,
                YearOverYearChange = 2.98m,
                MeasurementPeriodStart = _measurementPeriodStart,
                MeasurementPeriodEnd = _measurementPeriodEnd,
                GapDetails = GenerateGapsForMeasure("SPC", 166)
            },
            
            // BEHAVIORAL HEALTH
            new()
            {
                MeasureId = "FUH-7",
                MeasureName = "Follow-Up After Hospitalization for Mental Illness (7 days)",
                MeasureDescription = "Members 6+ who had follow-up within 7 days after mental health hospitalization",
                Category = "Behavioral Health",
                Domain = "Clinical Quality",
                EligiblePopulation = 245,
                Numerator = 143,
                Denominator = 245,
                ComplianceRate = 58.37m,
                NationalAverage = 52.5m,
                NcqaTarget50thPercentile = 55.0m,
                NcqaTarget90thPercentile = 68.0m,
                PerformanceLevel = "Average",
                GapsInCare = 102,
                PriorYearRate = 55.2m,
                YearOverYearChange = 3.17m,
                MeasurementPeriodStart = _measurementPeriodStart,
                MeasurementPeriodEnd = _measurementPeriodEnd,
                GapDetails = GenerateGapsForMeasure("FUH-7", 102)
            },
            new()
            {
                MeasureId = "AMM",
                MeasureName = "Antidepressant Medication Management",
                MeasureDescription = "Adults with depression who remained on antidepressant for 12 weeks",
                Category = "Behavioral Health",
                Domain = "Clinical Quality",
                EligiblePopulation = 678,
                Numerator = 488,
                Denominator = 678,
                ComplianceRate = 71.98m,
                NationalAverage = 68.2m,
                NcqaTarget50thPercentile = 70.0m,
                NcqaTarget90thPercentile = 78.0m,
                PerformanceLevel = "Above Average",
                GapsInCare = 190,
                PriorYearRate = 69.5m,
                YearOverYearChange = 2.48m,
                MeasurementPeriodStart = _measurementPeriodStart,
                MeasurementPeriodEnd = _measurementPeriodEnd,
                GapDetails = GenerateGapsForMeasure("AMM", 190)
            },
            
            // UTILIZATION
            new()
            {
                MeasureId = "AAP",
                MeasureName = "Adults' Access to Preventive/Ambulatory Care",
                MeasureDescription = "Adults 20+ who had ambulatory or preventive care visit",
                Category = "Access & Utilization",
                Domain = "Access",
                EligiblePopulation = 12450,
                Numerator = 10537,
                Denominator = 12450,
                ComplianceRate = 84.63m,
                NationalAverage = 81.5m,
                NcqaTarget50thPercentile = 83.0m,
                NcqaTarget90thPercentile = 89.0m,
                PerformanceLevel = "Above Average",
                GapsInCare = 1913,
                PriorYearRate = 82.8m,
                YearOverYearChange = 1.83m,
                MeasurementPeriodStart = _measurementPeriodStart,
                MeasurementPeriodEnd = _measurementPeriodEnd,
                GapDetails = GenerateGapsForMeasure("AAP", 1913)
            },
            new()
            {
                MeasureId = "PPC-Prenatal",
                MeasureName = "Prenatal and Postpartum Care - Prenatal Visit",
                MeasureDescription = "Women who had prenatal visit in first trimester",
                Category = "Maternal Care",
                Domain = "Clinical Quality",
                EligiblePopulation = 456,
                Numerator = 398,
                Denominator = 456,
                ComplianceRate = 87.28m,
                NationalAverage = 83.5m,
                NcqaTarget50thPercentile = 85.0m,
                NcqaTarget90thPercentile = 92.0m,
                PerformanceLevel = "Above Average",
                GapsInCare = 58,
                PriorYearRate = 85.3m,
                YearOverYearChange = 1.98m,
                MeasurementPeriodStart = _measurementPeriodStart,
                MeasurementPeriodEnd = _measurementPeriodEnd,
                GapDetails = GenerateGapsForMeasure("PPC-Prenatal", 58)
            },
            
            // MEDICATION ADHERENCE
            new()
            {
                MeasureId = "SAA",
                MeasureName = "Adherence to Asthma Medication",
                MeasureDescription = "Members 5-64 with persistent asthma who had ≥50% controller medication days",
                Category = "Medication Adherence",
                Domain = "Clinical Quality",
                EligiblePopulation = 567,
                Numerator = 413,
                Denominator = 567,
                ComplianceRate = 72.84m,
                NationalAverage = 68.5m,
                NcqaTarget50thPercentile = 70.0m,
                NcqaTarget90thPercentile = 78.0m,
                PerformanceLevel = "Above Average",
                GapsInCare = 154,
                PriorYearRate = 70.2m,
                YearOverYearChange = 2.64m,
                MeasurementPeriodStart = _measurementPeriodStart,
                MeasurementPeriodEnd = _measurementPeriodEnd,
                GapDetails = GenerateGapsForMeasure("SAA", 154)
            }
        };
    }

    /// <summary>
    /// Get category summaries
    /// </summary>
    private List<HedisCategorySummary> GetCategorySummaries(List<HedisMeasure> measures)
    {
        return measures
            .GroupBy(m => m.Category)
            .Select(g => new HedisCategorySummary
            {
                Category = g.Key,
                MeasureCount = g.Count(),
                AverageComplianceRate = g.Average(m => m.ComplianceRate),
                TotalGaps = g.Sum(m => m.GapsInCare),
                TrendIndicator = g.Average(m => m.YearOverYearChange ?? 0) > 1 ? "Improving" :
                                 g.Average(m => m.YearOverYearChange ?? 0) < -1 ? "Declining" : "Stable",
                Measures = g.ToList()
            })
            .OrderByDescending(c => c.AverageComplianceRate)
            .ToList();
    }

    /// <summary>
    /// Generate sample gaps in care for a measure
    /// </summary>
    private List<GapInCare> GenerateGapsForMeasure(string measureId, int count)
    {
        var gaps = new List<GapInCare>();
        var random = new Random(measureId.GetHashCode());
        
        // Generate first 10 gaps for display
        for (int i = 0; i < Math.Min(count, 10); i++)
        {
            var daysOverdue = random.Next(0, 365);
            gaps.Add(new GapInCare
            {
                MemberId = $"MEM{random.Next(10000, 99999)}",
                MemberName = GetRandomMemberName(random),
                MeasureId = measureId,
                MeasureName = GetMeasureShortName(measureId),
                GapDescription = GetGapDescription(measureId),
                RecommendedAction = GetRecommendedAction(measureId),
                DueDate = DateTime.Now.AddDays(-daysOverdue),
                DaysOverdue = daysOverdue,
                Priority = daysOverdue > 180 ? "Critical" : daysOverdue > 90 ? "High" : daysOverdue > 30 ? "Medium" : "Low",
                Status = "Open",
                ProviderId = $"PROV{random.Next(100, 999)}",
                ProviderName = GetRandomProviderName(random),
                LastContactDate = random.Next(0, 2) == 0 ? DateTime.Now.AddDays(-random.Next(7, 60)) : null,
                OutreachAttempts = GetOutreachAttempts(random)
            });
        }
        
        return gaps;
    }

    private string GetRandomMemberName(Random random)
    {
        var firstNames = new[] { "James", "Mary", "John", "Patricia", "Robert", "Jennifer", "Michael", "Linda", "William", "Elizabeth" };
        var lastNames = new[] { "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis", "Rodriguez", "Martinez" };
        return $"{firstNames[random.Next(firstNames.Length)]} {lastNames[random.Next(lastNames.Length)]}";
    }

    private string GetRandomProviderName(Random random)
    {
        var providers = new[] 
        { 
            "City Medical Center", "Regional Health Partners", "Primary Care Associates", 
            "Women's Health Clinic", "Family Medicine Group", "Cardiology Specialists",
            "Behavioral Health Center", "Diabetes Care Clinic", "Preventive Care Associates"
        };
        return providers[random.Next(providers.Length)];
    }

    private List<string> GetOutreachAttempts(Random random)
    {
        var attempts = new List<string>();
        var count = random.Next(0, 4);
        var attemptTypes = new[] { "Phone call", "Text message", "Email", "Mail letter", "Patient portal message" };
        
        for (int i = 0; i < count; i++)
        {
            attempts.Add($"{attemptTypes[random.Next(attemptTypes.Length)]} - {DateTime.Now.AddDays(-random.Next(1, 30)):MM/dd/yyyy}");
        }
        
        return attempts;
    }

    private string GetMeasureShortName(string measureId) => measureId switch
    {
        "BCS" => "Breast Cancer Screening",
        "COL" => "Colorectal Cancer Screening",
        "CCS" => "Cervical Cancer Screening",
        "CDC-HbA1c" => "Diabetes HbA1c Test",
        "CDC-Eye" => "Diabetic Eye Exam",
        "CDC-BP" => "Diabetes BP Control",
        "CBP" => "Blood Pressure Control",
        "SPC" => "Statin Therapy",
        "FUH-7" => "Mental Health Follow-Up",
        "AMM" => "Antidepressant Management",
        "AAP" => "Preventive Care Visit",
        "PPC-Prenatal" => "Prenatal Care",
        "SAA" => "Asthma Medication",
        _ => measureId
    };

    private string GetGapDescription(string measureId) => measureId switch
    {
        "BCS" => "Due for mammogram screening",
        "COL" => "Due for colonoscopy or FIT test",
        "CCS" => "Due for Pap smear or HPV test",
        "CDC-HbA1c" => "Missing HbA1c test this year",
        "CDC-Eye" => "Missing diabetic retinal eye exam",
        "CDC-BP" => "Blood pressure not controlled",
        "CBP" => "Hypertension not controlled",
        "SPC" => "Not on statin therapy",
        "FUH-7" => "Missing post-hospitalization follow-up",
        "AMM" => "Antidepressant medication discontinued early",
        "AAP" => "No preventive care visit this year",
        "PPC-Prenatal" => "Missing prenatal visit",
        "SAA" => "Asthma medication non-adherent",
        _ => "Care gap identified"
    };

    private string GetRecommendedAction(string measureId) => measureId switch
    {
        "BCS" => "Schedule mammogram appointment",
        "COL" => "Schedule colonoscopy or order FIT test",
        "CCS" => "Schedule Pap smear appointment",
        "CDC-HbA1c" => "Order HbA1c lab test",
        "CDC-Eye" => "Schedule ophthalmology appointment",
        "CDC-BP" => "Schedule BP check and medication review",
        "CBP" => "Schedule follow-up for hypertension management",
        "SPC" => "Evaluate for statin therapy initiation",
        "FUH-7" => "Schedule outpatient mental health appointment",
        "AMM" => "Review medication adherence and barriers",
        "AAP" => "Schedule annual wellness visit",
        "PPC-Prenatal" => "Schedule OB appointment immediately",
        "SAA" => "Refill controller medication and educate on use",
        _ => "Contact member to schedule appointment"
    };

    /// <summary>
    /// Get member HEDIS profile
    /// </summary>
    public async Task<MemberHedisProfile> GetMemberHedisProfile(string memberId)
    {
        await Task.Delay(50);
        
        var allMeasures = GetAllHedisMeasures();
        var applicableMeasures = allMeasures.Take(5).ToList(); // Simulate member-specific measures
        
        return new MemberHedisProfile
        {
            MemberId = memberId,
            MemberName = "John Smith",
            Age = 58,
            Gender = "Male",
            ChronicConditions = new List<string> { "Type 2 Diabetes", "Hypertension", "Hyperlipidemia" },
            TotalApplicableMeasures = applicableMeasures.Count,
            MeasuresMet = 3,
            GapsInCare = 2,
            ComplianceScore = 60.0m,
            ApplicableMeasures = applicableMeasures,
            OpenGaps = applicableMeasures.SelectMany(m => m.GapDetails.Take(2)).ToList(),
            RiskCategory = "High",
            LastAssessment = DateTime.Now
        };
    }
}
