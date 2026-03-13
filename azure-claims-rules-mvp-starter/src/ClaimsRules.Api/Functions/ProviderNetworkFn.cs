using System.Net;
using System.Text.Json;
using ClaimsRules.Api.Models;
using ClaimsRules.Api.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace ClaimsRules.Api.Functions;

public class ProviderNetworkFn
{
    private readonly ILogger<ProviderNetworkFn> _logger;
    private readonly ProviderNetworkService _providerService;

    public ProviderNetworkFn(
        ILogger<ProviderNetworkFn> logger,
        ProviderNetworkService providerService)
    {
        _logger = logger;
        _providerService = providerService;
    }

    [Function("SearchProviders")]
    public async Task<HttpResponseData> SearchProviders(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "providers/search")] HttpRequestData req)
    {
        _logger.LogInformation("Processing provider search request");

        try
        {
            var requestBody = await req.ReadAsStringAsync() ?? string.Empty;
            var searchRequest = JsonSerializer.Deserialize<ProviderSearchRequest>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new ProviderSearchRequest();

            var result = await _providerService.SearchProvidersAsync(searchRequest);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(result);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching providers");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = ex.Message });
            return errorResponse;
        }
    }

    [Function("GetProviderByNPI")]
    public async Task<HttpResponseData> GetProviderByNPI(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "providers/{npi}")] HttpRequestData req,
        string npi)
    {
        _logger.LogInformation($"Getting provider with NPI: {npi}");

        try
        {
            var provider = await _providerService.GetProviderByNPIAsync(npi);

            if (provider == null)
            {
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteAsJsonAsync(new { error = $"Provider with NPI {npi} not found" });
                return notFoundResponse;
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(provider);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting provider {npi}");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = ex.Message });
            return errorResponse;
        }
    }

    [Function("AddProvider")]
    public async Task<HttpResponseData> AddProvider(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "providers")] HttpRequestData req)
    {
        _logger.LogInformation("Adding new provider to network");

        try
        {
            var requestBody = await req.ReadAsStringAsync() ?? string.Empty;
            var provider = JsonSerializer.Deserialize<Provider>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (provider == null)
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteAsJsonAsync(new { error = "Invalid provider data" });
                return badRequest;
            }

            var result = await _providerService.AddProviderToNetworkAsync(provider);

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(result);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding provider");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = ex.Message });
            return errorResponse;
        }
    }

    [Function("UpdateProvider")]
    public async Task<HttpResponseData> UpdateProvider(
        [HttpTrigger(AuthorizationLevel.Function, "put", Route = "providers/{npi}")] HttpRequestData req,
        string npi)
    {
        _logger.LogInformation($"Updating provider with NPI: {npi}");

        try
        {
            var requestBody = await req.ReadAsStringAsync() ?? string.Empty;
            var provider = JsonSerializer.Deserialize<Provider>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (provider == null || provider.Npi != npi)
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteAsJsonAsync(new { error = "Invalid provider data or NPI mismatch" });
                return badRequest;
            }

            var result = await _providerService.UpdateProviderAsync(provider);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(result);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating provider {npi}");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = ex.Message });
            return errorResponse;
        }
    }

    [Function("UpdateProviderTier")]
    public async Task<HttpResponseData> UpdateProviderTier(
        [HttpTrigger(AuthorizationLevel.Function, "put", Route = "providers/{npi}/tier")] HttpRequestData req,
        string npi)
    {
        _logger.LogInformation($"Updating network tier for provider: {npi}");

        try
        {
            var requestBody = await req.ReadAsStringAsync() ?? string.Empty;
            var tierUpdate = JsonSerializer.Deserialize<Dictionary<string, string>>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (tierUpdate == null || !tierUpdate.ContainsKey("networkTier"))
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteAsJsonAsync(new { error = "Network tier is required" });
                return badRequest;
            }

            var provider = await _providerService.GetProviderByNPIAsync(npi);
            if (provider == null)
            {
                var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                await notFound.WriteAsJsonAsync(new { error = $"Provider with NPI {npi} not found" });
                return notFound;
            }

            provider.NetworkIds = new List<string> { tierUpdate["networkTier"] };
            var result = await _providerService.UpdateProviderAsync(provider);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(result);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating tier for provider {npi}");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = ex.Message });
            return errorResponse;
        }
    }

    [Function("RemoveProvider")]
    public async Task<HttpResponseData> RemoveProvider(
        [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "providers/{npi}")] HttpRequestData req,
        string npi)
    {
        _logger.LogInformation($"Removing provider with NPI: {npi}");

        try
        {
            var success = await _providerService.RemoveProviderFromNetworkAsync(npi);

            if (!success)
            {
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteAsJsonAsync(new { error = $"Provider with NPI {npi} not found" });
                return notFoundResponse;
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new { message = $"Provider {npi} removed from network" });
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error removing provider {npi}");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = ex.Message });
            return errorResponse;
        }
    }

    [Function("GetContracts")]
    public async Task<HttpResponseData> GetContracts(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "contracts")] HttpRequestData req)
    {
        _logger.LogInformation("Getting provider contracts");

        try
        {
            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            var providerNpi = query["providerNpi"];
            var status = query["status"];

            var contracts = await _providerService.GetContractsAsync(providerNpi, status);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(contracts);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting contracts");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = ex.Message });
            return errorResponse;
        }
    }

    [Function("GetContractById")]
    public async Task<HttpResponseData> GetContractById(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "contracts/{contractId}")] HttpRequestData req,
        string contractId)
    {
        _logger.LogInformation($"Getting contract: {contractId}");

        try
        {
            var contract = await _providerService.GetContractByIdAsync(contractId);

            if (contract == null)
            {
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteAsJsonAsync(new { error = $"Contract {contractId} not found" });
                return notFoundResponse;
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(contract);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting contract {contractId}");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = ex.Message });
            return errorResponse;
        }
    }

    [Function("CreateContract")]
    public async Task<HttpResponseData> CreateContract(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "contracts")] HttpRequestData req)
    {
        _logger.LogInformation("Creating new provider contract");

        try
        {
            var requestBody = await req.ReadAsStringAsync() ?? string.Empty;
            var contract = JsonSerializer.Deserialize<ProviderContract>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (contract == null)
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteAsJsonAsync(new { error = "Invalid contract data" });
                return badRequest;
            }

            var result = await _providerService.CreateContractAsync(contract);

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(result);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating contract");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = ex.Message });
            return errorResponse;
        }
    }

    [Function("UpdateContract")]
    public async Task<HttpResponseData> UpdateContract(
        [HttpTrigger(AuthorizationLevel.Function, "put", Route = "contracts/{contractId}")] HttpRequestData req,
        string contractId)
    {
        _logger.LogInformation($"Updating contract: {contractId}");

        try
        {
            var requestBody = await req.ReadAsStringAsync() ?? string.Empty;
            var contract = JsonSerializer.Deserialize<ProviderContract>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (contract == null || contract.Id != contractId)
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteAsJsonAsync(new { error = "Invalid contract data or ID mismatch" });
                return badRequest;
            }

            var result = await _providerService.UpdateContractAsync(contract);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(result);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating contract {contractId}");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = ex.Message });
            return errorResponse;
        }
    }

    [Function("GetNetworkTiers")]
    public async Task<HttpResponseData> GetNetworkTiers(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "network-tiers")] HttpRequestData req)
    {
        _logger.LogInformation("Getting network tiers");

        try
        {
            var tiers = await _providerService.GetNetworkTiersAsync();

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(tiers);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting network tiers");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = ex.Message });
            return errorResponse;
        }
    }
}
