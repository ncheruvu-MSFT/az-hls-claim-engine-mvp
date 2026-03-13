
# Azure HealthCare Intelligence Platform — Full Bundle (API + Portal + DevKit + Infra + CI/CD)

This repository includes a complete enterprise healthcare claims management platform with AI-powered fraud detection:
- **azure-claims-rules-mvp-starter/** — Azure Functions (.NET 8) API with rules engine + benefits service + eligibility checks
- **ClaimsPortal.BlazorWasm/** — Blazor WebAssembly portal (.NET 10) with natural language AI assistance and fraud detection
- **azure-claims-rules-mvp-devkit/** — Synthetic data generation tools + mock FHIR server
- **infra/** — Bicep templates for AHDS FHIR R4 + Azure Functions + Cosmos + Fabric + Static Web Apps
- **scripts/** — PowerShell automation for Service Principal setup and repository initialization
- **.github/workflows/** — CI/CD pipelines for automated build and deployment to Azure

## 🚀 Quick Start (Local Development)

1. **Deploy Infrastructure**: See [`infra/README.md`](infra/README.md)
2. **Build & Deploy Function App**: See [`QUICK-REFERENCE.md`](QUICK-REFERENCE.md#function-app-deployment)
3. **Test Endpoints**: Rules engine, benefits API, and eligibility checker
4. **Run Portal Locally**: `cd ClaimsPortal.BlazorWasm; dotnet run` (opens at http://localhost:5090)
5. **Deploy to Azure**: Push to `main` branch to trigger automated CI/CD workflows

## 📋 Available Features

### Backend APIs (Azure Functions - .NET 8)
- ✅ **CSV/EDI Ingestion** - Import claims data to FHIR format
- ✅ **Rules Engine** - Validate claims against benefit plans with 4-step validation workflow
- ✅ **Benefits Service** - Query benefit plans and check patient eligibility (270/271 HIPAA)
- ✅ **FHIR Integration** - Azure Health Data Services R4 compatible with Coverage, Claim, ClaimResponse resources

### Frontend Portal (Blazor WebAssembly - .NET 10)
- ✅ **Benefits Configuration** — Enterprise plan management with real-time FHIR Coverage resource mapping
- ✅ **Claims Engine** — Real-time adjudication with 4-step validation and financial breakdowns
- ✅ **Eligibility Verification** — 270/271 HIPAA-compliant checks with deductible/OOP progress tracking
- ✅ **Member Accumulators** — Real-time tracking of deductibles, OOP max, service utilization by enrollment period
- ✅ **AI-Powered Fraud Detection** — Pattern analysis with geographic anomaly detection, impossible travel checks, risk scoring (0-100)
- ✅ **Natural Language Assistant** — Context-aware AI help for benefits, claims, FHIR mappings, and fraud concepts
- ✅ **Eligibility Verification** — 270/271 HIPAA-compliant coverage checks
- ✅ **Natural Language AI** — Context-aware assistant for product configuration

### Infrastructure
- ✅ **Auto-Shutdown** - Logic Apps pause resources at 6 PM ET to save costs
- ✅ **Serverless** - Consumption-based pricing (Function App Y1, Cosmos serverless)
- 🔄 **Fabric F2** - (Pending) Analytics workspace in Central US with auto-pause
- 🔄 **Power BI Reports** - (Pending) Revenue, claims count, top 10, industry KPIs

## 🔐 GitHub CI/CD Setup

Want automated deployments? See [`SETUP-GITHUB.md`](SETUP-GITHUB.md) for:
- Private repository setup with OIDC authentication
- 4 automated workflows (CI, Infra, Function App, Static Web App)
- Branch protection and security best practices
- **Note**: Workflows are ready but not required - continue using local deployment for now

## 📚 Documentation

- **[QUICK-REFERENCE.md](QUICK-REFERENCE.md)** - Common commands for local development
- **[SETUP-GITHUB.md](SETUP-GITHUB.md)** - GitHub repository and CI/CD configuration
- **[infra/README.md](infra/README.md)** - Infrastructure deployment guide
- **[scripts/README.md](scripts/README.md)** - Service principal setup for external tenants

## 🧪 Testing

```powershell
# Test Function App endpoints
$api = "https://funcclaimstest001ncv.azurewebsites.net/api"
Invoke-RestMethod -Uri "$api/health"
Invoke-RestMethod -Uri "$api/get-plans"

# Run unit tests
cd tests/ClaimsRules.Api.Tests
dotnet test
```

## 🌐 Deployed Resources

- **Function App**: https://funcclaimstest001ncv.azurewebsites.net
- **Static Web App**: https://lively-moss-0f1ea001e.4.azurestaticapps.net
- **FHIR Endpoint**: fhirr4test001 (westus2)
- **Resource Group**: rg-claims-rules-test

## ⚠️ Important Notes

> **Non-prod only**: Uses synthetic data, no PHI. Enable Private Endpoints & Microsoft Defender for Cloud when moving to test/prod.

> **Cost Optimization**: Logic Apps auto-shutdown at 6 PM ET. Cosmos DB serverless has no pause (pay-per-RU). Function App on free tier (1M requests/month).

## 📊 Next Steps

1. Deploy Blazor WebAssembly portal to Static Web App
2. Integrate Azure OpenAI for enhanced NL assistant
3. Resolve Fabric capacity principal validation for analytics
4. Build Power BI reports for revenue and claims analytics
5. Test GitHub Actions workflows for automated deployment
