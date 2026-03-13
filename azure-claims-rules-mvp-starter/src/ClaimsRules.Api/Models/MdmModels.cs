using System;
using System.Collections.Generic;

namespace ClaimsRules.Api.Models;

/// <summary>
/// MDM Link represents the relationship between a source resource and a golden resource
/// </summary>
public class MdmLink
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string PartitionKey { get; set; } = string.Empty; // PatientId, ProviderId, or OrganizationId
    
    // Resource identifiers
    public string GoldenResourceId { get; set; } = string.Empty;
    public string SourceResourceId { get; set; } = string.Empty;
    public string ResourceType { get; set; } = "Patient"; // Patient, Practitioner, Organization
    
    // Match information
    public MdmMatchResult MatchResult { get; set; } = MdmMatchResult.POSSIBLE_MATCH;
    public MdmLinkSource LinkSource { get; set; } = MdmLinkSource.AUTO;
    
    // Match scoring
    public double? MatchScore { get; set; }
    public long? RuleCount { get; set; }
    public Dictionary<string, object> MatchDetails { get; set; } = new();
    
    // EID (Enterprise Identifier) matching
    public bool? EidMatch { get; set; }
    public bool HadToCreateNewGoldenResource { get; set; }
    
    // Audit fields
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public DateTime Updated { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = "System";
    public string UpdatedBy { get; set; } = "System";
    public string Version { get; set; } = "1";
}

/// <summary>
/// Match result enumeration following HAPI FHIR MDM standards
/// </summary>
public enum MdmMatchResult
{
    NO_MATCH,           // Manually confirmed to not be a match
    POSSIBLE_MATCH,     // Enough of a match to warrant manual review
    MATCH,              // Strong enough match to consider matched
    POSSIBLE_DUPLICATE, // Link between two Golden Records indicating they may be duplicates
    GOLDEN_RECORD,      // Link between Golden Record and Source Resource
    REDIRECT            // Link after merge - inactive golden resource points to active one
}

/// <summary>
/// Source of the MDM link
/// </summary>
public enum MdmLinkSource
{
    AUTO,   // Created automatically by matching engine
    MANUAL  // Created/confirmed manually by user
}

/// <summary>
/// Golden Resource - the authoritative master record
/// </summary>
public class GoldenResource
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string PartitionKey { get; set; } = string.Empty;
    public string ResourceType { get; set; } = "Patient";
    
    // FHIR resource reference
    public string FhirResourceId { get; set; } = string.Empty;
    public string FhirResourceVersion { get; set; } = "1";
    
    // Golden resource data (denormalized for performance)
    public Dictionary<string, object> Data { get; set; } = new();
    
    // Enterprise identifiers
    public List<Identifier> Identifiers { get; set; } = new();
    
    // Status
    public bool IsActive { get; set; } = true;
    public DateTime? DeactivatedDate { get; set; }
    public string? MergedIntoGoldenResourceId { get; set; }
    
    // Metadata
    public int LinkedSourceCount { get; set; }
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public DateTime Updated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Identifier structure for enterprise IDs
/// </summary>
public class Identifier
{
    public string System { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Use { get; set; }
}

/// <summary>
/// MDM Configuration for matching rules
/// </summary>
public class MdmConfiguration
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string PartitionKey { get; set; } = "config";
    public string ResourceType { get; set; } = "Patient";
    
    // Matching rules
    public List<MatchRule> MatchRules { get; set; } = new();
    
    // System settings
    public bool Enabled { get; set; } = true;
    public MdmMode Mode { get; set; } = MdmMode.MATCH_AND_LINK;
    public bool AutoCreateGoldenResource { get; set; } = true;
    public bool PreventMultipleMatches { get; set; } = true;
    
    // Thresholds
    public double MatchThreshold { get; set; } = 0.8;
    public double PossibleMatchThreshold { get; set; } = 0.6;
    
    // Audit
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public DateTime Updated { get; set; } = DateTime.UtcNow;
    public string UpdatedBy { get; set; } = "System";
}

/// <summary>
/// MDM operation modes
/// </summary>
public enum MdmMode
{
    MATCH_AND_LINK, // Normal MDM processing - creates golden resources and maintains links
    MATCH_ONLY      // Only matching, no link creation (useful for $match operation)
}

/// <summary>
/// Matching rule definition
/// </summary>
public class MatchRule
{
    public string Name { get; set; } = string.Empty;
    public string FieldPath { get; set; } = string.Empty; // e.g., "name[0].family", "identifier[?(@.system=='SSN')].value"
    public MatchAlgorithm Algorithm { get; set; } = MatchAlgorithm.EXACT;
    public double Weight { get; set; } = 1.0;
    public bool Required { get; set; } = false;
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// Matching algorithm types
/// </summary>
public enum MatchAlgorithm
{
    EXACT,           // Exact string match
    NORMALIZED,      // Case-insensitive, trimmed
    PHONETIC,        // Soundex/Metaphone/DoubleMetaphone
    FUZZY,           // Levenshtein distance
    DATE_RANGE,      // Date within range
    NUMERIC_RANGE,   // Numeric within range
    SUBSTRING        // Contains substring
}

/// <summary>
/// Manual review queue item
/// </summary>
public class MdmReviewQueueItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string PartitionKey { get; set; } = "queue";
    
    public string LinkId { get; set; } = string.Empty;
    public string GoldenResourceId { get; set; } = string.Empty;
    public string SourceResourceId { get; set; } = string.Empty;
    public string ResourceType { get; set; } = "Patient";
    
    // Queue status
    public ReviewStatus Status { get; set; } = ReviewStatus.PENDING;
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedDate { get; set; }
    public string? ReviewedBy { get; set; }
    public string? ReviewNotes { get; set; }
    
    // Match information
    public double MatchScore { get; set; }
    public Dictionary<string, object> MatchDetails { get; set; } = new();
    
    // Priority
    public int Priority { get; set; } = 5; // 1-10, higher = more urgent
}

/// <summary>
/// Review queue status
/// </summary>
public enum ReviewStatus
{
    PENDING,
    IN_PROGRESS,
    APPROVED,
    REJECTED,
    ESCALATED
}

/// <summary>
/// MDM Metrics for dashboard
/// </summary>
public class MdmMetrics
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string ResourceType { get; set; } = "Patient";
    
    // Link counts by match result
    public Dictionary<MdmMatchResult, long> LinkCountsByResult { get; set; } = new();
    
    // Link counts by source
    public long AutoLinksCount { get; set; }
    public long ManualLinksCount { get; set; }
    
    // Golden resources
    public long TotalGoldenResources { get; set; }
    public long ActiveGoldenResources { get; set; }
    public long MergedGoldenResources { get; set; }
    
    // Queue metrics
    public long PendingReviewCount { get; set; }
    public long ReviewedTodayCount { get; set; }
    public double AverageReviewTime { get; set; } // in minutes
    
    // Match performance
    public long MatchesProcessedToday { get; set; }
    public double AverageMatchScore { get; set; }
}

/// <summary>
/// Match operation request
/// </summary>
public class PatientMatchRequest
{
    public string ResourceType { get; set; } = "Patient";
    public Dictionary<string, object> Resource { get; set; } = new();
    public int? Count { get; set; } = 10; // Max results to return
    public bool OnlyReturnMatches { get; set; } = false; // If true, only return MATCH results
}

/// <summary>
/// Match operation response
/// </summary>
public class MatchResponse
{
    public List<MatchCandidate> Candidates { get; set; } = new();
    public int TotalCount { get; set; }
}

/// <summary>
/// Match candidate result
/// </summary>
public class MatchCandidate
{
    public string GoldenResourceId { get; set; } = string.Empty;
    public string FhirResourceId { get; set; } = string.Empty;
    public double Score { get; set; }
    public MdmMatchResult MatchResult { get; set; }
    public Dictionary<string, object> MatchDetails { get; set; } = new();
    public Dictionary<string, object> Resource { get; set; } = new();
}

/// <summary>
/// Merge golden resources request
/// </summary>
public class MergeGoldenResourcesRequest
{
    public string FromGoldenResourceId { get; set; } = string.Empty;
    public string ToGoldenResourceId { get; set; } = string.Empty;
    public Dictionary<string, object>? MergedResourceData { get; set; }
}

/// <summary>
/// Create/Update link request
/// </summary>
public class CreateUpdateLinkRequest
{
    public string GoldenResourceId { get; set; } = string.Empty;
    public string SourceResourceId { get; set; } = string.Empty;
    public MdmMatchResult MatchResult { get; set; } = MdmMatchResult.MATCH;
}
