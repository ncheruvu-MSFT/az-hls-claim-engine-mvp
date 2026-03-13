using System;
using System.Collections.Generic;

namespace ClaimsRules.Api.Models;

/// <summary>
/// Baseline metrics computed from FHIR data for a specific segment and time period
/// </summary>
public class BaselineMetrics
{
    public string BaselineId { get; set; } = string.Empty;
    public string Segment { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    
    // Financial
    public decimal TotalAllowed { get; set; }
    public decimal ProviderAllowed { get; set; }  // IP + OP + Professional
    public decimal PharmacyAllowed { get; set; }
    public decimal OtherAllowed { get; set; }
    
    // Member-months
    public decimal MemberMonths { get; set; }
    public int UniqueMemberCount { get; set; }
    
    // PMPM
    public decimal TotalPMPM { get; set; }
    public decimal ProviderPMPM { get; set; }
    public decimal PharmacyPMPM { get; set; }
    public decimal OtherPMPM { get; set; }
    
    // Service bucket breakdown
    public ServiceBucketMetrics ServiceBuckets { get; set; } = new();
    
    // Risk and utilization indices
    public decimal PopulationRiskIndex { get; set; }  // 1.0 = average
    public decimal UtilizationIndex { get; set; }     // 1.0 = average
    
    // Utilization rates (per 1000 members)
    public UtilizationRates Utilization { get; set; } = new();
    
    // Clinical summary
    public List<ConditionPrevalence> TopConditions { get; set; } = new();
    public RiskDistribution RiskDistribution { get; set; } = new();
    
    // Metadata
    public DateTime CachedAt { get; set; }
    public int? TtlSeconds { get; set; }
}

public class ServiceBucketMetrics
{
    public decimal InpatientAllowed { get; set; }
    public decimal InpatientPMPM { get; set; }
    
    public decimal OutpatientAllowed { get; set; }
    public decimal OutpatientPMPM { get; set; }
    
    public decimal ProfessionalAllowed { get; set; }
    public decimal ProfessionalPMPM { get; set; }
    
    public decimal PharmacyAllowed { get; set; }
    public decimal PharmacyPMPM { get; set; }
    
    public decimal OtherAllowed { get; set; }
    public decimal OtherPMPM { get; set; }
}

public class UtilizationRates
{
    public decimal EdVisitsPer1000 { get; set; }
    public decimal IpAdmitsPer1000 { get; set; }
    public decimal OpVisitsPer1000 { get; set; }
    public decimal OfficeVisitsPer1000 { get; set; }
    
    public int TotalEdVisits { get; set; }
    public int TotalIpAdmits { get; set; }
    public int TotalOpVisits { get; set; }
    public int TotalOfficeVisits { get; set; }
}

public class ConditionPrevalence
{
    public string ConditionGroup { get; set; } = string.Empty;
    public string ConditionName { get; set; } = string.Empty;
    public int MemberCount { get; set; }
    public decimal PrevalenceRate { get; set; }  // % of population
    public decimal AvgRiskScore { get; set; }
}

public class RiskDistribution
{
    public decimal MinScore { get; set; }
    public decimal MaxScore { get; set; }
    public decimal AvgScore { get; set; }
    public decimal MedianScore { get; set; }
    public List<RiskBucket> Buckets { get; set; } = new();
}

public class RiskBucket
{
    public string Label { get; set; } = string.Empty;  // e.g., "0-1", "1-2", "2-3"
    public decimal MinScore { get; set; }
    public decimal MaxScore { get; set; }
    public int MemberCount { get; set; }
    public decimal Percentage { get; set; }
}

/// <summary>
/// Scenario parameters and adjustable knobs
/// </summary>
public class Scenario
{
    public string ScenarioId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public string BaselineId { get; set; } = string.Empty;
    
    public ScenarioKnobs Knobs { get; set; } = new();
    public ScenarioResult? Result { get; set; }
    
    public string Status { get; set; } = "created";  // created, running, completed, failed
    public DateTime CreatedAt { get; set; }
    public DateTime? ComputedAt { get; set; }
    
    // For Cosmos DB partitioning
    public string PartitionKey => Owner;
}

/// <summary>
/// Adjustable knobs for scenario modeling
/// </summary>
public class ScenarioKnobs
{
    // Core financial trends
    public decimal ProviderTrendPct { get; set; }  // -20% to +20%
    public decimal PharmacyTrendPct { get; set; }  // -30% to +30%
    public decimal OtherTrendPct { get; set; }     // -10% to +10%
    
    // Membership
    public decimal MembershipDeltaPct { get; set; }  // -30% to +30%
    
    // Utilization
    public decimal UtilizationDeltaPct { get; set; }  // -15% to +15%
    
    // Population risk
    public decimal PopulationRiskDeltaPct { get; set; }  // -10% to +10%
    
    // Clinical knobs: condition prevalence adjustments
    public Dictionary<string, decimal> ConditionPrevalenceShiftPct { get; set; } = new();
    // e.g., { "HCC_018_Diabetes": 5.0, "HCC_085_CHF": 2.0 }
    
    // Encounter mix adjustments
    public Dictionary<string, decimal> EncounterMixShiftPct { get; set; } = new();
    // e.g., { "ED": -10.0, "OP": +5.0, "Office": +5.0 }
    
    // Optional: observation severity adjustments
    public Dictionary<string, decimal> ObservationSeverityShiftPct { get; set; } = new();
    // e.g., { "A1c": +3.0, "BP": +2.0 }
}

/// <summary>
/// Forecast results from scenario calculation
/// </summary>
public class ScenarioResult
{
    // Forecast values
    public decimal ForecastAllowed { get; set; }
    public decimal ForecastMemberMonths { get; set; }
    public decimal ForecastPMPM { get; set; }
    
    // Service bucket forecasts
    public ServiceBucketMetrics ForecastServiceBuckets { get; set; } = new();
    
    // Deltas from baseline
    public decimal DeltaAllowed { get; set; }
    public decimal DeltaPMPM { get; set; }
    public decimal DeltaPct { get; set; }
    
    // Waterfall drivers
    public DeltaDrivers DeltaDrivers { get; set; } = new();
    
    // Forecast utilization
    public UtilizationRates ForecastUtilization { get; set; } = new();
    
    // Forecast risk
    public decimal ForecastPopulationRiskIndex { get; set; }
}

/// <summary>
/// Delta drivers for waterfall chart
/// </summary>
public class DeltaDrivers
{
    public decimal ProviderTrendDelta { get; set; }     // PMPM impact
    public decimal PharmacyTrendDelta { get; set; }     // PMPM impact
    public decimal OtherTrendDelta { get; set; }        // PMPM impact
    public decimal RiskDelta { get; set; }              // PMPM impact
    public decimal UtilizationDelta { get; set; }       // PMPM impact
    public decimal MembershipDelta { get; set; }        // PMPM impact (denominator effect)
    
    // Clinical driver details
    public Dictionary<string, decimal> ConditionImpacts { get; set; } = new();
    public Dictionary<string, decimal> EncounterMixImpacts { get; set; } = new();
}

/// <summary>
/// Risk scoring weights configuration
/// </summary>
public class RiskWeights
{
    public string WeightsId { get; set; } = "risk-weights-v1";
    public int Version { get; set; }
    public DateTime LastModified { get; set; }
    public string ModifiedBy { get; set; } = string.Empty;
    
    // Demographic risk factors
    public Dictionary<string, decimal> AgeGenderWeights { get; set; } = new();
    
    // Condition-based risk (HCC-like)
    public Dictionary<string, decimal> ConditionGroupWeights { get; set; } = new();
    public Dictionary<string, string> Icd10Mapping { get; set; } = new();  // ICD-10 -> HCC group
    
    // Encounter-based risk
    public Dictionary<string, decimal> EncounterWeights { get; set; } = new();
    
    // Observation severity weights (optional)
    public Dictionary<string, decimal> ObservationSeverityWeights { get; set; } = new();
    
    // Disease interaction multipliers (optional)
    public Dictionary<string, decimal> DiseaseInteractionMultipliers { get; set; } = new();
    
    // For Cosmos DB
    public string PartitionKey => "config";
}

/// <summary>
/// Individual member risk score
/// </summary>
public class MemberRiskScore
{
    public string PatientId { get; set; } = string.Empty;
    
    // Component scores
    public decimal DemographicScore { get; set; }
    public decimal ConditionScore { get; set; }
    public decimal EncounterScore { get; set; }
    public decimal SeverityAdjustment { get; set; }
    
    // Total
    public decimal TotalRiskScore { get; set; }
    
    // Details
    public string AgeGenderBand { get; set; } = string.Empty;
    public List<string> ActiveConditionGroups { get; set; } = new();
    public Dictionary<string, int> EncounterCounts { get; set; } = new();
}

/// <summary>
/// Population-level risk index
/// </summary>
public class PopulationRiskIndex
{
    public decimal Index { get; set; }  // 1.0 = average population
    public decimal AvgMemberRiskScore { get; set; }
    public decimal PopulationAvgCalibration { get; set; }  // Calibrated baseline (e.g., 2.0)
    
    public int TotalMembers { get; set; }
    public List<MemberRiskScore> TopRiskMembers { get; set; } = new();  // Top 10
}

/// <summary>
/// Service bucket enum for categorization
/// </summary>
public enum ServiceBucketType
{
    Inpatient,
    Outpatient,
    Professional,
    Pharmacy,
    Other
}

/// <summary>
/// Encounter class types
/// </summary>
public enum EncounterClass
{
    Emergency,    // ED
    Inpatient,    // IP
    Outpatient,   // OP
    Ambulatory,   // Office visit
    Other
}
