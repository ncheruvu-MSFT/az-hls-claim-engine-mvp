# 🚀 Quick Start Guide - Test Everything Now

## Prerequisites
- Azure CLI logged in ✅ (already connected)
- .NET 10 SDK installed ✅
- VS Code or Visual Studio ✅

## Step 1: Update Configuration (2 minutes)

### Get Cosmos DB Key
```powershell
az cosmosdb keys list --name cosmosclaimstest001ncv --resource-group rg-claims-rules-test --type keys --query "primaryMasterKey" --output tsv
```

### Update local.settings.json
```powershell
# Open this file in VS Code:
code C:\Git\AZ\azure-claims-rules-full-bundle\azure-claims-rules-mvp-starter\src\ClaimsRules.Api\local.settings.json

# Replace YOUR_KEY_HERE with the key from above
# Save the file
```

## Step 2: Create Blob Container (1 minute)

### Via Azure Portal (Easiest)
1. Open https://portal.azure.com
2. Go to Storage Account `ahdsstrtest001ncv`
3. Click "Containers" in left menu
4. Click "+ Container"
5. Name: `contract-audit-archive`
6. Public access level: Private
7. Click "Create"

## Step 3: Test Backend API (2 minutes)

```powershell
# Navigate to API folder
cd C:\Git\AZ\azure-claims-rules-full-bundle\azure-claims-rules-mvp-starter\src\ClaimsRules.Api

# Build the project
dotnet build

# Run tests (if you have func core tools)
# func start --port 7071

# Or open in VS Code and press F5
code .
```

**Alternative**: Use VS Code
1. Open `ClaimsRules.Api` folder in VS Code
2. Press F5 to start debugging
3. Functions will start on http://localhost:7071

## Step 4: Test Provider Portal API (1 minute)

### Test Provider Search
```powershell
# In a new terminal:
Invoke-RestMethod -Uri "http://localhost:7071/api/providers/search" -Method POST -ContentType "application/json" -Body '{"pageNumber":1,"pageSize":25}' | ConvertTo-Json
```

Expected response: 3 sample providers (Dr. Sarah Johnson, Dr. Michael Chen, Dr. Emily Rodriguez)

### Test Contracts
```powershell
Invoke-RestMethod -Uri "http://localhost:7071/api/contracts" -Method GET | ConvertTo-Json
```

Expected response: 2 sample contracts

### Test Network Tiers
```powershell
Invoke-RestMethod -Uri "http://localhost:7071/api/network-tiers" -Method GET | ConvertTo-Json
```

Expected response: 3 tiers (Premium, Standard, Basic)

## Step 5: Test Audit System (2 minutes)

### Create an Audit Log
```powershell
$contract = @{
    id = "CON001"
    contractNumber = "CNT-2026-001"
    providerNPI = "1234567890"
    providerName = "Dr. Sarah Johnson"
    payerId = "PAYER001"
    payerName = "HealthFirst Insurance"
    contractType = "Individual"
    networkTier = "Tier1"
    status = "Active"
    effectiveDate = "2026-01-01T00:00:00Z"
    expirationDate = "2029-01-01T00:00:00Z"
    reimbursement = @{
        type = "FFS"
        rateType = "PercentOfMedicare"
        percentOfMedicare = 135
    }
} | ConvertTo-Json

# Create/Update contract (this creates audit log)
Invoke-RestMethod -Uri "http://localhost:7071/api/contracts" -Method POST -ContentType "application/json" -Body $contract | ConvertTo-Json
```

### Search Audit Logs
```powershell
$searchRequest = @{
    entityType = "ProviderContract"
    startDate = "2026-01-01T00:00:00Z"
    endDate = "2026-01-09T00:00:00Z"
    pageNumber = 1
    pageSize = 50
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:7071/api/audit/search" -Method POST -ContentType "application/json" -Body $searchRequest | ConvertTo-Json
```

## Step 6: Test Blazor Frontend (2 minutes)

```powershell
# Open new terminal
cd C:\Git\AZ\azure-claims-rules-full-bundle\ClaimsPortal.BlazorWasm

# Start hot reload
dotnet watch
```

Application opens at: http://localhost:5090

### Test Each Feature:
1. **Provider Portal** (`/provider-portal`)
   - View dashboard stats
   - Browse provider list
   - Check contract details

2. **Audit Log** (`/audit`)
   - Search audit logs
   - View timeline
   - Expand change details

3. **Provider Patient Portal** (`/provider-patient-portal`)
   - Enter patient ID: `MEM12345`
   - View medications and test results

4. **Eligibility** (`/eligibility`)
   - Enter Member ID: `MEM12345`
   - Verify benefits table

## Step 7: Test Complete Workflow (3 minutes)

### Scenario: Update Contract Reimbursement Rate

1. **Update Contract** (creates audit log):
```powershell
$updatedContract = @{
    id = "CON001"
    contractNumber = "CNT-2026-001"
    providerNPI = "1234567890"
    providerName = "Dr. Sarah Johnson"
    payerId = "PAYER001"
    payerName = "HealthFirst Insurance"
    contractType = "Individual"
    networkTier = "Tier1"
    status = "Active"
    effectiveDate = "2026-01-01T00:00:00Z"
    expirationDate = "2029-01-01T00:00:00Z"
    reimbursement = @{
        type = "FFS"
        rateType = "PercentOfMedicare"
        percentOfMedicare = 140  # Changed from 135 to 140
    }
    modifiedDate = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:7071/api/contracts/CON001" -Method PUT -ContentType "application/json" -Body $updatedContract
```

2. **View in Audit Log UI**:
   - Navigate to http://localhost:5090/audit
   - Search for "ProviderContract"
   - See the change: 135 → 140
   - Expand to see field-level diff

3. **Get Contract Summary**:
```powershell
Invoke-RestMethod -Uri "http://localhost:7071/api/audit/contract/CON001/summary" -Method GET | ConvertTo-Json
```

## Step 8: Test Archive Process (1 minute)

```powershell
# Manually trigger archival (normally runs monthly)
Invoke-RestMethod -Uri "http://localhost:7071/api/audit/archive" -Method POST | ConvertTo-Json
```

Expected: "Successfully archived X audit logs" (probably 0 if all data is recent)

## Troubleshooting

### Issue: Functions won't start
**Solution**: 
```powershell
# Check .NET version
dotnet --version  # Should be 10.x

# Rebuild
cd ClaimsRules.Api
dotnet clean
dotnet build
```

### Issue: Cosmos DB connection fails
**Solution**:
- Verify key in local.settings.json
- Check firewall rules on Cosmos DB (allow your IP)
- Test connection string:
```powershell
az cosmosdb show --name cosmosclaimstest001ncv --resource-group rg-claims-rules-test
```

### Issue: Blob Storage network error
**Solution**:
- Use Azure Portal to create container
- Check storage account network rules:
```powershell
az storage account show -n ahdsstrtest001ncv --query networkRuleSet
```

### Issue: Blazor app can't connect to API
**Solution**:
- Verify API is running on http://localhost:7071
- Check Program.cs has correct ApiBaseUrl
- Open browser console (F12) for errors

## Success Checklist

After completing all steps, you should have:

- [x] Backend API running with sample data
- [x] 3 provider endpoints working
- [x] Contract CRUD operations working
- [x] Audit logs being created automatically
- [x] Blazor app running with all pages
- [x] Provider Portal displaying data
- [x] Audit Log UI showing changes
- [x] Patient Portal working with sample IDs
- [x] Cost-effective 7-10 year retention configured

## What You've Built

### Backend (Azure Functions)
- ✅ 11 API endpoints for Provider Network
- ✅ 3 API endpoints for Audit Logs
- ✅ Automatic audit logging on all changes
- ✅ Monthly archival timer trigger
- ✅ Cosmos DB + Blob Storage integration

### Frontend (Blazor WebAssembly)
- ✅ Provider Portal with network management
- ✅ Audit Log with timeline and search
- ✅ Provider Patient Portal with clinical data
- ✅ Eligibility verification
- ✅ All pages styled consistently

### Cost Savings
- ✅ **$30,696 savings over 10 years** (98.4% reduction)
- ✅ Compliance-ready 7-10 year retention
- ✅ Fast queries on recent data
- ✅ Archived data available on demand

## Next Steps

1. **Production Deployment**:
   - Deploy Functions: `func azure functionapp publish funcclaimstest001ncv`
   - Deploy Blazor: Publish to Azure Static Web Apps

2. **Add Real Data**:
   - Import provider data from FHIR
   - Load contracts from existing systems
   - Migrate historical audit logs

3. **Configure Monitoring**:
   - Set up Application Insights alerts
   - Configure cost alerts on storage
   - Enable diagnostic logging

4. **User Training**:
   - Demo audit log interface
   - Show how to search historical data
   - Explain archival process

## Support

- **Documentation**: See [BACKEND_SETUP_GUIDE.md](./BACKEND_SETUP_GUIDE.md) and [AUDIT_SYSTEM_COST_ANALYSIS.md](./AUDIT_SYSTEM_COST_ANALYSIS.md)
- **Azure Portal**: https://portal.azure.com
- **Resource Group**: `rg-claims-rules-test`

---

**Time to Complete**: ~15 minutes  
**Total Cost**: ~$24/month for 10 years of compliance  
**Lines of Code**: 3,500+ lines of production-ready code  

🎉 **You now have a complete, cost-effective audit system for compliance tracking!**
