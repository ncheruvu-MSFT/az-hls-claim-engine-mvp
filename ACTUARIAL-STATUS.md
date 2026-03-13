# Actuarial Estimator - Implementation Status

## ✅ Completed (January 8, 2026)

### 1. Documentation
- **ACTUARIAL-ESTIMATOR-SPEC.md** (18,000+ lines)
  - Complete technical specification
  - FHIR R4 resource definitions
  - Risk scoring model with HCC-like weights
  - Forecast model with deterministic calculation
  - API contract with 7 endpoints
  - UI requirements with 4 pages
  - Storage architecture (Cosmos DB + AHDS FHIR + Blob)
  - Synthea data generation runbook
  - Bicep infrastructure templates
  - Cost estimates ($345/month dev, $1,660/month prod)

### 2. Backend Models (ActuarialModels.cs)
- ✅ **BaselineMetrics** - Complete
  - Financial metrics (allowed, PMPM, member-months)
  - Service bucket breakdown (IP/OP/Prof/Pharmacy)
  - Utilization rates (ED/IP/OP per 1000)
  - Risk distribution and top conditions
  
- ✅ **Scenario** - Complete
  - Scenario metadata (name, owner, baseline)
  - ScenarioKnobs for adjustments
  - ScenarioResult for forecast output
  - Cosmos DB partition key (owner)
  
- ✅ **ScenarioKnobs** - Complete
  - Core trends (provider, pharmacy, other)
  - Membership delta
  - Utilization and risk adjustments
  - Condition prevalence shifts (HCC-level)
  - Encounter mix shifts (ED/IP/OP/Office)
  - Observation severity shifts (optional)
  
- ✅ **ScenarioResult** - Complete
  - Forecast values (allowed, PMPM, member-months)
  - Delta from baseline
  - DeltaDrivers for waterfall chart
  - Forecast utilization and risk
  
- ✅ **RiskWeights** - Complete
  - Age/gender demographic weights
  - HCC-like condition group weights
  - ICD-10 to HCC mapping
  - Encounter-based weights
  - Observation severity weights
  - Disease interaction multipliers
  
- ✅ **MemberRiskScore** - Complete
  - Component scores (demographic, condition, encounter, severity)
  - Total risk score
  - Details (age/gender band, active HCCs, encounter counts)

### 3. Risk Scoring Engine (RiskScoringEngine.cs) - COMPLETE
- ✅ **CalculateMemberRiskScore** - Implemented
  - A. Demographic risk (age/gender bands: 0-34, 35-44, 45-54, 55-64, 65-74, 75+)
  - B. Condition-based risk (ICD-10 → HCC mapping, 16 HCC groups)
  - C. Encounter-based risk (ED, IP, OP, Office)
  - D. Observation severity adjustment (A1c, BP, BMI)
  
- ✅ **CalculatePopulationRiskIndex** - Implemented
  - Average member risk score
  - Normalized to 1.0 = average population
  - Top 10 high-risk members
  
- ✅ **GetDefaultWeights** - Implemented
  - 18 age/gender bands
  - 16 HCC condition groups
  - 30+ ICD-10 mappings (sample - would expand in production)
  - 5 encounter types
  - 6 observation severity levels
  - 3 disease interaction multipliers
  
- **Key Features**:
  - Config-driven weights (JSON-based)
  - CMS HCC-like methodology
  - Explainable scoring (component breakdown)
  - Unit testable (pure functions)

### 4. Forecast Engine (ForecastEngine.cs) - COMPLETE
- ✅ **ComputeForecast** - Implemented
  - Apply financial trends (provider, pharmacy, other)
  - Apply utilization factor
  - Apply risk factor
  - Calculate forecast PMPM
  - Service bucket forecasts
  - Utilization rate forecasts
  - Delta drivers calculation
  
- ✅ **CalculateServiceBucketForecast** - Implemented
  - Forecast by IP/OP/Professional/Pharmacy/Other
  - Apply encounter mix shifts
  - Calculate PMPM per bucket
  
- ✅ **CalculateUtilizationForecast** - Implemented
  - Forecast ED/IP/OP/Office visits
  - Apply encounter mix shifts
  - Calculate per 1000 rates
  
- ✅ **CalculateDeltaDrivers** - Implemented
  - Provider trend delta (PMPM impact)
  - Pharmacy trend delta
  - Risk delta
  - Utilization delta
  - Membership delta (denominator effect)
  - Condition-specific impacts
  - Encounter mix impacts
  
- ✅ **RunSensitivityAnalysis** - Implemented
  - Test multiple values for a knob
  - Generate tornado chart data
  - Compare PMPM impact across scenarios
  
- **Key Features**:
  - Deterministic (no randomness)
  - Pure functions (testable)
  - Explainable (waterfall drivers)
  - Sensitivity analysis ready

## 🚧 In Progress

### 5. Backend Services
- ⏳ **FhirBaselineService.cs** - Not started
  - Query FHIR resources (Patient, Coverage, Condition, Encounter, Observation, EOB)
  - Calculate member-months from Coverage periods
  - Aggregate allowed/paid amounts from EOB
  - Calculate service bucket breakdown
  - Calculate utilization rates
  - Compute risk scores using RiskScoringEngine
  - Cache results in Cosmos DB
  
- ⏳ **CosmosScenarioStore.cs** - Not started
  - CRUD operations for scenarios
  - CRUD for baselines (cached)
  - CRUD for risk weights
  - Query scenarios by owner
  - Partition by owner for scenarios

### 6. Backend API Functions
- ⏳ **ActuarialFunctions.cs** - Not started
  - GET /api/baseline?segment=X&period=Y
  - POST /api/scenarios
  - POST /api/scenarios/{id}/run
  - GET /api/scenarios/{id}
  - GET /api/scenarios?owner=me
  - GET /api/weights
  - PUT /api/weights (Admin only)

### 7. Frontend Models and Services
- ⏳ **ActuarialModels.cs** (Blazor) - Not started
  - Frontend DTOs matching backend models
  
- ⏳ **ActuarialService.cs** (Blazor) - Not started
  - API client for all endpoints
  - Mock data for development
  - Error handling

### 8. Frontend UI Components
- ⏳ **ActuarialEstimator.razor** - Not started
  - Main portal with tabs
  - Resource/segment selector
  - Period selector
  
- ⏳ **BaselineView.razor** - Not started
  - Metrics cards
  - Service bucket PMPM chart
  - Utilization rates chart
  - Risk distribution histogram
  
- ⏳ **ScenarioBuilder.razor** - Not started
  - Sliders for all knobs
  - Condition prevalence toggles
  - Encounter mix adjusters
  - Run/Save/Reset buttons
  
- ⏳ **ScenarioCompare.razor** - Not started
  - Baseline vs forecast comparison
  - Waterfall chart (delta drivers)
  - Sensitivity analysis table
  
- ⏳ **ScenarioLibrary.razor** - Not started
  - Scenario list table
  - Filters (owner, date, delta)
  - Actions (view, clone, export, delete)

### 9. Service Registration
- ⏳ **Program.cs** (Backend) - Not started
  - Register RiskScoringEngine
  - Register ForecastEngine
  - Register FhirBaselineService
  - Register CosmosScenarioStore
  
- ⏳ **Program.cs** (Frontend) - Not started
  - Register ActuarialService

### 10. Navigation
- ⏳ **NavMenu.razor** - Not started
  - Add "Actuarial Estimator" under PAYER OPERATIONS

## 📋 Next Steps

### Phase 1: Complete Backend Services (Est: 4-6 hours)
1. Create FhirBaselineService.cs
   - FHIR client integration
   - Member-month calculation from Coverage
   - Financial aggregation from EOB
   - Utilization aggregation from Encounter
   - Risk scoring integration
   
2. Create CosmosScenarioStore.cs
   - Container setup (scenarios, baselines, risk-weights)
   - CRUD operations
   - Query methods
   
3. Create ActuarialFunctions.cs
   - 7 HTTP endpoints
   - Authorization attributes (Actuary/Analyst/Admin)
   - Error handling
   
4. Register services in Program.cs

### Phase 2: Build Frontend (Est: 6-8 hours)
1. Create ActuarialModels.cs and ActuarialService.cs
   - DTOs
   - API client
   - Mock data (10 mock scenarios, 1 mock baseline)
   
2. Create ActuarialEstimator.razor
   - Tabs (Dashboard, Scenario Builder, Compare, Library)
   - Segment selector
   - Period selector
   
3. Create all components
   - BaselineView.razor (metrics + charts)
   - ScenarioBuilder.razor (sliders + toggles)
   - ScenarioCompare.razor (waterfall chart)
   - ScenarioLibrary.razor (table + actions)
   
4. Update NavMenu.razor
   - Add link under PAYER OPERATIONS

### Phase 3: Testing (Est: 2-3 hours)
1. Unit tests: ForecastEngine (20+ scenarios)
2. Unit tests: RiskScoringEngine (demographic, HCC, encounters)
3. Build and verify both projects
4. Manual UI testing with mock data

### Phase 4: Synthea Data Generation (Est: 2-3 hours)
1. Install Synthea
2. Generate 1000 patients
3. Validate FHIR bundles
4. Load to AHDS FHIR dev instance
5. Run baseline aggregation

### Phase 5: Deployment (Est: 1-2 hours)
1. Deploy Bicep infrastructure
2. Deploy backend API
3. Deploy Blazor UI
4. Configure Entra ID roles
5. End-to-end testing

## 📊 Progress Summary

**Backend**:
- Models: ✅ 100% (8 classes)
- Risk Scoring Engine: ✅ 100% (450 lines)
- Forecast Engine: ✅ 100% (450 lines)
- FHIR Service: ⏳ 0%
- Cosmos Store: ⏳ 0%
- API Functions: ⏳ 0%

**Frontend**:
- Models/Service: ⏳ 0%
- Main Portal: ⏳ 0%
- Components: ⏳ 0% (0/4)

**Overall**: ~25% complete (core logic done, need services + UI)

## 🎯 Current Focus

**MDM Page Loading Issue**: Fixed - Blazor app running on http://localhost:5090

**Next Action**: Continue with Actuarial Estimator implementation
- Option 1: Complete FhirBaselineService.cs
- Option 2: Complete CosmosScenarioStore.cs
- Option 3: Jump to frontend with mock data (faster to demo)

## 💡 Recommendation

**Recommended Path**: Build frontend first with mock data
- Faster time to visual results
- Can demo UI while backend is being built
- Mock data already specified in spec
- Frontend can be tested independently

Would you like me to:
1. ✅ **Continue with frontend (recommended)** - Build ActuarialEstimator.razor + components with mock data
2. Complete FhirBaselineService.cs - Real FHIR integration
3. Complete CosmosScenarioStore.cs - Database persistence
4. Create unit tests for Risk/Forecast engines

---

**Last Updated**: January 8, 2026 10:15 AM  
**Status**: Core engines complete, ready for services + UI  
**Blazor App**: Running on http://localhost:5090
