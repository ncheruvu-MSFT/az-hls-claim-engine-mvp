# AI MDM + NCQA HEDIS Implementation Plan

**Date**: January 9, 2026  
**Budget**: <$100/month for OpenAI  
**Timeline**: 2-3 weeks  

---

## OpenAI Cost Optimization

### Selected Model: **gpt-3.5-turbo-0125** (Cheapest Option)
- **Input**: $0.0005 per 1K tokens (~750 words)
- **Output**: $0.0015 per 1K tokens
- **Average MDM match evaluation**: ~500 tokens input + 100 tokens output = $0.0004 per match
- **Monthly budget**: $100 / $0.0004 = **250,000 matches/month** (more than enough!)

### Usage Estimates (500K Member Payer):
- **Patient Matching**: 1,000 new members/month × $0.0004 = **$0.40/month**
- **Provider Matching**: 200 new providers/month × $0.0004 = **$0.08/month**
- **Learning Updates**: 500 reviewer decisions/month × $0.0002 = **$0.10/month**
- **Total**: **<$1/month** (well under budget!)

### Fallback Options:
- Cache results in Cosmos DB to avoid re-scoring same pairs
- Rate limit: 100 AI calls/hour (still plenty)
- Batch processing for non-urgent matches

---

## AI MDM Implementation

### Architecture

```
┌─────────────────────────────────────────────────┐
│              MDM Matching Flow                   │
├─────────────────────────────────────────────────┤
│                                                  │
│  1. Traditional Matching (7 algorithms)         │
│     ↓ Produces candidate pairs                  │
│                                                  │
│  2. AI Confidence Scoring (OpenAI)              │
│     ↓ Returns 0-100% confidence + explanation   │
│                                                  │
│  3. Decision Router:                            │
│     • 95-100%: Auto-approve ✅                  │
│     • 80-94%: Priority review ⚠️               │
│     • 0-79%: Standard review 🔍                 │
│                                                  │
│  4. Human Reviewer (if needed)                  │
│     ↓ Approve/Reject                            │
│                                                  │
│  5. Learning Loop (feedback to AI)              │
│     ↓ Model improves over time                  │
│                                                  │
└─────────────────────────────────────────────────┘
```

### Code Structure

**New Files**:
1. `ClaimsRules.Api/Services/AIMatchingService.cs` (400 lines)
2. `ClaimsRules.Api/Functions/AIMdmFunctions.cs` (250 lines)
3. `ClaimsRules.Api/Models/AIMatchingModels.cs` (150 lines)
4. `ClaimsRules.Api.Tests/AIMatchingServiceTests.cs` (300 lines)
5. `data/mdm-test-cases.json` (sample data)

**Updated Files**:
1. `ClaimsRules.Api/Functions/MdmOperationsFn.cs` (integrate AI scoring)
2. `ClaimsPortal.BlazorWasm/Components/MdmReviewQueue.razor` (show AI confidence)
3. `local.settings.json` (add OpenAI key)

### AI Prompt Engineering

**Prompt Template**:
```
You are an expert healthcare Master Data Management (MDM) system. 
Evaluate if these two patient records refer to the same person.

Record 1:
- Name: {name1}
- DOB: {dob1}
- SSN (last 4): {ssn1}
- Address: {address1}
- Phone: {phone1}

Record 2:
- Name: {name2}
- DOB: {dob2}
- SSN (last 4): {ssn2}
- Address: {address2}
- Phone: {phone2}

Consider:
- Name variations (nicknames, typos, maiden names)
- Date typos vs. actual different people
- Address moves (same person, new location)
- Phone number changes

Respond ONLY with valid JSON:
{
  "confidence": 0-100,
  "isMatch": true/false,
  "reasoning": "brief explanation",
  "matchingFactors": ["factor1", "factor2"],
  "concerningFactors": ["concern1"]
}
```

### Sample Test Cases

**Test Case 1: High Confidence Match (98%)**
```json
{
  "record1": {
    "name": "John Michael Smith",
    "dob": "1980-03-15",
    "ssn": "5678",
    "address": "123 Main St, Seattle WA 98101",
    "phone": "206-555-1234"
  },
  "record2": {
    "name": "John M. Smith",
    "dob": "1980-03-15",
    "ssn": "5678",
    "address": "456 Oak Ave, Seattle WA 98102",
    "phone": "206-555-1234"
  },
  "expectedMatch": true,
  "expectedConfidence": 95-100,
  "reasoning": "Same DOB, SSN, phone. Name is shortened version. Address change likely due to move."
}
```

**Test Case 2: Medium Confidence (85%)**
```json
{
  "record1": {
    "name": "Jennifer Lynn Johnson",
    "dob": "1985-07-22",
    "ssn": "1234",
    "address": "789 Pine St, Portland OR 97201",
    "phone": "503-555-7890"
  },
  "record2": {
    "name": "Jenny Johnson",
    "dob": "1985-07-22",
    "ssn": "1234",
    "address": "789 Pine St, Portland OR 97201",
    "phone": "503-555-9999"
  },
  "expectedMatch": true,
  "expectedConfidence": 80-90,
  "reasoning": "Same DOB, SSN, address. Jenny is nickname for Jennifer. Phone change concerning but not definitive."
}
```

**Test Case 3: Low Confidence Non-Match (15%)**
```json
{
  "record1": {
    "name": "Michael Anderson",
    "dob": "1975-11-10",
    "ssn": "4321",
    "address": "100 1st Ave, New York NY 10001",
    "phone": "212-555-0001"
  },
  "record2": {
    "name": "Michael Anderson",
    "dob": "1975-11-15",
    "ssn": "9876",
    "address": "200 2nd Ave, New York NY 10002",
    "phone": "212-555-0002"
  },
  "expectedMatch": false,
  "expectedConfidence": 0-20,
  "reasoning": "Common name. Different DOB (not a typo - 5 days apart), different SSN, different address, different phone. Likely two different people."
}
```

**Test Case 4: Typo Detection (92%)**
```json
{
  "record1": {
    "name": "Sarah Elizabeth Williams",
    "dob": "1992-01-30",
    "ssn": "7890",
    "address": "555 Maple Dr, Austin TX 78701",
    "phone": "512-555-3333"
  },
  "record2": {
    "name": "Sara Williams",
    "dob": "1992-01-03",
    "ssn": "7890",
    "address": "555 Maple Dr, Austin TX 78701",
    "phone": "512-555-3333"
  },
  "expectedMatch": true,
  "expectedConfidence": 90-95,
  "reasoning": "Sarah vs Sara (common spelling variation). DOB appears transposed (01/30 → 01/03 likely data entry error). Same SSN, address, phone. High confidence match despite DOB discrepancy."
}
```

### Learning Loop Implementation

**Feedback Storage**:
```json
{
  "matchId": "match-12345",
  "aiConfidence": 87,
  "aiRecommendation": true,
  "humanDecision": true,
  "humanReviewer": "jane.doe@healthplan.com",
  "reviewedAt": "2026-01-09T10:30:00Z",
  "reviewTimeSeconds": 45,
  "correct": true
}
```

**Model Improvement Metrics**:
- **Accuracy**: % of AI recommendations matching human decisions
- **Precision**: % of AI "match" recommendations that were correct
- **Recall**: % of actual matches that AI identified
- **Auto-Approve Rate**: % of cases with 95%+ confidence (target: 60-70%)

---

## NCQA HEDIS Implementation

### Measure Selection (Phase 1 - Top 15 Measures)

#### Effectiveness of Care (Clinical)
1. **BCS** - Breast Cancer Screening (mammography)
2. **COL** - Colorectal Cancer Screening
3. **CBP** - Controlling High Blood Pressure
4. **CDC** - Comprehensive Diabetes Care (HbA1c control, eye exam, kidney test)
5. **HBD** - HbA1c Control for Patients with Diabetes
6. **KED** - Kidney Health Evaluation for Patients with Diabetes
7. **EED** - Eye Exam for Patients with Diabetes
8. **SPC** - Statin Therapy for Patients with Cardiovascular Disease
9. **OMW** - Osteoporosis Management in Women Who Had a Fracture
10. **PBH** - Persistence of Beta-Blocker Treatment After a Heart Attack

#### Prevention & Screening
11. **IMA** - Immunizations for Adolescents (HPV, Tdap, Meningococcal)
12. **CIS** - Childhood Immunization Status
13. **W30** - Well-Child Visits in the First 30 Months of Life
14. **AWC** - Adolescent Well-Care Visits
15. **ABA** - Adult BMI Assessment

### FHIR Resource Mappings

| HEDIS Measure | FHIR Resources | Query Logic |
|---------------|----------------|-------------|
| BCS | DiagnosticReport (mammography), Procedure | LOINC: 24604-1, 24605-8, 24606-6 |
| COL | Procedure (colonoscopy, FIT), DiagnosticReport | SNOMED: 73761001, 12350003 |
| CBP | Observation (BP readings) | LOINC: 85354-9 (systolic), 85354-9 (diastolic) |
| HBD | Observation (HbA1c) | LOINC: 4548-4, 17856-6 |
| CDC | Observation (HbA1c, creatinine), Procedure (eye exam) | Multiple LOINC codes |

### Code Structure

**New Files**:
1. `ClaimsRules.Api/Models/HedisModels.cs` (600 lines)
2. `ClaimsRules.Api/Services/HedisService.cs` (800 lines)
3. `ClaimsRules.Api/Services/FhirHedisQueryService.cs` (600 lines)
4. `ClaimsRules.Api/Functions/HedisFunctions.cs` (400 lines)
5. `ClaimsPortal.BlazorWasm/Pages/HedisDashboard.razor` (500 lines)
6. `ClaimsPortal.BlazorWasm/Components/MeasureCard.razor` (200 lines)
7. `ClaimsPortal.BlazorWasm/Components/GapClosureList.razor` (300 lines)

### HEDIS Calculation Logic (Example: BCS)

```csharp
// Breast Cancer Screening (BCS)
// Denominator: Women age 50-74
// Numerator: Mammogram in past 27 months

public async Task<HedisMeasureResult> CalculateBCS(string planId, int measurementYear)
{
    // 1. Identify eligible population
    var eligibleWomen = await GetEligiblePopulation(
        planId, 
        measurementYear,
        minAge: 50, 
        maxAge: 74, 
        gender: "female"
    );
    
    // 2. Query FHIR for mammography results
    var numeratorPatients = new List<string>();
    
    foreach (var patient in eligibleWomen)
    {
        var mammograms = await _fhirClient.SearchDiagnosticReports(
            patientId: patient.Id,
            loincCodes: new[] { "24604-1", "24605-8", "24606-6", "37768-3" },
            fromDate: measurementYear.AddMonths(-27),
            toDate: measurementYear
        );
        
        if (mammograms.Any())
            numeratorPatients.Add(patient.Id);
    }
    
    // 3. Calculate rate
    return new HedisMeasureResult
    {
        MeasureId = "BCS",
        MeasureName = "Breast Cancer Screening",
        Denominator = eligibleWomen.Count,
        Numerator = numeratorPatients.Count,
        Rate = (double)numeratorPatients.Count / eligibleWomen.Count * 100,
        GapCount = eligibleWomen.Count - numeratorPatients.Count,
        GapPatients = eligibleWomen.Where(p => !numeratorPatients.Contains(p.Id)).ToList()
    };
}
```

### Gap Closure Workflow

```
┌──────────────────────────────────────────────────┐
│         HEDIS Gap Closure Workflow               │
├──────────────────────────────────────────────────┤
│                                                   │
│  1. Calculate Measure (e.g., BCS = 68%)          │
│     ↓ Identify 1,200 women without mammogram    │
│                                                   │
│  2. Prioritize by Impact:                        │
│     • Star Rating impact (high priority)         │
│     • Patient risk level (diabetic = higher)     │
│     • Last contact date (engaged patients first) │
│     ↓ Ranked list of 1,200 patients             │
│                                                   │
│  3. Outreach Assignment:                         │
│     • Care manager: 400 patients (high-risk)     │
│     • Automated calls: 600 patients (low-risk)   │
│     • Mail: 200 patients (unreachable)           │
│     ↓ Track outreach attempts                    │
│                                                   │
│  4. Measure Completion:                          │
│     • Patient schedules mammogram                │
│     • Provider submits claim/result              │
│     • FHIR updates automatically                 │
│     ↓ Recalculate measure (now 72%)             │
│                                                   │
│  5. Star Rating Impact:                          │
│     • 4% improvement = 0.1 star increase         │
│     • 0.5 star increase = $50M revenue impact    │
│                                                   │
└──────────────────────────────────────────────────┘
```

### NCQA Certification Process (Informational)

**Note**: Full NCQA certification requires external audit, but code implementation is 90% of the work.

**Steps for Certification** (6-12 months):
1. **Software Submission** (Month 1-2):
   - Submit code for NCQA review
   - Provide technical documentation
   - Cost: $15K-$25K application fee

2. **Source Code Review** (Month 3-4):
   - NCQA auditors review measure logic
   - Verify HEDIS specifications compliance
   - Test with sample data sets

3. **Beta Testing** (Month 5-6):
   - Test with real health plan data
   - Submit 3 health plans as references
   - Validate results against manual chart review

4. **Certification Audit** (Month 7-9):
   - NCQA conducts on-site or virtual audit
   - Review 200-300 member records
   - Verify <5% error rate

5. **Annual Recertification** (Ongoing):
   - Update measure definitions annually
   - Submit change logs
   - Maintain <5% error rate

**Cost**: $15K-$25K initial + $5K-$10K annual

**Our Strategy**: Build code now, pursue certification once we have 2-3 customers willing to be references.

---

## UI Enhancements

### MDM Review Queue (with AI Confidence)

**Before**:
```
┌────────────────────────────────────────────┐
│ Patient Match Review Queue                 │
├────────────────────────────────────────────┤
│ John Smith vs. Jon Smith                   │
│ DOB: 1980-03-15 vs. 1980-03-15            │
│ [Approve] [Reject]                         │
└────────────────────────────────────────────┘
```

**After (with AI)**:
```
┌────────────────────────────────────────────┐
│ Patient Match Review Queue                 │
│ AI Confidence: 95% ✅ (High - Auto-approved)│
├────────────────────────────────────────────┤
│ John Smith vs. Jon Smith                   │
│ DOB: 1980-03-15 vs. 1980-03-15            │
│                                            │
│ 🤖 AI Analysis:                            │
│ "High confidence match. Same DOB, SSN,     │
│ phone. Name is common typo variation."     │
│                                            │
│ Matching Factors:                          │
│ ✓ Exact DOB match                          │
│ ✓ Same SSN (last 4)                        │
│ ✓ Same phone number                        │
│ ✓ Name variation (John/Jon)                │
│                                            │
│ Status: Auto-approved at 2026-01-09 10:30 │
│ [Override] [View Details]                 │
└────────────────────────────────────────────┘
```

### HEDIS Dashboard

```
┌─────────────────────────────────────────────────┐
│ 📊 HEDIS Quality Dashboard - 2025               │
├─────────────────────────────────────────────────┤
│                                                  │
│ Overall Star Rating: ⭐⭐⭐⭐ (4.0)              │
│ Projected 2026 Rating: ⭐⭐⭐⭐⭐ (4.5)          │
│                                                  │
│ ┌──────────┬──────────┬──────────┬─────────┐   │
│ │ Measure  │ Rate     │ Gap      │ Status  │   │
│ ├──────────┼──────────┼──────────┼─────────┤   │
│ │ BCS      │ 68% 📈  │ 1,200    │ ⚠️      │   │
│ │ COL      │ 72% ✅  │ 800      │ 🟢      │   │
│ │ CBP      │ 85% ✅  │ 450      │ 🟢      │   │
│ │ HBD      │ 62% ⚠️  │ 2,100    │ 🔴      │   │
│ │ CDC      │ 75% 📈  │ 1,500    │ ⚠️      │   │
│ └──────────┴──────────┴──────────┴─────────┘   │
│                                                  │
│ 🎯 Priority Actions:                             │
│ • HBD: 2,100 diabetic patients need HbA1c test  │
│ • BCS: 1,200 women due for mammogram            │
│ • CDC: 1,500 patients missing eye exam          │
│                                                  │
│ [View Gap Closure List] [Export Report]         │
└─────────────────────────────────────────────────┘
```

---

## Implementation Timeline

### Week 1: AI MDM
- **Day 1-2**: Create AIMatchingService.cs, integrate OpenAI API
- **Day 3-4**: Create AI MDM Functions, add to MdmOperationsFn.cs
- **Day 5**: Generate test data, create unit tests
- **Day 6-7**: Update MdmReviewQueue.razor, test end-to-end

### Week 2: NCQA HEDIS Backend
- **Day 1-2**: Create HedisModels.cs with 15 measure definitions
- **Day 3-4**: Create HedisService.cs with calculation logic
- **Day 5-6**: Create FhirHedisQueryService.cs for data extraction
- **Day 7**: Create HedisFunctions.cs, test with mock data

### Week 3: HEDIS UI + Testing
- **Day 1-2**: Create HedisDashboard.razor with measure cards
- **Day 3-4**: Create GapClosureList.razor with patient outreach
- **Day 5**: Create HedisService.cs (frontend) and update Program.cs
- **Day 6**: Integration testing (AI MDM + HEDIS)
- **Day 7**: Performance testing, cost monitoring, documentation

---

## Cost Monitoring

### OpenAI Usage Dashboard

```csharp
public class OpenAIUsageTracker
{
    public async Task<UsageReport> GetMonthlyUsage()
    {
        return new UsageReport
        {
            TotalCalls = 12_500,
            TotalTokens = 6_250_000,
            EstimatedCost = 4.50m,
            TopOperations = new[]
            {
                ("Patient Matching", 10_000, "$3.00"),
                ("Provider Matching", 2_000, "$0.80"),
                ("Learning Updates", 500, "$0.70")
            },
            BudgetRemaining = 95.50m,
            ProjectedMonthEnd = 5.25m
        };
    }
}
```

**Alerts**:
- ⚠️ Daily cost > $5
- 🔴 Monthly cost > $80 (80% of budget)
- 📧 Weekly email with usage summary

---

## Success Metrics

### AI MDM KPIs:
- ✅ **Auto-Approve Rate**: 60-70% (target)
- ✅ **Accuracy**: 95%+ (AI matches human decisions)
- ✅ **Review Time**: 3 min → 30 sec (83% reduction)
- ✅ **Cost per Match**: $0.0004 (vs. $5-$10 manual review)
- ✅ **Monthly Cost**: <$5 (vs. $100 budget)

### HEDIS KPIs:
- ✅ **Measure Coverage**: 15 measures (Phase 1)
- ✅ **Calculation Time**: Real-time (vs. 3 months manual)
- ✅ **Gap Identification**: Automated (vs. quarterly)
- ✅ **Accuracy**: 95%+ (vs. manual chart review)
- ✅ **Cost Savings**: $300K-$600K annually

---

## Next Steps

1. **Get OpenAI API Key** (or Azure OpenAI)
2. **Start with AI MDM implementation** (Week 1)
3. **Test with sample data**
4. **Monitor costs daily** (<$1/day target)
5. **Proceed to HEDIS** (Week 2-3)
6. **Demo to stakeholders** (Week 4)

**Let's build this! 🚀**
