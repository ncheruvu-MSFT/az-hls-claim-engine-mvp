# Blazor WebAssembly Claims Portal

## Overview

A modern healthcare claims management portal built with **Blazor WebAssembly** (.NET 10), inspired by Trizetto Facets with full FHIR R4 compliance and AI-powered natural language assistance.

## Features

### 1. Benefits Configuration Manager
- **Multi-Plan Support**: PPO, HMO, EPO, Catastrophic plans
- **Comprehensive Details**: Deductibles, coinsurance, copays, covered services
- **Network Management**: In-network provider lists
- **FHIR Mapping**: Direct Coverage resource visualization
- **Interactive Cards**: Click to expand full plan details

### 2. Claims Adjudication Engine
- **Real-time Validation**: 4-step claims processing
  1. Service coverage check (CPT codes)
  2. Network provider validation
  3. Patient responsibility calculation
  4. Out-of-pocket maximum enforcement
- **Detailed Results**: Approved/denied with explanations
- **Financial Breakdown**: Patient vs. insurance responsibility
- **FHIR ClaimResponse**: Auto-generated resource structure

### 3. Eligibility Verification
- **270/271 HIPAA Compliance**: Standard eligibility inquiry/response
- **Real-time Checks**: Instant patient coverage verification
- **Progress Tracking**: Visual deductible and OOP max meters
- **YTD Spending**: Current year accumulation display
- **Coverage Summary**: Plain-language benefit explanations

### 4. Natural Language AI Assistant
Each module includes a context-aware assistant that answers questions about:
- Benefit configuration concepts (deductibles, coinsurance, copays)
- Claims processing rules and validation logic
- CPT codes and medical terminology
- FHIR resource mappings and structures
- Prior authorization requirements
- Network provider concepts

**Example Questions:**
- "What is coinsurance?"
- "How do I configure prior authorization?"
- "What FHIR resource represents deductibles?"
- "Explain CPT codes"

## Technology Stack

- **.NET 10** - Latest framework
- **Blazor WebAssembly** - Client-side SPA
- **C# 12** - Shared models with API
- **CSS3** - Custom Trizetto-inspired design
- **HttpClient** - REST API integration

## Architecture

```
ClaimsPortal.BlazorWasm/
├── Models/
│   └── BenefitPlan.cs          # Shared data models
├── Services/
│   ├── ClaimsApiService.cs     # API client
│   └── NaturalLanguageService.cs # AI assistant
├── Pages/
│   ├── Home.razor              # Landing page
│   ├── Benefits.razor          # Benefits configuration
│   ├── ClaimsEngine.razor      # Claims validation
│   └── Eligibility.razor       # Eligibility checks
├── Layout/
│   ├── MainLayout.razor        # App shell
│   └── NavMenu.razor           # Navigation
└── wwwroot/
    ├── css/app.css             # Trizetto-inspired styles
    └── index.html              # Entry point
```

## Local Development

### Prerequisites
- .NET 10 SDK installed
- Function App API running at: `https://funcclaimstest001ncv.azurewebsites.net/api`

### Run Locally

```powershell
cd c:\Git\AZ\azure-claims-rules-full-bundle\ClaimsPortal.BlazorWasm
dotnet run
```

Access at: **https://localhost:5001** (or port shown in console)

### Hot Reload
Code changes automatically reload in the browser during development.

## Deployment to Azure Static Web Apps

### Method 1: Manual Deployment

```powershell
# Build for production
dotnet publish -c Release -o ./publish

# Get deployment token
$token = az staticwebapp secrets list `
  --name claims-portal-swa `
  --resource-group rg-claims-rules-test `
  --query properties.apiKey -o tsv

# Deploy
$env:AZURE_STATIC_WEB_APPS_API_TOKEN = $token
npx @azure/static-web-apps-cli deploy ./publish/wwwroot `
  --deployment-token $token `
  --env production
```

### Method 2: GitHub Actions (Automated)

The `.github/workflows/deploy-blazor-portal.yml` workflow automatically deploys on push to main:

```yaml
- Build Blazor WASM
- Publish to wwwroot
- Deploy to Static Web App
- Test deployment
```

## API Configuration

The API base URL is configured in `Program.cs`:

```csharp
builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
{
    ["ApiBaseUrl"] = "https://funcclaimstest001ncv.azurewebsites.net/api"
});
```

For production, use environment-specific appsettings:

```json
// wwwroot/appsettings.Production.json
{
  "ApiBaseUrl": "https://your-production-api.azurewebsites.net/api"
}
```

## FHIR Mappings

### Coverage (Benefit Plans)
```
BenefitPlan.Deductible → Coverage.costToBeneficiary[type=deductible].valueMoney
BenefitPlan.CoinsuranceRate → Coverage.costToBeneficiary[type=coinsurance].value
BenefitPlan.Copay → Coverage.costToBeneficiary[type=copay].valueMoney
BenefitPlan.OutOfPocketMax → Coverage.costToBeneficiary[type=oopmax].valueMoney
```

### Claim & ClaimResponse
```
ClaimValidationRequest → Claim resource
ClaimValidationResult → ClaimResponse resource
PatientResponsibility → ClaimResponse.patient.amount
InsurancePayment → ClaimResponse.item[0].adjudication[category=benefit].amount
```

### CoverageEligibilityResponse
```
EligibilityResult.IsEligible → CoverageEligibilityResponse.outcome
YearToDateDeductible → insurance.item.benefit[type=deductible].usedMoney
RemainingDeductible → insurance.item.benefit[type=deductible].allowedMoney - used
```

## Trizetto Facets Comparison

| Feature | Trizetto Facets | This Portal |
|---------|----------------|-------------|
| Benefits Config | ✓ Multi-plan | ✓ PPO/HMO/EPO/Catastrophic |
| Claims Adjudication | ✓ Real-time | ✓ 4-step validation |
| Eligibility | ✓ 270/271 | ✓ HIPAA compliant |
| Prior Auth | ✓ Workflow | ✓ Flag indicator |
| Network Management | ✓ Provider lists | ✓ In-network tracking |
| Natural Language Help | ✗ Not available | ✓ AI-powered assistant |
| FHIR Compliance | Partial | ✓ Full R4 |

## Styling

The portal uses a custom CSS design inspired by Trizetto Facets:
- **Color Scheme**: Microsoft Fluent Design (Azure blues, greens, purples)
- **Layout**: Card-based responsive grid
- **Typography**: Segoe UI system font
- **Animations**: Smooth transitions and slide-ins
- **Accessibility**: High contrast, keyboard navigation

## Future Enhancements

1. **Azure OpenAI Integration**
   - Replace rule-based NL service with GPT-4
   - Context-aware responses using function calling
   - FHIR resource generation from natural language

2. **Advanced Claims Features**
   - Bulk claim upload (CSV/EDI 837)
   - Claim status tracking dashboard
   - Appeals and adjustments workflow
   - Payment remittance (EDI 835)

3. **Product Configuration**
   - Visual benefit plan designer
   - Rule builder for custom validation
   - Prior authorization workflow engine
   - Multi-tenant organization support

4. **Reporting & Analytics**
   - Real-time claims dashboard
   - Denial rate tracking
   - Cost trend analysis
   - Provider network utilization

5. **Authentication & Security**
   - Azure AD B2C integration
   - Role-based access control (Provider/Payer/Patient)
   - HIPAA compliance features
   - Audit logging

## Testing

### Manual Testing
1. Navigate to Benefits → Select a plan → Verify FHIR mapping
2. Go to Claims Engine → Submit test claim → Validate result
3. Check Eligibility → Enter PAT001/PLAN001 → View YTD tracking
4. Ask NL Assistant: "What is coinsurance?" → Verify response

### Test Data
- **Plans**: PLAN001 (Gold PPO), PLAN002 (Silver HMO), PLAN003 (Bronze)
- **Patients**: PAT001, PAT002, PAT003
- **Providers**: PROV001, PROV002
- **CPT Codes**: 99213, 99214, 99215

## Troubleshooting

**Build Errors:**
```powershell
# Clear obj/bin folders
Remove-Item -Recurse -Force bin, obj
dotnet build
```

**API Connection Issues:**
- Verify Function App is running
- Check CORS settings allow Static Web App domain
- Confirm API base URL in Program.cs

**Deployment Failures:**
- Ensure Static Web App exists in Azure
- Verify deployment token is valid
- Check build output in publish/wwwroot

## Resources

- [Blazor Documentation](https://learn.microsoft.com/en-us/aspnet/core/blazor/)
- [FHIR R4 Specification](https://www.hl7.org/fhir/R4/)
- [Azure Static Web Apps](https://learn.microsoft.com/en-us/azure/static-web-apps/)
- [Trizetto Facets](https://www.cognizant.com/us/en/trizetto/products-and-solutions/facets)
