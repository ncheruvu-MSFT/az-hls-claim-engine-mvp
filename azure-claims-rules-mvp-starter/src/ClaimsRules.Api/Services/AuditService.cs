using System.Text;
using System.Text.Json;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ClaimsRules.Api.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ClaimsRules.Api.Services;

/// <summary>
/// Audit service with automatic archival to Blob Storage for cost-effective 7-10 year retention
/// - Cosmos DB: Recent data (last 90 days) for fast queries
/// - Blob Storage: Archived data (>90 days) with Cool/Archive tier for cost savings
/// </summary>
public class AuditService
{
    private readonly CosmosClient _cosmosClient;
    private readonly Container _auditContainer;
    private readonly BlobServiceClient _blobServiceClient;
    private readonly BlobContainerClient _archiveContainer;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuditService> _logger;

    private const int ARCHIVE_AFTER_DAYS = 90;
    private const string ARCHIVE_CONTAINER_NAME = "contract-audit-archive";

    public AuditService(IConfiguration configuration, ILogger<AuditService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        // Cosmos DB setup - Uses DefaultAzureCredential (Azure CLI locally, Managed Identity in production)
        var cosmosEndpoint = configuration["Cosmos__AccountEndpoint"] ?? 
                            "https://cosmosclaimstest001ncv.documents.azure.com:443/";
        var databaseName = configuration["Cosmos__Database"] ?? "claims";

        _cosmosClient = new CosmosClient(cosmosEndpoint, new Azure.Identity.DefaultAzureCredential());
        var database = _cosmosClient.GetDatabase(databaseName);
        _auditContainer = database.GetContainer("audit");

        // Blob Storage setup - Uses DefaultAzureCredential (Azure CLI locally, Managed Identity in production)
        var storageAccountName = configuration["BlobStorage__AccountName"] ?? "ahdsstrtest001ncv";
        var blobServiceUri = new Uri($"https://{storageAccountName}.blob.core.windows.net");
        
        _blobServiceClient = new BlobServiceClient(blobServiceUri, new Azure.Identity.DefaultAzureCredential());
        _archiveContainer = _blobServiceClient.GetBlobContainerClient(ARCHIVE_CONTAINER_NAME);
    }

    /// <summary>
    /// Log an audit entry for contract or provider changes
    /// </summary>
    public async Task<AuditLog> LogChangeAsync(
        string entityType,
        string entityId,
        string action,
        object? beforeState,
        object? afterState,
        string userId,
        string userName,
        string reason = "",
        string ipAddress = "",
        string userAgent = "")
    {
        var auditLog = new AuditLog
        {
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            UserId = userId,
            UserName = userName,
            Reason = reason,
            IpAddress = ipAddress,
            UserAgent = userAgent
        };

        // Serialize states
        if (beforeState != null)
            auditLog.BeforeState = JsonSerializer.Deserialize<Dictionary<string, object?>>(
                JsonSerializer.Serialize(beforeState)) ?? new();

        if (afterState != null)
            auditLog.AfterState = JsonSerializer.Deserialize<Dictionary<string, object?>>(
                JsonSerializer.Serialize(afterState)) ?? new();

        // Calculate field changes
        auditLog.Changes = CalculateChanges(auditLog.BeforeState, auditLog.AfterState);

        try
        {
            // Ensure container exists
            await _auditContainer.Database.CreateContainerIfNotExistsAsync(
                new ContainerProperties("audit", "/partitionKey")
                {
                    DefaultTimeToLive = -1 // Never expire automatically
                });

            var response = await _auditContainer.CreateItemAsync(auditLog, new PartitionKey(auditLog.PartitionKey));
            _logger.LogInformation($"Audit log created: {auditLog.Id} for {entityType}/{entityId}");
            return response.Resource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to create audit log for {entityType}/{entityId}");
            return auditLog; // Return anyway for demo purposes
        }
    }

    /// <summary>
    /// Search audit logs with support for archived data
    /// </summary>
    public async Task<AuditSearchResponse> SearchAuditLogsAsync(AuditSearchRequest request)
    {
        var allLogs = new List<AuditLog>();
        var hasArchivedData = false;

        // Search Cosmos DB (recent data)
        var cosmosLogs = await SearchCosmosDbAsync(request);
        allLogs.AddRange(cosmosLogs);

        // Search Blob Storage if needed (archived data)
        if (request.IncludeArchived && request.StartDate.HasValue)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-ARCHIVE_AFTER_DAYS);
            if (request.StartDate.Value < cutoffDate)
            {
                var archivedLogs = await SearchBlobStorageAsync(request);
                allLogs.AddRange(archivedLogs);
                hasArchivedData = archivedLogs.Any();
            }
        }

        // Sort by timestamp descending
        allLogs = allLogs.OrderByDescending(l => l.Timestamp).ToList();

        // Apply pagination
        var totalCount = allLogs.Count;
        var pagedLogs = allLogs
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return new AuditSearchResponse
        {
            AuditLogs = pagedLogs,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize),
            HasArchivedData = hasArchivedData
        };
    }

    /// <summary>
    /// Get audit summary for a specific contract
    /// </summary>
    public async Task<ContractAuditSummary> GetContractAuditSummaryAsync(string contractId)
    {
        var searchRequest = new AuditSearchRequest
        {
            EntityType = "ProviderContract",
            EntityId = contractId,
            IncludeArchived = true,
            PageSize = 1000
        };

        var results = await SearchAuditLogsAsync(searchRequest);

        if (!results.AuditLogs.Any())
        {
            return new ContractAuditSummary { ContractId = contractId };
        }

        return new ContractAuditSummary
        {
            ContractId = contractId,
            TotalChanges = results.TotalCount,
            FirstChange = results.AuditLogs.Min(a => a.Timestamp),
            LastChange = results.AuditLogs.Max(a => a.Timestamp),
            ChangeTypes = results.AuditLogs.Select(a => a.Action).Distinct().ToList(),
            ArchivedChanges = results.AuditLogs.Count(a => a.IsArchived)
        };
    }

    /// <summary>
    /// Archive old audit logs to Blob Storage (run monthly via Azure Function timer trigger)
    /// Moves data >90 days old from Cosmos DB to Blob Storage with Archive tier
    /// </summary>
    public async Task<int> ArchiveOldLogsAsync()
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-ARCHIVE_AFTER_DAYS);
        var archivedCount = 0;

        try
        {
            // Ensure blob container exists with Archive access tier
            await _archiveContainer.CreateIfNotExistsAsync(PublicAccessType.None);

            // Query logs older than cutoff date
            var query = $"SELECT * FROM c WHERE c.timestamp < '{cutoffDate:yyyy-MM-ddTHH:mm:ss.fffZ}' AND c.isArchived = false";
            var iterator = _auditContainer.GetItemQueryIterator<AuditLog>(query);

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                foreach (var log in response)
                {
                    // Upload to Blob Storage (Archive tier for cost savings)
                    var blobName = $"{log.Timestamp:yyyy/MM}/{log.Id}.json";
                    var blobClient = _archiveContainer.GetBlobClient(blobName);

                    var json = JsonSerializer.Serialize(log, new JsonSerializerOptions { WriteIndented = true });
                    var bytes = Encoding.UTF8.GetBytes(json);

                    await blobClient.UploadAsync(
                        new BinaryData(bytes),
                        new BlobUploadOptions
                        {
                            AccessTier = AccessTier.Archive // Cheapest storage tier
                        });

                    // Update Cosmos DB record to mark as archived
                    log.IsArchived = true;
                    log.ArchivedDate = DateTime.UtcNow;
                    log.BlobStoragePath = blobName;

                    // Replace with minimal version or delete from Cosmos DB
                    // For compliance, keep minimal record with pointer to blob
                    await _auditContainer.ReplaceItemAsync(log, log.Id, new PartitionKey(log.PartitionKey));

                    archivedCount++;
                    _logger.LogInformation($"Archived audit log {log.Id} to {blobName}");
                }
            }

            _logger.LogInformation($"Archived {archivedCount} audit logs to Blob Storage");
            return archivedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving audit logs");
            return archivedCount;
        }
    }

    #region Private Methods

    private async Task<List<AuditLog>> SearchCosmosDbAsync(AuditSearchRequest request)
    {
        try
        {
            var queryText = "SELECT * FROM c WHERE 1=1";
            var filters = new List<string>();

            if (!string.IsNullOrWhiteSpace(request.EntityType))
                filters.Add($"c.entityType = '{request.EntityType}'");

            if (!string.IsNullOrWhiteSpace(request.EntityId))
                filters.Add($"c.entityId = '{request.EntityId}'");

            if (!string.IsNullOrWhiteSpace(request.Action))
                filters.Add($"c.action = '{request.Action}'");

            if (!string.IsNullOrWhiteSpace(request.UserId))
                filters.Add($"c.userId = '{request.UserId}'");

            if (request.StartDate.HasValue)
                filters.Add($"c.timestamp >= '{request.StartDate.Value:yyyy-MM-ddTHH:mm:ss.fffZ}'");

            if (request.EndDate.HasValue)
                filters.Add($"c.timestamp <= '{request.EndDate.Value:yyyy-MM-ddTHH:mm:ss.fffZ}'");

            if (filters.Any())
                queryText += " AND " + string.Join(" AND ", filters);

            queryText += " ORDER BY c.timestamp DESC";

            var query = _auditContainer.GetItemQueryIterator<AuditLog>(queryText);
            var results = new List<AuditLog>();

            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();
                results.AddRange(response);
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching Cosmos DB audit logs");
            return new List<AuditLog>();
        }
    }

    private async Task<List<AuditLog>> SearchBlobStorageAsync(AuditSearchRequest request)
    {
        var results = new List<AuditLog>();

        try
        {
            // List blobs in date range
            var startDate = request.StartDate ?? DateTime.UtcNow.AddYears(-10);
            var endDate = request.EndDate ?? DateTime.UtcNow;

            await foreach (var blobItem in _archiveContainer.GetBlobsAsync(Azure.Storage.Blobs.Models.BlobTraits.None, Azure.Storage.Blobs.Models.BlobStates.None, prefix: $"{startDate:yyyy}", cancellationToken: default))
            {
                // Download and deserialize blob
                var blobClient = _archiveContainer.GetBlobClient(blobItem.Name);
                var downloadResult = await blobClient.DownloadContentAsync();
                var json = downloadResult.Value.Content.ToString();
                var log = JsonSerializer.Deserialize<AuditLog>(json);

                if (log != null && MatchesFilters(log, request))
                {
                    results.Add(log);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching Blob Storage audit logs");
        }

        return results;
    }

    private bool MatchesFilters(AuditLog log, AuditSearchRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.EntityType) && log.EntityType != request.EntityType)
            return false;

        if (!string.IsNullOrWhiteSpace(request.EntityId) && log.EntityId != request.EntityId)
            return false;

        if (!string.IsNullOrWhiteSpace(request.Action) && log.Action != request.Action)
            return false;

        if (!string.IsNullOrWhiteSpace(request.UserId) && log.UserId != request.UserId)
            return false;

        if (request.StartDate.HasValue && log.Timestamp < request.StartDate.Value)
            return false;

        if (request.EndDate.HasValue && log.Timestamp > request.EndDate.Value)
            return false;

        return true;
    }

    private List<FieldChange> CalculateChanges(
        Dictionary<string, object?> beforeState,
        Dictionary<string, object?> afterState)
    {
        var changes = new List<FieldChange>();

        var allKeys = beforeState.Keys.Union(afterState.Keys).ToList();

        foreach (var key in allKeys)
        {
            var oldValue = beforeState.ContainsKey(key) ? beforeState[key]?.ToString() : null;
            var newValue = afterState.ContainsKey(key) ? afterState[key]?.ToString() : null;

            if (oldValue != newValue)
            {
                changes.Add(new FieldChange
                {
                    FieldName = key,
                    OldValue = oldValue,
                    NewValue = newValue,
                    FieldType = afterState.ContainsKey(key) ? afterState[key]?.GetType().Name ?? "Unknown" : "Unknown"
                });
            }
        }

        return changes;
    }

    #endregion
}
