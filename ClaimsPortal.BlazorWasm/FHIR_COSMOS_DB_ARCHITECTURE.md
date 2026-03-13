# FHIR & Cosmos DB Architecture Guide

## Question 1: Should Cosmos DB Containers Align with FHIR Resources?

**YES! You are absolutely correct.** The current design with separate Cosmos DB containers (BenefitPlans, Accounts, Members, Claims, Providers) violates FHIR principles and creates unnecessary data duplication.

### Current Architecture (❌ NOT RECOMMENDED)

```
┌─────────────────────────────────────────────────────────────────┐
│                     Blazor WebAssembly App                      │
└────────┬──────────────────────────────────────┬─────────────────┘
         │                                      │
         ▼                                      ▼
┌──────────────────┐                  ┌──────────────────────────┐
│ FhirDataService  │                  │  CosmosDbService         │
│ (HTTP REST API)  │                  │  (HTTP REST API)         │
└────────┬─────────┘                  └────────┬─────────────────┘
         │                                      │
         ▼                                      ▼
┌────────────────────────┐          ┌─────────────────────────────┐
│ Azure Health Data      │          │   Azure Cosmos DB (SQL API) │
│ Services (FHIR R4)     │          │                             │
│                        │          │  ├── BenefitPlans container │
│ ├── Patient resources  │          │  ├── Accounts container     │
│ ├── Coverage resources │          │  ├── Members container      │
│ ├── Claim resources    │          │  ├── Claims container       │
│ ├── Practitioner       │          │  └── Providers container    │
│ └── Organization       │          │                             │
└────────────────────────┘          └─────────────────────────────┘
     ⚠️ DUPLICATION!                      ⚠️ DUPLICATION!
```

**Problems with this approach:**
- **Data Duplication**: Members exist in FHIR (Patient) AND Cosmos DB (Members container)
- **Synchronization Hell**: Every update requires coordinating between FHIR and Cosmos DB
- **FHIR Violations**: Healthcare data should follow FHIR standards, not custom schemas
- **Compliance Risk**: Audit trails are split across two systems
- **Increased Cost**: Storing same data twice

---

## Recommended Architecture (✅ FHIR-FIRST APPROACH)

### Principle: "FHIR for Healthcare Data, Cosmos DB for Extensions Only"

```
┌─────────────────────────────────────────────────────────────────┐
│                     Blazor WebAssembly App                      │
└────────┬──────────────────────────────────────┬─────────────────┘
         │                                      │
         ▼                                      ▼
┌──────────────────┐                  ┌──────────────────────────┐
│ FhirDataService  │                  │  CosmosDbService         │
│ (HTTP REST API)  │                  │  (HTTP REST API)         │
│                  │                  │  ONLY FOR NON-FHIR DATA  │
└────────┬─────────┘                  └────────┬─────────────────┘
         │                                      │
         │ PRIMARY DATA SOURCE                  │ METADATA ONLY
         │ (Healthcare Domain)                  │ (App-Specific)
         ▼                                      ▼
┌────────────────────────┐          ┌─────────────────────────────┐
│ Azure Health Data      │          │   Azure Cosmos DB (SQL API) │
│ Services (FHIR R4)     │          │                             │
│                        │          │  ├── UserPreferences        │
│ ├── Patient            │          │  ├── AuditLogs (app-level)  │
│ ├── Coverage           │          │  ├── WorkflowState          │
│ ├── Claim              │          │  ├── CacheMetadata          │
│ ├── ExplanationOfBenefit          │  └── FeatureFlags           │
│ ├── Practitioner       │          │                             │
│ ├── PractitionerRole   │          │  (NO HEALTHCARE DATA)       │
│ ├── Organization       │          │                             │
│ ├── Location           │          └─────────────────────────────┘
│ ├── Medication         │
│ └── MedicationRequest  │
└────────────────────────┘
```

---

## FHIR Resource Mapping for ClaimsIQ Platform

### 1. **Members → FHIR Patient Resource**

**Current (❌ Wrong):**
```json
// Cosmos DB "Members" container
{
  "memberId": "MEM12345",
  "firstName": "John",
  "lastName": "Smith",
  "dateOfBirth": "1985-03-15",
  "gender": "Male",
  "accountId": "ACC001"
}
```

**Correct (✅ FHIR Patient):**
```json
// Azure Health Data Services FHIR API
{
  "resourceType": "Patient",
  "id": "MEM12345",
  "identifier": [{
    "system": "https://claimsiq.com/member-id",
    "value": "MEM12345"
  }],
  "name": [{
    "family": "Smith",
    "given": ["John"]
  }],
  "gender": "male",
  "birthDate": "1985-03-15",
  "extension": [{
    "url": "https://claimsiq.com/fhir/StructureDefinition/account-reference",
    "valueReference": {
      "reference": "Organization/ACC001"
    }
  }]
}
```

---

### 2. **BenefitPlans → FHIR Coverage Resource**

**Current (❌ Wrong):**
```csharp
// Custom C# model in Cosmos DB
public class BenefitPlan
{
    public string PlanId { get; set; }
    public string PlanName { get; set; }
    public string PlanType { get; set; } // HMO, PPO, EPO
    public decimal Deductible { get; set; }
    public decimal OutOfPocketMax { get; set; }
}
```

**Correct (✅ FHIR Coverage):**
```json
{
  "resourceType": "Coverage",
  "id": "PLAN001",
  "identifier": [{
    "system": "https://claimsiq.com/plan-id",
    "value": "PLAN001"
  }],
  "status": "active",
  "type": {
    "coding": [{
      "system": "http://terminology.hl7.org/CodeSystem/v3-ActCode",
      "code": "HMO",
      "display": "Health Maintenance Organization"
    }]
  },
  "subscriber": {
    "reference": "Patient/MEM12345"
  },
  "beneficiary": {
    "reference": "Patient/MEM12345"
  },
  "period": {
    "start": "2026-01-01",
    "end": "2026-12-31"
  },
  "payor": [{
    "reference": "Organization/CLAIMSIQ",
    "display": "ClaimsIQ Health Plan"
  }],
  "class": [{
    "type": {
      "coding": [{
        "system": "http://terminology.hl7.org/CodeSystem/coverage-class",
        "code": "plan"
      }]
    },
    "value": "Premium Gold HMO",
    "name": "Premium Gold HMO"
  }],
  "costToBeneficiary": [{
    "type": {
      "coding": [{
        "system": "http://terminology.hl7.org/CodeSystem/coverage-copay-type",
        "code": "gpvisit",
        "display": "General Practitioner Office Visit"
      }]
    },
    "valueMoney": {
      "value": 25,
      "currency": "USD"
    }
  }, {
    "type": {
      "coding": [{
        "code": "deductible"
      }]
    },
    "valueMoney": {
      "value": 1500,
      "currency": "USD"
    }
  }]
}
```

---

### 3. **Claims → FHIR Claim & ExplanationOfBenefit Resources**

**Current (❌ Wrong):**
```csharp
// Custom model in Cosmos DB "Claims" container
public class ClaimRecord
{
    public string ClaimId { get; set; }
    public string PatientId { get; set; }
    public DateTime ServiceDate { get; set; }
    public decimal TotalAmount { get; set; }
    public List<string> DiagnosisCodes { get; set; }
}
```

**Correct (✅ FHIR Claim):**
```json
{
  "resourceType": "Claim",
  "id": "CLM20260108001",
  "identifier": [{
    "system": "https://claimsiq.com/claim-id",
    "value": "CLM20260108001"
  }],
  "status": "active",
  "type": {
    "coding": [{
      "system": "http://terminology.hl7.org/CodeSystem/claim-type",
      "code": "professional"
    }]
  },
  "use": "claim",
  "patient": {
    "reference": "Patient/MEM12345"
  },
  "created": "2026-01-08T10:30:00Z",
  "provider": {
    "reference": "Practitioner/DR12345"
  },
  "priority": {
    "coding": [{
      "code": "normal"
    }]
  },
  "insurance": [{
    "sequence": 1,
    "focal": true,
    "coverage": {
      "reference": "Coverage/PLAN001"
    }
  }],
  "diagnosis": [{
    "sequence": 1,
    "diagnosisCodeableConcept": {
      "coding": [{
        "system": "http://hl7.org/fhir/sid/icd-10-cm",
        "code": "E11.9",
        "display": "Type 2 diabetes mellitus without complications"
      }]
    }
  }],
  "procedure": [{
    "sequence": 1,
    "procedureCodeableConcept": {
      "coding": [{
        "system": "http://www.ama-assn.org/go/cpt",
        "code": "99213",
        "display": "Office visit"
      }]
    }
  }],
  "total": {
    "value": 150.00,
    "currency": "USD"
  }
}
```

---

### 4. **Providers → FHIR Practitioner & Organization Resources**

**Current (❌ Wrong):**
```json
// Cosmos DB "Providers" container
{
  "providerId": "DR12345",
  "providerName": "Dr. Sarah Johnson",
  "specialty": "Cardiology",
  "npi": "1234567890"
}
```

**Correct (✅ FHIR Practitioner):**
```json
{
  "resourceType": "Practitioner",
  "id": "DR12345",
  "identifier": [{
    "system": "http://hl7.org/fhir/sid/us-npi",
    "value": "1234567890"
  }],
  "active": true,
  "name": [{
    "family": "Johnson",
    "given": ["Sarah"],
    "prefix": ["Dr."]
  }],
  "gender": "female",
  "qualification": [{
    "code": {
      "coding": [{
        "system": "http://terminology.hl7.org/CodeSystem/v2-0360",
        "code": "MD",
        "display": "Doctor of Medicine"
      }]
    }
  }]
}
```

**FHIR PractitionerRole (Links Practitioner to Organization & Specialty):**
```json
{
  "resourceType": "PractitionerRole",
  "id": "ROLE-DR12345-VALLEYMED",
  "active": true,
  "practitioner": {
    "reference": "Practitioner/DR12345"
  },
  "organization": {
    "reference": "Organization/VALLEYMED"
  },
  "specialty": [{
    "coding": [{
      "system": "http://snomed.info/sct",
      "code": "394579002",
      "display": "Cardiology"
    }]
  }]
}
```

---

### 5. **Accounts → FHIR Organization Resource**

**Current (❌ Wrong):**
```csharp
// Custom model in Cosmos DB "Accounts" container
public class Account
{
    public string AccountId { get; set; }
    public string AccountName { get; set; }
    public string AccountType { get; set; } // "Family", "Individual"
    public decimal TotalPremium { get; set; }
}
```

**Correct (✅ FHIR Organization with Extension):**
```json
{
  "resourceType": "Organization",
  "id": "ACC001",
  "identifier": [{
    "system": "https://claimsiq.com/account-id",
    "value": "ACC001"
  }],
  "active": true,
  "type": [{
    "coding": [{
      "system": "https://claimsiq.com/fhir/CodeSystem/account-type",
      "code": "family",
      "display": "Family Account"
    }]
  }],
  "name": "Smith Family Account",
  "extension": [{
    "url": "https://claimsiq.com/fhir/StructureDefinition/total-premium",
    "valueMoney": {
      "value": 1200.00,
      "currency": "USD"
    }
  }]
}
```

---

## When to Use Cosmos DB vs FHIR

### ✅ Use FHIR Server for:
- **Clinical Data**: Diagnoses, procedures, medications, allergies
- **Administrative Data**: Patients, providers, organizations, locations
- **Financial Data**: Claims, ExplanationOfBenefit, Coverage, ChargeItem
- **Scheduling**: Appointment, Schedule, Slot
- **Care Management**: CarePlan, Goal, ServiceRequest

### ✅ Use Cosmos DB for:
- **User Preferences**: UI settings, saved filters, dashboard layouts
- **Application State**: Workflow status, background jobs, processing queues
- **Audit Logs**: Application-level logging (not PHI-related)
- **Feature Flags**: A/B testing, feature toggles
- **Cache Metadata**: Cache invalidation timestamps, ETags
- **AI Training Data**: De-identified analytics datasets

---

## Refactoring Steps

### Step 1: Update CosmosDbService to Remove Healthcare Containers

**Before:**
```csharp
// CosmosDbService.cs (❌ WRONG)
public async Task<List<BenefitPlan>> GetBenefitPlansAsync()
{
    return await QueryCosmosAsync<BenefitPlan>(
        "BenefitPlans", 
        "SELECT * FROM c"
    );
}
```

**After:**
```csharp
// CosmosDbService.cs (✅ CORRECT)
// Remove all healthcare-related methods
// Keep only:
public async Task SaveUserPreferenceAsync(string userId, UserPreference pref);
public async Task<UserPreference> GetUserPreferenceAsync(string userId);
public async Task SaveAuditLogAsync(AuditLog log);
```

### Step 2: Update FhirDataService to Be Primary Data Source

**Update BenefitPlanService.cs:**
```csharp
public class BenefitPlanService
{
    private readonly FhirDataService _fhirService;
    // REMOVE: private readonly CosmosDbService _cosmosService;
    
    public BenefitPlanService(FhirDataService fhirService)
    {
        _fhirService = fhirService;
    }
    
    public async Task<List<BenefitPlan>> GetBenefitPlansAsync()
    {
        // Load from FHIR Coverage resources ONLY
        return await _fhirService.GetBenefitPlansAsync();
    }
    
    public async Task SaveBenefitPlanAsync(BenefitPlan plan)
    {
        // Save to FHIR as Coverage resource
        await _fhirService.SaveCoverageAsync(plan);
        
        // NO Cosmos DB call for healthcare data
    }
}
```

### Step 3: Update Azure Infrastructure (main.bicep)

**Remove duplicate Cosmos DB containers:**
```bicep
// ❌ REMOVE these containers from main.bicep
resource benefitPlansContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2023-04-15'
resource accountsContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2023-04-15'
resource membersContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2023-04-15'
resource claimsContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2023-04-15'
resource providersContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2023-04-15'

// ✅ KEEP only these containers
resource userPreferencesContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2023-04-15'
resource auditLogsContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2023-04-15'
resource workflowStateContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2023-04-15'
```

### Step 4: Data Migration Plan

1. **Export existing Cosmos DB data** (if any production data exists)
2. **Transform to FHIR resources** using mappings above
3. **POST to FHIR API** using FhirDataService
4. **Delete Cosmos DB containers** after verification
5. **Update all UI components** to call FhirDataService

---

## Benefits of FHIR-First Architecture

### 1. **Interoperability** 🌐
- Exchange data with EMRs (Epic, Cerner, Allscripts)
- Support CMS Interoperability Rules (TEFCA, QHP)
- Enable patient data portability (Blue Button 2.0)

### 2. **Standards Compliance** 📜
- HIPAA-compliant by design
- CMS mandate for FHIR R4 support (21st Century Cures Act)
- ONC Health IT Certification ready

### 3. **Single Source of Truth** 🎯
- No synchronization overhead
- Consistent audit trail
- Simplified backup/disaster recovery

### 4. **Cost Efficiency** 💰
- Eliminate duplicate storage
- Reduce API calls (no syncing)
- Lower Cosmos DB RU/s requirements

### 5. **Developer Experience** 👩‍💻
- One API to learn (FHIR R4)
- Rich ecosystem of libraries (FHIR.NET, HAPI FHIR)
- Better TypeScript/C# type safety with FHIR models

---

## Implementation Checklist

- [ ] Update `CosmosDbService.cs` to remove healthcare methods
- [ ] Update `FhirDataService.cs` as primary data source
- [ ] Refactor `BenefitPlanService.cs` to use FHIR only
- [ ] Refactor `AccountService.cs` to use FHIR Organization
- [ ] Update all Razor pages to call FHIR services
- [ ] Remove healthcare containers from `main.bicep`
- [ ] Add FHIR custom extensions for ClaimsIQ-specific fields
- [ ] Update `appsettings.json` to remove Cosmos DB healthcare endpoints
- [ ] Create data migration scripts (if needed)
- [ ] Update documentation and architecture diagrams
- [ ] Test end-to-end workflows with FHIR data
- [ ] Validate FHIR conformance using Crucible or Inferno tests

---

## Summary

**Your question was 100% correct.** The Cosmos DB containers for BenefitPlans, Accounts, Members, Claims, and Providers **SHOULD align with FHIR resources** and should NOT exist as separate containers. Instead:

1. **Use Azure Health Data Services FHIR API** for ALL healthcare domain data
2. **Use Cosmos DB ONLY** for application-specific metadata that doesn't fit FHIR schemas
3. **Adopt FHIR resources** as your primary data models:
   - Members → `Patient`
   - BenefitPlans → `Coverage`
   - Claims → `Claim` + `ExplanationOfBenefit`
   - Providers → `Practitioner` + `PractitionerRole`
   - Accounts → `Organization`

This architecture ensures compliance, reduces complexity, and positions ClaimsIQ for future interoperability requirements.
