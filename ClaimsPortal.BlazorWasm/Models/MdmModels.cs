using System;
using System.Collections.Generic;

namespace ClaimsPortal.BlazorWasm.Models;

/// <summary>
/// MDM Link model - matches backend API model
/// </summary>
public class MdmLinkModel
{
    public string Id { get; set; } = string.Empty;
    public string GoldenResourceId { get; set; } = string.Empty;
    public string SourceResourceId { get; set; } = string.Empty;
    public string ResourceType { get; set; } = "Patient";
    public string MatchResult { get; set; } = "POSSIBLE_MATCH";
    public string LinkSource { get; set; } = "AUTO";
    public double? MatchScore { get; set; }
    public Dictionary<string, object> MatchDetails { get; set; } = new();
    public DateTime Created { get; set; }
    public DateTime Updated { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;
}

/// <summary>
/// Golden Resource model
/// </summary>
public class GoldenResourceModel
{
    public string Id { get; set; } = string.Empty;
    public string ResourceType { get; set; } = "Patient";
    public string FhirResourceId { get; set; } = string.Empty;
    public Dictionary<string, object> Data { get; set; } = new();
    public int LinkedSourceCount { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime Created { get; set; }
    public DateTime Updated { get; set; }
}

/// <summary>
/// Review queue item model
/// </summary>
public class ReviewQueueItemModel
{
    public string Id { get; set; } = string.Empty;
    public string LinkId { get; set; } = string.Empty;
    public string GoldenResourceId { get; set; } = string.Empty;
    public string SourceResourceId { get; set; } = string.Empty;
    public string ResourceType { get; set; } = "Patient";
    public string Status { get; set; } = "PENDING";
    public DateTime Created { get; set; }
    public double MatchScore { get; set; }
    public Dictionary<string, object> MatchDetails { get; set; } = new();
    public int Priority { get; set; }
    
    // UI-only properties
    public Dictionary<string, object>? GoldenResourceData { get; set; }
    public Dictionary<string, object>? SourceResourceData { get; set; }
}

/// <summary>
/// MDM Configuration model
/// </summary>
public class MdmConfigModel
{
    public string Id { get; set; } = string.Empty;
    public string ResourceType { get; set; } = "Patient";
    public List<MatchRuleModel> MatchRules { get; set; } = new();
    public bool Enabled { get; set; } = true;
    public double MatchThreshold { get; set; } = 0.8;
    public double PossibleMatchThreshold { get; set; } = 0.6;
    public DateTime Created { get; set; }
    public DateTime Updated { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
}

/// <summary>
/// Match rule model
/// </summary>
public class MatchRuleModel
{
    public string Name { get; set; } = string.Empty;
    public string FieldPath { get; set; } = string.Empty;
    public string Algorithm { get; set; } = "EXACT";
    public double Weight { get; set; } = 1.0;
    public bool Required { get; set; } = false;
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// MDM Metrics model
/// </summary>
public class MdmMetricsModel
{
    public string ResourceType { get; set; } = "Patient";
    public Dictionary<string, long> LinkCountsByResult { get; set; } = new();
    public long AutoLinksCount { get; set; }
    public long ManualLinksCount { get; set; }
    public long TotalGoldenResources { get; set; }
    public long ActiveGoldenResources { get; set; }
    public long MergedGoldenResources { get; set; }
    public long PendingReviewCount { get; set; }
    public long ReviewedTodayCount { get; set; }
    public double AverageReviewTime { get; set; }
}

/// <summary>
/// Match candidate model
/// </summary>
public class MatchCandidateModel
{
    public string GoldenResourceId { get; set; } = string.Empty;
    public string FhirResourceId { get; set; } = string.Empty;
    public double Score { get; set; }
    public string MatchResult { get; set; } = "POSSIBLE_MATCH";
    public Dictionary<string, object> MatchDetails { get; set; } = new();
    public Dictionary<string, object> Resource { get; set; } = new();
    
    // AI Confidence Scoring
    public double? AiConfidence { get; set; }
    public string? AiReasoning { get; set; }
    public List<string> AiMatchingFactors { get; set; } = new();
    public List<string> AiConcerningFactors { get; set; } = new();
    public string? AiRecommendedDecision { get; set; } // AutoApprove, PriorityReview, StandardReview, AutoReject
    public bool IsAiAutoApproved => AiConfidence >= 95;
    public string AiConfidenceColor => AiConfidence switch
    {
        >= 95 => "#107c10", // Green - Auto-approve
        >= 80 => "#0078d4", // Blue - Priority review
        >= 50 => "#ff8c00", // Orange - Standard review
        _ => "#d13438"      // Red - Auto-reject
    };
}

/// <summary>
/// Matching algorithm metadata
/// </summary>
public class AlgorithmMetadata
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, string> Parameters { get; set; } = new();
}
