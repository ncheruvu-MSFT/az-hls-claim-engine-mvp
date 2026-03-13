using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ClaimsRules.Api.Models;
using Microsoft.Azure.Cosmos;

namespace ClaimsRules.Api.Services;

/// <summary>
/// MDM Link Service - manages MDM links and golden resources
/// </summary>
public class MdmLinkService
{
    private readonly CosmosAudit _cosmosAudit;
    private readonly FhirClient _fhirClient;
    private readonly MdmMatchingService _matchingService;

    public MdmLinkService(CosmosAudit cosmosAudit, FhirClient fhirClient, MdmMatchingService matchingService)
    {
        _cosmosAudit = cosmosAudit;
        _fhirClient = fhirClient;
        _matchingService = matchingService;
    }

    /// <summary>
    /// Process a new source resource for MDM matching
    /// </summary>
    public async Task<MdmLink> ProcessSourceResourceAsync(
        string resourceType,
        string sourceResourceId,
        Dictionary<string, object> sourceResource,
        string userName = "System")
    {
        // Get MDM configuration for this resource type
        var config = await GetConfigurationAsync(resourceType);
        if (!config.Enabled)
            throw new InvalidOperationException($"MDM is not enabled for {resourceType}");

        // Find matching golden resources
        var matches = await _matchingService.FindMatchesAsync(resourceType, sourceResource, config);

        MdmLink link;

        if (matches.Any(m => m.MatchResult == MdmMatchResult.MATCH))
        {
            // Strong match found - create AUTO link
            var bestMatch = matches.First(m => m.MatchResult == MdmMatchResult.MATCH);
            link = await CreateLinkAsync(
                bestMatch.GoldenResourceId,
                sourceResourceId,
                resourceType,
                MdmMatchResult.MATCH,
                MdmLinkSource.AUTO,
                bestMatch.Score,
                bestMatch.MatchDetails,
                userName);
        }
        else if (matches.Any(m => m.MatchResult == MdmMatchResult.POSSIBLE_MATCH))
        {
            // Possible match - create link and add to review queue
            var bestMatch = matches.First(m => m.MatchResult == MdmMatchResult.POSSIBLE_MATCH);
            link = await CreateLinkAsync(
                bestMatch.GoldenResourceId,
                sourceResourceId,
                resourceType,
                MdmMatchResult.POSSIBLE_MATCH,
                MdmLinkSource.AUTO,
                bestMatch.Score,
                bestMatch.MatchDetails,
                userName);

            // Add to review queue
            await AddToReviewQueueAsync(link, bestMatch.Score, bestMatch.MatchDetails);
        }
        else
        {
            // No match - create new golden resource
            var goldenResource = await CreateGoldenResourceAsync(resourceType, sourceResource, userName);
            link = await CreateLinkAsync(
                goldenResource.Id,
                sourceResourceId,
                resourceType,
                MdmMatchResult.MATCH,
                MdmLinkSource.AUTO,
                1.0,
                new Dictionary<string, object> { { "reason", "New golden resource created" } },
                userName);
            link.HadToCreateNewGoldenResource = true;
        }

        return link;
    }

    /// <summary>
    /// Create a new MDM link
    /// </summary>
    public async Task<MdmLink> CreateLinkAsync(
        string goldenResourceId,
        string sourceResourceId,
        string resourceType,
        MdmMatchResult matchResult,
        MdmLinkSource linkSource,
        double? matchScore,
        Dictionary<string, object> matchDetails,
        string userName = "System")
    {
        // Check if link already exists
        var existingLink = await GetLinkBySourceIdAsync(sourceResourceId);
        if (existingLink != null)
        {
            // Update existing link
            existingLink.GoldenResourceId = goldenResourceId;
            existingLink.MatchResult = matchResult;
            existingLink.LinkSource = linkSource;
            existingLink.MatchScore = matchScore;
            existingLink.MatchDetails = matchDetails;
            existingLink.Updated = DateTime.UtcNow;
            existingLink.UpdatedBy = userName;
            existingLink.Version = (int.Parse(existingLink.Version) + 1).ToString();

            await _cosmosAudit.UpsertAsync(existingLink);
            return existingLink;
        }

        // Create new link
        var link = new MdmLink
        {
            Id = Guid.NewGuid().ToString(),
            PartitionKey = goldenResourceId,
            GoldenResourceId = goldenResourceId,
            SourceResourceId = sourceResourceId,
            ResourceType = resourceType,
            MatchResult = matchResult,
            LinkSource = linkSource,
            MatchScore = matchScore,
            MatchDetails = matchDetails,
            Created = DateTime.UtcNow,
            Updated = DateTime.UtcNow,
            CreatedBy = userName,
            UpdatedBy = userName
        };

        await _cosmosAudit.UpsertAsync(link);
        return link;
    }

    /// <summary>
    /// Update an existing link (e.g., after manual review)
    /// </summary>
    public async Task<MdmLink> UpdateLinkAsync(
        string linkId,
        MdmMatchResult matchResult,
        string userName = "System")
    {
        var link = await GetLinkByIdAsync(linkId);
        if (link == null)
            throw new InvalidOperationException($"Link {linkId} not found");

        link.MatchResult = matchResult;
        link.LinkSource = MdmLinkSource.MANUAL; // Manual confirmation
        link.Updated = DateTime.UtcNow;
        link.UpdatedBy = userName;
        link.Version = (int.Parse(link.Version) + 1).ToString();

        await _cosmosAudit.UpsertAsync(link);

        // If confirmed as MATCH, learn from this decision
        if (matchResult == MdmMatchResult.MATCH)
        {
            await LearnFromManualDecisionAsync(link);
        }

        return link;
    }

    /// <summary>
    /// Learn from manual match decision to improve automatic matching
    /// </summary>
    private async Task LearnFromManualDecisionAsync(MdmLink link)
    {
        // Get configuration
        var config = await GetConfigurationAsync(link.ResourceType);

        // Analyze which rules matched and adjust weights
        foreach (var detail in link.MatchDetails)
        {
            if (detail.Value is double score && score > 0)
            {
                var rule = config.MatchRules.FirstOrDefault(r => r.Name == detail.Key);
                if (rule != null)
                {
                    // Increase weight for rules that contributed to correct match
                    rule.Weight = Math.Min(rule.Weight * 1.1, 10.0);
                }
            }
        }

        // Save updated configuration
        await UpdateConfigurationAsync(config, link.UpdatedBy);
    }

    /// <summary>
    /// Merge two golden resources
    /// </summary>
    public async Task<GoldenResource> MergeGoldenResourcesAsync(
        string fromGoldenResourceId,
        string toGoldenResourceId,
        Dictionary<string, object>? mergedData,
        string userName = "System")
    {
        var fromGolden = await GetGoldenResourceByIdAsync(fromGoldenResourceId);
        var toGolden = await GetGoldenResourceByIdAsync(toGoldenResourceId);

        if (fromGolden == null || toGolden == null)
            throw new InvalidOperationException("One or both golden resources not found");

        // Update all links from 'from' to point to 'to'
        var linksToUpdate = await GetLinksByGoldenResourceIdAsync(fromGoldenResourceId);
        foreach (var link in linksToUpdate)
        {
            link.GoldenResourceId = toGoldenResourceId;
            link.PartitionKey = toGoldenResourceId;
            link.Updated = DateTime.UtcNow;
            link.UpdatedBy = userName;
            await _cosmosAudit.UpsertAsync(link);
        }

        // Deactivate 'from' golden resource
        fromGolden.IsActive = false;
        fromGolden.DeactivatedDate = DateTime.UtcNow;
        fromGolden.MergedIntoGoldenResourceId = toGoldenResourceId;
        await _cosmosAudit.UpsertAsync(fromGolden);

        // Update 'to' golden resource with merged data
        if (mergedData != null)
        {
            toGolden.Data = mergedData;
        }
        toGolden.LinkedSourceCount += fromGolden.LinkedSourceCount;
        toGolden.Updated = DateTime.UtcNow;
        await _cosmosAudit.UpsertAsync(toGolden);

        // Create REDIRECT link
        var redirectLink = new MdmLink
        {
            Id = Guid.NewGuid().ToString(),
            PartitionKey = toGoldenResourceId,
            GoldenResourceId = toGoldenResourceId,
            SourceResourceId = fromGoldenResourceId,
            ResourceType = toGolden.ResourceType,
            MatchResult = MdmMatchResult.REDIRECT,
            LinkSource = MdmLinkSource.MANUAL,
            CreatedBy = userName,
            UpdatedBy = userName
        };
        await _cosmosAudit.UpsertAsync(redirectLink);

        return toGolden;
    }

    /// <summary>
    /// Create a new golden resource
    /// </summary>
    private async Task<GoldenResource> CreateGoldenResourceAsync(
        string resourceType,
        Dictionary<string, object> sourceData,
        string userName = "System")
    {
        // Create FHIR resource for golden record
        var fhirJson = JsonSerializer.Serialize(sourceData);
        var fhirResult = await _fhirClient.CreateAsync(resourceType, fhirJson);
        var fhirResource = JsonSerializer.Deserialize<Dictionary<string, object>>(fhirResult);
        var fhirId = fhirResource?["id"]?.ToString() ?? Guid.NewGuid().ToString();

        var goldenResource = new GoldenResource
        {
            Id = Guid.NewGuid().ToString(),
            PartitionKey = fhirId,
            ResourceType = resourceType,
            FhirResourceId = fhirId,
            Data = sourceData,
            Identifiers = ExtractIdentifiers(sourceData),
            LinkedSourceCount = 1,
            Created = DateTime.UtcNow,
            Updated = DateTime.UtcNow
        };

        await _cosmosAudit.UpsertAsync(goldenResource);
        return goldenResource;
    }

    /// <summary>
    /// Extract identifiers from resource data
    /// </summary>
    private List<Identifier> ExtractIdentifiers(Dictionary<string, object> data)
    {
        var identifiers = new List<Identifier>();

        if (data.TryGetValue("identifier", out var identifierObj))
        {
            if (identifierObj is JsonElement identifierElement && identifierElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in identifierElement.EnumerateArray())
                {
                    identifiers.Add(new Identifier
                    {
                        System = item.TryGetProperty("system", out var sys) ? sys.GetString() ?? "" : "",
                        Value = item.TryGetProperty("value", out var val) ? val.GetString() ?? "" : "",
                        Use = item.TryGetProperty("use", out var use) ? use.GetString() : null
                    });
                }
            }
        }

        return identifiers;
    }

    /// <summary>
    /// Add link to manual review queue
    /// </summary>
    private async Task AddToReviewQueueAsync(
        MdmLink link,
        double matchScore,
        Dictionary<string, object> matchDetails)
    {
        var queueItem = new MdmReviewQueueItem
        {
            LinkId = link.Id,
            GoldenResourceId = link.GoldenResourceId,
            SourceResourceId = link.SourceResourceId,
            ResourceType = link.ResourceType,
            MatchScore = matchScore,
            MatchDetails = matchDetails,
            Priority = CalculatePriority(matchScore)
        };

        await _cosmosAudit.UpsertAsync(queueItem);
    }

    /// <summary>
    /// Calculate review priority based on match score
    /// </summary>
    private int CalculatePriority(double matchScore)
    {
        // Higher score = higher priority (closer to auto-match threshold)
        if (matchScore >= 0.75) return 10;
        if (matchScore >= 0.70) return 8;
        if (matchScore >= 0.65) return 6;
        if (matchScore >= 0.60) return 4;
        return 2;
    }

    // Query methods
    public async Task<MdmLink?> GetLinkByIdAsync(string linkId)
    {
        var query = $"SELECT * FROM c WHERE c.id = '{linkId}'";
        var links = await _cosmosAudit.QueryAsync<MdmLink>(query);
        return links.FirstOrDefault();
    }

    public async Task<MdmLink?> GetLinkBySourceIdAsync(string sourceResourceId)
    {
        var query = $"SELECT * FROM c WHERE c.sourceResourceId = '{sourceResourceId}'";
        var links = await _cosmosAudit.QueryAsync<MdmLink>(query);
        return links.FirstOrDefault();
    }

    public async Task<List<MdmLink>> GetLinksByGoldenResourceIdAsync(string goldenResourceId)
    {
        var query = $"SELECT * FROM c WHERE c.goldenResourceId = '{goldenResourceId}'";
        var links = await _cosmosAudit.QueryAsync<MdmLink>(query);
        return links.ToList();
    }

    public async Task<GoldenResource?> GetGoldenResourceByIdAsync(string goldenResourceId)
    {
        var query = $"SELECT * FROM c WHERE c.id = '{goldenResourceId}' AND c.resourceType != null";
        var resources = await _cosmosAudit.QueryAsync<GoldenResource>(query);
        return resources.FirstOrDefault();
    }

    public async Task<List<MdmReviewQueueItem>> GetPendingReviewItemsAsync(int limit = 50)
    {
        var query = $"SELECT TOP {limit} * FROM c WHERE c.status = 'PENDING' ORDER BY c.priority DESC, c.created ASC";
        var items = await _cosmosAudit.QueryAsync<MdmReviewQueueItem>(query);
        return items.ToList();
    }

    public async Task<MdmConfiguration> GetConfigurationAsync(string resourceType)
    {
        var query = $"SELECT * FROM c WHERE c.resourceType = '{resourceType}' AND c.partitionKey = 'config'";
        var configs = await _cosmosAudit.QueryAsync<MdmConfiguration>(query);
        var config = configs.FirstOrDefault();

        if (config == null)
        {
            // Return default configuration
            config = CreateDefaultConfiguration(resourceType);
            await _cosmosAudit.UpsertAsync(config);
        }

        return config;
    }

    public async Task UpdateConfigurationAsync(MdmConfiguration config, string userName = "System")
    {
        config.Updated = DateTime.UtcNow;
        config.UpdatedBy = userName;
        await _cosmosAudit.UpsertAsync(config);
    }

    /// <summary>
    /// Create default MDM configuration for a resource type
    /// </summary>
    private MdmConfiguration CreateDefaultConfiguration(string resourceType)
    {
        var config = new MdmConfiguration
        {
            ResourceType = resourceType,
            PartitionKey = "config",
            Enabled = true,
            MatchThreshold = 0.8,
            PossibleMatchThreshold = 0.6
        };

        // Default rules for Patient
        if (resourceType == "Patient")
        {
            config.MatchRules = new List<MatchRule>
            {
                new() { Name = "SSN", FieldPath = "identifier[?(@.system=='http://hl7.org/fhir/sid/us-ssn')].value", Algorithm = MatchAlgorithm.EXACT, Weight = 5.0, Required = false },
                new() { Name = "MRN", FieldPath = "identifier[0].value", Algorithm = MatchAlgorithm.EXACT, Weight = 4.0, Required = false },
                new() { Name = "FamilyName", FieldPath = "name[0].family", Algorithm = MatchAlgorithm.PHONETIC, Weight = 2.0, Required = true },
                new() { Name = "GivenName", FieldPath = "name[0].given[0]", Algorithm = MatchAlgorithm.PHONETIC, Weight = 2.0, Required = true },
                new() { Name = "BirthDate", FieldPath = "birthDate", Algorithm = MatchAlgorithm.EXACT, Weight = 3.0, Required = true },
                new() { Name = "Gender", FieldPath = "gender", Algorithm = MatchAlgorithm.EXACT, Weight = 1.0, Required = false }
            };
        }
        // Default rules for Practitioner
        else if (resourceType == "Practitioner")
        {
            config.MatchRules = new List<MatchRule>
            {
                new() { Name = "NPI", FieldPath = "identifier[?(@.system=='http://hl7.org/fhir/sid/us-npi')].value", Algorithm = MatchAlgorithm.EXACT, Weight = 5.0, Required = false },
                new() { Name = "FamilyName", FieldPath = "name[0].family", Algorithm = MatchAlgorithm.NORMALIZED, Weight = 2.0, Required = true },
                new() { Name = "GivenName", FieldPath = "name[0].given[0]", Algorithm = MatchAlgorithm.NORMALIZED, Weight = 2.0, Required = true }
            };
        }
        // Default rules for Organization
        else if (resourceType == "Organization")
        {
            config.MatchRules = new List<MatchRule>
            {
                new() { Name = "TaxID", FieldPath = "identifier[?(@.type.coding[0].code=='TAX')].value", Algorithm = MatchAlgorithm.EXACT, Weight = 5.0, Required = false },
                new() { Name = "Name", FieldPath = "name", Algorithm = MatchAlgorithm.FUZZY, Weight = 3.0, Required = true, Parameters = new Dictionary<string, object> { { "threshold", 0.8 } } }
            };
        }

        return config;
    }
}
