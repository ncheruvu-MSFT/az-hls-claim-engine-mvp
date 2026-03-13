using System.Net.Http.Json;

namespace ClaimsPortal.BlazorWasm.Services;

public class AuditLogService
{
    private readonly HttpClient _http;
    private readonly string _apiBaseUrl;

    public AuditLogService(HttpClient http, IConfiguration configuration)
    {
        _http = http;
        _apiBaseUrl = configuration["ApiBaseUrl"] ?? "http://localhost:7071/api";
    }

    public async Task<AuditSearchResponse> SearchAuditLogsAsync(AuditSearchRequest request)
    {
        try
        {
            var response = await _http.PostAsJsonAsync($"{_apiBaseUrl}/audit/search", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<AuditSearchResponse>() ?? new();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error searching audit logs: {ex.Message}");
            return new AuditSearchResponse();
        }
    }

    public async Task<ContractAuditSummary?> GetContractAuditSummaryAsync(string contractId)
    {
        try
        {
            return await _http.GetFromJsonAsync<ContractAuditSummary>(
                $"{_apiBaseUrl}/audit/contract/{contractId}/summary");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting contract audit summary: {ex.Message}");
            return null;
        }
    }
}

// Models
public class AuditLog
{
    public string Id { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public Dictionary<string, object?> BeforeState { get; set; } = new();
    public Dictionary<string, object?> AfterState { get; set; } = new();
    public List<FieldChange> Changes { get; set; } = new();
    public string Reason { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public bool IsArchived { get; set; }
    public DateTime? ArchivedDate { get; set; }
    public string? BlobStoragePath { get; set; }
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
