# Actuarial Estimator - Technical Specification

## Executive Summary
Build a healthcare payer actuarial forecasting system that combines clinical FHIR data with claims data to compute population risk and forecast next-year costs through interactive "what-if" scenario modeling.

## Architecture Overview

### Data Sources
1. **Azure Health Data Services (AHDS) FHIR R4** - Source of truth for clinical + claims data
2. **Cosmos DB** - Scenario storage and cached aggregates
3. **Azure Blob/ADLS Gen2** - Baseline snapshots and exports

### Technology Stack
- **Backend**: .NET 8 Azure Functions / ASP.NET Core
- **Frontend**: Blazor WebAssembly .NET 10
- **Authentication**: Microsoft Entra ID (roles: Actuary, Analyst, Admin)
- **Storage**: Cosmos DB (scenarios), AHDS FHIR (clinical), Blob (exports)

## FHIR R4 Resources (Synthetic Data from MITRE Synthea)

### Clinical Resources
- **Patient**: Demographics (birthDate, gender, address.state)
- **Coverage**: Plan/product + coverage period for member-months calculation
- **Condition**: Chronic conditions / morbidity (HCC mapping)
- **Encounter**: Utilization signals (ED, IP, OP, office visits)
- **Observation**: Clinical severity (A1c, BP, BMI, labs)

### Claims Resources
- **ExplanationOfBenefit** (preferred): Allowed/paid amounts, adjudication
- **Claim** (fallback): Claim-level financial data

### Synthea Data Generation
```bash
# Install Synthea
git clone https://github.com/synthetichealth/synthea.git
cd synthea

# Generate 1000 patients in Massachusetts
./run_synthea -p 1000 Massachusetts

# Output: FHIR R4 JSON bundles in output/fhir/
```

**Required Synthea Modules**:
- Demographics (age, gender, geography)
- Conditions (diabetes, CHF, hypertension, asthma, COPD)
- Encounters (ED, IP, OP, ambulatory)
- Observations (vital signs, labs)
- Claims/EOB generation

## Baseline Time Window
- **Default**: Last 12 months from current date
- **Configurable**: UI selector for 6/12/18/24 months
- **Filters**: Plan code, region, age band, condition cohort

## Baseline Metrics Calculation

### 1. Member Months
```sql
-- FHIR Query: Coverage resources
GET /Coverage?period=ge2025-01-01&period=le2025-12-31

-- Calculate member-months from Coverage.period
MemberMonths = SUM(days_covered / 30.44) per patient
```

### 2. Allowed/Paid Amounts
```sql
-- FHIR Query: ExplanationOfBenefit
GET /ExplanationOfBenefit?created=ge2025-01-01&created=le2025-12-31

-- Aggregate
BaselineAllowed = SUM(total.value where total.code='submitted')
BaselinePaid = SUM(total.value where total.code='benefit')
BaselinePMPM = BaselineAllowed / MemberMonths
```

### 3. Service Bucket PMPM
```csharp
// EOB.type.coding maps to service buckets
IP (Inpatient):      type = 'institutional' + facility = 'inpatient'
OP (Outpatient):     type = 'institutional' + facility = 'outpatient'
Professional:        type = 'professional'
Pharmacy:            type = 'pharmacy'
```

### 4. Utilization Rates
```sql
-- FHIR Query: Encounter
GET /Encounter?date=ge2025-01-01&date=le2025-12-31

-- Calculate
ED_Visits_Per_1000 = (COUNT(class='emergency') / MemberMonths * 12) * 1000
IP_Admits_Per_1000 = (COUNT(class='inpatient') / MemberMonths * 12) * 1000
OP_Visits_Per_1000 = (COUNT(class='outpatient') / MemberMonths * 12) * 1000
```

### 5. Clinical Risk Summary
```sql
-- FHIR Query: Condition
GET /Condition?recorded-date=ge2025-01-01

-- Top 10 conditions by prevalence
-- Risk score distribution (0-10 scale)
```

## Risk Scoring Model (Explainable HCC-Like)

### A. Demographic Risk Factors
```json
{
  "ageGenderWeights": {
    "M_0_34": 0.5,
    "M_35_44": 0.8,
    "M_45_54": 1.2,
    "M_55_64": 1.8,
    "M_65_74": 2.5,
    "M_75+": 3.5,
    "F_0_34": 0.6,
    "F_35_44": 0.9,
    "F_45_54": 1.3,
    "F_55_64": 1.9,
    "F_65_74": 2.6,
    "F_75+": 3.8
  }
}
```

### B. Condition-Based Risk (HCC-Like)
```json
{
  "conditionGroupWeights": {
    "HCC_001_HIV": 1.5,
    "HCC_008_MetastaticCancer": 2.5,
    "HCC_018_Diabetes_Complications": 1.3,
    "HCC_019_Diabetes_NoComplications": 0.8,
    "HCC_085_CHF": 1.8,
    "HCC_096_COPD": 1.2,
    "HCC_106_Hypertension": 0.6,
    "HCC_111_Asthma": 0.5
  },
  "icd10Mapping": {
    "E11.65": "HCC_018_Diabetes_Complications",
    "E11.9": "HCC_019_Diabetes_NoComplications",
    "I50.9": "HCC_085_CHF",
    "J44.9": "HCC_096_COPD",
    "I10": "HCC_106_Hypertension",
    "J45.909": "HCC_111_Asthma"
  }
}
```

### C. Encounter-Based Risk
```json
{
  "encounterWeights": {
    "ED_Visit": 0.3,
    "IP_Admit": 1.0,
    "OP_Visit": 0.1,
    "Office_Visit": 0.05
  }
}
```

### D. Observation-Based Severity Adjustment (Optional)
```json
{
  "observationSeverityWeights": {
    "A1c_High": 0.2,      // > 9.0%
    "BP_High": 0.15,       // SBP > 160
    "BMI_High": 0.1        // > 35
  }
}
```

### Risk Score Calculation
```csharp
public decimal CalculateMemberRiskScore(Patient patient, List<Condition> conditions, 
                                        List<Encounter> encounters, List<Observation> observations)
{
    // Base demographic score
    var ageGender = GetAgeGenderBand(patient);
    var demographicScore = riskWeights.AgeGenderWeights[ageGender];
    
    // Condition-based score (sum of HCC weights)
    var conditionScore = conditions
        .Select(c => MapToHCC(c.Code))
        .Where(hcc => hcc != null)
        .Sum(hcc => riskWeights.ConditionGroupWeights[hcc]);
    
    // Encounter-based score (count * weight)
    var encounterScore = encounters
        .GroupBy(e => e.Class.Code)
        .Sum(g => g.Count() * riskWeights.EncounterWeights[g.Key]);
    
    // Observation severity adjustment
    var severityAdjustment = CalculateSeverityAdjustment(observations);
    
    // Total risk score
    return demographicScore + conditionScore + encounterScore + severityAdjustment;
}

public decimal CalculatePopulationRiskIndex(List<MemberRiskScore> memberScores)
{
    // Average risk score normalized to 1.0 = average population
    var avgScore = memberScores.Average(m => m.TotalRiskScore);
    var populationAvg = 2.0m; // Calibrated baseline
    
    return avgScore / populationAvg;
}
```

## Forecast Model (Phase 1 - Deterministic)

### Baseline Components
```csharp
public class BaselineMetrics
{
    // Financial
    public decimal ProviderAllowed { get; set; }  // IP + OP + Professional
    public decimal PharmacyAllowed { get; set; }
    public decimal OtherAllowed { get; set; }
    public decimal TotalAllowed { get; set; }
    
    // Member-months
    public decimal MemberMonths { get; set; }
    
    // PMPM
    public decimal ProviderPMPM { get; set; }
    public decimal PharmacyPMPM { get; set; }
    public decimal TotalPMPM { get; set; }
    
    // Risk and utilization
    public decimal PopulationRiskIndex { get; set; }  // 1.0 = average
    public decimal UtilizationIndex { get; set; }     // 1.0 = average
    
    // Utilization rates
    public decimal EdVisitsPer1000 { get; set; }
    public decimal IpAdmitsPer1000 { get; set; }
    public decimal OpVisitsPer1000 { get; set; }
    
    // Time period
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public string Segment { get; set; }  // e.g., "Plan_HMO_East"
}
```

### Scenario Knobs
```csharp
public class ScenarioKnobs
{
    // Core financial trends
    public decimal ProviderTrendPct { get; set; }      // -20% to +20%
    public decimal PharmacyTrendPct { get; set; }      // -30% to +30%
    public decimal OtherTrendPct { get; set; }         // -10% to +10%
    
    // Membership
    public decimal MembershipDeltaPct { get; set; }    // -30% to +30%
    
    // Utilization
    public decimal UtilizationDeltaPct { get; set; }   // -15% to +15%
    
    // Population risk
    public decimal PopulationRiskDeltaPct { get; set; } // -10% to +10%
    
    // Clinical knobs
    public Dictionary<string, decimal> ConditionPrevalenceShiftPct { get; set; }
    // e.g., { "HCC_018_Diabetes": 5.0, "HCC_085_CHF": 2.0 }
    
    public Dictionary<string, decimal> EncounterMixShiftPct { get; set; }
    // e.g., { "ED": -10.0, "OP": +5.0, "Office": +5.0 }
    
    // Optional
    public Dictionary<string, decimal> ObservationSeverityShiftPct { get; set; }
    // e.g., { "A1c": +3.0 }
}
```

### Forecast Calculation
```csharp
public class ForecastEngine
{
    public ScenarioResult ComputeForecast(BaselineMetrics baseline, ScenarioKnobs knobs)
    {
        // 1. Apply trend to financial components
        var providerComponent = baseline.ProviderAllowed * (1 + knobs.ProviderTrendPct / 100m);
        var pharmacyComponent = baseline.PharmacyAllowed * (1 + knobs.PharmacyTrendPct / 100m);
        var otherComponent = baseline.OtherAllowed * (1 + knobs.OtherTrendPct / 100m);
        
        var baseAllowed = providerComponent + pharmacyComponent + otherComponent;
        
        // 2. Apply utilization and risk adjustments
        var utilizationFactor = 1 + (knobs.UtilizationDeltaPct / 100m);
        var riskFactor = 1 + (knobs.PopulationRiskDeltaPct / 100m);
        
        var forecastAllowed = baseAllowed * utilizationFactor * riskFactor;
        
        // 3. Apply membership change
        var forecastMemberMonths = baseline.MemberMonths * (1 + knobs.MembershipDeltaPct / 100m);
        
        // 4. Calculate forecast PMPM
        var forecastPMPM = forecastAllowed / forecastMemberMonths;
        
        // 5. Calculate delta drivers for waterfall
        var deltaDrivers = CalculateDeltaDrivers(baseline, knobs, forecastAllowed, forecastMemberMonths);
        
        return new ScenarioResult
        {
            ForecastAllowed = forecastAllowed,
            ForecastMemberMonths = forecastMemberMonths,
            ForecastPMPM = forecastPMPM,
            DeltaFromBaseline = forecastPMPM - baseline.TotalPMPM,
            DeltaDrivers = deltaDrivers
        };
    }
    
    private DeltaDrivers CalculateDeltaDrivers(BaselineMetrics baseline, ScenarioKnobs knobs, 
                                               decimal forecastAllowed, decimal forecastMemberMonths)
    {
        // Waterfall components
        var providerDelta = baseline.ProviderAllowed * (knobs.ProviderTrendPct / 100m) / baseline.MemberMonths;
        var pharmacyDelta = baseline.PharmacyAllowed * (knobs.PharmacyTrendPct / 100m) / baseline.MemberMonths;
        var riskDelta = (baseline.TotalAllowed * (knobs.PopulationRiskDeltaPct / 100m)) / baseline.MemberMonths;
        var utilizationDelta = (baseline.TotalAllowed * (knobs.UtilizationDeltaPct / 100m)) / baseline.MemberMonths;
        var membershipDelta = CalculateMembershipImpact(baseline, knobs);
        
        return new DeltaDrivers
        {
            ProviderTrendDelta = providerDelta,
            PharmacyTrendDelta = pharmacyDelta,
            RiskDelta = riskDelta,
            UtilizationDelta = utilizationDelta,
            MembershipDelta = membershipDelta
        };
    }
}
```

## UI Requirements (Blazor)

### Pages Structure
```
ActuarialEstimator.razor (main portal)
├── Components/
│   ├── BaselineView.razor       (baseline metrics + charts)
│   ├── ScenarioBuilder.razor    (knobs + sliders + toggles)
│   ├── ScenarioCompare.razor    (baseline vs scenario + waterfall)
│   └── ScenarioLibrary.razor    (list/clone/export scenarios)
```

### 1. Baseline View
**Features**:
- Segment selector (plan, region, age band)
- Time period selector (6/12/18/24 months)
- Baseline metrics cards (PMPM, member-months, risk index)
- Service bucket PMPM chart (bar chart)
- Utilization rates chart (bar chart)
- Top 10 conditions chart (horizontal bar)
- Risk score distribution histogram

**Mock Data**:
```csharp
var mockBaseline = new BaselineMetrics
{
    TotalAllowed = 12_500_000m,
    ProviderAllowed = 9_000_000m,
    PharmacyAllowed = 3_000_000m,
    OtherAllowed = 500_000m,
    MemberMonths = 25_000m,
    TotalPMPM = 500m,
    ProviderPMPM = 360m,
    PharmacyPMPM = 120m,
    PopulationRiskIndex = 1.05m,
    UtilizationIndex = 1.00m,
    EdVisitsPer1000 = 450m,
    IpAdmitsPer1000 = 85m,
    OpVisitsPer1000 = 3200m
};
```

### 2. Scenario Builder
**Knobs Layout**:
```html
<!-- Core Trends -->
<div class="knobs-section">
    <h4>Core Trends</h4>
    <div class="slider-group">
        <label>Provider Trend: @providerTrend%</label>
        <input type="range" min="-20" max="20" step="0.5" @bind="providerTrend" />
    </div>
    <div class="slider-group">
        <label>Pharmacy Trend: @pharmacyTrend%</label>
        <input type="range" min="-30" max="30" step="0.5" @bind="pharmacyTrend" />
    </div>
</div>

<!-- Membership -->
<div class="knobs-section">
    <h4>Membership</h4>
    <div class="slider-group">
        <label>Membership Change: @membershipDelta%</label>
        <input type="range" min="-30" max="30" step="1" @bind="membershipDelta" />
    </div>
</div>

<!-- Clinical -->
<div class="knobs-section">
    <h4>Clinical Risk</h4>
    <div class="slider-group">
        <label>Population Risk: @riskDelta%</label>
        <input type="range" min="-10" max="10" step="0.5" @bind="riskDelta" />
    </div>
    <div class="slider-group">
        <label>Utilization: @utilizationDelta%</label>
        <input type="range" min="-15" max="15" step="0.5" @bind="utilizationDelta" />
    </div>
</div>

<!-- Condition Prevalence Toggles -->
<div class="knobs-section">
    <h4>Condition Prevalence Adjustments</h4>
    <div class="condition-toggle">
        <label>Diabetes: @diabetesShift%</label>
        <input type="range" min="-10" max="10" step="1" @bind="diabetesShift" />
    </div>
    <div class="condition-toggle">
        <label>CHF: @chfShift%</label>
        <input type="range" min="-10" max="10" step="1" @bind="chfShift" />
    </div>
</div>

<!-- Action Buttons -->
<button class="btn-primary" @onclick="RunForecast">▶️ Run Forecast</button>
<button class="btn-secondary" @onclick="SaveScenario">💾 Save Scenario</button>
<button class="btn-secondary" @onclick="ResetKnobs">🔄 Reset</button>
```

### 3. Scenario Compare
**Layout**:
```
┌─────────────────────────────────────────────────────┐
│ Baseline PMPM: $500.00    Forecast PMPM: $532.15   │
│ Delta: +$32.15 (+6.4%)                              │
└─────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────┐
│ Waterfall Chart                                     │
│ Baseline ─→ Provider ─→ Pharmacy ─→ Risk ─→ ...    │
│  $500       +$15         +$10        +$5            │
└─────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────┐
│ Sensitivity Analysis                                 │
│ Provider Trend: -10% / Base / +10%                  │
│ Pharmacy Trend: -20% / Base / +20%                  │
└─────────────────────────────────────────────────────┘
```

### 4. Scenario Library
**Features**:
- Table: Name, Created, Owner, Base PMPM, Forecast PMPM, Delta
- Actions: View, Clone, Export (JSON), Delete
- Filters: Owner (me/all), Date range, Delta range

## Backend API Contract

### Endpoints

#### 1. Get Baseline
```http
GET /api/baseline?segment={segment}&period={period}

Response:
{
  "baselineId": "baseline-2025-plan-hmo-east-12mo",
  "segment": "Plan_HMO_East",
  "periodStart": "2024-01-01",
  "periodEnd": "2024-12-31",
  "totalAllowed": 12500000,
  "totalPMPM": 500.00,
  "memberMonths": 25000,
  "populationRiskIndex": 1.05,
  "serviceBuckets": {...},
  "utilizationRates": {...},
  "topConditions": [...]
}
```

#### 2. Create Scenario
```http
POST /api/scenarios

Request:
{
  "name": "2026 Budget - High Growth",
  "description": "10% membership growth, 5% provider trend",
  "baselineId": "baseline-2025-plan-hmo-east-12mo",
  "knobs": {
    "providerTrendPct": 5.0,
    "pharmacyTrendPct": 8.0,
    "membershipDeltaPct": 10.0,
    "utilizationDeltaPct": 2.0,
    "populationRiskDeltaPct": 1.5
  }
}

Response:
{
  "scenarioId": "scenario-xyz123",
  "status": "created",
  "createdAt": "2026-01-08T10:00:00Z"
}
```

#### 3. Run Scenario
```http
POST /api/scenarios/{id}/run

Response:
{
  "scenarioId": "scenario-xyz123",
  "status": "completed",
  "result": {
    "forecastAllowed": 14250000,
    "forecastMemberMonths": 27500,
    "forecastPMPM": 518.18,
    "deltaFromBaseline": 18.18,
    "deltaPct": 3.64,
    "deltaDrivers": {
      "providerTrendDelta": 12.50,
      "pharmacyTrendDelta": 8.00,
      "riskDelta": 2.50,
      "utilizationDelta": 3.00,
      "membershipDelta": -7.82
    }
  },
  "computedAt": "2026-01-08T10:01:23Z"
}
```

#### 4. Get Scenario
```http
GET /api/scenarios/{id}

Response:
{
  "scenarioId": "scenario-xyz123",
  "name": "2026 Budget - High Growth",
  "owner": "john.actuary@claimsiq.com",
  "baselineId": "baseline-2025-plan-hmo-east-12mo",
  "knobs": {...},
  "result": {...},
  "createdAt": "2026-01-08T10:00:00Z"
}
```

#### 5. List Scenarios
```http
GET /api/scenarios?owner=me&limit=50

Response:
{
  "scenarios": [
    {
      "scenarioId": "scenario-xyz123",
      "name": "2026 Budget - High Growth",
      "owner": "john.actuary@claimsiq.com",
      "forecastPMPM": 518.18,
      "deltaFromBaseline": 18.18,
      "createdAt": "2026-01-08T10:00:00Z"
    }
  ],
  "totalCount": 12
}
```

#### 6. Get Risk Weights
```http
GET /api/weights

Response:
{
  "ageGenderWeights": {...},
  "conditionGroupWeights": {...},
  "encounterWeights": {...},
  "observationSeverityWeights": {...},
  "lastModified": "2025-12-15T09:00:00Z",
  "modifiedBy": "admin@claimsiq.com"
}
```

#### 7. Update Risk Weights (Admin Only)
```http
PUT /api/weights

Request:
{
  "conditionGroupWeights": {
    "HCC_018_Diabetes_Complications": 1.4
  }
}

Response:
{
  "status": "updated",
  "updatedAt": "2026-01-08T10:05:00Z"
}
```

## Storage Architecture

### Cosmos DB Containers

#### 1. scenarios
```json
{
  "id": "scenario-xyz123",
  "partitionKey": "john.actuary@claimsiq.com",
  "name": "2026 Budget - High Growth",
  "description": "10% membership growth, 5% provider trend",
  "owner": "john.actuary@claimsiq.com",
  "baselineId": "baseline-2025-plan-hmo-east-12mo",
  "knobs": {
    "providerTrendPct": 5.0,
    "pharmacyTrendPct": 8.0,
    "membershipDeltaPct": 10.0,
    "utilizationDeltaPct": 2.0,
    "populationRiskDeltaPct": 1.5
  },
  "result": {
    "forecastPMPM": 518.18,
    "deltaFromBaseline": 18.18
  },
  "status": "completed",
  "createdAt": "2026-01-08T10:00:00Z",
  "computedAt": "2026-01-08T10:01:23Z"
}
```

#### 2. baselines
```json
{
  "id": "baseline-2025-plan-hmo-east-12mo",
  "partitionKey": "baseline",
  "segment": "Plan_HMO_East",
  "periodStart": "2024-01-01",
  "periodEnd": "2024-12-31",
  "totalAllowed": 12500000,
  "totalPMPM": 500.00,
  "memberMonths": 25000,
  "serviceBuckets": {...},
  "cachedAt": "2026-01-07T08:00:00Z",
  "ttl": 86400
}
```

#### 3. risk-weights
```json
{
  "id": "risk-weights-v2",
  "partitionKey": "config",
  "ageGenderWeights": {...},
  "conditionGroupWeights": {...},
  "version": 2,
  "lastModified": "2025-12-15T09:00:00Z",
  "modifiedBy": "admin@claimsiq.com"
}
```

### Azure Blob Storage
- **Container**: `actuarial-exports`
- **Format**: NDJSON (FHIR $export format)
- **Naming**: `{segment}/{yyyy-MM-dd}/Patient.ndjson`
- **Purpose**: Reproducible baselines, ML training data

## Authentication & Authorization

### Entra ID Roles
```csharp
[Authorize(Roles = "Actuary")]
public async Task<IActionResult> CreateScenario(...)

[Authorize(Roles = "Actuary,Analyst")]
public async Task<IActionResult> GetScenario(...)

[Authorize(Roles = "Admin")]
public async Task<IActionResult> UpdateRiskWeights(...)
```

### Role Definitions
- **Actuary**: Full access (create/edit/delete scenarios, edit risk weights)
- **Analyst**: Read-only access (view baselines, view scenarios, run existing scenarios)
- **Admin**: System admin (manage risk weights, manage all scenarios)

## Synthea-to-FHIR Load Runbook

### Step 1: Generate Synthea Data
```bash
cd synthea

# Generate 1000 patients with full clinical + claims history
./run_synthea -p 1000 Massachusetts

# Output: output/fhir/
```

### Step 2: Validate FHIR Bundles
```bash
# Install FHIR validator
wget https://github.com/hapifhir/org.hl7.fhir.core/releases/latest/download/validator_cli.jar

# Validate each bundle
java -jar validator_cli.jar output/fhir/*.json -version 4.0
```

### Step 3: Load to AHDS FHIR
```powershell
# PowerShell script: Load-SyntheaToFhir.ps1

$fhirEndpoint = "https://fhir-claimsiq-dev.azurehealthcareapis.com"
$accessToken = az account get-access-token --resource $fhirEndpoint --query accessToken -o tsv

Get-ChildItem "output/fhir/*.json" | ForEach-Object {
    $bundle = Get-Content $_.FullName -Raw
    
    Invoke-RestMethod -Method POST `
        -Uri "$fhirEndpoint/" `
        -Headers @{ Authorization = "Bearer $accessToken" } `
        -ContentType "application/fhir+json" `
        -Body $bundle
    
    Write-Host "Loaded $($_.Name)"
}
```

### Step 4: Aggregate Baseline
```bash
# Call baseline aggregation API
curl -X POST https://funcactuarialest001.azurewebsites.net/api/baseline/aggregate \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"segment": "All", "period": "last12mo"}'
```

### Step 5: Verify Data
```bash
# Check patient count
curl "$fhirEndpoint/Patient?_summary=count"

# Check EOB count
curl "$fhirEndpoint/ExplanationOfBenefit?_summary=count"

# Check baseline PMPM
curl "https://funcactuarialest001.azurewebsites.net/api/baseline?segment=All&period=last12mo"
```

## Infrastructure as Code (Bicep)

### main.bicep
```bicep
param location string = 'eastus'
param environment string = 'dev'

// FHIR Service
resource fhirService 'Microsoft.HealthcareApis/workspaces/fhirservices@2023-11-01' = {
  name: 'fhir-actuarial-${environment}'
  location: location
  kind: 'fhir-R4'
  properties: {
    authenticationConfiguration: {
      authority: 'https://login.microsoftonline.com/${tenant().tenantId}'
      audience: 'https://fhir-actuarial-${environment}.azurehealthcareapis.com'
    }
  }
}

// Cosmos DB
resource cosmosAccount 'Microsoft.DocumentDB/databaseAccounts@2023-11-15' = {
  name: 'cosmos-actuarial-${environment}'
  location: location
  kind: 'GlobalDocumentDB'
  properties: {
    databaseAccountOfferType: 'Standard'
    consistencyPolicy: {
      defaultConsistencyLevel: 'Session'
    }
    locations: [
      {
        locationName: location
        failoverPriority: 0
      }
    ]
  }
}

resource cosmosDatabase 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2023-11-15' = {
  parent: cosmosAccount
  name: 'ActuarialDB'
  properties: {
    resource: {
      id: 'ActuarialDB'
    }
  }
}

resource scenariosContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2023-11-15' = {
  parent: cosmosDatabase
  name: 'scenarios'
  properties: {
    resource: {
      id: 'scenarios'
      partitionKey: {
        paths: ['/owner']
        kind: 'Hash'
      }
    }
  }
}

// Storage Account
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: 'stactuarial${environment}${uniqueString(resourceGroup().id)}'
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    isHnsEnabled: true  // Enable ADLS Gen2
  }
}

// App Service Plan
resource appServicePlan 'Microsoft.Web/serverfarms@2022-09-01' = {
  name: 'asp-actuarial-${environment}'
  location: location
  sku: {
    name: 'B1'
    tier: 'Basic'
  }
}

// Function App (Backend API)
resource functionApp 'Microsoft.Web/sites@2022-09-01' = {
  name: 'func-actuarial-${environment}'
  location: location
  kind: 'functionapp'
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      appSettings: [
        {
          name: 'FHIR_ENDPOINT'
          value: fhirService.properties.authenticationConfiguration.audience
        }
        {
          name: 'COSMOS_ENDPOINT'
          value: cosmosAccount.properties.documentEndpoint
        }
      ]
    }
  }
}

// Static Web App (Blazor)
resource staticWebApp 'Microsoft.Web/staticSites@2022-09-01' = {
  name: 'stapp-actuarial-${environment}'
  location: location
  sku: {
    name: 'Free'
    tier: 'Free'
  }
}
```

## Deliverables Summary

### 1. Solution Structure
```
ActuarialEstimator/
├── src/
│   ├── ActuarialEstimator.Api/          (.NET 8 Functions)
│   │   ├── Functions/
│   │   │   ├── BaselineFn.cs
│   │   │   ├── ScenariosFn.cs
│   │   │   └── RiskWeightsFn.cs
│   │   ├── Services/
│   │   │   ├── FhirBaselineService.cs
│   │   │   ├── RiskScoringEngine.cs
│   │   │   ├── ForecastEngine.cs
│   │   │   └── CosmosScenarioStore.cs
│   │   ├── Models/
│   │   │   ├── BaselineMetrics.cs
│   │   │   ├── Scenario.cs
│   │   │   ├── ScenarioKnobs.cs
│   │   │   ├── ScenarioResult.cs
│   │   │   ├── RiskWeights.cs
│   │   │   └── RiskScore.cs
│   │   └── Program.cs
│   └── ActuarialEstimator.BlazorWasm/    (.NET 10 Blazor)
│       ├── Pages/
│       │   └── ActuarialEstimator.razor
│       ├── Components/
│       │   ├── BaselineView.razor
│       │   ├── ScenarioBuilder.razor
│       │   ├── ScenarioCompare.razor
│       │   └── ScenarioLibrary.razor
│       ├── Services/
│       │   └── ActuarialService.cs
│       └── Models/
│           └── ActuarialModels.cs
├── tests/
│   └── ActuarialEstimator.Tests/
│       ├── ForecastEngineTests.cs
│       └── RiskScoringTests.cs
├── infra/
│   ├── main.bicep
│   └── params.dev.json
└── scripts/
    ├── Load-SyntheaToFhir.ps1
    └── Generate-SyntheaData.sh
```

### 2. Testing Checklist
- [ ] Unit tests: ForecastEngine (20+ scenarios)
- [ ] Unit tests: RiskScoringEngine (demographic, condition, encounter)
- [ ] Integration tests: FHIR baseline aggregation
- [ ] Integration tests: Cosmos scenario CRUD
- [ ] UI tests: Scenario builder knobs
- [ ] UI tests: Waterfall chart rendering
- [ ] Load tests: 1000 concurrent baseline queries
- [ ] Load tests: 100 concurrent forecast calculations

### 3. Cost Estimate (Dev Environment)
- AHDS FHIR: ~$300/month
- Cosmos DB: ~$25/month (serverless)
- Storage: ~$5/month
- App Service: ~$15/month (B1)
- Static Web App: Free
- **Total**: ~$345/month

### 4. Cost Estimate (Production Environment)
- AHDS FHIR: ~$1,200/month (with throughput)
- Cosmos DB: ~$200/month (autoscale)
- Storage: ~$50/month
- App Service: ~$200/month (P1v2)
- Static Web App: ~$10/month (Standard)
- **Total**: ~$1,660/month

## Next Steps
1. Generate Synthea data (1000 patients)
2. Load to AHDS FHIR dev instance
3. Implement baseline aggregation service
4. Implement risk scoring engine with weights
5. Implement forecast engine with tests
6. Build Blazor UI with mock data
7. Connect UI to API
8. Deploy to Azure dev environment
9. User acceptance testing with actuaries
10. Production deployment

---

**Document Version**: 1.0  
**Last Updated**: January 8, 2026  
**Author**: GitHub Copilot + User  
**Status**: Ready for Implementation
