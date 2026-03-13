using System;
using System.Collections.Generic;

namespace ClaimsPortal.BlazorWasm.Models;

/// <summary>
/// Frontend models for Actuarial Estimator (matching backend DTOs)
/// </summary>

public class BaselineMetricsModel
{
    public string BaselineId { get; set; } = string.Empty;
    public string Segment { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    
    public decimal TotalAllowed { get; set; }
    public decimal ProviderAllowed { get; set; }
    public decimal PharmacyAllowed { get; set; }
    public decimal OtherAllowed { get; set; }
    
    public decimal MemberMonths { get; set; }
    public int UniqueMemberCount { get; set; }
    
    public decimal TotalPMPM { get; set; }
    public decimal ProviderPMPM { get; set; }
    public decimal PharmacyPMPM { get; set; }
    public decimal OtherPMPM { get; set; }
    
    public ServiceBucketMetricsModel ServiceBuckets { get; set; } = new();
    public decimal PopulationRiskIndex { get; set; }
    public decimal UtilizationIndex { get; set; }
    public UtilizationRatesModel Utilization { get; set; } = new();
    public List<ConditionPrevalenceModel> TopConditions { get; set; } = new();
    public RiskDistributionModel RiskDistribution { get; set; } = new();
}

public class ServiceBucketMetricsModel
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

public class UtilizationRatesModel
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

public class ConditionPrevalenceModel
{
    public string ConditionGroup { get; set; } = string.Empty;
    public string ConditionName { get; set; } = string.Empty;
    public int MemberCount { get; set; }
    public decimal PrevalenceRate { get; set; }
    public decimal AvgRiskScore { get; set; }
}

public class RiskDistributionModel
{
    public decimal MinScore { get; set; }
    public decimal MaxScore { get; set; }
    public decimal AvgScore { get; set; }
    public decimal MedianScore { get; set; }
    public List<RiskBucketModel> Buckets { get; set; } = new();
}

public class RiskBucketModel
{
    public string Label { get; set; } = string.Empty;
    public decimal MinScore { get; set; }
    public decimal MaxScore { get; set; }
    public int MemberCount { get; set; }
    public decimal Percentage { get; set; }
}

public class ScenarioModel
{
    public string ScenarioId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public string BaselineId { get; set; } = string.Empty;
    public ScenarioKnobsModel Knobs { get; set; } = new();
    public ScenarioResultModel? Result { get; set; }
    public string Status { get; set; } = "created";
    public DateTime CreatedAt { get; set; }
    public DateTime? ComputedAt { get; set; }
}

public class ScenarioKnobsModel
{
    public decimal ProviderTrendPct { get; set; }
    public decimal PharmacyTrendPct { get; set; }
    public decimal OtherTrendPct { get; set; }
    public decimal MembershipDeltaPct { get; set; }
    public decimal UtilizationDeltaPct { get; set; }
    public decimal PopulationRiskDeltaPct { get; set; }
    public Dictionary<string, decimal> ConditionPrevalenceShiftPct { get; set; } = new();
    public Dictionary<string, decimal> EncounterMixShiftPct { get; set; } = new();
    public Dictionary<string, decimal> ObservationSeverityShiftPct { get; set; } = new();
}

public class ScenarioResultModel
{
    public decimal ForecastAllowed { get; set; }
    public decimal ForecastMemberMonths { get; set; }
    public decimal ForecastPMPM { get; set; }
    public ServiceBucketMetricsModel ForecastServiceBuckets { get; set; } = new();
    public decimal DeltaAllowed { get; set; }
    public decimal DeltaPMPM { get; set; }
    public decimal DeltaPct { get; set; }
    public DeltaDriversModel DeltaDrivers { get; set; } = new();
    public UtilizationRatesModel ForecastUtilization { get; set; } = new();
    public decimal ForecastPopulationRiskIndex { get; set; }
}

public class DeltaDriversModel
{
    public decimal ProviderTrendDelta { get; set; }
    public decimal PharmacyTrendDelta { get; set; }
    public decimal OtherTrendDelta { get; set; }
    public decimal RiskDelta { get; set; }
    public decimal UtilizationDelta { get; set; }
    public decimal MembershipDelta { get; set; }
    public Dictionary<string, decimal> ConditionImpacts { get; set; } = new();
    public Dictionary<string, decimal> EncounterMixImpacts { get; set; } = new();
}

public class RiskWeightsModel
{
    public string WeightsId { get; set; } = string.Empty;
    public int Version { get; set; }
    public DateTime LastModified { get; set; }
    public string ModifiedBy { get; set; } = string.Empty;
    public Dictionary<string, decimal> AgeGenderWeights { get; set; } = new();
    public Dictionary<string, decimal> ConditionGroupWeights { get; set; } = new();
    public Dictionary<string, decimal> Icd10Mapping { get; set; } = new();
    public Dictionary<string, decimal> EncounterWeights { get; set; } = new();
    public Dictionary<string, decimal> ObservationSeverityWeights { get; set; } = new();
}
