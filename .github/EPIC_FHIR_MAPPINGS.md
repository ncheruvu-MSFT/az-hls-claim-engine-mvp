# Epic MyChart & Provider Portal - FHIR R4 Resource Mappings

## Overview
Epic is the leading EHR system, and their patient portal (MyChart) and provider interface follow specific UX patterns. Here's how to replicate their functionality using FHIR R4 resources.

---

## EPIC MYCHART (PATIENT PORTAL) - FHIR MAPPINGS

### 1. Patient Dashboard / Home Screen

#### Epic MyChart Layout
```
┌─────────────────────────────────────────────────────────┐
│ [Epic Logo]  Hello, John Smith                    [👤] │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  📋 HEALTH SUMMARY                                      │
│  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐   │
│  │ Medications  │ │  Allergies   │ │ Appointments │   │
│  │     5        │ │      2       │ │  Upcoming: 1 │   │
│  └──────────────┘ └──────────────┘ └──────────────┘   │
│                                                         │
│  📊 TEST RESULTS (New)                                  │
│  • Complete Blood Count - Jan 5, 2026                  │
│  • Lipid Panel - Dec 28, 2025                          │
│                                                         │
│  💬 MESSAGES (3 Unread)                                 │
│  • Dr. Johnson replied to your message                 │
│  • Prescription renewal approved                       │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

#### FHIR R4 Resources

**Patient Demographics**
```
Resource: Patient
Endpoint: GET /Patient/{id}
Used for: Name, DOB, Contact Info, Photo
```

**Medications Count**
```
Resource: MedicationRequest
Endpoint: GET /MedicationRequest?patient={id}&status=active
Display: Count of active medications
Epic Shows: Drug name, dose, instructions, prescriber
```

**Allergies Count**
```
Resource: AllergyIntolerance
Endpoint: GET /AllergyIntolerance?patient={id}&clinical-status=active
Display: Count and list of active allergies
Epic Shows: Allergen, reaction, severity
```

**Appointments**
```
Resource: Appointment
Endpoint: GET /Appointment?patient={id}&status=booked&date=ge{today}
Display: Count of upcoming appointments
Epic Shows: Date, time, provider, location, visit reason
```

**Test Results**
```
Resource: DiagnosticReport
Endpoint: GET /DiagnosticReport?patient={id}&status=final&_sort=-date
Display: Latest lab results with "New" badge
Epic Shows: Test name, date, status badge
```

**Messages**
```
Resource: Communication
Endpoint: GET /Communication?patient={id}&status=in-progress,completed&_sort=-sent
Display: Unread message count
Epic Shows: Sender, subject, preview, timestamp
```

---

### 2. Epic MyChart - Medications Screen

#### Epic Layout
```
┌─────────────────────────────────────────────────────────┐
│ < Back    MEDICATIONS                                    │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  🔍 Search medications...                               │
│                                                         │
│  📋 ACTIVE MEDICATIONS (5)                              │
│  ┌───────────────────────────────────────────────────┐ │
│  │ 💊 Metformin 500mg                                │ │
│  │    Twice daily with meals                         │ │
│  │    Last filled: Jan 1, 2026                       │ │
│  │    Refills remaining: 3                           │ │
│  │    Prescribed by: Dr. Sarah Johnson               │ │
│  │    [Request Refill]  [Details]                    │ │
│  └───────────────────────────────────────────────────┘ │
│                                                         │
│  📋 DISCONTINUED (View 12 older medications)            │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

#### FHIR Implementation

**MedicationRequest Resource**
```json
GET /MedicationRequest?patient=Patient/{id}&_include=MedicationRequest:medication

Epic Field → FHIR Mapping:
- Drug Name → medicationCodeableConcept.coding.display
- Dose/Instructions → dosageInstruction[0].text
- Frequency → dosageInstruction[0].timing.repeat
- Last Filled → dispenseRequest.validityPeriod.start
- Refills → dispenseRequest.numberOfRepeatsAllowed
- Prescribed By → requester.display (references Practitioner)
- Status → status (active, completed, stopped)
```

**Code Example**:
```csharp
public async Task<List<MedicationInfo>> GetMedicationsAsync(string patientId)
{
    var url = $"{_fhirBaseUrl}/MedicationRequest?patient=Patient/{patientId}&status=active&_include=MedicationRequest:medication";
    var bundle = await _fhirClient.SearchAsync<MedicationRequest>(url);
    
    return bundle.Entry.Select(e => new MedicationInfo
    {
        Name = e.Resource.MedicationCodeableConcept?.Coding?[0]?.Display,
        Dosage = e.Resource.DosageInstruction?[0]?.Text,
        LastFilled = e.Resource.DispenseRequest?.ValidityPeriod?.Start,
        RefillsRemaining = e.Resource.DispenseRequest?.NumberOfRepeatsAllowed,
        Prescriber = e.Resource.Requester?.Display,
        Status = e.Resource.Status.ToString()
    }).ToList();
}
```

---

### 3. Epic MyChart - Test Results Screen

#### Epic Layout
```
┌─────────────────────────────────────────────────────────┐
│ < Back    TEST RESULTS                                   │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  📊 RECENT RESULTS                                      │
│                                                         │
│  ┌───────────────────────────────────────────────────┐ │
│  │ Complete Blood Count (CBC)          [NEW] 🔴      │ │
│  │ Collected: Jan 5, 2026, 8:30 AM                   │ │
│  │ Ordered by: Dr. Sarah Johnson                     │ │
│  │                                                    │ │
│  │ Component          Value    Range      Flag       │ │
│  │ ─────────────────────────────────────────────     │ │
│  │ WBC Count          8.5      4.5-11.0   Normal    │ │
│  │ RBC Count          4.2      4.5-5.5    Low  ⬇️    │ │
│  │ Hemoglobin         13.1     14.0-18.0  Low  ⬇️    │ │
│  │ Hematocrit         39%      42-52%     Low  ⬇️    │ │
│  │ Platelet Count     250      150-400    Normal    │ │
│  │                                                    │ │
│  │ [View Provider Comments]  [Download PDF]          │ │
│  └───────────────────────────────────────────────────┘ │
│                                                         │
│  📋 PAST RESULTS (View 24 older results)                │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

#### FHIR Implementation

**DiagnosticReport Resource** (for panel/report level)
```json
GET /DiagnosticReport?patient=Patient/{id}&_include=DiagnosticReport:result

Epic Field → FHIR Mapping:
- Test Name → code.coding.display
- Collection Date/Time → effectiveDateTime
- Ordered By → performer[0].display (references Practitioner)
- Status Badge → status (final, preliminary, corrected)
- Provider Comments → conclusion
```

**Observation Resource** (for individual test components)
```json
GET /Observation?patient=Patient/{id}&category=laboratory

Epic Field → FHIR Mapping:
- Component Name → code.coding.display
- Value → valueQuantity.value + valueQuantity.unit
- Reference Range → referenceRange[0].low.value + referenceRange[0].high.value
- Flag → interpretation.coding.code ("L"=Low, "H"=High, "N"=Normal)
```

**Code Example**:
```csharp
public async Task<DiagnosticReportDetails> GetTestResultsAsync(string reportId)
{
    var report = await _fhirClient.ReadAsync<DiagnosticReport>($"DiagnosticReport/{reportId}");
    
    var observations = new List<ObservationResult>();
    foreach (var resultRef in report.Result)
    {
        var obs = await _fhirClient.ReadAsync<Observation>(resultRef.Reference);
        observations.Add(new ObservationResult
        {
            Name = obs.Code.Coding[0].Display,
            Value = $"{obs.Value.Quantity.Value} {obs.Value.Quantity.Unit}",
            ReferenceRange = $"{obs.ReferenceRange[0].Low.Value}-{obs.ReferenceRange[0].High.Value}",
            Flag = obs.Interpretation?.Coding?[0]?.Code, // "L", "H", "N"
            IsAbnormal = obs.Interpretation?.Coding?[0]?.Code != "N"
        });
    }
    
    return new DiagnosticReportDetails
    {
        TestName = report.Code.Coding[0].Display,
        CollectionDateTime = report.EffectiveDateTime,
        OrderedBy = report.Performer[0].Display,
        Status = report.Status.ToString(),
        Observations = observations,
        Comments = report.Conclusion
    };
}
```

---

### 4. Epic MyChart - Appointments Screen

#### Epic Layout
```
┌─────────────────────────────────────────────────────────┐
│ < Back    APPOINTMENTS                                   │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  [Schedule New Appointment]                             │
│                                                         │
│  📅 UPCOMING APPOINTMENTS (2)                           │
│                                                         │
│  ┌───────────────────────────────────────────────────┐ │
│  │ 📅 Jan 15, 2026, 10:00 AM                         │ │
│  │ 👨‍⚕️ Dr. Sarah Johnson, MD                          │ │
│  │ 📍 Northridge Medical Center, Suite 200           │ │
│  │ 🏥 Annual Physical Exam                            │ │
│  │                                                    │ │
│  │ Pre-visit forms: [Complete Questionnaire]         │ │
│  │                                                    │ │
│  │ [Video Visit Link] [Cancel] [Reschedule]          │ │
│  └───────────────────────────────────────────────────┘ │
│                                                         │
│  ⏱️ PRE-CHECK-IN (Opens 24 hours before)                │
│  • Update insurance information                        │
│  • Review medications                                  │
│  • Complete health questionnaire                       │
│                                                         │
│  📋 PAST APPOINTMENTS (View 15 past visits)             │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

#### FHIR Implementation

**Appointment Resource**
```json
GET /Appointment?patient=Patient/{id}&date=ge{today}&_include=Appointment:practitioner&_include=Appointment:location

Epic Field → FHIR Mapping:
- Date/Time → start (ISO 8601 format)
- Duration → minutesDuration
- Provider → participant[practitioner].actor.display
- Location → participant[location].actor.display
- Visit Type → serviceType.coding.display
- Status → status (booked, fulfilled, cancelled)
- Video Visit → supportingInformation (reference to endpoint)
```

**Questionnaire & QuestionnaireResponse** (for pre-visit forms)
```json
GET /Questionnaire?context=Appointment/{appointmentId}
GET /QuestionnaireResponse?subject=Patient/{id}&questionnaire={questionnaireId}

Used for: Pre-visit questionnaires, health screening forms
```

**Code Example**:
```csharp
public async Task<List<AppointmentInfo>> GetUpcomingAppointmentsAsync(string patientId)
{
    var searchParams = new SearchParams()
        .Where($"patient=Patient/{patientId}")
        .Where($"date=ge{DateTime.Now:yyyy-MM-dd}")
        .Include("Appointment:practitioner")
        .Include("Appointment:location")
        .OrderBy("date");
    
    var bundle = await _fhirClient.SearchAsync<Appointment>(searchParams);
    
    return bundle.Entry.Select(e => 
    {
        var appt = e.Resource as Appointment;
        var practitioner = bundle.Entry.FirstOrDefault(inc => 
            inc.Resource is Practitioner && inc.FullUrl == appt.Participant[0].Actor.Reference)?.Resource as Practitioner;
        var location = bundle.Entry.FirstOrDefault(inc => 
            inc.Resource is Location && inc.FullUrl == appt.Participant[1].Actor.Reference)?.Resource as Location;
        
        return new AppointmentInfo
        {
            DateTime = appt.Start,
            Duration = appt.MinutesDuration,
            Provider = practitioner?.Name?[0]?.Text,
            Location = location?.Name,
            VisitType = appt.ServiceType?[0]?.Coding?[0]?.Display,
            Status = appt.Status.ToString(),
            IsVideoVisit = appt.SupportingInformation?.Any(s => s.Reference.Contains("Endpoint")) ?? false
        };
    }).ToList();
}
```

---

### 5. Epic MyChart - Messages Screen

#### Epic Layout
```
┌─────────────────────────────────────────────────────────┐
│ < Back    MESSAGES                         [Compose ✉️] │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  📬 INBOX (3 unread)                                    │
│                                                         │
│  ┌───────────────────────────────────────────────────┐ │
│  │ 🟦 Dr. Sarah Johnson                    Jan 6     │ │
│  │    RE: Lab Results Discussion                     │ │
│  │    Your recent CBC shows mild anemia. Let's...    │ │
│  └───────────────────────────────────────────────────┘ │
│                                                         │
│  ┌───────────────────────────────────────────────────┐ │
│  │ 🟦 Pharmacy Department                  Jan 5     │ │
│  │    Prescription Renewal Approved                  │ │
│  │    Your Metformin prescription has been...        │ │
│  └───────────────────────────────────────────────────┘ │
│                                                         │
│  ┌───────────────────────────────────────────────────┐ │
│  │ ⬜ Medical Records                      Jan 3     │ │
│  │    Documents Ready for Download                   │ │
│  │    Your requested medical records are now...      │ │
│  └───────────────────────────────────────────────────┘ │
│                                                         │
│  📤 SENT (12)   📁 DRAFTS (1)   🗑️ TRASH (8)           │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

#### FHIR Implementation

**Communication Resource**
```json
GET /Communication?recipient=Patient/{id}&_sort=-sent

Epic Field → FHIR Mapping:
- Sender → sender.display (Practitioner, Organization)
- Subject → topic.text
- Message Body → payload[0].contentString
- Sent Date → sent
- Read/Unread → status (in-progress = unread, completed = read)
- Thread → partOf (reference to parent Communication)
```

**Code Example**:
```csharp
public async Task<List<MessageInfo>> GetMessagesAsync(string patientId)
{
    var url = $"{_fhirBaseUrl}/Communication?recipient=Patient/{patientId}&_sort=-sent&_count=50";
    var bundle = await _fhirClient.SearchAsync<Communication>(url);
    
    return bundle.Entry.Select(e =>
    {
        var comm = e.Resource as Communication;
        return new MessageInfo
        {
            MessageId = comm.Id,
            From = comm.Sender?.Display,
            Subject = comm.Topic?.Text,
            Preview = comm.Payload?[0]?.ContentString?.Substring(0, Math.Min(50, comm.Payload[0].ContentString.Length)),
            SentDate = comm.Sent,
            IsRead = comm.Status == Communication.CommunicationStatus.Completed,
            IsUnread = comm.Status == Communication.CommunicationStatus.InProgress
        };
    }).ToList();
}
```

---

## EPIC PROVIDER INTERFACE - FHIR MAPPINGS

### 1. Epic Provider Dashboard (Haiku/Hyperspace)

#### Epic Layout
```
┌─────────────────────────────────────────────────────────┐
│ Epic    [In Basket: 12]  [Patient Lists]  [Schedule]   │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  👨‍⚕️ DR. SARAH JOHNSON                                  │
│                                                         │
│  📋 MY SCHEDULE - Today, Jan 8, 2026                    │
│  ┌──────────────────────────────────────────────────┐  │
│  │ 9:00 AM  John Smith (45y M)        Room 201      │  │
│  │          Chief Complaint: Follow-up visit        │  │
│  │          [Open Chart] [Check In]                 │  │
│  ├──────────────────────────────────────────────────┤  │
│  │ 9:30 AM  Mary Jones (62y F)        Room 203      │  │
│  │          Chief Complaint: New patient            │  │
│  │          [Open Chart] [Check In]                 │  │
│  └──────────────────────────────────────────────────┘  │
│                                                         │
│  📊 RESULTS TO REVIEW (8)                               │
│  • CBC - John Smith - Abnormal ⚠️                      │
│  • Chest X-Ray - Sarah Lee - Final                    │
│                                                         │
│  📝 ORDERS TO SIGN (5)                                  │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

#### FHIR Resources

**Provider Schedule**
```
Resource: Appointment
Endpoint: GET /Appointment?practitioner=Practitioner/{id}&date={today}
Includes: Patient demographics, encounter reason
```

**Results to Review**
```
Resource: DiagnosticReport
Endpoint: GET /DiagnosticReport?performer=Practitioner/{id}&status=final&_sort=-date
Filter: Reports not yet reviewed (add custom extension)
```

**Orders to Sign**
```
Resource: ServiceRequest
Endpoint: GET /ServiceRequest?requester=Practitioner/{id}&status=draft
Display: Orders awaiting signature
```

---

### 2. Epic Patient Chart View

#### Epic Layout
```
┌─────────────────────────────────────────────────────────┐
│ [Patient: John Smith, 45y M]  [Allergy: Penicillin 🔴] │
│                                                         │
│ [Chart Review] [Orders] [Notes] [Results] [Meds]       │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  📊 SNAPSHOT                                            │
│  • Diabetes Type 2 (Controlled)                        │
│  • Hypertension (Stable)                               │
│  • Last A1C: 6.5% (3 months ago)                       │
│  • BP Today: 128/82                                    │
│                                                         │
│  💊 ACTIVE MEDICATIONS (5)                              │
│  • Metformin 500mg BID                                 │
│  • Lisinopril 10mg daily                               │
│                                                         │
│  📋 PROBLEM LIST                                        │
│  • Type 2 Diabetes Mellitus (E11.9) - 2020            │
│  • Essential Hypertension (I10) - 2018                 │
│  • Hyperlipidemia (E78.5) - 2019                       │
│                                                         │
│  🩺 VITAL SIGNS (Today)                                 │
│  • BP: 128/82  • HR: 72  • Temp: 98.6°F               │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

#### FHIR Resources

**Patient Snapshot**
```
Resource: Condition
Endpoint: GET /Condition?patient=Patient/{id}&clinical-status=active
Display: Active problem list with ICD-10 codes
```

**Recent Observations**
```
Resource: Observation
Endpoint: GET /Observation?patient=Patient/{id}&category=laboratory&code={LOINC}
Examples:
- A1C: LOINC 4548-4
- Blood Pressure: LOINC 85354-9 (systolic), 85354-9 (diastolic)
```

**Vital Signs**
```
Resource: Observation
Endpoint: GET /Observation?patient=Patient/{id}&category=vital-signs&date={today}
Display: Today's vitals from encounter
```

---

## AZURE FHIR SERVER CONFIGURATION

### Step 1: Get Your Azure FHIR Endpoint

```bash
# List your FHIR services
az healthcareapis service list --resource-group <your-rg>

# Get the endpoint URL
az healthcareapis service show \
  --resource-group <your-rg> \
  --name <your-fhir-service> \
  --query "properties.authenticationConfiguration.audience" -o tsv

# Example output: https://your-fhir-service.azurehealthcareapis.com
```

### Step 2: Configure Authentication

**Option A: Managed Identity (Recommended)**
```csharp
// In MemberPortalService.cs
public MemberPortalService(HttpClient httpClient, IConfiguration config, TokenCredential credential)
{
    _httpClient = httpClient;
    _fhirBaseUrl = config["FhirServerUrl"];
    
    // Add Azure AD bearer token
    var token = credential.GetToken(
        new TokenRequestContext(new[] { "https://YOUR-FHIR-SERVICE.azurehealthcareapis.com/.default" }), 
        default);
    
    _httpClient.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", token.Token);
}
```

**Option B: Service Principal**
```bash
# Create service principal
az ad sp create-for-rbac --name "ClaimsPortalFHIR" --role "FHIR Data Contributor"

# Assign FHIR Data Contributor role
az role assignment create \
  --assignee <sp-app-id> \
  --role "FHIR Data Contributor" \
  --scope /subscriptions/<sub-id>/resourceGroups/<rg>/providers/Microsoft.HealthcareApis/services/<fhir-service>
```

### Step 3: Update Configuration

```csharp
// In Program.cs
builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
{
    ["FhirServerUrl"] = "https://YOUR-FHIR-SERVICE.azurehealthcareapis.com",
    ["AzureAd:TenantId"] = "your-tenant-id",
    ["AzureAd:ClientId"] = "your-client-id",
    ["AzureAd:ClientSecret"] = "your-client-secret", // Or use Key Vault
    ["AzureAd:Scope"] = "https://YOUR-FHIR-SERVICE.azurehealthcareapis.com/.default"
});
```

---

## LOCAL COSMOSDB EMULATOR SETUP

### Step 1: Start CosmosDB Emulator

```powershell
# Start emulator (if not running)
Start-Process "C:\Program Files\Azure Cosmos DB Emulator\CosmosDB.Emulator.exe"

# Or via command line with specific settings
& "C:\Program Files\Azure Cosmos DB Emulator\CosmosDB.Emulator.exe" /EnableMongoDbEndpoint=3.6 /EnableCassandraEndpoint
```

### Step 2: Create Database and Containers

```csharp
// Create CosmosDB setup script
using Microsoft.Azure.Cosmos;

var cosmosClient = new CosmosClient(
    "https://localhost:8081",
    "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==",
    new CosmosClientOptions { 
        HttpClientFactory = () => new HttpClient(new HttpClientHandler { 
            ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true 
        }),
        ConnectionMode = ConnectionMode.Gateway 
    }
);

// Create database
var database = await cosmosClient.CreateDatabaseIfNotExistsAsync("ClaimsIQDB");

// Create Accumulators container
await database.Database.CreateContainerIfNotExistsAsync(
    "Accumulators",
    "/memberId"
);

// Create Claims container
await database.Database.CreateContainerIfNotExistsAsync(
    "Claims",
    "/memberId"
);
```

### Step 3: Seed Sample Data

```json
// Accumulators/SUB001.json
{
  "id": "SUB001",
  "memberId": "SUB001",
  "year": 2026,
  "medicalDeductibleYTD": 800.00,
  "medicalDeductibleMax": 2000.00,
  "medicalOOPYTD": 1200.00,
  "medicalOOPMax": 5000.00,
  "dentalSpentYTD": 450.00,
  "dentalMax": 2000.00,
  "pharmacyOOPYTD": 180.00,
  "pharmacyOOPMax": 2000.00,
  "qualityScore": 85,
  "lastUpdated": "2026-01-08T00:00:00Z"
}
```

### Step 4: Update Service to Use CosmosDB

```csharp
// In MemberPortalService.cs
public async Task<MemberAccumulators?> GetAccumulatorsAsync(string memberId)
{
    try
    {
        // Try CosmosDB first
        var container = _cosmosClient.GetContainer("ClaimsIQDB", "Accumulators");
        var response = await container.ReadItemAsync<MemberAccumulators>(
            memberId, 
            new PartitionKey(memberId)
        );
        
        return response.Resource;
    }
    catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
    {
        // Fall back to API if item not in CosmosDB
        var apiResponse = await _httpClient.GetAsync($"{_apiBaseUrl}/accumulators/{memberId}");
        if (apiResponse.IsSuccessStatusCode)
        {
            return await apiResponse.Content.ReadFromJsonAsync<MemberAccumulators>();
        }
        
        return null;
    }
}
```

---

## RECOMMENDED IMPLEMENTATION ORDER

1. **Azure FHIR Setup** (Week 1)
   - Configure authentication
   - Load sample Patient resources
   - Load sample Coverage resources
   - Test connectivity from app

2. **CosmosDB Local** (Week 1)
   - Start emulator
   - Create containers
   - Seed accumulator data
   - Update service layer

3. **Epic-Style Member Portal** (Week 2)
   - Dashboard with health summary cards
   - Medications list with refill requests
   - Test results with abnormal flags
   - Appointments with pre-check-in

4. **Epic-Style Provider Portal** (Week 3)
   - Provider schedule view
   - Patient chart snapshot
   - Results to review queue
   - Orders to sign workflow

5. **Advanced Features** (Week 4)
   - Secure messaging (Communication)
   - Pre-visit questionnaires
   - Document downloads
   - Video visit integration

---

## TESTING WITH POSTMAN

### Test Azure FHIR Connectivity

```http
GET https://YOUR-FHIR-SERVICE.azurehealthcareapis.com/Patient
Authorization: Bearer {token}
Accept: application/fhir+json
```

### Test CosmosDB Emulator

```http
GET https://localhost:8081/_explorer/index.html
# Use web UI to browse containers
```

---

Would you like me to:
1. **Create the Epic-style provider dashboard** with FHIR integration?
2. **Build the medication refill request workflow** using MedicationRequest?
3. **Implement the test results viewer** with abnormal value highlighting?
4. **Set up the Azure FHIR authentication** code?

Let me know your Azure FHIR service name and I'll update the configuration with the exact endpoints!
