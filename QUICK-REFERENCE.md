# Quick Reference - Local Development Commands

## Infrastructure Deployment

```powershell
# Deploy all infrastructure (without Fabric)
cd c:\Git\AZ\azure-claims-rules-full-bundle\infra
az deployment group create `
  --resource-group rg-claims-rules-test `
  --template-file main.bicep `
  --parameters '@params.json' `
  --parameters fabricCapacityAdminId='' `
  --parameters staticWebAppName='claims-portal-swa'

# Deploy with Fabric F2 (requires valid admin principal)
az deployment group create `
  --resource-group rg-claims-rules-test `
  --template-file main.bicep `
  --parameters '@params.json' `
  --parameters fabricCapacityAdminId='YOUR_PRINCIPAL_ID' `
  --parameters staticWebAppName='claims-portal-swa' `
  --parameters fabricLocation='centralus'
```

## Function App Deployment

```powershell
# Build and publish
cd c:\Git\AZ\azure-claims-rules-full-bundle\azure-claims-rules-mvp-starter\src\ClaimsRules.Api
dotnet build
dotnet publish -c Release -o ./output

# Create deployment package
Compress-Archive -Path ./output/* -DestinationPath function-app.zip -Force

# Deploy to Azure
az functionapp deployment source config-zip `
  --resource-group rg-claims-rules-test `
  --name funcclaimstest001ncv `
  --src function-app.zip

# Test endpoints
Invoke-RestMethod -Uri "https://funcclaimstest001ncv.azurewebsites.net/api/health"
Invoke-RestMethod -Uri "https://funcclaimstest001ncv.azurewebsites.net/api/get-plans"
```

## React Portal Deployment

```powershell
# Install Node.js first (if not installed)
# Download from: https://nodejs.org/

# Build React app
cd c:\Git\AZ\azure-claims-rules-full-bundle\claims-portal
npm install
npm run build

# Deploy to Static Web App
$deploymentToken = az staticwebapp secrets list `
  --name claims-portal-swa `
  --resource-group rg-claims-rules-test `
  --query "properties.apiKey" -o tsv

npx @azure/static-web-apps-cli deploy ./build `
  --deployment-token $deploymentToken `
  --env production
```

## Testing

```powershell
# Run .NET tests
cd c:\Git\AZ\azure-claims-rules-full-bundle\tests\ClaimsRules.Api.Tests
dotnet test

# Test Function App endpoints
$apiBase = "https://funcclaimstest001ncv.azurewebsites.net/api"

# Health check
Invoke-RestMethod -Uri "$apiBase/health"

# Get benefit plans
Invoke-RestMethod -Uri "$apiBase/get-plans"

# Validate claim
$claimBody = @{
    PatientId = "PAT001"
    PlanId = "PLAN001"
    ServiceDate = "2026-01-05"
    ServiceCode = "99213"
    BilledAmount = 500
    ProviderId = "PROV001"
    YearToDateSpending = 300
} | ConvertTo-Json

Invoke-RestMethod -Uri "$apiBase/validate-claim" -Method POST -Body $claimBody -ContentType "application/json"

# Check eligibility
Invoke-RestMethod -Uri "$apiBase/check-eligibility?patientId=PAT001&planId=PLAN001"
```

## Git Commands

```powershell
# Initialize repository (first time only)
cd c:\Git\AZ\azure-claims-rules-full-bundle
.\scripts\init-git-repo.ps1

# Daily workflow
git status
git add .
git commit -m "Your commit message"
git push

# Create feature branch
git checkout -b feature/new-feature
git push -u origin feature/new-feature

# Pull latest changes
git pull origin main
```

## Azure Resource Queries

```powershell
# List all resources in resource group
az resource list --resource-group rg-claims-rules-test --output table

# Get Function App URL
az functionapp show `
  --resource-group rg-claims-rules-test `
  --name funcclaimstest001ncv `
  --query defaultHostName -o tsv

# Get Static Web App URL
az staticwebapp show `
  --resource-group rg-claims-rules-test `
  --name claims-portal-swa `
  --query defaultHostname -o tsv

# Get FHIR endpoint
az healthcareapis service show `
  --resource-group rg-claims-rules-test `
  --workspace-name fhirr4test001 `
  --resource-name fhirr4test001 `
  --query properties.authenticationConfiguration.audience -o tsv

# Check Logic App status (auto-shutdown)
az logic workflow show `
  --resource-group rg-claims-rules-test `
  --name logic-shutdown-services `
  --query state -o tsv
```

## Data Ingestion

```powershell
# Upload synthetic CSV data
$csvPath = "c:\Git\AZ\azure-claims-rules-full-bundle\azure-claims-rules-mvp-devkit\data\synthetic\claims_20.csv"
$apiUrl = "https://funcclaimstest001ncv.azurewebsites.net/api/csv-ingest"

# Read CSV and send to API
$csvData = Get-Content $csvPath -Raw
Invoke-RestMethod -Uri $apiUrl -Method POST -Body $csvData -ContentType "text/csv"
```

## Monitoring

```powershell
# View Function App logs (live streaming)
az webapp log tail `
  --resource-group rg-claims-rules-test `
  --name funcclaimstest001ncv

# Query Application Insights
$appInsightsId = az monitor app-insights component show `
  --resource-group rg-claims-rules-test `
  --app appiclaimstest001 `
  --query id -o tsv

# Get recent traces
az monitor app-insights query `
  --app $appInsightsId `
  --analytics-query "traces | where timestamp > ago(1h) | order by timestamp desc | take 50" `
  --output table
```

## Cleanup

```powershell
# Delete entire resource group (USE WITH CAUTION)
az group delete --name rg-claims-rules-test --yes --no-wait

# Stop Function App (to save costs)
az functionapp stop `
  --resource-group rg-claims-rules-test `
  --name funcclaimstest001ncv

# Start Function App
az functionapp start `
  --resource-group rg-claims-rules-test `
  --name funcclaimstest001ncv
```

## Endpoints Reference

### Function App Base URL
`https://funcclaimstest001ncv.azurewebsites.net/api`

### Available Endpoints
- `GET /health` - Health check (Anonymous)
- `POST /csv-ingest` - Ingest CSV claims (Anonymous)
- `POST /edi-ingest` - Ingest EDI 837 (Anonymous)
- `POST /validate-claim` - Validate claim against rules (Anonymous)
- `GET /get-plans` - Get all benefit plans (Anonymous)
- `GET /plans/{planId}` - Get specific plan (Anonymous)
- `GET /check-eligibility?patientId=X&planId=Y` - Check eligibility (Anonymous)

### Static Web App URL
`https://lively-moss-0f1ea001e.4.azurestaticapps.net`

### React Portal Features
- Benefits Plans View - Browse available plans with details
- Eligibility Checker - Verify patient coverage
- Claim Validator - Real-time claim validation with error/warning display
