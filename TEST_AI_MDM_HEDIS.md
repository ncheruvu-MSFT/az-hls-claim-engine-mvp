# Test AI MDM and HEDIS Endpoints

## Prerequisites
```powershell
# Start local Azure Functions
cd C:\Git\AZ\azure-claims-rules-full-bundle\azure-claims-rules-mvp-starter\src\ClaimsRules.Api
func start
```

## Test AI MDM Endpoints

### 1. Test Match Evaluation (High Confidence)
```powershell
$body = @{
    matchId = "test-001"
    record1 = @{
        name = "John Michael Smith"
        dob = "1980-01-30"
        ssn = "123-45-6789"
        address = "123 Main St, Seattle, WA"
        phone = "206-555-1234"
    }
    record2 = @{
        name = "John M. Smith"
        dob = "1980-01-30"
        ssn = "123-45-6789"
        address = "123 Main Street, Seattle, WA"
        phone = "206-555-1234"
    }
} | ConvertTo-Json

Invoke-RestMethod `
    -Method Post `
    -Uri "http://localhost:7071/api/mdm/ai-score" `
    -Body $body `
    -ContentType "application/json" `
    | ConvertTo-Json -Depth 10
```

### 2. Test Match Evaluation (Edge Case - Gender Mismatch)
```powershell
$body = @{
    matchId = "test-002"
    record1 = @{
        name = "Alex Johnson"
        dob = "1992-05-15"
        ssn = "234-56-7890"
        address = "456 Oak Ave, Seattle, WA"
        gender = "Male"
    }
    record2 = @{
        name = "Alex Johnson"
        dob = "1992-05-15"
        ssn = "234-56-7890"
        address = "456 Oak Ave, Seattle, WA"
        gender = "Female"
    }
} | ConvertTo-Json

Invoke-RestMethod `
    -Method Post `
    -Uri "http://localhost:7071/api/mdm/ai-score" `
    -Body $body `
    -ContentType "application/json" `
    | ConvertTo-Json -Depth 10
```

### 3. Test Learning Feedback
```powershell
$body = @{
    matchId = "test-001"
    aiConfidence = 97.5
    humanDecision = "Match"
    feedback = "Correct match - same person with abbreviated middle name"
} | ConvertTo-Json

Invoke-RestMethod `
    -Method Post `
    -Uri "http://localhost:7071/api/mdm/ai-learn" `
    -Body $body `
    -ContentType "application/json" `
    | ConvertTo-Json -Depth 10
```

### 4. Test Model Metrics
```powershell
Invoke-RestMethod `
    -Method Get `
    -Uri "http://localhost:7071/api/mdm/ai-metrics" `
    | ConvertTo-Json -Depth 10
```

### 5. Test Batch Scoring
```powershell
$body = @{
    matches = @(
        @{
            matchId = "batch-001"
            record1 = @{ name = "Mary Wilson"; dob = "1975-03-20"; ssn = "345-67-8901" }
            record2 = @{ name = "Mary Wilson"; dob = "1975-03-20"; ssn = "345-67-8901" }
        },
        @{
            matchId = "batch-002"
            record1 = @{ name = "Robert Brown"; dob = "1988-11-10"; ssn = "456-78-9012" }
            record2 = @{ name = "Bob Brown"; dob = "1988-11-10"; ssn = "456-78-9012" }
        }
    )
} | ConvertTo-Json -Depth 10

Invoke-RestMethod `
    -Method Post `
    -Uri "http://localhost:7071/api/mdm/ai-batch-score" `
    -Body $body `
    -ContentType "application/json" `
    | ConvertTo-Json -Depth 10
```

## Test HEDIS Endpoints

### 1. Get All Measure Definitions
```powershell
Invoke-RestMethod `
    -Method Get `
    -Uri "http://localhost:7071/api/hedis/measures" `
    | ConvertTo-Json -Depth 10
```

### 2. Get Full Dashboard (Mock Data)
```powershell
Invoke-RestMethod `
    -Method Get `
    -Uri "http://localhost:7071/api/hedis/dashboard?planSegment=Commercial&year=2025" `
    | ConvertTo-Json -Depth 10
```

### 3. Calculate Specific Measure (Breast Cancer Screening)
```powershell
Invoke-RestMethod `
    -Method Get `
    -Uri "http://localhost:7071/api/hedis/measure/BCS?planSegment=Commercial&year=2025" `
    | ConvertTo-Json -Depth 10
```

### 4. Get Care Gaps for Diabetes HbA1c Control
```powershell
Invoke-RestMethod `
    -Method Get `
    -Uri "http://localhost:7071/api/hedis/gaps/HBD?planSegment=Commercial&year=2025&top=10" `
    | ConvertTo-Json -Depth 10
```

### 5. Get Care Gaps for Blood Pressure Control
```powershell
Invoke-RestMethod `
    -Method Get `
    -Uri "http://localhost:7071/api/hedis/gaps/CBP?planSegment=Commercial&year=2025&top=25" `
    | ConvertTo-Json -Depth 10
```

### 6. Test All 15 Measures
```powershell
$measures = @("BCS", "COL", "CBP", "HBD", "CDC", "KED", "EED", "SPC", "OMW", "PBH", "IMA", "CIS", "W30", "AWC", "ABA")

foreach ($measure in $measures) {
    Write-Host "`n===== Testing $measure =====" -ForegroundColor Cyan
    Invoke-RestMethod `
        -Method Get `
        -Uri "http://localhost:7071/api/hedis/measure/$measure?planSegment=Commercial&year=2025" `
        | Select-Object -Property measure, calculation `
        | ConvertTo-Json -Depth 5
}
```

## Expected Results

### AI MDM - High Confidence Match
```json
{
  "confidence": 97.5,
  "isMatch": true,
  "reasoning": "Very high confidence match. Same SSN, DOB, and similar addresses.",
  "matchingFactors": [
    "SSN exact match",
    "DOB exact match",
    "Name abbreviation (John M.)"
  ],
  "concerningFactors": [],
  "recommendedDecision": "AutoApprove",
  "estimatedCost": 0.0004
}
```

### AI MDM - Edge Case (Gender Mismatch)
```json
{
  "confidence": 55.0,
  "isMatch": false,
  "reasoning": "Conflicting gender information requires manual review.",
  "matchingFactors": ["SSN match", "Name match", "DOB match", "Address match"],
  "concerningFactors": ["Gender mismatch (Male vs Female)"],
  "recommendedDecision": "StandardReview",
  "estimatedCost": 0.0004
}
```

### HEDIS Dashboard
```json
{
  "measurementYear": 2025,
  "planSegment": "Commercial",
  "overallStarRating": 4.0,
  "projectedStarRating": 4.5,
  "totalMembers": 50000,
  "totalGaps": 6050,
  "measures": [
    {
      "measureId": "BCS",
      "measureName": "Breast Cancer Screening",
      "denominator": 3750,
      "numerator": 2550,
      "rate": 68.0,
      "gapCount": 1200
    }
  ],
  "priorityActions": [
    {
      "measureId": "HBD",
      "measureName": "HbA1c Control for Patients with Diabetes",
      "gapCount": 2100,
      "currentRate": 62.0,
      "targetRate": 77.0,
      "starRatingImpact": 0.45,
      "recommendedApproach": "Targeted outreach: Care manager calls + provider alerts"
    }
  ]
}
```

### HEDIS Care Gaps
```json
{
  "measureId": "HBD",
  "measureName": "HbA1c Control for Patients with Diabetes",
  "totalGaps": 2100,
  "returned": 10,
  "rate": 62.0,
  "gaps": [
    {
      "patientId": "patient-HBD-1",
      "patientName": "Patient 1",
      "dateOfBirth": "1965-07-15",
      "age": 59,
      "gender": "Female",
      "measureId": "HBD",
      "measureName": "HbA1c Control for Patients with Diabetes",
      "lastServiceDate": "2023-08-20",
      "daysSinceLastService": 523,
      "recommendedAction": "Order HbA1c lab test",
      "primaryCareProvider": "Dr. Smith 2",
      "phone": "206-555-1001",
      "priority": 4,
      "riskLevel": "High"
    }
  ]
}
```

## Troubleshooting

### If endpoints return 404
```powershell
# Check if Functions are running
Get-Process -Name "func" -ErrorAction SilentlyContinue

# Check local.settings.json has USE_MOCK_SERVICES = "true"
Get-Content C:\Git\AZ\azure-claims-rules-full-bundle\azure-claims-rules-mvp-starter\src\ClaimsRules.Api\local.settings.json | Select-String "USE_MOCK_SERVICES"
```

### If AI MDM returns errors
```powershell
# Verify Azure OpenAI config in local.settings.json
Get-Content C:\Git\AZ\azure-claims-rules-full-bundle\azure-claims-rules-mvp-starter\src\ClaimsRules.Api\local.settings.json | Select-String "AzureOpenAI"

# For local testing, use mock services
# Set "USE_MOCK_SERVICES": "true" in local.settings.json
```

### View Function Logs
```powershell
# Logs appear in the func start terminal window
# Look for:
# - "Getting HEDIS measure definitions"
# - "Calculating dashboard for {Segment} {Year}"
# - "Calculating measure {MeasureId}"
```

## Performance Benchmarks

### AI MDM
- **Single Match Evaluation**: ~500-800ms (including OpenAI API call)
- **Batch 100 Matches**: ~30-40 seconds (parallel processing)
- **Learning Feedback**: ~50-100ms (Cosmos DB write)
- **Model Metrics**: ~200-300ms (Cosmos DB query)

### HEDIS
- **Dashboard (15 measures)**: ~2-3 seconds with mock data
- **Single Measure**: ~100-200ms with mock data
- **Care Gaps (100 patients)**: ~200-300ms with mock data

*Note: Production FHIR queries will be slower (2-5 seconds per measure) depending on data volume*

## Next Steps

1. ✅ Test all endpoints with mock data
2. ✅ Verify AI confidence scoring logic
3. ✅ Validate HEDIS measure calculations
4. ⏳ Deploy infrastructure to Azure (run Bicep)
5. ⏳ Test with real Azure OpenAI endpoint
6. ⏳ Connect FHIR for production HEDIS data
7. ⏳ Build Blazor UI components
