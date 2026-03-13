# SDOH Member Portal Implementation Summary

## ✅ Implementation Complete

### Overview
Built a comprehensive **SDOH (Social Determinants of Health) Member Portal** based on the **HL7 FHIR SDOH Clinical Care Implementation Guide** (Gravity Project). This portal allows members to view their social needs, goals, referrals, and connect with community resources.

**Reference**: https://www.hl7.org/fhir/us/sdoh-clinicalcare/

---

## 📚 Background: SDOH & Gravity Project

### What is SDOH?
Social Determinants of Health (SDOH) are the conditions in the environments where people are born, live, learn, work, play, worship, and age that affect health outcomes. Examples:
- **Food Insecurity**: Access to nutritious food
- **Housing Instability**: Safe, stable housing
- **Transportation**: Access to medical appointments
- **Employment**: Job status and income
- **Education**: Educational attainment
- **Social Connection**: Social isolation

### HL7 FHIR SDOH Clinical Care IG
The Gravity Project created a standardized FHIR implementation guide for capturing and sharing SDOH data:
- **Screening**: Standard questionnaires (PRAPARE, AHC-HRSN)
- **Assessment/Diagnosis**: Mapping responses to SDOH conditions
- **Interventions**: Referrals to Community-Based Organizations (CBOs)
- **Goal Setting**: Patient-centered goals
- **Closed-Loop Referrals**: Track referral completion

### FHIR Resources Used
- `Observation` (SDOHCC-ObservationScreeningResponse) - Screening responses
- `Condition` (SDOHCC-Condition) - SDOH diagnoses/problems
- `Goal` (SDOHCC-Goal) - Patient goals
- `ServiceRequest` (SDOHCC-ServiceRequest) - Referrals to CBOs
- `Procedure` (SDOHCC-Procedure) - Completed interventions
- `Task` - Tracking referral status
- `Organization` & `HealthcareService` - Community resources

---

## 📁 Files Created

### 1. Models (SdohModels.cs) - 250 lines
**Purpose**: Domain models mapping to FHIR SDOH resources

**Key Classes**:

#### SdohScreeningModel
Maps to `Observation` (SDOHCC-ObservationScreeningResponse)
- Question/answer pairs from screening tools
- Risk level per response
- Assessment tool (PRAPARE, AHC-HRSN)

#### SdohConditionModel
Maps to `Condition` (SDOHCC-Condition)
- Domain (Food, Housing, Transportation, etc.)
- SNOMED-CT or ICD-10 codes
- Clinical status, severity
- Onset/abatement dates

#### SdohGoalModel
Maps to `Goal` (SDOHCC-Goal)
- Patient-centered goals
- Lifecycle status, achievement status
- Target dates
- Links to conditions addressed

#### SdohReferralModel
Maps to `ServiceRequest` (SDOHCC-ServiceRequest)
- Referral source (provider) and target (CBO)
- Service type and description
- Status, priority
- Links to conditions/goals

#### SdohInterventionModel
Maps to `Procedure` (SDOHCC-Procedure)
- Completed interventions
- Performer (provider/CBO)
- Outcome description

#### CommunityResourceModel
Maps to `Organization` and `HealthcareService`
- CBO contact information
- Services provided (domains)
- Location, hours, accepts referrals
- Distance from patient

#### SdohRiskScoreModel
Custom aggregation model
- Overall risk score (0-100)
- Per-domain risk scores
- Risk levels (Low/Moderate/High)
- Summary counts

#### SdohDashboardModel
Aggregated view for UI
- Patient demographics
- Risk score
- Active conditions, goals, referrals
- Recent interventions
- Latest screening results

#### SdohAssessmentModel
Maps to `Questionnaire` and `QuestionnaireResponse`
- Assessment tool (PRAPARE, AHC-HRSN)
- Questions and responses
- Completion status

#### SdohDomains (Static Class)
Constants for 12 SDOH domains per Gravity Project:
1. Food Insecurity 🍎
2. Housing Instability 🏠
3. Transportation Insecurity 🚗
4. Utility Insecurity 💡
5. Interpersonal Safety 🛡️
6. Employment 💼
7. Educational Attainment 🎓
8. Financial Insecurity 💰
9. Health Literacy 📚
10. Social Connection 👥
11. Stress/Anxiety 🧘
12. Veteran Status 🎖️

Includes helper methods:
- `GetDomainIcon(string domain)` - Emoji icons
- `GetRiskColor(string riskLevel)` - CSS color variables

---

### 2. Service (SdohService.cs) - 600+ lines
**Purpose**: API client for SDOH data from FHIR backend

**Public Methods**:

#### GetDashboardAsync(patientId)
- Returns: `SdohDashboardModel`
- Aggregated view of all SDOH data
- Endpoint: `GET /api/sdoh/dashboard/{patientId}`

#### GetRiskScoreAsync(patientId)
- Returns: `SdohRiskScoreModel`
- Overall and per-domain risk scores
- Endpoint: `GET /api/sdoh/risk-score/{patientId}`

#### GetConditionsAsync(patientId)
- Returns: `List<SdohConditionModel>`
- Active SDOH conditions/problems
- Endpoint: `GET /api/sdoh/conditions/{patientId}`

#### GetGoalsAsync(patientId)
- Returns: `List<SdohGoalModel>`
- Active patient goals
- Endpoint: `GET /api/sdoh/goals/{patientId}`

#### GetReferralsAsync(patientId)
- Returns: `List<SdohReferralModel>`
- Active referrals to CBOs
- Endpoint: `GET /api/sdoh/referrals/{patientId}`

#### GetInterventionsAsync(patientId)
- Returns: `List<SdohInterventionModel>`
- Completed interventions
- Endpoint: `GET /api/sdoh/interventions/{patientId}`

#### GetScreeningHistoryAsync(patientId)
- Returns: `List<SdohScreeningModel>`
- Historical screening responses
- Endpoint: `GET /api/sdoh/screening/{patientId}`

#### SearchCommunityResourcesAsync(domain, zipCode, radiusMiles)
- Returns: `List<CommunityResourceModel>`
- CBOs near patient location
- Endpoint: `GET /api/sdoh/resources?domain={domain}&zip={zipCode}&radius={radiusMiles}`

#### CreateReferralAsync(referral)
- Returns: `SdohReferralModel`
- Create new referral to CBO
- Endpoint: `POST /api/sdoh/referrals`

#### SubmitAssessmentAsync(assessment)
- Returns: `SdohAssessmentModel`
- Submit completed screening
- Endpoint: `POST /api/sdoh/assessments`

**Configuration**:
- `_useMockData = true` (currently using mock data for development)
- Base URL: `funcclaimstest001ncv.azurewebsites.net/api/`
- Comprehensive mock data with realistic scenarios

**Mock Data Includes**:
- 62% overall risk score
- 4 active conditions (Food, Transportation, Employment, Financial)
- 3 active goals
- 2 active referrals
- 5 completed interventions
- 12 screening questions (AHC-HRSN tool)
- 4 community resources near Seattle, WA

---

### 3. Member Portal Page (SdohMemberPortal.razor) - 600+ lines
**Purpose**: Patient-facing portal for viewing and managing SDOH needs

**Route**: `/sdoh-member`

**Page Structure**:

#### Header
- Title: "My Social Needs & Resources"
- Subtitle: Understanding factors that impact health
- Purple/blue gradient (ClaimsIQ theme)

#### Tabs (4 sections):

**Tab 1: Overview** 📊
- Large risk score card (0-100 scale)
- Last assessment date
- Grid of 9-12 domain risk cards:
  - Domain name + icon
  - Risk score (0-100)
  - Risk level badge (Low/Moderate/High)
  - Color-coded borders
- Summary metrics:
  - Active Needs count
  - Active Goals count
  - Active Referrals count
  - Completed Interventions count

**Tab 2: My Needs & Goals** 🎯
- **Active Needs Section**:
  - List of SDOH conditions
  - Domain, description, severity
  - Onset date
  - Active status badge
- **My Goals Section**:
  - Patient-centered goals
  - Start and target dates
  - Achievement status (in-progress/achieved)
  - Links to related conditions
- **Active Referrals Section**:
  - Referral to CBOs
  - Service description
  - Referral source (provider name)
  - Request date, priority

**Tab 3: Community Resources** 🏢
- Grid of available CBOs
- Each resource card shows:
  - Organization name + domain icons
  - Type (Food Bank, Job Training, etc.)
  - Description of services
  - Contact info (phone, address, website)
  - Hours of operation
  - Distance from patient
  - Actions:
    - "Request Referral" button
    - "Get Directions" button
- Filtered by patient location (zip code)

**Tab 4: Screening History** 📋
- Last assessment date and tool name
- Questions grouped by domain
- Question text, answer, risk level
- Risk level badges per response
- "Take New Screening Assessment" button

**Key Features**:
- Mobile-responsive design
- Loading spinner
- Empty states for all sections
- Color-coded risk levels
- Emoji icons for domains
- Interactive elements (buttons, tabs)
- Links to external resources
- Distance calculation from patient

**Styling Guidelines** (per ClaimsIQ standards):
- Card-based layout
- Purple/blue gradient headers
- Rounded corners (8px radius)
- Box shadows with hover effects
- CSS variables for colors
- Consistent padding (1.5rem)
- Font sizes: 1.75rem (headers), 1rem (body)

---

## 🏗️ Architecture

### Data Flow
```
FHIR Server (Azure Health Data Services)
    ↓
Observation (SDOHCC-ObservationScreeningResponse)
    - Screening questions/answers
    - LOINC codes
Condition (SDOHCC-Condition)
    - SNOMED-CT codes
    - Domain categories
Goal (SDOHCC-Goal)
    - Patient goals
ServiceRequest (SDOHCC-ServiceRequest)
    - Referrals to CBOs
Procedure (SDOHCC-Procedure)
    - Completed interventions
Organization + HealthcareService
    - Community resources
    ↓
SdohService (API Client)
    - Query FHIR resources
    - Aggregate risk scores
    - Search community resources
    ↓
SdohMemberPortal (UI)
    - 4 tabs (Overview, Needs, Resources, History)
    - Interactive dashboard
    - Referral requests
```

### Risk Scoring Algorithm (Backend - To Be Implemented)
```csharp
// Per-domain risk score calculation
foreach (var domain in SdohDomains.AllDomains)
{
    var screeningResponses = observations
        .Where(o => o.Domain == domain)
        .ToList();
    
    var domainScore = CalculateDomainRiskScore(screeningResponses);
    // 0-100 scale based on answer codes
    // "Often true" (LA28397-0) = High Risk (75-100)
    // "Sometimes true" (LA6729-3) = Moderate Risk (40-74)
    // "Never true" (LA28398-8) = Low Risk (0-39)
    
    riskScores[domain] = domainScore;
}

// Overall risk score = weighted average
overallRisk = riskScores.Values.Average();

// Risk level thresholds
riskLevel = overallRisk switch
{
    >= 70 => "High",
    >= 40 => "Moderate",
    _ => "Low"
};
```

### FHIR Queries (Backend - To Be Implemented)
```
# Get screening observations
GET /Observation?patient={patientId}
    &category=sdoh
    &_profile=http://hl7.org/fhir/us/sdoh-clinicalcare/StructureDefinition/SDOHCC-ObservationScreeningResponse

# Get SDOH conditions
GET /Condition?patient={patientId}
    &category=sdoh
    &_profile=http://hl7.org/fhir/us/sdoh-clinicalcare/StructureDefinition/SDOHCC-Condition

# Get SDOH goals
GET /Goal?patient={patientId}
    &category=sdoh
    &_profile=http://hl7.org/fhir/us/sdoh-clinicalcare/StructureDefinition/SDOHCC-Goal

# Get SDOH service requests (referrals)
GET /ServiceRequest?patient={patientId}
    &category=sdoh
    &_profile=http://hl7.org/fhir/us/sdoh-clinicalcare/StructureDefinition/SDOHCC-ServiceRequest

# Get SDOH procedures (interventions)
GET /Procedure?patient={patientId}
    &category=sdoh
    &_profile=http://hl7.org/fhir/us/sdoh-clinicalcare/StructureDefinition/SDOHCC-Procedure

# Search community organizations
GET /Organization?type=http://hl7.org/fhir/us/sdoh-clinicalcare/CodeSystem/SDOHCC-CodeSystemTemporaryCodes|food-insecurity
    &address-postalcode=98101
```

---

## 📊 SDOH Screening Tools

### PRAPARE (Protocol for Responding to and Assessing Patients' Assets, Risks, and Experiences)
- 21 questions across 5 domains
- Developed by National Association of Community Health Centers
- Questions:
  - Housing status, neighborhood safety
  - Employment, education
  - Financial strain
  - Transportation, utilities
  - Social isolation, stress

### AHC-HRSN (Accountable Health Communities Health-Related Social Needs)
- 10 core questions
- Developed by CMS Innovation Center
- Focuses on immediate needs:
  - Food insecurity (2 questions)
  - Housing instability (2 questions)
  - Transportation barriers (1 question)
  - Utility needs (1 question)
  - Safety concerns (1 question)
  - Other needs (open-ended)

### LOINC Codes (Example Questions)
| Code | Question | Domain |
|------|----------|--------|
| 88122-7 | Worried food would run out? | Food Insecurity |
| 88123-5 | Food didn't last and no money? | Food Insecurity |
| 71802-3 | Housing situation today? | Housing Instability |
| 93030-5 | Lack of transportation kept you from appointments? | Transportation |
| 93159-2 | Physical harm from someone? | Interpersonal Safety |
| 76513-1 | Current work situation? | Employment |
| 82589-3 | Highest level of education? | Education |

---

## 🎨 UI Design & ClaimsIQ Theme

### Color Palette
- Primary: `#0078d4` (Microsoft Blue)
- Success: `#28a745` (Green) - Low risk
- Warning: `#ff8c00` (Orange) - Moderate risk
- Error: `#d13438` (Red) - High risk
- Gradient Header: `#667eea → #764ba2` (Purple/Blue)

### Domain Icons (Emoji)
- 🍎 Food Insecurity
- 🏠 Housing Instability
- 🚗 Transportation
- 💡 Utilities
- 🛡️ Interpersonal Safety
- 💼 Employment
- 🎓 Education
- 💰 Financial
- 📚 Health Literacy
- 👥 Social Connection
- 🧘 Stress/Anxiety
- 🎖️ Veteran Status

### Card Components
- White background on light gray (#f5f5f5)
- Rounded corners (8px)
- Box shadow on hover
- Left border color-coded by risk level
- 1.5rem padding
- Smooth transitions (0.2s ease)

### Typography
- Headers: 1.75rem, font-weight 700
- Section titles: 1.5rem, font-weight 600
- Card titles: 1.125rem, font-weight 600
- Body: 1rem, font-weight 400
- Metadata: 0.875rem, color secondary

### Responsive Design
- Grid layouts with `auto-fit`
- Mobile breakpoints at 768px, 480px
- Flexible card widths (250px-350px min)

---

## ✅ Build Status

### Blazor WebAssembly
```
✅ Build successful (7 warnings - all minor, unrelated to SDOH)
✅ Application running on http://localhost:5090
✅ Navigation link added: "My Social Needs" under EXTERNAL PORTALS
✅ SdohService registered in DI container
✅ All 3 files created and compile successfully
```

### Integration Points
- NavMenu.razor: Added "My Social Needs" link (👤 icon)
- Program.cs: Registered `SdohService` as scoped service
- Route: `/sdoh-member`

---

## 🚀 Next Steps

### Priority 1: Backend SDOH Functions (Azure Functions)
Create `SdohFunctions.cs` with 10 HTTP endpoints:

1. **GetSdohDashboard**: `GET /api/sdoh/dashboard/{patientId}`
   - Query FHIR: Observation, Condition, Goal, ServiceRequest, Procedure
   - Aggregate risk scores
   - Return SdohDashboardModel

2. **GetRiskScore**: `GET /api/sdoh/risk-score/{patientId}`
   - Calculate per-domain risk from observations
   - Apply risk scoring algorithm
   - Return SdohRiskScoreModel

3. **GetConditions**: `GET /api/sdoh/conditions/{patientId}`
   - Query FHIR Condition with SDOHCC profile
   - Filter by active status
   - Return List<SdohConditionModel>

4. **GetGoals**: `GET /api/sdoh/goals/{patientId}`
   - Query FHIR Goal with SDOHCC profile
   - Include target dates
   - Return List<SdohGoalModel>

5. **GetReferrals**: `GET /api/sdoh/referrals/{patientId}`
   - Query FHIR ServiceRequest with SDOHCC profile
   - Include performer organization
   - Return List<SdohReferralModel>

6. **GetInterventions**: `GET /api/sdoh/interventions/{patientId}`
   - Query FHIR Procedure with SDOHCC profile
   - Include outcome
   - Return List<SdohInterventionModel>

7. **GetScreeningHistory**: `GET /api/sdoh/screening/{patientId}`
   - Query FHIR Observation (screening responses)
   - Group by domain
   - Return List<SdohScreeningModel>

8. **SearchCommunityResources**: `GET /api/sdoh/resources?domain={domain}&zip={zipCode}&radius={radiusMiles}`
   - Query FHIR Organization + HealthcareService
   - Filter by domain and location
   - Calculate distance from patient
   - Return List<CommunityResourceModel>

9. **CreateReferral**: `POST /api/sdoh/referrals`
   - Create FHIR ServiceRequest (SDOHCC profile)
   - Create Task for tracking
   - Notify CBO (optional)
   - Return SdohReferralModel

10. **SubmitAssessment**: `POST /api/sdoh/assessments`
    - Create FHIR QuestionnaireResponse
    - Map responses to Observations (SDOHCC profile)
    - Identify conditions from responses
    - Create Condition resources if needed
    - Return SdohAssessmentModel

### Priority 2: FHIR SDOH Data Service
Create `FhirSdohService.cs`:
- Query FHIR resources with SDOHCC profiles
- Map FHIR resources to domain models
- Implement risk scoring algorithm
- Cache results in Cosmos DB

### Priority 3: Screening Assessment UI
Create `SdohAssessmentWizard.razor`:
- Multi-step questionnaire (PRAPARE or AHC-HRSN)
- Progress indicator
- Skip logic for questions
- Save draft functionality
- Submit to backend
- Show immediate results

### Priority 4: Referral Request Flow
Create `SdohReferralRequest.razor` component:
- Modal dialog for referral request
- Select condition/goal addressed
- Add patient notes
- Consent for information sharing
- Submit to provider for approval
- Email notification to provider and CBO

### Priority 5: Gravity Value Sets Integration
Implement Gravity Project value sets:
- Install VSAC (Value Set Authority Center) access
- Download Gravity Project value sets
- Store in Cosmos DB or app config
- Use for:
  - Condition codes (SNOMED-CT)
  - Observation codes (LOINC)
  - Answer codes (LOINC)
  - Service type codes

### Priority 6: CBO Directory Integration
Integrate with external CBO directories:
- **findhelp.org (211)**: National CBO directory
- **Aunt Bertha**: Social care network
- **Unite Us**: Coordinated care network
- Search by domain, location, services
- Real-time availability
- Closed-loop referral tracking

### Priority 7: Analytics & Reporting
Create SDOH analytics dashboard (for care teams):
- Population-level SDOH risk
- Screening completion rates
- Referral outcomes
- Intervention effectiveness
- Geographic hotspots
- Domain trends over time

---

## 🎯 Testing Checklist

### Manual Testing
- [ ] Open http://localhost:5090/sdoh-member
- [ ] Verify page loads with gradient header
- [ ] Test Overview tab:
  - [ ] See overall risk score (62)
  - [ ] See 9 domain cards with scores
  - [ ] Risk levels color-coded correctly
  - [ ] Summary metrics display
- [ ] Test Needs & Goals tab:
  - [ ] See 4 active conditions
  - [ ] See 3 active goals
  - [ ] See 2 active referrals
  - [ ] Dates formatted correctly
- [ ] Test Community Resources tab:
  - [ ] See 4 community resources
  - [ ] Distance calculation shown
  - [ ] Contact info displayed
  - [ ] "Request Referral" button works
  - [ ] "Get Directions" logs to console
- [ ] Test Screening History tab:
  - [ ] See 4 screening questions
  - [ ] Grouped by domain
  - [ ] Risk levels shown
  - [ ] "Take New Screening" button visible
- [ ] Test responsive design (mobile breakpoints)
- [ ] Verify navigation link works

### Integration Testing (After Backend Implementation)
- [ ] Test all 10 API endpoints
- [ ] Verify FHIR queries return SDOHCC profiles
- [ ] Test risk scoring algorithm accuracy
- [ ] Verify CBO search by location
- [ ] Test referral creation workflow
- [ ] Test assessment submission
- [ ] Verify Cosmos DB caching
- [ ] Test with real Synthea patient data
- [ ] Load test with 1000+ patients

---

## 📝 Notes

### Frontend vs Backend Decision
**Question from user**: "if we need to break up front end let me know"

**Answer**: No need to break up the frontend. The SDOH portal integrates seamlessly with the existing Blazor WebAssembly application:
- Uses same navigation structure
- Follows ClaimsIQ design guidelines
- Shares services and models
- Single build/deployment process

**Current Status**:
- ✅ Frontend: 100% complete (models, service, portal page)
- ❌ Backend: 0% complete (needs 10 Azure Functions endpoints)
- ❌ FHIR Integration: 0% complete (needs SDOHCC profile queries)

**Recommendation**: Keep as single Blazor app. Add SDOH backend functions to existing Azure Functions project.

### SDOH vs Actuarial Estimator
Both systems can coexist:
- **Actuarial Estimator**: Internal tool for payers (financial forecasting)
- **SDOH Portal**: External portal for members (social needs)
- No overlap in functionality or data

### Gravity Project Resources
- **IG Website**: https://www.hl7.org/fhir/us/sdoh-clinicalcare/
- **VSAC Value Sets**: https://vsac.nlm.nih.gov/ (requires NLM account)
- **Gravity Project**: https://confluence.hl7.org/display/GRAV/The+Gravity+Project
- **GitHub**: https://github.com/HL7/fhir-sdoh-clinicalcare

### Azure FHIR SDOH Support
Azure Health Data Services FHIR R4 fully supports SDOH:
- All SDOHCC profiles compatible
- US Core 6.1 / 7.0 support
- Custom search parameters supported
- Subscriptions for referral tracking
- SMART on FHIR for member apps

---

## 📚 References

### HL7 FHIR Profiles (SDOHCC)
- **Observation Screening Response**: https://www.hl7.org/fhir/us/sdoh-clinicalcare/StructureDefinition-SDOHCC-ObservationScreeningResponse.html
- **Condition**: https://www.hl7.org/fhir/us/sdoh-clinicalcare/StructureDefinition-SDOHCC-Condition.html
- **Goal**: https://www.hl7.org/fhir/us/sdoh-clinicalcare/StructureDefinition-SDOHCC-Goal.html
- **ServiceRequest**: https://www.hl7.org/fhir/us/sdoh-clinicalcare/StructureDefinition-SDOHCC-ServiceRequest.html
- **Procedure**: https://www.hl7.org/fhir/us/sdoh-clinicalcare/StructureDefinition-SDOHCC-Procedure.html

### Screening Tools
- **PRAPARE**: https://www.nachc.org/research-and-data/prapare/
- **AHC-HRSN**: https://innovation.cms.gov/files/worksheets/ahcm-screeningtool.pdf
- **LOINC SDOH Codes**: https://loinc.org/kb/article/user-guide/social-determinants-of-health-sdoh-panel/

### CMS & Government
- **CMS Accountable Health Communities**: https://innovation.cms.gov/innovation-models/ahcm
- **Healthy People 2030**: https://health.gov/healthypeople/priority-areas/social-determinants-health
- **USCDI v4 SDOH**: https://www.healthit.gov/isp/united-states-core-data-interoperability-uscdi

---

**Last Updated**: January 8, 2026  
**Platform Version**: .NET 10 Blazor WebAssembly  
**FHIR Version**: R4  
**SDOH IG Version**: 2.3.0 (STU 2.3)  
**Status**: Frontend Complete, Backend 0% Complete
