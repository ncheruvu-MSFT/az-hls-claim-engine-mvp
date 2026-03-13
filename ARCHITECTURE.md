# Architecture Overview

## System Components

```
┌─────────────────────────────────────────────────────────────────┐
│                    Azure Claims Rules Platform                  │
└─────────────────────────────────────────────────────────────────┘

┌──────────────────────┐
│   React Portal UI    │  Static Web App (Free)
│   (claims-portal/)   │  • Benefits Plans View
│                      │  • Eligibility Checker  
│   lively-moss-...    │  • Claim Validator
│   .azurestaticapps   │
└──────────┬───────────┘
           │ HTTPS
           ▼
┌──────────────────────┐
│   Azure Functions    │  Consumption Plan (Windows, Y1)
│   (.NET 8 Isolated)  │  • Health endpoint
│                      │  • CSV/EDI ingestion
│   funcclaimstest...  │  • Rules engine validation
│   .azurewebsites.net │  • Benefits API
└──────────┬───────────┘  • Eligibility checks
           │
           ├────────────────────┬─────────────────┬──────────────────┐
           ▼                    ▼                 ▼                  ▼
    ┌──────────────┐   ┌──────────────┐  ┌──────────────┐  ┌─────────────┐
    │ AHDS FHIR R4 │   │  Cosmos DB   │  │   Storage    │  │  Event Grid │
    │  (westus2)   │   │ (Serverless) │  │  ADLS Gen2   │  │   Topics    │
    │              │   │              │  │              │  │             │
    │ Claims data  │   │ Audit trail  │  │ FHIR export  │  │ Events pub  │
    └──────────────┘   └──────────────┘  └──────────────┘  └─────────────┘

                    ┌──────────────────────────────┐
                    │  Application Insights        │
                    │  Custom Log Analytics        │
                    │  (Monitoring & Diagnostics)  │
                    └──────────────────────────────┘

    ┌─────────────────────────────────────────────────────────┐
    │         Microsoft Fabric F2 (centralus)                 │
    │         Status: PENDING - Principal validation issue    │
    │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐ │
    │  │  OneLake     │  │  Lakehouse   │  │  Power BI    │ │
    │  │  Mirror      │  │  Data Store  │  │  Reports     │ │
    │  └──────────────┘  └──────────────┘  └──────────────┘ │
    └─────────────────────────────────────────────────────────┘

    ┌─────────────────────────────────────────────────────────┐
    │              Logic Apps (Schedulers)                     │
    │  • Function App auto-shutdown: 6 PM ET ✓                │
    │  • Fabric pause/resume: 6 PM ET (pending)               │
    └─────────────────────────────────────────────────────────┘
```

## Data Flow

### 1. Claims Ingestion
```
CSV/EDI File → POST /api/csv-ingest → Map to FHIR → AHDS FHIR R4
                                     → Audit to Cosmos DB
                                     → Publish Event (Event Grid)
```

### 2. Claims Validation
```
React UI → POST /api/validate-claim → Rules Engine
                                     ↓
                    ┌─────────────────┴──────────────────┐
                    ▼                                    ▼
           Get Benefit Plan                    Calculate Amounts
           Check Coverage                       Apply Deductible
           Validate Network                     Apply Coinsurance
           Check Eligibility                    Check OOP Max
                    │                                    │
                    └─────────────────┬──────────────────┘
                                     ▼
                         Return Validation Result
                         (Approved/Denied + Amounts)
```

### 3. Benefits Lookup
```
React UI → GET /api/get-plans → Return All Plans
        → GET /api/plans/{id} → Return Specific Plan
        → GET /api/check-eligibility → Verify Patient Coverage
```

### 4. Analytics Pipeline (Future)
```
Cosmos DB Audit Trail ──┐
                        ├──► OneLake Mirror ──► Lakehouse ──► Power BI
FHIR Export to ADLS ────┘                                     Reports
```

## Technology Stack

### Frontend
- **React 18.2.0** - UI library
- **Axios** - HTTP client
- **CSS3** - Styling (no framework)
- **Azure Static Web Apps** - Hosting (Free tier)

### Backend
- **.NET 8** - Runtime (Isolated worker model)
- **Azure Functions** - Serverless compute
- **C#** - Programming language
- **Anonymous Auth** - Dev/testing (change for prod)

### Data Layer
- **AHDS FHIR R4** - Healthcare data standard
- **Cosmos DB** - NoSQL for audit trail (Serverless)
- **Azure Storage** - FHIR exports (ADLS Gen2)
- **Event Grid** - Event-driven architecture

### Analytics (Planned)
- **Microsoft Fabric F2** - Analytics platform
- **OneLake** - Data lakehouse
- **Power BI** - Reporting and dashboards
- **DirectLake** - Real-time semantic models

### DevOps
- **Bicep** - Infrastructure as Code
- **GitHub Actions** - CI/CD (4 workflows ready)
- **OIDC Authentication** - Passwordless Azure access
- **Logic Apps** - Auto-shutdown schedulers

## Security Model

### Authentication
- **FHIR**: Azure AD OAuth 2.0
- **Function App**: Anonymous (dev), Azure AD (prod recommended)
- **Static Web App**: Anonymous public access
- **GitHub Actions**: OIDC federated credentials

### Network
- **Current**: Public endpoints
- **Production**: Enable Private Endpoints for FHIR, Cosmos, Storage

### Secrets Management
- **Local Dev**: local.settings.json (gitignored)
- **Azure**: Function App Configuration (App Settings)
- **GitHub**: Repository secrets + OIDC

### RBAC Roles
- Function App → FHIR: FHIR Data Contributor
- Function App → Cosmos: Cosmos DB Built-in Data Contributor
- Function App → Storage: Storage Blob Data Contributor
- GitHub Actions → Azure: Contributor (resource group)

## Cost Optimization

| Resource | SKU | Cost Mitigation |
|----------|-----|----------------|
| Function App | Consumption Y1 | 1M free requests/month + auto-shutdown @ 6 PM |
| Cosmos DB | Serverless | Pay-per-RU (no minimum) |
| Storage | Standard LRS | Pay-per-GB + lifecycle policies |
| FHIR | No charge | Standard tier |
| Static Web App | Free | 100 GB bandwidth/month |
| Fabric F2 | ~$1/hour | Auto-pause @ 6 PM ET (pending deployment) |
| Logic Apps | Consumption | Pay-per-execution |
| Event Grid | Free | 100K ops/month free |

**Estimated Monthly Cost (without Fabric)**: ~$10-30
**With Fabric F2 (8 hrs/day)**: ~$240/month

## Deployment Options

### Option 1: Local Deployment (Current)
```powershell
# Infrastructure
az deployment group create -g rg-claims-rules-test -f infra/main.bicep

# Function App
dotnet publish
az functionapp deployment source config-zip

# Static Web App
npm run build
npx @azure/static-web-apps-cli deploy
```

### Option 2: GitHub Actions (Ready, Not Active)
```yaml
# Automated on push to main
- CI: Build & test all components
- Deploy Function App: Auto-deploy on code changes
- Deploy Static Web App: Auto-deploy on React changes
- Deploy Infra: Manual workflow dispatch
```

## Current Status

✅ **Completed**
- Infrastructure deployment (FHIR, Functions, Cosmos, Storage, Event Grid)
- Function App with 7 endpoints (health, ingest, rules, benefits)
- React portal with 3 views (Benefits, Eligibility, Validator)
- Static Web App resource created
- GitHub CI/CD workflows ready
- Auto-shutdown Logic App (6 PM ET)

🔄 **In Progress**
- React portal deployment (awaiting Node.js installation)

❌ **Pending**
- Fabric F2 capacity deployment (principal validation blocker)
- Fabric scheduler Logic Apps
- OneLake data mirroring
- Power BI reports (revenue, claims, KPIs)

## API Reference

### Base URL
`https://funcclaimstest001ncv.azurewebsites.net/api`

### Endpoints

#### Health Check
```http
GET /health
Response: { "status": "healthy", "timestamp": "2026-01-07T..." }
```

#### Get Benefit Plans
```http
GET /get-plans
Response: { "value": [
  { "PlanId": "PLAN001", "PlanName": "Gold PPO", "Deductible": 1000, ... },
  { "PlanId": "PLAN002", "PlanName": "Silver HMO", "Deductible": 2000, ... },
  { "PlanId": "PLAN003", "PlanName": "Bronze Catastrophic", "Deductible": 5000, ... }
]}
```

#### Validate Claim
```http
POST /validate-claim
Body: {
  "PatientId": "PAT001",
  "PlanId": "PLAN001",
  "ServiceDate": "2026-01-05",
  "ServiceCode": "99213",
  "BilledAmount": 500,
  "ProviderId": "PROV001",
  "YearToDateSpending": 300
}
Response: {
  "IsApproved": true,
  "Errors": [],
  "Warnings": [],
  "PatientResponsibility": 500,
  "InsurancePayment": 0,
  "Explanation": "Patient has not met deductible..."
}
```

#### Check Eligibility
```http
GET /check-eligibility?patientId=PAT001&planId=PLAN001
Response: {
  "IsEligible": true,
  "PlanName": "Gold PPO",
  "YearToDateDeductible": 300,
  "RemainingDeductible": 700,
  "YearToDateOutOfPocket": 300,
  "RemainingOutOfPocket": 4700
}
```

## Future Enhancements

1. **Authentication & Authorization**
   - Implement Azure AD B2C for patient portal
   - Provider portal with role-based access
   - API Management for rate limiting

2. **Advanced Rules Engine**
   - Configurable rules via Cosmos DB
   - Machine learning for fraud detection
   - Prior authorization workflow

3. **Real-time Analytics**
   - Stream Analytics for real-time claims monitoring
   - Anomaly detection
   - Cost prediction models

4. **Integration**
   - X12 EDI 837/835 batch processing
   - HL7 FHIR subscription notifications
   - Third-party payer integrations

5. **Compliance**
   - HIPAA audit logging
   - Data encryption at rest/in transit
   - Private endpoints for all services
   - Microsoft Defender for Cloud
