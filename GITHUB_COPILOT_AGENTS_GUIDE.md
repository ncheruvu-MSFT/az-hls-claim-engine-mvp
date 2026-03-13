# GitHub Copilot Agent Setup for Healthcare Payer SDLC

## Overview

This guide shows you how to create specialized GitHub Copilot agents (background/virtual agents) for different SDLC roles in your healthcare payer platform, based on the `payer-virtual-agents-prompts.md` file.

---

## Method 1: Using GitHub Copilot Workspace Instructions (.github/copilot-instructions.md)

### Step 1: Create `.github` Directory

```powershell
# In your repository root
cd C:\Git\AZ\azure-claims-rules-full-bundle
mkdir .github -ErrorAction SilentlyContinue
```

### Step 2: Create Agent Configuration Files

Create specialized instruction files for each agent role:

#### `.github/copilot-instructions.md` (Main Configuration)

```markdown
# GitHub Copilot Instructions for ClaimsIQ Platform

## Project Context
**ClaimsIQ Platform** - Healthcare payer operations and external portal system following FHIR R4 standards and CMS compliance requirements.

## Universal Operating Principles

When assisting with this project, GitHub Copilot should:

- **Be factual, precise, audit-friendly, and policy-driven**
- **Use ONLY input data and approved policy documents**
- **Follow HIPAA/CMS compliance rules strictly**
- **Never fabricate member, provider, or claim data**
- **Never provide medical advice**
- **Never expose PHI/PII beyond minimum necessary**

## Hard Guardrails

🚫 **NEVER:**
- Suggest exposing PHI in logs or error messages
- Recommend storing credentials in code
- Generate fake patient data without clear "SYNTHETIC" markers
- Bypass authentication or authorization checks
- Use non-HIPAA-compliant external APIs without approval

✅ **ALWAYS:**
- Validate FHIR resource conformance (R4 specification)
- Include audit logging for data access
- Apply least-privilege access patterns
- Use Azure Managed Identities for authentication
- Follow CMS Interoperability and Patient Access Final Rules

---

## Agent Roles

### When code relates to claims processing → **Claims Adjudication Agent**
### When code relates to prior authorization → **Prior Authorization Agent**
### When code relates to fraud detection → **Payment Integrity & FWA Agent**
### When code relates to member/provider portals → **Member/Provider Support Agent**
### When code relates to FHIR resources → **Interoperability/FHIR Agent**
### When code relates to deployments → **DevOps & Release Orchestrator Agent**

See specialized instructions below for each role.
```

#### `.github/agents/claims-adjudication-agent.md`

```markdown
# Claims Adjudication Agent Instructions

## Mission
Apply benefits, contracts, and policy rules to claims for accurate payment determination.

## When This Agent Activates
- Files in `Pages/ClaimsEngine.razor`, `Services/ClaimsAdjudicationService.cs`
- Code containing: `Claim`, `ExplanationOfBenefit`, `adjudication`, `pricing`

## Specialized Knowledge

### Claims Processing Rules
- **Eligibility Check First**: Always verify Coverage.status = "active" and period contains serviceDate
- **Benefits Exhaustion**: Check remainingBenefit against claim.total before approval
- **Provider Network Status**: Verify PractitionerRole.active and network participation
- **Coordination of Benefits**: Handle primary/secondary payer logic (COB)

### FHIR Resource Expectations

**Claim Resource** must include:
```json
{
  "resourceType": "Claim",
  "status": "active",
  "type": { "coding": [{ "code": "professional|institutional|pharmacy|oral" }] },
  "patient": { "reference": "Patient/[id]" },
  "insurance": [{
    "sequence": 1,
    "focal": true,
    "coverage": { "reference": "Coverage/[planId]" }
  }],
  "diagnosis": [{ "diagnosisCodeableConcept": { "coding": [{ "system": "http://hl7.org/fhir/sid/icd-10-cm" }] } }],
  "procedure": [{ "procedureCodeableConcept": { "coding": [{ "system": "http://www.ama-assn.org/go/cpt" }] } }]
}
```

**ExplanationOfBenefit** must include:
```json
{
  "resourceType": "ExplanationOfBenefit",
  "status": "active",
  "type": { "coding": [{ "system": "http://terminology.hl7.org/CodeSystem/claim-type" }] },
  "outcome": "complete|error|partial",
  "adjudication": [{
    "category": { "coding": [{ "code": "submitted|copay|eligible|deductible|benefit" }] },
    "amount": { "value": 0.00, "currency": "USD" }
  }],
  "total": [{
    "category": { "coding": [{ "code": "submitted|benefit" }] },
    "amount": { "value": 0.00, "currency": "USD" }
  }]
}
```

### Code Suggestions

**Eligibility Verification:**
```csharp
// Copilot should suggest this pattern
public async Task<bool> VerifyEligibilityAsync(string patientId, string coverageId, DateTime serviceDate)
{
    var coverage = await _fhirService.GetCoverageAsync(coverageId);
    
    if (coverage.Status != "active")
        return false;
    
    if (coverage.Period.Start > serviceDate || coverage.Period.End < serviceDate)
        return false;
    
    var patient = await _fhirService.GetPatientAsync(patientId);
    if (patient.Identifier.All(i => i.Value != coverage.Subscriber.Reference))
        return false;
    
    return true;
}
```

**Adjudication Logic:**
```csharp
public async Task<ExplanationOfBenefit> AdjudicateClaimAsync(Claim claim)
{
    // 1. Verify eligibility
    var isEligible = await VerifyEligibilityAsync(
        claim.Patient.Reference.Split('/')[1],
        claim.Insurance[0].Coverage.Reference.Split('/')[1],
        claim.Created.Value
    );
    
    if (!isEligible)
        return CreateDenialEOB(claim, "Member not eligible");
    
    // 2. Apply benefits
    var coverage = await _fhirService.GetCoverageAsync(
        claim.Insurance[0].Coverage.Reference.Split('/')[1]
    );
    
    decimal submittedAmount = claim.Total.Value;
    decimal deductibleRemaining = CalculateRemainingDeductible(coverage);
    decimal copay = GetCopayAmount(coverage, claim.Type.Coding[0].Code);
    
    decimal patientResponsibility = Math.Min(deductibleRemaining, submittedAmount) + copay;
    decimal benefitAmount = Math.Max(0, submittedAmount - patientResponsibility);
    
    // 3. Create EOB
    return new ExplanationOfBenefit
    {
        Status = "active",
        Outcome = "complete",
        Claim = new ResourceReference($"Claim/{claim.Id}"),
        Patient = claim.Patient,
        Adjudication = new List<AdjudicationComponent>
        {
            new() { Category = CodeableConcept("submitted"), Amount = Money(submittedAmount) },
            new() { Category = CodeableConcept("deductible"), Amount = Money(deductibleRemaining) },
            new() { Category = CodeableConcept("copay"), Amount = Money(copay) },
            new() { Category = CodeableConcept("benefit"), Amount = Money(benefitAmount) }
        }
    };
}
```

## Escalation Triggers

Suggest `HUMAN_REVIEW_REQUIRED` when:
- Claim amount exceeds $10,000
- Diagnosis codes indicate experimental treatment
- Provider is flagged in fraud database
- Prior authorization is required but missing
- Coordination of Benefits (COB) involves Medicare/Medicaid
```

#### `.github/agents/fhir-interoperability-agent.md`

```markdown
# FHIR Interoperability Agent Instructions

## Mission
Ensure compliant healthcare data exchange using FHIR R4 standards.

## When This Agent Activates
- Files: `Services/FhirDataService.cs`, `Mapping/CsvToFhirMapper.cs`
- Code containing: `FHIR`, `Patient`, `Coverage`, `Claim`, `Bundle`, `resourceType`

## Specialized Knowledge

### FHIR R4 Conformance Rules

**Must-Support Elements** (never omit):
```json
// Patient resource
{
  "resourceType": "Patient",
  "id": "required",
  "identifier": "required - at least one",
  "name": "required - at least one",
  "gender": "required",
  "birthDate": "optional but recommended"
}

// Coverage resource
{
  "resourceType": "Coverage",
  "id": "required",
  "status": "required (active|cancelled|draft|entered-in-error)",
  "beneficiary": "required",
  "payor": "required - at least one"
}

// Claim resource
{
  "resourceType": "Claim",
  "id": "required",
  "status": "required (active|cancelled|draft|entered-in-error)",
  "type": "required",
  "use": "required (claim|preauthorization|predetermination)",
  "patient": "required",
  "created": "required",
  "provider": "required",
  "insurance": "required - at least one"
}
```

### Code Patterns

**FHIR Resource Validation:**
```csharp
public static class FhirValidator
{
    public static bool ValidatePatient(Patient patient, out List<string> errors)
    {
        errors = new List<string>();
        
        if (string.IsNullOrEmpty(patient.Id))
            errors.Add("Patient.id is required");
        
        if (patient.Identifier == null || !patient.Identifier.Any())
            errors.Add("Patient.identifier is required (at least one)");
        
        if (patient.Name == null || !patient.Name.Any())
            errors.Add("Patient.name is required (at least one)");
        
        if (string.IsNullOrEmpty(patient.Gender))
            errors.Add("Patient.gender is required");
        
        // Validate coding systems
        foreach (var identifier in patient.Identifier ?? Enumerable.Empty<Identifier>())
        {
            if (string.IsNullOrEmpty(identifier.System))
                errors.Add($"Identifier.system is required");
            
            if (string.IsNullOrEmpty(identifier.Value))
                errors.Add($"Identifier.value is required");
        }
        
        return errors.Count == 0;
    }
}
```

**FHIR Search Parameters:**
```csharp
// Copilot should suggest proper FHIR search syntax
public async Task<List<Patient>> SearchPatientsAsync(string familyName, DateTime? birthDate)
{
    var searchParams = new List<string>();
    
    if (!string.IsNullOrEmpty(familyName))
        searchParams.Add($"family={Uri.EscapeDataString(familyName)}");
    
    if (birthDate.HasValue)
        searchParams.Add($"birthdate={birthDate.Value:yyyy-MM-dd}");
    
    var query = string.Join("&", searchParams);
    var bundle = await _httpClient.GetFromJsonAsync<Bundle>(
        $"{_fhirBaseUrl}/Patient?{query}"
    );
    
    return bundle.Entry
        .Select(e => e.Resource as Patient)
        .Where(p => p != null)
        .ToList();
}
```

**Batch Operations with FHIR Bundle:**
```csharp
public async Task<Bundle> CreateBatchBundleAsync(List<Resource> resources)
{
    var bundle = new Bundle
    {
        Type = Bundle.BundleType.Batch,
        Entry = resources.Select(r => new Bundle.EntryComponent
        {
            FullUrl = $"urn:uuid:{Guid.NewGuid()}",
            Resource = r,
            Request = new Bundle.RequestComponent
            {
                Method = Bundle.HTTPVerb.POST,
                Url = r.TypeName
            }
        }).ToList()
    };
    
    return await _httpClient.PostAsJsonAsync($"{_fhirBaseUrl}", bundle);
}
```

### CMS Interoperability Rules

**Patient Access API (FHIR R4):**
- Must support: Patient, Coverage, ExplanationOfBenefit
- Must respond within 1 second for single resource GET
- Must support `_since` parameter for incremental sync

**Provider Directory API:**
- Must support: Practitioner, PractitionerRole, Organization, Location
- Must update within 30 days of changes

## Code Review Checklist

When reviewing FHIR-related code, Copilot should verify:
- [ ] All FHIR resources have valid `resourceType`
- [ ] Required fields are populated (id, status, etc.)
- [ ] Coding systems use standard URIs (SNOMED, LOINC, ICD-10-CM, CPT)
- [ ] Dates follow ISO 8601 format (YYYY-MM-DD)
- [ ] References use format `ResourceType/id`
- [ ] HTTP status codes are handled (200, 201, 400, 404, 500)
- [ ] Pagination is implemented for Bundle searches
- [ ] Authentication uses Bearer tokens or OAuth 2.0
```

#### `.github/agents/devops-agent.md`

```markdown
# DevOps & Release Orchestrator Agent Instructions

## Mission
Safely deploy payer platform changes with zero-downtime and compliance validation.

## When This Agent Activates
- Files: `infra/*.bicep`, `.github/workflows/*.yml`, `scripts/*.ps1`
- Code containing: `deployment`, `pipeline`, `terraform`, `bicep`, `docker`, `kubernetes`

## Specialized Knowledge

### Azure Infrastructure as Code (Bicep)

**FHIR API Deployment:**
```bicep
resource fhirService 'Microsoft.HealthcareApis/workspaces/fhirservices@2023-02-28' = {
  name: '${workspaceName}/fhir-api'
  kind: 'fhir-R4'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    authenticationConfiguration: {
      authority: 'https://login.microsoftonline.com/${tenantId}'
      audience: 'https://${workspaceName}-fhir.fhir.azurehealthcareapis.com'
      smartProxyEnabled: false
    }
    corsConfiguration: {
      origins: ['https://${appServiceName}.azurewebsites.net']
      headers: ['*']
      methods: ['GET', 'POST', 'PUT', 'DELETE', 'OPTIONS']
      maxAge: 3600
      allowCredentials: true
    }
    publicNetworkAccess: 'Enabled'
  }
}
```

**Cosmos DB with Private Endpoint:**
```bicep
resource cosmosDb 'Microsoft.DocumentDB/databaseAccounts@2023-04-15' = {
  name: cosmosAccountName
  location: location
  kind: 'GlobalDocumentDB'
  properties: {
    databaseAccountOfferType: 'Standard'
    consistencyPolicy: {
      defaultConsistencyLevel: 'Session'
    }
    locations: [{
      locationName: location
      failoverPriority: 0
      isZoneRedundant: false
    }]
    publicNetworkAccess: 'Disabled'
    networkAclBypass: 'AzureServices'
    ipRules: []
  }
}

// Private Endpoint for Cosmos DB
resource privateEndpoint 'Microsoft.Network/privateEndpoints@2023-04-01' = {
  name: '${cosmosAccountName}-pe'
  location: location
  properties: {
    subnet: {
      id: subnet.id
    }
    privateLinkServiceConnections: [{
      name: 'cosmosdb-connection'
      properties: {
        privateLinkServiceId: cosmosDb.id
        groupIds: ['Sql']
      }
    }]
  }
}
```

### GitHub Actions Workflows

**CI/CD Pipeline for Blazor WASM:**
```yaml
name: Deploy ClaimsIQ Platform

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

env:
  AZURE_WEBAPP_NAME: claimsiq-portal
  DOTNET_VERSION: '10.0.x'

jobs:
  build:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: ${{ env.DOTNET_VERSION }}
    
    - name: Restore dependencies
      run: dotnet restore ClaimsPortal.BlazorWasm/ClaimsPortal.BlazorWasm.csproj
    
    - name: Build
      run: dotnet build ClaimsPortal.BlazorWasm/ClaimsPortal.BlazorWasm.csproj --configuration Release --no-restore
    
    - name: Run FHIR Validation Tests
      run: dotnet test tests/ClaimsRules.Api.Tests/ClaimsRules.Api.Tests.csproj --filter "Category=FHIR"
    
    - name: Publish
      run: dotnet publish ClaimsPortal.BlazorWasm/ClaimsPortal.BlazorWasm.csproj -c Release -o ${{env.DOTNET_ROOT}}/webapp
    
    - name: Upload artifact
      uses: actions/upload-artifact@v3
      with:
        name: webapp
        path: ${{env.DOTNET_ROOT}}/webapp
  
  deploy-staging:
    runs-on: ubuntu-latest
    needs: build
    environment:
      name: 'staging'
      url: ${{ steps.deploy-webapp.outputs.webapp-url }}
    
    steps:
    - name: Download artifact
      uses: actions/download-artifact@v3
      with:
        name: webapp
    
    - name: Azure Login
      uses: azure/login@v1
      with:
        client-id: ${{ secrets.AZURE_CLIENT_ID }}
        tenant-id: ${{ secrets.AZURE_TENANT_ID }}
        subscription-id: ${{ secrets.AZURE_SUBSCRIPTION_ID }}
    
    - name: Deploy Infrastructure (Bicep)
      uses: azure/arm-deploy@v1
      with:
        subscriptionId: ${{ secrets.AZURE_SUBSCRIPTION_ID }}
        resourceGroupName: rg-claimsiq-staging
        template: infra/main.bicep
        parameters: infra/params-staging.json
    
    - name: Deploy to Azure Web App
      id: deploy-webapp
      uses: azure/webapps-deploy@v2
      with:
        app-name: ${{ env.AZURE_WEBAPP_NAME }}-staging
        package: .
    
    - name: Run Smoke Tests
      run: |
        curl -f https://${{ env.AZURE_WEBAPP_NAME }}-staging.azurewebsites.net/health || exit 1
        curl -f https://${{ env.AZURE_WEBAPP_NAME }}-staging.azurewebsites.net/api/fhir/metadata || exit 1
```

### Deployment Checklist

Copilot should remind developers to:
- [ ] **Run FHIR conformance tests** before deploying
- [ ] **Backup Cosmos DB** before schema changes
- [ ] **Enable Application Insights** for monitoring
- [ ] **Configure Managed Identity** for FHIR/Cosmos DB access
- [ ] **Set up Key Vault** for secrets (no secrets in code!)
- [ ] **Enable Azure Policy** for HIPAA compliance
- [ ] **Configure Azure Monitor alerts** for API failures
- [ ] **Test rollback plan** in staging environment
- [ ] **Update FHIR CapabilityStatement** if APIs change
- [ ] **Notify stakeholders** 24 hours before production deploy

### Rollback Scripts

```powershell
# Rollback to previous deployment slot
az webapp deployment slot swap `
  --resource-group rg-claimsiq-prod `
  --name claimsiq-portal `
  --slot staging `
  --target-slot production `
  --action swap

# Restore Cosmos DB from backup
az cosmosdb sql database restore `
  --resource-group rg-claimsiq-prod `
  --account-name claimsiq-cosmos `
  --database-name ClaimsIQ `
  --restore-timestamp "2026-01-08T10:00:00Z"
```

## Security Scanning

Copilot should suggest:
```yaml
- name: Run Trivy vulnerability scanner
  uses: aquasecurity/trivy-action@master
  with:
    scan-type: 'fs'
    scan-ref: '.'
    format: 'sarif'
    output: 'trivy-results.sarif'

- name: Upload Trivy results to GitHub Security
  uses: github/codeql-action/upload-sarif@v2
  with:
    sarif_file: 'trivy-results.sarif'
```
```

---

## Method 2: Using GitHub Copilot Chat `.context` File

Create a `.github/copilot-context.md` file to provide context for inline chat:

```markdown
# Copilot Chat Context for ClaimsIQ

When I ask questions about this codebase, please consider:

## Project Type
Healthcare payer platform with:
- Blazor WebAssembly frontend (.NET 10)
- Azure Health Data Services (FHIR R4 backend)
- Azure Cosmos DB (application metadata only)
- CMS compliance requirements

## Key Constraints
- Must follow FHIR R4 specification
- Must be HIPAA compliant
- Must support CMS Interoperability Final Rule
- Must use Azure Managed Identities (no hardcoded credentials)

## Reference Documents
- FHIR R4 Spec: https://hl7.org/fhir/R4/
- CMS Interoperability Rules: https://www.cms.gov/regulations-and-guidance/guidance/interoperability
- Azure Health Data Services: https://learn.microsoft.com/azure/healthcare-apis/

## Code Style
- Follow Microsoft C# Coding Conventions
- Use CSS variables from `:root` in app.css
- Never store PHI in localStorage
- Always use `@inject` for dependency injection in Razor files
```

---

## Method 3: Using Copilot Workspace Extensions (Beta)

### Step 1: Install GitHub Copilot Workspace Extension

```bash
# Install GitHub CLI
winget install GitHub.cli

# Authenticate
gh auth login

# Enable Copilot Workspace (if available in your organization)
gh extension install github/gh-copilot
```

### Step 2: Create Agent Definitions

Create `agents.json` in `.github` directory:

```json
{
  "agents": [
    {
      "name": "claims-adjudication-agent",
      "description": "Specialized agent for claims processing and adjudication logic",
      "triggers": {
        "files": [
          "Pages/ClaimsEngine.razor",
          "Services/ClaimsAdjudicationService.cs",
          "Services/FhirDataService.cs"
        ],
        "keywords": ["Claim", "ExplanationOfBenefit", "adjudication", "pricing"]
      },
      "instructions": ".github/agents/claims-adjudication-agent.md",
      "model": "gpt-4"
    },
    {
      "name": "fhir-interoperability-agent",
      "description": "Ensures FHIR R4 compliance and healthcare data standards",
      "triggers": {
        "files": [
          "Services/FhirDataService.cs",
          "Mapping/**/*.cs",
          "Models/**/*.cs"
        ],
        "keywords": ["FHIR", "Patient", "Coverage", "resourceType", "Bundle"]
      },
      "instructions": ".github/agents/fhir-interoperability-agent.md",
      "model": "gpt-4"
    },
    {
      "name": "devops-orchestrator-agent",
      "description": "Manages deployment pipelines and infrastructure",
      "triggers": {
        "files": [
          "infra/**/*.bicep",
          ".github/workflows/**/*.yml",
          "scripts/**/*.ps1"
        ],
        "keywords": ["deployment", "bicep", "terraform", "pipeline"]
      },
      "instructions": ".github/agents/devops-agent.md",
      "model": "gpt-4"
    }
  ]
}
```

---

## Method 4: Custom Copilot Extension (Advanced)

For more advanced scenarios, create a custom GitHub Copilot Extension:

### Step 1: Create Extension Manifest

File: `copilot-extension/manifest.json`

```json
{
  "name": "ClaimsIQ Healthcare Payer Assistant",
  "version": "1.0.0",
  "description": "Specialized Copilot extension for healthcare payer operations",
  "engine": "github-copilot",
  "capabilities": {
    "chat": {
      "enabled": true,
      "commands": [
        {
          "name": "/adjudicate-claim",
          "description": "Generate claims adjudication logic",
          "handler": "handlers/adjudication.js"
        },
        {
          "name": "/fhir-validate",
          "description": "Validate FHIR resource conformance",
          "handler": "handlers/fhir-validator.js"
        },
        {
          "name": "/deploy-check",
          "description": "Run pre-deployment compliance checks",
          "handler": "handlers/deployment-checker.js"
        }
      ]
    },
    "completions": {
      "enabled": true,
      "contextProviders": [
        {
          "name": "fhir-resource-definitions",
          "type": "json-schema",
          "source": "https://hl7.org/fhir/R4/fhir.schema.json"
        }
      ]
    }
  }
}
```

### Step 2: Create Handler Scripts

File: `copilot-extension/handlers/adjudication.js`

```javascript
// Custom handler for /adjudicate-claim command
export async function handleAdjudicationCommand(context) {
  const { claim, coverage } = context.parameters;
  
  // Load FHIR schemas
  const claimSchema = await fetch('https://hl7.org/fhir/R4/claim.schema.json').then(r => r.json());
  const eobSchema = await fetch('https://hl7.org/fhir/R4/explanationofbenefit.schema.json').then(r => r.json());
  
  // Generate code using schemas
  const code = await generateAdjudicationCode(claimSchema, eobSchema);
  
  return {
    type: 'code-snippet',
    language: 'csharp',
    content: code
  };
}

async function generateAdjudicationCode(claimSchema, eobSchema) {
  return `
public async Task<ExplanationOfBenefit> AdjudicateClaimAsync(Claim claim)
{
    // Validate claim against FHIR R4 schema
    var validationErrors = ValidateFhirResource(claim, claimSchema);
    if (validationErrors.Any())
        throw new FhirValidationException(validationErrors);
    
    // Load coverage (benefit plan)
    var coverage = await _fhirService.GetCoverageAsync(
        claim.Insurance[0].Coverage.Reference.Split('/')[1]
    );
    
    // Apply adjudication rules
    var eob = new ExplanationOfBenefit
    {
        Status = "active",
        Outcome = "complete",
        Claim = new ResourceReference($"Claim/{claim.Id}"),
        // ... rest of adjudication logic
    };
    
    return eob;
}
  `;
}
```

---

## Usage Examples

### Example 1: Inline Code Suggestions

When writing code in `Services/ClaimsAdjudicationService.cs`:

```csharp
// Start typing a comment to trigger Copilot
// Calculate copay based on coverage class and service type

// Copilot will suggest:
public decimal CalculateCopay(Coverage coverage, string serviceType)
{
    var copayClass = coverage.CostToBeneficiary
        .FirstOrDefault(c => c.Type.Coding.Any(cd => cd.Code == serviceType));
    
    return copayClass?.ValueMoney?.Value ?? 0m;
}
```

### Example 2: Chat Commands

In GitHub Copilot Chat panel:

```
You: @workspace How do I validate a FHIR Claim resource before adjudication?

Copilot (using FHIR Agent context):
Based on FHIR R4 specifications, here's a validation method:

[provides code using .github/agents/fhir-interoperability-agent.md context]
```

### Example 3: PR Reviews

When creating a pull request with FHIR resource changes, Copilot will automatically:
1. Validate FHIR resource schemas
2. Check for PHI exposure risks
3. Verify audit logging is present
4. Suggest HIPAA compliance improvements

---

## Testing Your Agents

### Test 1: FHIR Validation

```csharp
// In VS Code, start typing:
// Create a new FHIR Patient resource

// Expected Copilot suggestion should include:
var patient = new Patient
{
    Id = Guid.NewGuid().ToString(),
    Identifier = new List<Identifier>
    {
        new() { System = "https://claimsiq.com/member-id", Value = "MEM12345" }
    },
    Name = new List<HumanName>
    {
        new() { Family = "Smith", Given = new[] { "John" } }
    },
    Gender = AdministrativeGender.Male,
    BirthDate = "1985-03-15"
};
```

### Test 2: Claims Adjudication

```csharp
// Type: // Adjudicate a claim with prior authorization check

// Expected suggestion:
public async Task<ExplanationOfBenefit> AdjudicateClaimAsync(Claim claim)
{
    // Check if prior authorization is required
    var requiresPriorAuth = claim.Procedure.Any(p => 
        _priorAuthRequiredCodes.Contains(p.ProcedureCodeableConcept.Coding[0].Code)
    );
    
    if (requiresPriorAuth)
    {
        var priorAuth = await _fhirService.SearchClaimAsync(
            $"patient={claim.Patient.Reference}&type=preauthorization&status=active"
        );
        
        if (priorAuth == null || priorAuth.Count == 0)
            return CreateDenialEOB(claim, "Prior authorization required");
    }
    
    // Continue with adjudication...
}
```

### Test 3: Deployment Validation

In `infra/main.bicep`, Copilot should suggest:

```bicep
// When typing: // Deploy FHIR service with private endpoint

// Expected suggestion includes security best practices:
resource fhirService 'Microsoft.HealthcareApis/workspaces/fhirservices@2023-02-28' = {
  name: '${workspaceName}/fhir-api'
  kind: 'fhir-R4'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    authenticationConfiguration: {
      authority: 'https://login.microsoftonline.com/${tenantId}'
      audience: 'https://${workspaceName}-fhir.fhir.azurehealthcareapis.com'
    }
    publicNetworkAccess: 'Disabled' // ✅ Copilot should enforce this
  }
}

resource privateEndpoint 'Microsoft.Network/privateEndpoints@2023-04-01' = {
  // ✅ Copilot should suggest private endpoint automatically
}
```

---

## Monitoring Agent Performance

### GitHub Copilot Metrics Dashboard

Track how well your agents are performing:

1. **Acceptance Rate**: % of suggestions accepted by developers
2. **Context Accuracy**: Does agent use correct FHIR schemas?
3. **Compliance Violations**: Does agent suggest non-HIPAA-compliant code?

Access via:
```
GitHub → Settings → Copilot → Usage Metrics
```

---

## Summary

**To create background agents for your healthcare payer SDLC:**

1. ✅ **Create `.github/copilot-instructions.md`** - Main configuration file
2. ✅ **Create agent-specific instruction files** in `.github/agents/`
3. ✅ **Reference `payer-virtual-agents-prompts.md`** for agent missions and operating principles
4. ✅ **Test agents** by typing comments and observing suggestions
5. ✅ **Iterate based on developer feedback** and acceptance rates

**Key Files to Create:**
```
.github/
  ├── copilot-instructions.md (main configuration)
  ├── agents/
  │   ├── claims-adjudication-agent.md
  │   ├── fhir-interoperability-agent.md
  │   ├── prior-authorization-agent.md
  │   ├── fraud-detection-agent.md
  │   ├── member-support-agent.md
  │   ├── provider-support-agent.md
  │   └── devops-agent.md
  └── copilot-context.md (chat context)
```

This setup will give you specialized Copilot agents that understand healthcare payer operations, FHIR standards, and CMS compliance requirements!
