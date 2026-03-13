namespace ClaimsPortal.BlazorWasm.Models;

/// <summary>
/// HEDIS (Healthcare Effectiveness Data and Information Set) measure result
/// Used for quality performance reporting and NCQA accreditation
/// </summary>
public class HedisMeasure
{
    public string MeasureId { get; set; } = string.Empty;
    public string MeasureName { get; set; } = string.Empty;
    public string MeasureDescription { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // Prevention, Chronic Care, Behavioral Health, etc.
    public string Domain { get; set; } = string.Empty; // Clinical Quality, Utilization, Access
    
    // Performance metrics
    public int EligiblePopulation { get; set; }
    public int Numerator { get; set; } // Members who met the measure
    public int Denominator { get; set; } // Members eligible for the measure
    public decimal ComplianceRate { get; set; } // Numerator / Denominator
    
    // Benchmarks
    public decimal? NationalAverage { get; set; }
    public decimal? NcqaTarget50thPercentile { get; set; }
    public decimal? NcqaTarget90thPercentile { get; set; }
    public string PerformanceLevel { get; set; } = string.Empty; // Below Average, Average, Above Average, Excellent
    
    // Gaps in care
    public int GapsInCare { get; set; } // Members who should receive service but haven't
    public List<GapInCare> GapDetails { get; set; } = new();
    
    // Trending
    public decimal? PriorYearRate { get; set; }
    public decimal? YearOverYearChange { get; set; }
    
    public DateTime MeasurementPeriodStart { get; set; }
    public DateTime MeasurementPeriodEnd { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.Now;
}

/// <summary>
/// Individual gap in care for a member
/// </summary>
public class GapInCare
{
    public string MemberId { get; set; } = string.Empty;
    public string MemberName { get; set; } = string.Empty;
    public string MeasureId { get; set; } = string.Empty;
    public string MeasureName { get; set; } = string.Empty;
    public string GapDescription { get; set; } = string.Empty;
    public string RecommendedAction { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public int DaysOverdue { get; set; }
    public string Priority { get; set; } = "Medium"; // Low, Medium, High, Critical
    public string Status { get; set; } = "Open"; // Open, In Progress, Closed, Excluded
    public string ProviderId { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public DateTime? LastContactDate { get; set; }
    public List<string> OutreachAttempts { get; set; } = new();
}

/// <summary>
/// HEDIS measure category summary
/// </summary>
public class HedisCategorySummary
{
    public string Category { get; set; } = string.Empty;
    public int MeasureCount { get; set; }
    public decimal AverageComplianceRate { get; set; }
    public int TotalGaps { get; set; }
    public string TrendIndicator { get; set; } = "Stable"; // Improving, Stable, Declining
    public List<HedisMeasure> Measures { get; set; } = new();
}

/// <summary>
/// Member HEDIS profile showing all applicable measures
/// </summary>
public class MemberHedisProfile
{
    public string MemberId { get; set; } = string.Empty;
    public string MemberName { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Gender { get; set; } = string.Empty;
    public List<string> ChronicConditions { get; set; } = new();
    
    public int TotalApplicableMeasures { get; set; }
    public int MeasuresMet { get; set; }
    public int GapsInCare { get; set; }
    public decimal ComplianceScore { get; set; }
    
    public List<HedisMeasure> ApplicableMeasures { get; set; } = new();
    public List<GapInCare> OpenGaps { get; set; } = new();
    
    public string RiskCategory { get; set; } = string.Empty; // Low, Moderate, High
    public DateTime LastAssessment { get; set; } = DateTime.Now;
}

/// <summary>
/// HEDIS dashboard summary
/// </summary>
public class HedisDashboard
{
    public DateTime MeasurementPeriod { get; set; } = DateTime.Now;
    public int TotalMembers { get; set; }
    public int TotalMeasures { get; set; }
    public decimal OverallComplianceRate { get; set; }
    public int TotalGapsInCare { get; set; }
    
    public List<HedisCategorySummary> Categories { get; set; } = new();
    public List<HedisMeasure> TopPerformingMeasures { get; set; } = new();
    public List<HedisMeasure> ImprovementOpportunities { get; set; } = new();
    
    public decimal StarRating { get; set; } // CMS Star Rating 1-5
    public string StarRatingTrend { get; set; } = "Stable";
    
    public DateTime LastRefresh { get; set; } = DateTime.Now;
}
