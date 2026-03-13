# 🔐 Identity-Based Authentication Setup

## Overview
The application now uses **Azure Identity** for authentication instead of connection strings/keys:
- **Local Development**: Uses your Azure CLI identity token (`az login`)
- **Production (Azure)**: Uses Managed Identity (no keys needed)

## ✅ What's Been Configured

### 1. Code Updates
All services now use `DefaultAzureCredential`:
- ✅ **AuditService.cs** - Cosmos DB + Blob Storage
- ✅ **CosmosAudit.cs** - Cosmos DB
- ✅ **FhirClient.cs** - Azure Health Data Services FHIR

### 2. Configuration Updates
**local.settings.json** now uses endpoints only (no keys):
```json
{
  "Cosmos__AccountEndpoint": "https://cosmosclaimstest001ncv.documents.azure.com:443/",
  "Cosmos__Database": "claims",
  "Cosmos__AuditContainer": "audit",
  "BlobStorage__AccountName": "ahdsstrtest001ncv"
}
```

### 3. Azure RBAC Assignments
✅ **Your Identity** (da3db46c-39d9-460f-9b16-021688c98898):
- **Cosmos DB Built-in Data Contributor** - Read/write access to Cosmos DB
- **Storage Blob Data Contributor** - Read/write access to Blob Storage
- **IP Address** (98.118.154.232) - Added to storage firewall

## 🚀 Local Development Setup

### Prerequisites
```powershell
# 1. Login to Azure CLI
az login

# 2. Set correct subscription
az account set --subscription "64e1939f-6460-4656-ad75-dcc277b155f1"

# 3. Verify your identity
az account show --query user.name
```

### Test Authentication
```powershell
# Test Cosmos DB access
az cosmosdb sql database show --account-name cosmosclaimstest001ncv --resource-group rg-claims-rules-test --name claims

# Test your role assignments
az role assignment list --assignee da3db46c-39d9-460f-9b16-021688c98898 --resource-group rg-claims-rules-test --output table
```

### Run Functions Locally
```powershell
cd C:\Git\AZ\azure-claims-rules-full-bundle\azure-claims-rules-mvp-starter\src\ClaimsRules.Api

# Build
dotnet build

# Run (DefaultAzureCredential will automatically use your Azure CLI token)
func start
# OR press F5 in VS Code
```

## 🔒 Production Deployment Setup

### Step 1: Enable Managed Identity on Function App
```powershell
# Enable System-Assigned Managed Identity
az functionapp identity assign --name funcclaimstest001ncv --resource-group rg-claims-rules-test

# Get the Managed Identity Principal ID (save this)
$functionAppIdentity = az functionapp identity show --name funcclaimstest001ncv --resource-group rg-claims-rules-test --query principalId -o tsv
Write-Host "Function App Managed Identity: $functionAppIdentity"
```

### Step 2: Assign Cosmos DB Permissions
```powershell
# Assign Cosmos DB Built-in Data Contributor role to Function App
az cosmosdb sql role assignment create `
  --account-name cosmosclaimstest001ncv `
  --resource-group rg-claims-rules-test `
  --role-definition-name "Cosmos DB Built-in Data Contributor" `
  --principal-id $functionAppIdentity `
  --scope "/"
```

### Step 3: Assign Blob Storage Permissions
```powershell
# Assign Storage Blob Data Contributor role
az role assignment create `
  --assignee $functionAppIdentity `
  --role "Storage Blob Data Contributor" `
  --scope "/subscriptions/64e1939f-6460-4656-ad75-dcc277b155f1/resourceGroups/rg-claims-rules-test/providers/Microsoft.Storage/storageAccounts/ahdsstrtest001ncv"
```

### Step 4: Update Function App Configuration
```powershell
# Set application settings (no keys needed!)
az functionapp config appsettings set --name funcclaimstest001ncv --resource-group rg-claims-rules-test --settings `
  "Cosmos__AccountEndpoint=https://cosmosclaimstest001ncv.documents.azure.com:443/" `
  "Cosmos__Database=claims" `
  "Cosmos__AuditContainer=audit" `
  "BlobStorage__AccountName=ahdsstrtest001ncv" `
  "USE_MOCK_SERVICES=false"
```

### Step 5: Create Blob Container (Via Portal)
Since storage account has `publicNetworkAccess: Disabled`, create via Azure Portal:

1. Go to https://portal.azure.com
2. Navigate to **Storage Account** → `ahdsstrtest001ncv`
3. Click **Containers** in left menu
4. Click **+ Container**
5. Name: `contract-audit-archive`
6. Public access level: **Private**
7. Click **Create**

### Step 6: Deploy Function App
```powershell
cd C:\Git\AZ\azure-claims-rules-full-bundle\azure-claims-rules-mvp-starter\src\ClaimsRules.Api

# Publish to Azure
func azure functionapp publish funcclaimstest001ncv

# Verify deployment
az functionapp show --name funcclaimstest001ncv --resource-group rg-claims-rules-test --query state
```

## 🔍 Troubleshooting

### Issue: "AuthenticationFailed" or "Forbidden" locally
**Solution**: 
```powershell
# Re-login to Azure CLI
az logout
az login

# Clear token cache
az account clear
az login

# Verify correct subscription
az account show
```

### Issue: "Network rules blocked the request"
**Cause**: Storage account has `publicNetworkAccess: Disabled`

**Solutions**:
1. **Use Azure Portal** to create container (recommended for secure environments)
2. **Or** temporarily enable public access:
```powershell
az storage account update --name ahdsstrtest001ncv --resource-group rg-claims-rules-test --public-network-access Enabled
# Create container
az storage container create --name contract-audit-archive --account-name ahdsstrtest001ncv --auth-mode login
# Disable public access again
az storage account update --name ahdsstrtest001ncv --resource-group rg-claims-rules-test --public-network-access Disabled
```

### Issue: Function App can't access Cosmos DB in production
**Check**:
```powershell
# Verify Managed Identity is enabled
az functionapp identity show --name funcclaimstest001ncv --resource-group rg-claims-rules-test

# Verify RBAC assignment
az cosmosdb sql role assignment list --account-name cosmosclaimstest001ncv --resource-group rg-claims-rules-test
```

### Issue: DefaultAzureCredential fails
**Debug**:
```powershell
# Enable Azure Identity logging in local.settings.json
"AZURE_IDENTITY_ENABLE_LOGGING": "true"

# Check Function App logs
az functionapp log tail --name funcclaimstest001ncv --resource-group rg-claims-rules-test
```

## 🎯 Benefits of Identity-Based Authentication

### Security
- ✅ **No secrets in code** - Connection strings/keys removed
- ✅ **No secrets in configuration** - Only endpoints stored
- ✅ **Automatic token rotation** - Azure handles credential lifecycle
- ✅ **Audit trail** - Azure AD tracks all access via identity
- ✅ **Principle of least privilege** - RBAC grants only needed permissions

### Compliance
- ✅ **HIPAA compliant** - No shared keys
- ✅ **SOC 2 Type II ready** - Identity-based access control
- ✅ **Zero Trust architecture** - Identity verification on every request

### Operational
- ✅ **Simplified deployment** - No key management needed
- ✅ **Works across environments** - Same code for dev/test/prod
- ✅ **Developer productivity** - Just `az login` to test locally

## 📋 Comparison: Before vs After

### Before (Connection String Auth)
```json
{
  "CosmosDb__ConnectionString": "AccountEndpoint=...;AccountKey=mySuperSecretKey123..."
  "BlobStorage__ConnectionString": "DefaultEndpointsProtocol=https;AccountKey=..."
}
```
❌ Keys in configuration files  
❌ Manual key rotation  
❌ Risk of key leakage  
❌ Shared secrets across environments  

### After (Identity-Based Auth)
```json
{
  "Cosmos__AccountEndpoint": "https://cosmosclaimstest001ncv.documents.azure.com:443/"
  "BlobStorage__AccountName": "ahdsstrtest001ncv"
}
```
✅ No secrets stored  
✅ Automatic credential rotation  
✅ Per-identity access control  
✅ Azure AD audit trail  

## 🔐 How DefaultAzureCredential Works

`DefaultAzureCredential` tries authentication methods in this order:

1. **Environment Variables** (CI/CD pipelines)
2. **Managed Identity** (Azure resources - Function Apps, VMs, etc.)
3. **Azure CLI** (local development - `az login`)
4. **Azure PowerShell** (alternative local development)
5. **Visual Studio** (IDE authentication)
6. **VS Code Azure Account** (IDE authentication)

### Local Development Flow
```
Developer runs: az login
  ↓
DefaultAzureCredential detects Azure CLI token
  ↓
Uses your identity to access Cosmos DB & Blob Storage
  ↓
Same RBAC permissions you have in Azure Portal
```

### Production Flow
```
Function App starts
  ↓
DefaultAzureCredential detects Managed Identity
  ↓
Gets token from Azure Instance Metadata Service (IMDS)
  ↓
Uses Function App's identity to access resources
  ↓
RBAC permissions assigned to Managed Identity
```

## 🚀 Next Steps

1. **Test Locally**:
   ```powershell
   cd ClaimsRules.Api
   func start
   # Test endpoints - should work with your Azure CLI identity
   ```

2. **Create Blob Container** (via Azure Portal due to network restrictions)

3. **Deploy to Production**:
   ```powershell
   # Enable Managed Identity
   az functionapp identity assign --name funcclaimstest001ncv --resource-group rg-claims-rules-test
   
   # Assign RBAC roles (see Step 2-3 above)
   
   # Deploy
   func azure functionapp publish funcclaimstest001ncv
   ```

4. **Test Production**:
   ```powershell
   # Call API endpoint
   Invoke-RestMethod -Uri "https://funcclaimstest001ncv.azurewebsites.net/api/providers/search" -Method POST -Body '{}' -ContentType "application/json"
   ```

5. **Monitor**:
   - Check Function App logs in Azure Portal
   - Review Application Insights for authentication issues
   - Monitor Cosmos DB and Storage access logs

## 📚 Additional Resources

- [DefaultAzureCredential Documentation](https://learn.microsoft.com/en-us/dotnet/api/azure.identity.defaultazurecredential)
- [Cosmos DB RBAC](https://learn.microsoft.com/en-us/azure/cosmos-db/how-to-setup-rbac)
- [Azure Storage RBAC](https://learn.microsoft.com/en-us/azure/storage/blobs/assign-azure-role-data-access)
- [Managed Identity Overview](https://learn.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/overview)

---

**Status**: ✅ Local authentication configured with your Azure CLI identity  
**Next**: Create blob container via Portal, then test locally with `func start`
