using System.Text.Json;

namespace ClaimsRules.Api.Models;

/// <summary>
/// Audit log entry for compliance tracking (7-10 year retention)
/// Stored in Cosmos DB for recent data (90 days) and Blob Storage for archival
/// </summary>
public class AuditLog
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string EntityType { get; set; } = string.Empty; // "ProviderContract", "Provider", "NetworkTier"
    public string EntityId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty; // "Create", "Update", "Delete", "StatusChange"
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public Dictionary<string, object?> BeforeState { get; set; } = new();
    public Dictionary<string, object?> AfterState { get; set; } = new();
    public List<FieldChange> Changes { get; set; } = new();
    public string Reason { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public bool IsArchived { get; set; } = false;
    public DateTime? ArchivedDate { get; set; }
    public string? BlobStoragePath { get; set; }
    
    // Partition key for Cosmos DB (YYYY-MM format for efficient querying and archival)
    public string PartitionKey => Timestamp.ToString("yyyy-MM");
}

public class FieldChange
{
    public string FieldName { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string FieldType { get; set; } = string.Empty;
}

public class AuditSearchRequest
{
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string? Action { get; set; }
    public string? UserId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IncludeArchived { get; set; } = false;
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class AuditSearchResponse
{
    public List<AuditLog> AuditLogs { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasArchivedData { get; set; }
}

public class ContractAuditSummary
{
    public string ContractId { get; set; } = string.Empty;
    public string ContractNumber { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public int TotalChanges { get; set; }
    public DateTime FirstChange { get; set; }
    public DateTime LastChange { get; set; }
    public List<string> ChangeTypes { get; set; } = new();
    public int ArchivedChanges { get; set; }
}
