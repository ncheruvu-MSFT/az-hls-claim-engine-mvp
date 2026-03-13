
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace ClaimsRules.Api.Services
{
    public class FhirClient
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _config;
        private readonly string _endpoint;
        private readonly string _audience;
        private readonly bool _useMock;
        private readonly DefaultAzureCredential? _cred;

        public FhirClient(IHttpClientFactory factory, IConfiguration config)
        {
            _http = factory.CreateClient();
            _config = config;
            _endpoint = _config["FHIR:Endpoint"] ?? "http://localhost:5050";
            _audience = _config["FHIR:Audience"] ?? "https://azurehealthcareapis.com";
            _useMock = _config["USE_MOCK_SERVICES"] == "true" || _endpoint.Contains("localhost");
            
            if (!_useMock)
            {
                _cred = new DefaultAzureCredential();
            }
        }

        private async Task<string?> GetTokenAsync()
        {
            if (_useMock || _cred == null) return null;
            
            var ctx = new TokenRequestContext(new[] { $"{_audience}/.default" });
            var token = await _cred.GetTokenAsync(ctx);
            return token.Token;
        }

        public async Task<HttpResponseMessage> PostResourceAsync(object resource)
        {
            var token = await GetTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            var content = new StringContent(JsonConvert.SerializeObject(resource), Encoding.UTF8, "application/fhir+json");
            var resourceType = resource.GetType().Name;
            var url = $"{_endpoint}/{resourceType}";
            
            try
            {
                return await _http.PostAsync(url, content);
            }
            catch (HttpRequestException)
            {
                // Return mock success for local dev if FHIR server not running
                if (_useMock)
                {
                    return new HttpResponseMessage(System.Net.HttpStatusCode.Created);
                }
                throw;
            }
        }

        public async Task<string> CreateAsync(string resourceType, string jsonContent)
        {
            var token = await GetTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/fhir+json");
            var url = $"{_endpoint}/{resourceType}";
            
            try
            {
                var response = await _http.PostAsync(url, content);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                throw new HttpRequestException($"FHIR Create failed: {response.StatusCode}");
            }
            catch (HttpRequestException) when (_useMock)
            {
                // Return mock response for local dev
                return $"{{\"resourceType\":\"{resourceType}\",\"id\":\"mock-{Guid.NewGuid()}\",\"meta\":{{\"versionId\":\"1\"}}}}";
            }
        }

        public async Task<string> SearchAsync(string resourceType, string queryParams)
        {
            var token = await GetTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            
            var url = $"{_endpoint}/{resourceType}?{queryParams}";
            
            try
            {
                var response = await _http.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                throw new HttpRequestException($"FHIR Search failed: {response.StatusCode}");
            }
            catch (HttpRequestException) when (_useMock)
            {
                // Return empty bundle for local dev
                return $"{{\"resourceType\":\"Bundle\",\"type\":\"searchset\",\"total\":0,\"entry\":[]}}";
            }
        }

        public async Task<string> GetByIdAsync(string resourceType, string resourceId)
        {
            var token = await GetTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            
            var url = $"{_endpoint}/{resourceType}/{resourceId}";
            
            try
            {
                var response = await _http.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                throw new HttpRequestException($"FHIR Get failed: {response.StatusCode}");
            }
            catch (HttpRequestException) when (_useMock)
            {
                // Return mock resource for local dev
                return $"{{\"resourceType\":\"{resourceType}\",\"id\":\"{resourceId}\",\"meta\":{{\"versionId\":\"1\"}}}}";
            }
        }
    }
}
