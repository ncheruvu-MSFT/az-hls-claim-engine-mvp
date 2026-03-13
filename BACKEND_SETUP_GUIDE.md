# Backend API Setup Guide for Provider Portal

## Overview
This guide explains the backend API endpoints created for the Provider Portal, including Azure FHIR integration and Cosmos DB setup.

## ✅ What's Been Created

### 1. API Endpoints (Azure Functions)
**File:** `ClaimsRules.Api/Functions/ProviderNetworkFn.cs`

#### Provider Search & Management
- `POST /api/providers/search` - Search providers with filters
- `GET /api/providers/{npi}` - Get provider by NPI
- `POST /api/providers` - Add new provider to network
- `PUT /api/providers/{npi}` - Update provider details
- `PUT /api/providers/{npi}/tier` - Update network tier assignment
- `DELETE /api/providers/{npi}` - Remove provider from network

#### Contract Management
- `GET /api/contracts` - Get all contracts (optional filters: providerNpi, status)
- `GET /api/contracts/{contractId}` - Get contract by ID
- `POST /api/contracts` - Create new provider contract
- `PUT /api/contracts/{contractId}` - Update contract

#### Network Tiers
- `GET /api/network-tiers` - Get all network tier definitions

### 2. Service Layer
**File:** `ClaimsRules.Api/Services/ProviderNetworkService.cs`

Contains business logic for:
- Provider search with pagination
- Provider CRUD operations
- Contract management
- Network tier management
- **Sample data fallback** (works without database for demo)

### 3. Data Models
**File:** `ClaimsRules.Api/Models/ProviderNetwork.cs`

Extended with:
- `ProviderContract` - Contract terms and reimbursement
- `ReimbursementModel` - FFS, Capitation, Value-Based models
- `NetworkTier` - Tier definitions with member cost sharing
- `ProviderSearchRequest` - Search criteria
- `ProviderSearchResponse` - Paginated results

### 4. Configuration
**File:** `ClaimsRules.Api/local.settings.json`

Added Cosmos DB settings:
```json
{
  "CosmosDb__ConnectionString": "...",
  "CosmosDb__DatabaseName": "ClaimsIQ"
}
```

## 🚀 Quick Start (Demo Mode)

The API works **immediately** with sample data - no database setup required!

### Test the API
```powershell
cd C:\Git\AZ\azure-claims-rules-full-bundle\azure-claims-rules-mvp-starter\src\ClaimsRules.Api
func start
```

### Sample API Calls

#### Search Providers
```powershell
curl -X POST http://localhost:7071/api/providers/search `
  -H "Content-Type: application/json" `
  -d '{
    "searchTerm": "Johnson",
    "specialty": "Family Medicine",
    "pageNumber": 1,
    "pageSize": 25
  }'
```

#### Get Provider by NPI
```powershell
curl http://localhost:7071/api/providers/1234567890
```

#### Get Contracts
```powershell
curl http://localhost:7071/api/contracts
```

#### Get Network Tiers
```powershell
curl http://localhost:7071/api/network-tiers
```

## 📦 Sample Data Included

### Sample Providers (3)
1. **Dr. Sarah Johnson** (NPI: 1234567890)
   - Specialty: Family Medicine
   - Tier: Tier1
   - Quality Score: 95
   
2. **Dr. Michael Chen** (NPI: 2345678901)
   - Specialty: Cardiology
   - Tier: Tier1
   - Quality Score: 92

3. **Dr. Emily Rodriguez** (NPI: 3456789012)
   - Specialty: Orthopedics
   - Tier: Tier2
   - Quality Score: 88

### Sample Contracts (2)
- Individual FFS contracts with 125-130% of Medicare rates
- Active status with credentialing

### Sample Network Tiers (3)
- **Tier1 (Premium)**: $25 copay, 10% coinsurance, 130% reimbursement
- **Tier2 (Standard)**: $35 copay, 20% coinsurance, 115% reimbursement
- **Tier3 (Basic)**: $50 copay, 30% coinsurance, 100% reimbursement

## 🗄️ Cosmos DB Setup (Production)

### Step 1: Create Cosmos DB Account
```powershell
az cosmosdb create `
  --name claimsiq-cosmosdb `
  --resource-group claimsiq-rg `
  --locations regionName=WestUS
```

### Step 2: Create Database and Containers
```powershell
# Create database
az cosmosdb sql database create `
  --account-name claimsiq-cosmosdb `
  --resource-group claimsiq-rg `
  --name ClaimsIQ

# Create Providers container
az cosmosdb sql container create `
  --account-name claimsiq-cosmosdb `
  --resource-group claimsiq-rg `
  --database-name ClaimsIQ `
  --name Providers `
  --partition-key-path "/npi" `
  --throughput 400

# Create Contracts container
az cosmosdb sql container create `
  --account-name claimsiq-cosmosdb `
  --resource-group claimsiq-rg `
  --database-name ClaimsIQ `
  --name Contracts `
  --partition-key-path "/id" `
  --throughput 400

# Create NetworkTiers container
az cosmosdb sql container create `
  --account-name claimsiq-cosmosdb `
  --resource-group claimsiq-rg `
  --database-name ClaimsIQ `
  --name NetworkTiers `
  --partition-key-path "/id" `
  --throughput 400
```

### Step 3: Get Connection String
```powershell
az cosmosdb keys list `
  --name claimsiq-cosmosdb `
  --resource-group claimsiq-rg `
  --type connection-strings `
  --query "connectionStrings[0].connectionString" `
  --output tsv
```

### Step 4: Update local.settings.json
Replace the placeholder connection string with your actual connection string:
```json
{
  "CosmosDb__ConnectionString": "AccountEndpoint=https://YOUR-ACCOUNT.documents.azure.com:443/;AccountKey=YOUR-KEY=="
}
```

## 🔧 Azure FHIR Setup (Production)

### Step 1: Create Azure Health Data Services Workspace
```powershell
az healthcareapis workspace create `
  --name claimsiq-fhir-workspace `
  --resource-group claimsiq-rg `
  --location westus
```

### Step 2: Create FHIR Service
```powershell
az healthcareapis fhir-service create `
  --resource-group claimsiq-rg `
  --workspace-name claimsiq-fhir-workspace `
  --name claimsiq-fhir `
  --kind fhir-R4 `
  --location westus
```

### Step 3: Configure Authentication
```powershell
# Create service principal
$sp = az ad sp create-for-rbac --name "ClaimsIQ-FHIR-SP" | ConvertFrom-Json

# Assign FHIR Data Contributor role
az role assignment create `
  --assignee $sp.appId `
  --role "FHIR Data Contributor" `
  --scope "/subscriptions/{subscription-id}/resourceGroups/claimsiq-rg/providers/Microsoft.HealthcareApis/workspaces/claimsiq-fhir-workspace/fhirservices/claimsiq-fhir"
```

### Step 4: Upload Sample FHIR Data

#### Create Practitioner Resources
```powershell
# POST to https://claimsiq-fhir.azurehealthcareapis.com/Practitioner
{
  "resourceType": "Practitioner",
  "id": "practitioner-1234567890",
  "identifier": [
    {
      "system": "http://hl7.org/fhir/sid/us-npi",
      "value": "1234567890"
    }
  ],
  "name": [
    {
      "family": "Johnson",
      "given": ["Sarah"],
      "prefix": ["Dr."]
    }
  ],
  "telecom": [
    {
      "system": "phone",
      "value": "(555) 123-4567"
    }
  ],
  "address": [
    {
      "line": ["123 Main Street"],
      "city": "Los Angeles",
      "state": "CA",
      "postalCode": "90001"
    }
  ],
  "qualification": [
    {
      "code": {
        "coding": [
          {
            "system": "http://nucc.org/provider-taxonomy",
            "code": "207Q00000X",
            "display": "Family Medicine"
          }
        ]
      }
    }
  ]
}
```

## 🔌 Connect Blazor Frontend to API

### Update MemberPortalService or Create ProviderPortalService

**File:** `ClaimsPortal.BlazorWasm/Services/ProviderPortalService.cs`

```csharp
public class ProviderPortalService
{
    private readonly HttpClient _http;
    private readonly string _apiBaseUrl = "http://localhost:7071/api";

    public ProviderPortalService(HttpClient http)
    {
        _http = http;
    }

    public async Task<ProviderSearchResponse> SearchProvidersAsync(ProviderSearchRequest request)
    {
        var response = await _http.PostAsJsonAsync($"{_apiBaseUrl}/providers/search", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProviderSearchResponse>() ?? new();
    }

    public async Task<List<ProviderContract>> GetContractsAsync(string? providerNpi = null)
    {
        var url = $"{_apiBaseUrl}/contracts";
        if (!string.IsNullOrEmpty(providerNpi))
            url += $"?providerNpi={providerNpi}";
            
        return await _http.GetFromJsonAsync<List<ProviderContract>>(url) ?? new();
    }

    public async Task<List<NetworkTier>> GetNetworkTiersAsync()
    {
        return await _http.GetFromJsonAsync<List<NetworkTier>>($"{_apiBaseUrl}/network-tiers") ?? new();
    }
}
```

## 📝 API Usage Examples

### Add Provider to Network
```csharp
var newProvider = new Provider
{
    Npi = "9876543210",
    Name = "Dr. John Doe",
    ProviderType = "Physician",
    Specialties = new List<string> { "Internal Medicine" },
    NetworkIds = new List<string> { "TIER1" },
    IsActive = true,
    AcceptingNewPatients = true
};

var response = await _http.PostAsJsonAsync("/api/providers", newProvider);
```

### Update Network Tier
```csharp
var tierUpdate = new Dictionary<string, string>
{
    { "networkTier", "TIER2" }
};

await _http.PutAsJsonAsync("/api/providers/1234567890/tier", tierUpdate);
```

### Create Contract
```csharp
var contract = new ProviderContract
{
    ContractNumber = "CNT-2026-001",
    ProviderNPI = "1234567890",
    ProviderName = "Dr. Sarah Johnson",
    PayerId = "PAYER001",
    PayerName = "HealthFirst Insurance",
    ContractType = "Individual",
    Status = "Active",
    EffectiveDate = DateTime.UtcNow,
    ExpirationDate = DateTime.UtcNow.AddYears(3),
    Reimbursement = new ReimbursementModel
    {
        Type = "FFS",
        RateType = "PercentOfMedicare",
        PercentOfMedicare = 125m
    }
};

await _http.PostAsJsonAsync("/api/contracts", contract);
```

## 🎯 Next Steps

### Immediate (Works Now)
1. ✅ Start Azure Functions: `func start`
2. ✅ Test with sample data using curl or Postman
3. ✅ Update Provider Portal UI to call APIs

### Production Setup
1. Create Cosmos DB account and containers
2. Update connection strings in configuration
3. Create Azure FHIR service
4. Upload synthetic provider data to FHIR
5. Configure authentication for FHIR access
6. Deploy Azure Functions to Azure
7. Update Blazor app with production API URLs

## 📋 Configuration Checklist

- [ ] Cosmos DB connection string configured
- [ ] Cosmos DB containers created (Providers, Contracts, NetworkTiers)
- [ ] Azure FHIR service created
- [ ] FHIR service authentication configured
- [ ] Sample FHIR data uploaded
- [ ] Azure Functions deployed
- [ ] Blazor app connected to backend APIs
- [ ] CORS configured for Blazor app origin

## 🐛 Troubleshooting

### API Returns Sample Data
**Cause:** Cosmos DB not configured or containers don't exist  
**Solution:** Service automatically falls back to sample data for demo purposes

### FHIR 403 Forbidden
**Cause:** Authentication not configured  
**Solution:** Create service principal and assign FHIR Data Contributor role

### Function Not Found
**Cause:** Functions runtime not started  
**Solution:** Run `func start` in ClaimsRules.Api directory

## 📖 Additional Resources

- [Azure Cosmos DB Documentation](https://docs.microsoft.com/en-us/azure/cosmos-db/)
- [Azure Health Data Services](https://docs.microsoft.com/en-us/azure/healthcare-apis/)
- [Azure Functions HTTP Trigger](https://docs.microsoft.com/en-us/azure/azure-functions/functions-bindings-http-webhook-trigger)
- [FHIR R4 Specification](https://www.hl7.org/fhir/R4/)

---

**Note:** The API works immediately with sample data, so you can start testing the Provider Portal UI right away without any database setup!
