SYSTEM
You are an expert healthcare payer solution architect + senior .NET engineer.
Default to SYNTHETIC data in dev/test to avoid PHI exposure.
Design for auditability, explainability (actuarial transparency), and separation between clinical source-of-truth and simulation stores.

USER GOAL
Build an “Actuarial Estimator” app that:
1) Uses Azure Health Data Services FHIR service (FHIR R4) as baseline source-of-truth.
2) Uses BOTH clinical + claims signals to compute population risk and forecast next-year costs.
3) Provides a .NET UI (Blazor) to create “what-if” scenarios by adjusting knobs (membership, provider charges, pharmacy trend, clinical risk, utilization/encounters).
4) Stores scenario parameters and results separately from the clinical FHIR store.

SYNTHETIC DATA REQUIREMENT (MITRE SYNTHEA)
Use MITRE Synthea to generate synthetic clinical data with claim-like artifacts:
- Customize modules/population using: https://mitre.github.io/fhir-for-research/modules/synthea-customizing
- Run Synthea using: https://mitre.github.io/fhir-for-research/modules/synthea-running
- Ensure FHIR output includes at minimum:
  Patient, Coverage, Condition, Encounter, Observation
  AND (for claims) Claim and/or ExplanationOfBenefit.
- Use Synthea FHIR supported resources list as reference: https://github-wiki-see.page/m/synthetichealth/synthea/wiki/HL7-FHIR

FHIR R4 BASELINE RESOURCES (Clinical + Claims)
Use these FHIR R4 resources:
- Patient: demographics (birthDate, gender, geography)
- Coverage: plan/product + coverage period for member-months
- Condition: chronic conditions / morbidity
- Encounter: utilization signals (ED, IP, OP, visits)
- Observation: clinical severity proxies (A1c, BP, BMI, labs) when available
- ExplanationOfBenefit (preferred) OR Claim: allowed/paid amounts and claim categories

BASELINE TIME WINDOW
Use last 12 months as baseline (configurable):
- Clinical: Conditions active in window, Encounters in window, Observations in window
- Claims: EOB/Claim created/service date in window

BASELINE OUTPUTS
Compute:
- MemberMonths (from Coverage periods)
- BaselineAllowed, BaselinePaid, BaselinePMPM
- Service bucket PMPM: Inpatient, Outpatient, Professional, Pharmacy
- Utilization rates: ED visits/1k, IP admits/1k, OP visits/1k
- Clinical risk summary: risk score distribution + top conditions

RISK MODEL REQUIREMENTS (Explainable v1)
Implement a transparent, configurable risk model that combines:
A) Demographic risk (age bands, gender)
B) Condition-based risk (HCC-like categories or weighted condition groups)
C) Encounter-based risk (ED/IP utilization signals)
D) Optional Observation-based severity adjustment

Implementation guidance:
- Build a RiskScore per member and aggregate to PopulationRiskIndex.
- Use a config-driven table of weights (JSON/CSV) so actuaries can modify weights without code.
- If you implement HCC-like categories, provide a mapping approach:
  - Condition.code (ICD-10/SNOMED) -> ConditionGroup/HCC
  - Apply group weights; allow disease interaction multipliers (optional).

FORECAST MODEL (Phase 1 deterministic)
1) Compute baseline components:
   - providerAllowed (IP+OP+Prof)
   - pharmacyAllowed
   - otherAllowed (optional bucket)
   - baselineMemberMonths
   - baselinePopulationRiskIndex
   - baselineUtilizationIndex

2) Scenario knobs drive forecast:
   - providerTrendPct affects providerAllowed
   - pharmacyTrendPct affects pharmacyAllowed
   - utilizationDeltaPct affects encounter/utilization indices
   - populationRiskDeltaPct affects population risk
   - membershipDeltaPct affects memberMonths and optionally utilization volume

3) Forecast math (example):
   providerComponent = providerAllowed * (1 + providerTrendPct)
   pharmacyComponent = pharmacyAllowed * (1 + pharmacyTrendPct)
   baseAllowed = providerComponent + pharmacyComponent + otherAllowed

   utilizationFactor = (1 + utilizationDeltaPct)
   riskFactor = (1 + populationRiskDeltaPct)

   forecastAllowed = baseAllowed * utilizationFactor * riskFactor
   forecastMemberMonths = baselineMemberMonths * (1 + membershipDeltaPct)
   forecastPMPM = forecastAllowed / forecastMemberMonths

4) Include “delta drivers” output for a waterfall chart:
   Provider trend delta, Pharmacy trend delta, Risk delta, Utilization delta, Membership delta

UI REQUIREMENTS (.NET)
Frontend: Blazor Server (recommended) OR Blazor WASM + ASP.NET Core API
Pages:
1) Baseline: choose segment (plan/region/age band) + show baseline charts
2) Scenario Builder: sliders + condition prevalence toggles + run/save
3) Compare: baseline vs scenario + waterfall drivers + sensitivity chart
4) Scenario Library: list, clone, export

SCENARIO KNOBS (UI) – include clinical + encounter knobs
Core knobs:
- membershipDeltaPct (-30%..+30%)
- providerTrendPct (-20%..+20%)
- pharmacyTrendPct (-30%..+30%)
- utilizationDeltaPct (-15%..+15%)
- populationRiskDeltaPct (-10%..+10%)

Clinical knobs (at least two):
- conditionPrevalenceShiftPct per group (e.g., Diabetes +5%, CHF +2%)
- encounterMixShiftPct (ED vs OP vs IP)
Optional:
- observationSeverityShiftPct (e.g., A1c worsening/improving)

BACKEND (.NET 8)
- FHIR client integration (R4)
- Baseline aggregation service with caching (segment+period key)
- Risk scoring engine (config-driven weights)
- Forecast engine (deterministic, pure functions + unit tests)
- Scenario storage connector
- Auth: Microsoft Entra ID (roles: Actuary, Analyst, Admin)

API CONTRACT
- GET  /api/baseline?segment=...&period=last12mo
- POST /api/scenarios             (create)
- POST /api/scenarios/{id}/run    (compute)
- GET  /api/scenarios/{id}
- GET  /api/scenarios?owner=me
- GET  /api/weights              (get risk weights)
- PUT  /api/weights              (update risk weights; admin only)

STORAGE OPTIONS (pros/cons + recommendation)
Option 1) Secondary FHIR server for simulation artifacts
- Pros: FHIR-native interoperability
- Cons: scenario docs/aggregates not naturally FHIR; higher cost/ops

Option 2) Cosmos DB for scenarios/results (RECOMMENDED)
- Pros: best for interactive what-if; JSON docs; low latency; easy partitioning
- Cons: not FHIR-native; mapping needed if exporting back to FHIR

Option 3) Blob/ADLS Gen2 for snapshots/exports
- Pros: cheapest for reproducible baseline snapshots and ML training data
- Cons: not interactive; requires compute layer for querying

FINAL RECOMMENDATION
Use:
- AHDS FHIR service as source-of-truth (synthetic in dev)
- Cosmos DB for scenario inputs/outputs and cached aggregates
- ADLS/Blob for baseline snapshot exports ($export NDJSON) for reproducibility/ML

DELIVERABLES
1) Solution structure (folders)
2) C# models: Scenario, ScenarioKnobs, BaselineMetrics, RiskWeights, RiskScore, ScenarioResult
3) Sample FHIR queries (Condition/Encounter/Observation/EOB) + member-month calc from Coverage
4) ForecastEngine implementation + unit tests
5) UI component outline with sliders + condition toggles + charts
6) IaC skeleton (Bicep) for FHIR + Cosmos + App Service + Storage
7) A “synthea-to-fhir-load” runbook (generate -> validate -> load -> aggregate)

GUARDRAILS
- Never use real PHI in dev.
- Enforce least privilege and PHI minimization.
- Always produce explainable outputs suitable for actuaries and auditors.
new portal with tabs for this