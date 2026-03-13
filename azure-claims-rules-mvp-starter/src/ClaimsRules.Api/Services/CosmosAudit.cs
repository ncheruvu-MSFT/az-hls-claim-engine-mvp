
using System.Threading.Tasks;
using System.Collections.Generic;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.Cosmos;

namespace ClaimsRules.Api.Services
{
    public class CosmosAudit
    {
        private readonly CosmosClient? _client;
        private readonly Container? _container;
        private readonly bool _useMock;
        private readonly List<dynamic> _mockStore = new();

        public CosmosAudit(IConfiguration config)
        {
            _useMock = config["USE_MOCK_SERVICES"] == "true";
            
            if (!_useMock)
            {
                var endpoint = config["Cosmos:AccountEndpoint"]!;
                var dbName = config["Cosmos:Database"]!;
                var containerName = config["Cosmos:AuditContainer"]!;
                _client = new CosmosClient(endpoint, new DefaultAzureCredential());
                _container = _client.GetContainer(dbName, containerName);
            }
        }

        public async Task<ItemResponse<dynamic>?> WriteAsync(dynamic doc)
        {
            if (_useMock)
            {
                _mockStore.Add(doc);
                return null; // Mock mode - no actual Cosmos response
            }
            
            return await _container!.UpsertItemAsync(doc);
        }
        
        public async Task UpsertAsync<T>(T item) where T : class
        {
            if (_useMock)
            {
                _mockStore.Add(item!);
                return;
            }
            
            await _container!.UpsertItemAsync(item);
        }
        
        public async Task WriteAuditAsync(string type, object data)
        {
            var doc = new
            {
                id = Guid.NewGuid().ToString(),
                type = type,
                data = data,
                createdAt = DateTime.UtcNow
            };
            
            if (_useMock)
            {
                _mockStore.Add(doc);
                return;
            }
            
            await _container!.UpsertItemAsync(doc);
        }
        
        public async Task<List<dynamic>> QueryAuditAsync(string queryText, int maxItems = 100)
        {
            if (_useMock)
            {
                // Mock implementation - return filtered mockStore
                return _mockStore.Take(maxItems).ToList();
            }
            
            var query = _container!.GetItemQueryIterator<dynamic>(
                new QueryDefinition(queryText).WithParameter("@maxItems", maxItems));
            var results = new List<dynamic>();
            
            while (query.HasMoreResults && results.Count < maxItems)
            {
                var response = await query.ReadNextAsync();
                results.AddRange(response);
            }
            
            return results.Take(maxItems).ToList();
        }
        
        public async Task<IEnumerable<T>> QueryAsync<T>(string queryText)
        {
            if (_useMock)
            {
                // Mock implementation - return empty list
                return new List<T>();
            }
            
            var query = _container!.GetItemQueryIterator<T>(new QueryDefinition(queryText));
            var results = new List<T>();
            
            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();
                results.AddRange(response);
            }
            
            return results;
        }
        
        public List<dynamic> GetMockStore() => _mockStore;
    }
}
