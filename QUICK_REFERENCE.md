# Quick Reference Guide - Multi-Benefit Claims Management

## 📋 Page URLs

| Page | URL | Purpose |
|------|-----|---------|
| Claims Management | `/claims-management` | Submit medical, dental, pharmacy claims |
| Formulary Management | `/formulary` | Manage drug list, pricing, restrictions |
| Member Accumulators | `/accumulators` | Track medical, dental, pharmacy utilization |
| HEDIS Quality | `/hedis-quality` | Full quality measures dashboard |

---

## 🏥 Claims Management - Quick Actions

### Medical Claims
```
1. Click "Medical Claims" tab
2. Click "Add New Claim" sub-tab
3. Fill in:
   - Member ID (PAT001)
   - Plan ID (Northridge Medicare Prime HMO)
   - Provider NPI
   - Service Date
   - CPT Code (e.g., 99214 for office visit)
   - Diagnosis Code (e.g., E11.9 for diabetes)
   - Billed Amount
4. Click "Submit Claim"
```

### Dental Claims
```
1. Click "Dental Claims" tab
2. Click "Add New Claim" sub-tab
3. Fill in member and dentist info
4. For each service:
   - Select CDT Code (D1110 = cleaning)
   - Enter tooth number (1-32)
   - Enter billed amount
   - Click "Add Service Line"
5. Review total and click "Submit Claim"
```

### Pharmacy Claims
```
1. Click "Pharmacy Claims" tab
2. Click "Add New Claim" sub-tab
3. Fill in:
   - Member ID
   - Plan ID
   - Drug Name (auto-fills NDC)
   - Quantity (e.g., 30 tablets)
   - Days Supply (30 or 90)
   - Fill Date
   - Ingredient Cost
4. Click "Submit Claim"
```

---

## 💊 Formulary Management - Quick Actions

### Search Drugs
```
1. Type drug name in search bar (e.g., "Metformin")
2. Or search by NDC (e.g., "00093-0058-01")
3. Use filters:
   - Tier (1-5)
   - Restrictions (PA, ST, QL)
```

### Add New Drug
```
1. Click "Add Drug" button
2. Fill in required fields (*):
   - Drug Name *
   - NDC * (11 digits, e.g., 00093-0058-01)
   - Strength * (e.g., 500mg)
   - Tier * (1-5)
   - AWP * (Average Wholesale Price)
   - Plan Cost *
3. Optional fields:
   - Generic Name
   - Dosage Form
   - WAC (Wholesale Acquisition Cost)
   - Therapeutic Class
   - Restrictions (checkboxes)
   - Quantity Limit
   - Max Days Supply
4. Click "Save"
```

### Import Pricing
```
1. Click "Import Feed" button
2. Select data source:
   - First DataBank (industry standard)
   - Medi-Span (comprehensive database)
   - RED BOOK (AWP/WAC pricing)
   - Custom CSV (upload your own)
3. Configure options:
   ☑ Update prices only (don't add new drugs)
   ☑ Preview changes before applying
   ☑ Create backup before import
4. Click "Import"
```

---

## 📊 Accumulators - Tab Guide

### Medical Tab
Shows:
- Individual & Family Deductible (met/remaining)
- Individual & Family OOP Max (met/remaining)
- Office Visits (used/remaining)
- Hospital Days (used/remaining)
- Prescription Spend (YTD)

### Dental Tab
Shows:
- Annual Maximum ($2,000) - used/remaining
- Deductible ($50) - met/remaining
- Spending by Category:
  - Preventive (100% covered)
  - Basic (80% covered)
  - Major (50% covered)
  - Orthodontia ($1,500 lifetime max)
- Frequency Limits:
  - Cleanings: 2/year
  - Exams: 2/year
  - X-Rays: 1/year

### Pharmacy Tab
Shows:
- Drug Deductible (met/remaining)
- Drug OOP Max ($2,000) - met/remaining
- **Part D Coverage Phase**:
  - Initial Coverage (up to $5,030)
  - Catastrophic (after $8,000 TrOOP)
- **TrOOP** (True Out-of-Pocket)
- Rx Count by Tier (1-5)
- Spending by Tier

### Combined Tab
Shows:
- Summary cards for all 3 benefits
- **Total OOP Across All Benefits**:
  - Medical OOP
  - Dental Spend
  - Pharmacy OOP
  - Combined Total with progress bar

### Quality Measures Tab
Shows:
- HEDIS dashboard summary (compliance, star rating, gaps)
- Top 6 quality measures for member
- Benchmarks (National Average, NCQA 50th/90th percentile)
- Link to full HEDIS dashboard

---

## 🏷️ Formulary Tier Guide

| Tier | Name | Cost | Example Drugs |
|------|------|------|---------------|
| 1 | Preferred Generic | Lowest ($0) | Metformin, Lisinopril, Atorvastatin |
| 2 | Generic | Low ($10) | Omeprazole |
| 3 | Preferred Brand | Medium ($47) | Januvia, Eliquis |
| 4 | Non-Preferred Brand | High ($100) | Nexium |
| 5 | Specialty | Highest (33%) | Humira, Enbrel |

---

## 🦷 Dental CDT Code Quick Reference

| Code | Description | Category | Typical Fee |
|------|-------------|----------|-------------|
| D1110 | Adult Cleaning | Preventive | $100 |
| D0120 | Periodic Exam | Preventive | $50 |
| D0274 | Bitewing X-Rays (4) | Preventive | $75 |
| D2140 | Amalgam Filling (1 surface) | Basic | $150 |
| D2330 | Composite Filling | Basic | $175 |
| D7140 | Simple Extraction | Basic | $200 |
| D2740 | Porcelain Crown | Major | $1,200 |
| D6010 | Dental Implant | Major | $2,500 |
| D8080 | Comprehensive Ortho | Orthodontia | $5,000 |

---

## 💡 Common Use Cases

### Scenario 1: Member has routine office visit
```
Action: Submit medical claim
- CPT Code: 99214 (established patient visit)
- Diagnosis: E11.9 (diabetes)
- Billed: $165
Result: Updates medical deductible and OOP
```

### Scenario 2: Member gets dental cleaning
```
Action: Submit dental claim
- CDT Code: D1110 (adult cleaning)
- Billed: $100
Result: 
- Uses 1 of 2 annual cleanings
- 100% covered (preventive)
- Updates dental accumulator
```

### Scenario 3: Member fills prescription
```
Action: Submit pharmacy claim
- Drug: Metformin 500mg (Tier 1)
- NDC: 00093-0058-01
- Quantity: 30 tablets
- Days Supply: 30
Result:
- $0 copay (Tier 1)
- Updates pharmacy accumulator
- Adds to Tier 1 Rx count
```

### Scenario 4: Member needs specialty drug
```
Action: Submit pharmacy claim
- Drug: Humira 40mg (Tier 5)
- NDC: 59676-0580-02
- Quantity: 2 pens
Result:
- 33% coinsurance (~$2,600 for $7,850 AWP)
- Prior authorization required
- Quantity limit: 2 per fill
- Updates TrOOP for Part D
```

### Scenario 5: Plan needs to update drug prices
```
Action: Import pricing feed
- Source: RED BOOK
- Option: Update prices only
- Result: All AWP/WAC values updated
```

---

## 🔍 Sample Test Data

### Test Member: PAT001
```
Plan: MA-H1234-001 (Northridge Medicare Prime HMO)

Current Accumulators:
Medical:
- Individual Deductible: $300 / $1,000
- Individual OOP: $1,200 / $5,000
- Office Visits: 3 / 20

Dental:
- Annual Max: $450 / $2,000
- Cleanings: 2 / 2 (LIMIT REACHED)

Pharmacy:
- Drug OOP: $180 / $2,000
- Coverage Phase: Initial
- TrOOP: $180
```

### Sample Claims to Submit:
```
Medical:
- CPT 99214: Office visit - $165
- CPT 99213: Office visit - $120
- CPT 80053: Lab work - $45

Dental:
- D0120: Exam - $50
- D0274: X-rays - $75
- D2740: Crown - $1,200

Pharmacy:
- Metformin 500mg (30 tabs) - $0 copay
- Omeprazole 20mg (30 tabs) - $10 copay
- Januvia 100mg (30 tabs) - $47 copay
```

---

## 🎯 Key Features Summary

✅ **Multi-Benefit Support**: Medical, Dental, Pharmacy
✅ **Separate Accumulators**: Track each benefit type independently
✅ **Medicare Part D Compliant**: 2026 CMS rules
✅ **5-Tier Formulary**: Industry standard structure
✅ **External Feed Import**: First DataBank, Medi-Span, RED BOOK
✅ **Combined OOP View**: Total across all benefits
✅ **HEDIS Integration**: Quality measures embedded
✅ **Responsive Design**: Works on desktop, tablet, mobile

---

## 🚀 Getting Started

1. **Navigate to Claims Management** to submit your first claim
2. **View Formulary** to see available drugs and pricing
3. **Check Accumulators** to track member utilization
4. **Review Quality Measures** to see HEDIS compliance

Need help? Use the **AI Assistant** on the Accumulators page to ask questions!
