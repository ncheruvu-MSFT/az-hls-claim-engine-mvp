
# Infra (Bicep) — FREE TIER NONPROD DEPLOYMENT

Deploy AHDS FHIR R4 + Function App (Consumption) + Cosmos (Free Tier) + App Insights + **Microsoft Fabric (OneLake + Data Pipelines)** for testing and Power BI reporting.

## Cost Summary (Nonprod)
- **Function App**: FREE (Consumption Y1 - 1M executions/month)
- **Cosmos DB**: FREE tier (400 RU/s + 25 GB - one per subscription)
- **App Insights**: FREE (first 5GB/month)
- **Storage**: ~$0.50/month (Standard_LRS)
- **Event Grid**: FREE (first 100K operations)
- **FHIR**: Pay-per-use (~$1/GB + $0.45/1K calls)
- **Fabric F2**: ~$524/month (or use **Fabric Trial - 60 days FREE**)

**Expected total (without Fabric)**: $5-15/month for light testing  
**With Fabric Trial**: **FREE** for 60 days (full features)

> **💡 Cost Tip**: Start with Fabric Trial - no Synapse, no ADF needed. OneLake + Fabric Data Pipelines included.

## Architecture

**Compute in Azure:**
- FHIR service + Function Apps + Cosmos DB

**Data & Analytics in Fabric:**
- **OneLake** (unified data lake, no separate ADLS needed)
- **Fabric Data Pipelines** (replaces Azure Data Factory)
- **Power BI DirectLake** (fastest mode, real-time)
- **Fabric Lakehouse** (replaces Synapse SQL)

## Quick Deploy

```bash
# Login and set subscription
az login
az account set --subscription <YOUR_NONPROD_SUB_ID>

# Create resource group
az group create -n rg-claims-rules-nonprod -l eastus

# Deploy (takes ~5-10 minutes)
az deployment group create \
  -g rg-claims-rules-nonprod \
  -f infra/main.bicep \
  -p @infra/params.json

# Capture outputs (includes OneLake connection details)
az deployment group show \
  -g rg-claims-rules-nonprod \
  -n main \
  --query properties.outputs
```

## What Gets Deployed
✅ AHDS Workspace + FHIR R4 service (with managed identity RBAC)  
✅ Function App with Consumption plan (FREE tier)  
✅ Cosmos DB Serverless with FREE tier + **analytical storage enabled**  
✅ Storage Account (Standard_LRS) with analytics containers for OneLake  
✅ Event Grid Topic  
✅ Application Insights (FREE tier)  
✅ **Fabric Capacity F2** (OneLake + Data Pipelines + Power BI)  
✅ **Fabric Auto-Scheduler** (Logic Apps to pause/resume - saves ~$370/month)  
✅ Automatic RBAC assignments (Function App → FHIR + Cosmos, Logic Apps → Fabric)

**What's NOT deployed** (Fabric replaces these):
❌ Synapse Workspace (use Fabric Lakehouse instead)  
❌ Azure Data Factory (use Fabric Data Pipelines instead)

## Deploy Your Code

```bash
# Navigate to API project
cd azure-claims-rules-mvp-starter/src/ClaimsRules.Api

# Build and publish
dotnet publish -c Release -o ./publish

# Deploy to Azure Function App
cd publish
func azure functionapp publish func-claims-rules-dev
```

## Power BI & Fabric Reporting

See **[POWERBI-FABRIC-GUIDE.md](POWERBI-FABRIC-GUIDE.md)** for:
- **OneLake Shortcuts** to Azure Storage (zero-copy integration)
- **Cosmos DB Mirroring** for real-time Power BI reports
- **Fabric Data Pipelines** for FHIR $export automation
- **DirectLake mode** in Power BI (fastest performance)
- Sample notebooks and DAX measures

### Quick Start for Power BI (Fabric-Native)
1. Start **Fabric Trial**: [app.fabric.microsoft.com](https://app.fabric.microsoft.com)
2. Create **Lakehouse** in Fabric workspace
3. Create **OneLake Shortcut** to Azure Storage `fhir-export` container
4. Create **Mirrored Cosmos DB** for `claims.audit` container
5. Open Power BI Desktop → Connect to **OneLake data hub**
6. Select mirrored Cosmos data → Choose **DirectLake** mode
7. Build real-time dashboard (no refresh needed)

## Test from Local UI

The outputs include all connection strings needed for your local UI app. The Function App is configured with managed identity, so no secrets needed in production.

For local development, you can still use service principal auth (see scripts/setup-external-tenant-sp.ps1) or use Azure CLI credential with DefaultAzureCredential.

## Cost Management

**Use Fabric Trial (Recommended):**
```bash
# Remove Fabric capacity from Bicep to avoid charges
# Edit main.bicep - comment out or remove fabricCapacity resource
# Then use Fabric Trial for 60 days free
```

**If using paid Fabric capacity:**
- Fabric F2: ~$524/month (includes OneLake, Pipelines, Power BI, Spark, Data Science)
- Pause capacity when not in use (unlike Synapse, can pause Fabric)
- OneLake shortcuts = zero data copy cost

**💰 Automatic Cost Savings with Scheduler:**
The Fabric auto-suspend/resume scheduler is **included in the deployment** and saves **~70% on Fabric costs** (~$370/month):

**Schedule** (deployed automatically): 
- **Active**: Monday-Friday, 8 AM - 6 PM EST (50 hours/week)
- **Suspended**: Nights and weekends (118 hours/week)
- **Cost**: Logic Apps ~$0.10/month, Fabric ~$154/month (70% savings!)
- **Total savings**: ~$370/month 💰

The scheduler is deployed as part of the main infrastructure. View outputs after deployment:

```bash
# Check scheduler details in deployment outputs
az deployment group show \
  -g rg-claims-rules-nonprod \
  -n main \
  --query "properties.outputs.fabricScheduler.value"
```

See **[FABRIC-SCHEDULER-README.md](FABRIC-SCHEDULER-README.md)** for customization options and manual override command
See **[FABRIC-SCHEDULER-README.md](FABRIC-SCHEDULER-README.md)** for full details and customization options.
