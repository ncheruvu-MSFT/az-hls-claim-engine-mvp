using System.Net.Http.Json;
using System.Text.Json;
using ClaimsPortal.BlazorWasm.Models;

namespace ClaimsPortal.BlazorWasm.Services;

/// <summary>
/// Service for persisting non-FHIR data to Azure Cosmos DB
/// Used for application-specific data that doesn't fit FHIR resources
/// </summary>
public class CosmosDbService
{
    private readonly HttpClient _httpClient;
    private readonly string _cosmosDbApiUrl;
    private readonly string _cosmosDbKey;
    private readonly string _databaseId;

    public CosmosDbService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _cosmosDbApiUrl = configuration["CosmosDb:ApiUrl"] ?? "https://claimsiq-cosmos.documents.azure.com:443/";
        _cosmosDbKey = configuration["CosmosDb:Key"] ?? "";
        _databaseId = configuration["CosmosDb:DatabaseId"] ?? "ClaimsIQDB";
    }

    /// <summary>
    /// Save benefit plan configuration to Cosmos DB
    /// </summary>
    public async Task<(bool Success, string Message)> SaveBenefitPlanAsync(BenefitPlan plan, string containerId = "BenefitPlans")
    {
        try
        {
            var document = new
            {
                id = plan.PlanId,
                planId = plan.PlanId,
                planName = plan.PlanName,
                planType = plan.PlanType,
                deductible = plan.Deductible,
                outOfPocketMax = plan.OutOfPocketMax,
                coinsurance = plan.Coinsurance,
                primaryCopay = plan.PrimaryCopay,
                specialistCopay = plan.SpecialistCopay,
                requiresPriorAuth = plan.RequiresPriorAuth,
                startDate = plan.StartDate,
                endDate = plan.EndDate,
                inNetworkProviders = plan.InNetworkProviders,
                partitionKey = plan.PlanType, // Partition by plan type
                _ts = DateTimeOffset.Now.ToUnixTimeSeconds()
            };

            var response = await PostToCosmosAsync(containerId, document);
            
            if (response.IsSuccessStatusCode)
            {
                return (true, "Plan saved successfully to Cosmos DB");
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                return (false, $"Cosmos DB error: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            return (false, $"Error saving to Cosmos DB: {ex.Message}");
        }
    }

    /// <summary>
    /// Get all benefit plans from Cosmos DB
    /// </summary>
    public async Task<List<BenefitPlan>> GetBenefitPlansAsync(string containerId = "BenefitPlans")
    {
        try
        {
            var query = "SELECT * FROM c";
            var response = await QueryCosmosAsync(containerId, query);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<CosmosQueryResponse>();
                var plans = result?.Documents?.Select(doc => MapToBenefitPlan(doc)).ToList() ?? new List<BenefitPlan>();
                return plans;
            }
            
            return new List<BenefitPlan>();
        }
        catch (Exception)
        {
            return new List<BenefitPlan>();
        }
    }

    /// <summary>
    /// Save account to Cosmos DB
    /// </summary>
    public async Task<(bool Success, string Message)> SaveAccountAsync(Account account, string containerId = "Accounts")
    {
        try
        {
            var document = new
            {
                id = account.AccountId,
                accountId = account.AccountId,
                accountName = account.AccountName,
                employerName = account.EmployerName,
                planName = account.PlanName,
                planId = account.PlanId,
                memberCount = account.MemberCount,
                status = account.Status,
                totalPremium = account.TotalPremium,
                effectiveDate = account.EffectiveDate,
                partitionKey = account.Status, // Partition by status
                _ts = DateTimeOffset.Now.ToUnixTimeSeconds()
            };

            var response = await PostToCosmosAsync(containerId, document);
            
            if (response.IsSuccessStatusCode)
            {
                return (true, "Account saved successfully");
            }
            else
            {
                return (false, $"Cosmos DB error: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            return (false, $"Error saving account: {ex.Message}");
        }
    }

    /// <summary>
    /// Get all accounts from Cosmos DB
    /// </summary>
    public async Task<List<Account>> GetAccountsAsync(string containerId = "Accounts")
    {
        try
        {
            var query = "SELECT * FROM c ORDER BY c.accountName";
            var response = await QueryCosmosAsync(containerId, query);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<CosmosQueryResponse>();
                var accounts = result?.Documents?.Select(doc => MapToAccount(doc)).ToList() ?? new List<Account>();
                return accounts;
            }
            
            return new List<Account>();
        }
        catch (Exception)
        {
            return new List<Account>();
        }
    }

    /// <summary>
    /// Save member to Cosmos DB
    /// </summary>
    public async Task<(bool Success, string Message)> SaveMemberAsync(AccountMember member, string containerId = "Members")
    {
        try
        {
            var document = new
            {
                id = member.MemberId,
                memberId = member.MemberId,
                accountId = member.AccountId,
                firstName = member.FirstName,
                lastName = member.LastName,
                relationship = member.Relationship,
                dateOfBirth = member.DateOfBirth,
                planName = member.PlanName,
                status = member.Status,
                partitionKey = member.AccountId, // Partition by account
                _ts = DateTimeOffset.Now.ToUnixTimeSeconds()
            };

            var response = await PostToCosmosAsync(containerId, document);
            
            if (response.IsSuccessStatusCode)
            {
                return (true, "Member saved successfully");
            }
            else
            {
                return (false, $"Cosmos DB error: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            return (false, $"Error saving member: {ex.Message}");
        }
    }

    /// <summary>
    /// Get members by account ID
    /// </summary>
    public async Task<List<AccountMember>> GetMembersByAccountAsync(string accountId, string containerId = "Members")
    {
        try
        {
            var query = $"SELECT * FROM c WHERE c.accountId = '{accountId}'";
            var response = await QueryCosmosAsync(containerId, query);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<CosmosQueryResponse>();
                var members = result?.Documents?.Select(doc => MapToMember(doc)).ToList() ?? new List<AccountMember>();
                return members;
            }
            
            return new List<AccountMember>();
        }
        catch (Exception)
        {
            return new List<AccountMember>();
        }
    }

    /// <summary>
    /// Delete document from Cosmos DB
    /// </summary>
    public async Task<(bool Success, string Message)> DeleteDocumentAsync(string containerId, string documentId, string partitionKey)
    {
        try
        {
            var url = $"{_cosmosDbApiUrl}dbs/{_databaseId}/colls/{containerId}/docs/{documentId}";
            var request = new HttpRequestMessage(HttpMethod.Delete, url);
            
            // Add Cosmos DB headers
            request.Headers.Add("x-ms-date", DateTime.UtcNow.ToString("R"));
            request.Headers.Add("x-ms-version", "2018-12-31");
            request.Headers.Add("x-ms-documentdb-partitionkey", $"[\"{partitionKey}\"]");
            request.Headers.Add("Authorization", GenerateAuthToken("DELETE", "docs", containerId));

            var response = await _httpClient.SendAsync(request);
            
            if (response.IsSuccessStatusCode)
            {
                return (true, "Document deleted successfully");
            }
            else
            {
                return (false, $"Delete failed: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            return (false, $"Error deleting document: {ex.Message}");
        }
    }

    /// <summary>
    /// Check if Cosmos DB is available
    /// </summary>
    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            var url = $"{_cosmosDbApiUrl}dbs";
            var response = await _httpClient.GetAsync(url);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    #region Private Helper Methods

    private async Task<HttpResponseMessage> PostToCosmosAsync(string containerId, object document)
    {
        var url = $"{_cosmosDbApiUrl}dbs/{_databaseId}/colls/{containerId}/docs";
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        
        var json = JsonSerializer.Serialize(document);
        request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        
        // Add Cosmos DB headers
        request.Headers.Add("x-ms-date", DateTime.UtcNow.ToString("R"));
        request.Headers.Add("x-ms-version", "2018-12-31");
        request.Headers.Add("Authorization", GenerateAuthToken("POST", "docs", containerId));

        return await _httpClient.SendAsync(request);
    }

    private async Task<HttpResponseMessage> QueryCosmosAsync(string containerId, string query)
    {
        var url = $"{_cosmosDbApiUrl}dbs/{_databaseId}/colls/{containerId}/docs";
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        
        var queryDoc = new { query, parameters = Array.Empty<object>() };
        var json = JsonSerializer.Serialize(queryDoc);
        request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/query+json");
        
        // Add Cosmos DB headers
        request.Headers.Add("x-ms-date", DateTime.UtcNow.ToString("R"));
        request.Headers.Add("x-ms-version", "2018-12-31");
        request.Headers.Add("x-ms-documentdb-isquery", "True");
        request.Headers.Add("Authorization", GenerateAuthToken("POST", "docs", containerId));

        return await _httpClient.SendAsync(request);
    }

    private string GenerateAuthToken(string verb, string resourceType, string resourceId)
    {
        // Simplified - actual implementation would use HMACSHA256
        // For production, use Azure SDK or proper authentication
        return $"type=master&ver=1.0&sig={_cosmosDbKey}";
    }

    private BenefitPlan MapToBenefitPlan(JsonElement doc)
    {
        return new BenefitPlan
        {
            PlanId = doc.GetProperty("planId").GetString() ?? "",
            PlanName = doc.GetProperty("planName").GetString() ?? "",
            PlanType = doc.GetProperty("planType").GetString() ?? "",
            Deductible = doc.GetProperty("deductible").GetDecimal(),
            OutOfPocketMax = doc.GetProperty("outOfPocketMax").GetDecimal(),
            Coinsurance = doc.GetProperty("coinsurance").GetInt32(),
            PrimaryCopay = (int)doc.GetProperty("primaryCopay").GetDecimal(),
            SpecialistCopay = (int)doc.GetProperty("specialistCopay").GetDecimal(),
            RequiresPriorAuth = doc.GetProperty("requiresPriorAuth").GetBoolean(),
            StartDate = doc.GetProperty("startDate").GetDateTime(),
            EndDate = doc.GetProperty("endDate").GetDateTime()
        };
    }

    private Account MapToAccount(JsonElement doc)
    {
        return new Account
        {
            AccountId = doc.GetProperty("accountId").GetString() ?? "",
            AccountName = doc.GetProperty("accountName").GetString() ?? "",
            EmployerName = doc.GetProperty("employerName").GetString() ?? "",
            PlanName = doc.GetProperty("planName").GetString() ?? "",
            PlanId = doc.GetProperty("planId").GetString() ?? "",
            MemberCount = doc.GetProperty("memberCount").GetInt32(),
            Status = doc.GetProperty("status").GetString() ?? "",
            TotalPremium = doc.GetProperty("totalPremium").GetDecimal(),
            EffectiveDate = doc.GetProperty("effectiveDate").GetDateTime()
        };
    }

    private AccountMember MapToMember(JsonElement doc)
    {
        return new AccountMember
        {
            MemberId = doc.GetProperty("memberId").GetString() ?? "",
            AccountId = doc.GetProperty("accountId").GetString() ?? "",
            FirstName = doc.GetProperty("firstName").GetString() ?? "",
            LastName = doc.GetProperty("lastName").GetString() ?? "",
            Relationship = doc.GetProperty("relationship").GetString() ?? "",
            DateOfBirth = doc.GetProperty("dateOfBirth").GetDateTime(),
            PlanName = doc.GetProperty("planName").GetString() ?? "",
            Status = doc.GetProperty("status").GetString() ?? ""
        };
    }

    #endregion

    private class CosmosQueryResponse
    {
        public List<JsonElement>? Documents { get; set; }
        public int Count { get; set; }
    }
}
