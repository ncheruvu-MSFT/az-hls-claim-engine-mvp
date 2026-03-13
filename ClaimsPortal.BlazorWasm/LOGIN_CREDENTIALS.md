# ClaimsIQ Platform - Sample Login Credentials

## Overview
This document contains sample login credentials for testing the ClaimsIQ Platform portals. These are for development and demonstration purposes only.

---

## 🏢 Provider Portal (EPIC-Style)

### Primary Care Physicians
**Username:** `dr.sarah.johnson@valleymedical.com`  
**Password:** `Provider2026!`  
**Role:** Primary Care Physician  
**NPI:** 1234567890  
**Organization:** Valley Medical Group  
**Access Level:** Full (Patient Records, E-Prescribe, Lab Orders, Referrals)

**Username:** `dr.michael.chen@northridge.health`  
**Password:** `Provider2026!`  
**Role:** Primary Care Physician  
**NPI:** 1234567891  
**Organization:** Northridge Healthcare  
**Access Level:** Full

### Specialists
**Username:** `dr.emily.rodriguez@cardio.ucla.edu`  
**Password:** `Specialist2026!`  
**Role:** Cardiologist  
**NPI:** 1234567892  
**Organization:** UCLA Medical Center - Cardiology  
**Access Level:** Full + Advanced Diagnostics

**Username:** `dr.robert.williams@orthopedics.usc.edu`  
**Password:** `Specialist2026!`  
**Role:** Orthopedic Surgeon  
**NPI:** 1234567893  
**Organization:** USC Orthopedics  
**Access Level:** Full + Surgical Orders

### Administrative Staff
**Username:** `admin.maria@valleymedical.com`  
**Password:** `StaffAdmin2026!`  
**Role:** Office Manager  
**Access Level:** Limited (Scheduling, Billing, Basic Records)

**Username:** `nurse.john@northridge.health`  
**Password:** `NurseStaff2026!`  
**Role:** Registered Nurse  
**Access Level:** Limited (Vitals Entry, Med Administration, Basic Charts)

---

## 👤 Member Portal (Patient Self-Service)

### Sample Members

**Username:** `john.smith@email.com`  
**Password:** `Member2026!`  
**Member ID:** SUB001  
**Name:** John Smith  
**DOB:** 05/15/1980  
**Plan:** Northridge Medicare Prime HMO  
**Coverage:** Active  
**Family:** Spouse (Jane Smith) + 2 Children

**Username:** `jane.smith@email.com`  
**Password:** `Member2026!`  
**Member ID:** DEP001-01  
**Name:** Jane Smith (Dependent - Spouse)  
**DOB:** 08/22/1982  
**Plan:** Northridge Medicare Prime HMO  
**Coverage:** Active

**Username:** `michael.johnson@email.com`  
**Password:** `Member2026!`  
**Member ID:** SUB002  
**Name:** Michael Johnson  
**DOB:** 11/30/1975  
**Plan:** Gold PPO Comprehensive  
**Coverage:** Active  
**Family:** Spouse (Sarah Johnson) + 1 Child

**Username:** `sarah.johnson@email.com`  
**Password:** `Member2026!`  
**Member ID:** DEP002-01  
**Name:** Sarah Johnson (Dependent - Spouse)  
**DOB:** 04/18/1978  
**Plan:** Gold PPO Comprehensive  
**Coverage:** Active

**Username:** `robert.williams@email.com`  
**Password:** `Member2026!`  
**Member ID:** SUB003  
**Name:** Robert Williams  
**DOB:** 09/25/1985  
**Plan:** Silver EPO Value  
**Coverage:** Pending  
**Family:** Individual (No Dependents)

---

## 🛡️ Internal Tools (Payer Operations)

### Product Configuration Admin
**Username:** `admin.config@claimsiq.com`  
**Password:** `ConfigAdmin2026!`  
**Role:** Benefits Configuration Administrator  
**Access Level:** Full (Create/Edit/Delete Plans, CMS Export)

### Claims Processor
**Username:** `processor.claims@claimsiq.com`  
**Password:** `ClaimsProc2026!`  
**Role:** Claims Processing Specialist  
**Access Level:** Claims Management, Adjudication

### Fraud Investigator
**Username:** `fraud.analyst@claimsiq.com`  
**Password:** `FraudAnalyst2026!`  
**Role:** Fraud Detection Analyst  
**Access Level:** Fraud Analytics, Claim Review, Flag Management

### Quality Measures Analyst
**Username:** `quality.analyst@claimsiq.com`  
**Password:** `QualityMeasures2026!`  
**Role:** HEDIS Quality Analyst  
**Access Level:** Quality Measures, Star Ratings, Gap Analysis

### System Administrator
**Username:** `sysadmin@claimsiq.com`  
**Password:** `SysAdmin2026!`  
**Role:** Platform Administrator  
**Access Level:** Full System Access (All Tools, User Management, Configuration)

---

## 🔒 Security Notes

### Password Policy
- **Minimum Length:** 10 characters
- **Complexity:** Must contain uppercase, lowercase, number, and special character
- **Expiration:** 90 days (configurable)
- **History:** Cannot reuse last 5 passwords
- **Lockout:** 5 failed attempts = 15-minute lockout

### Multi-Factor Authentication (MFA)
- **Required for:** All Provider Portal users, All Internal Tool users
- **Optional for:** Member Portal users (recommended)
- **Methods:** SMS OTP, Authenticator App, Email OTP

### Role-Based Access Control (RBAC)

#### Provider Portal Roles
| Role | Patient Records | E-Prescribe | Lab Orders | Claims Submission | Scheduling |
|------|----------------|-------------|------------|-------------------|------------|
| Physician (Full) | ✅ Read/Write | ✅ | ✅ | ✅ | ✅ |
| Specialist | ✅ Read/Write | ✅ | ✅ Advanced | ✅ | ✅ |
| Nurse | ✅ Read + Limited Write | ❌ | ✅ Basic | ❌ | ✅ |
| Admin Staff | ✅ Read Only | ❌ | ❌ | ✅ Billing Only | ✅ |

#### Member Portal Roles
| Role | View Claims | View EOB | Find Provider | Request Auth | Update Profile |
|------|-------------|----------|---------------|--------------|----------------|
| Subscriber | ✅ Self + Family | ✅ Self + Family | ✅ | ✅ | ✅ Full |
| Dependent (Adult) | ✅ Self Only | ✅ Self Only | ✅ | ✅ | ✅ Limited |
| Dependent (Minor) | ❌ (Parent Access) | ❌ (Parent Access) | ❌ | ❌ | ❌ |

#### Internal Tools Roles
| Role | Benefits Config | Claims Processing | Fraud Analytics | User Management |
|------|-----------------|-------------------|-----------------|-----------------|
| System Admin | ✅ Full | ✅ Full | ✅ Full | ✅ |
| Config Admin | ✅ Full | ❌ | ❌ | ❌ |
| Claims Processor | ✅ View Only | ✅ Process | ✅ View Only | ❌ |
| Fraud Analyst | ✅ View Only | ✅ Review | ✅ Full | ❌ |

---

## 📱 Test Scenarios

### Provider Portal Test Scenario
1. Login as `dr.sarah.johnson@valleymedical.com`
2. View patient list (10 patients assigned)
3. Select patient John Smith (SUB001)
4. View medical history, lab results, medications
5. Create new prescription for Lisinopril 10mg
6. Order lab test (CBC, CMP)
7. Submit claim for office visit (CPT 99213)
8. Check claim status

### Member Portal Test Scenario
1. Login as `john.smith@email.com`
2. View family dashboard (self + spouse + 2 children)
3. Check deductible progress ($300 / $1,000 met)
4. View recent claims (5 claims)
5. Download EOB for last claim
6. Find in-network provider (search: cardiologist, Los Angeles)
7. Request prior authorization for MRI
8. Update contact information

### Internal Tools Test Scenario
1. Login as `admin.config@claimsiq.com`
2. Navigate to Product Configuration
3. Edit Gold HMO Plan deductible ($1,500 → $1,750)
4. Use AI Assistant: "Add office visit benefit with $30 copay for CPT 99214, prior auth not required per CMS for HMO plans"
5. Apply AI configuration
6. Save plan
7. Export to CMS format
8. Verify export file

---

## 🔧 Development Environment

### Local Development URLs
- **Provider Portal:** http://localhost:5090/provider-portal
- **Member Portal:** http://localhost:5090/member-portal
- **Internal Tools:** http://localhost:5090/benefits (Product Configuration)

### API Endpoints
- **FHIR Base URL:** https://claimsiq-fhir.azurehealthcareapis.com/
- **Cosmos DB API:** https://claimsiq-cosmos.documents.azure.com:443/
- **Authentication API:** https://claimsiq-auth.azurewebsites.net/api/

### Test Data
- **Synthetic Claims:** `/data/synthetic/claims_20.csv`
- **Sample EDI 837:** `/data/synthetic/sample_837.edi`
- **FHIR Resources:** Available via Mock FHIR Server

---

## 📞 Support

For issues with test credentials or access:
- **Email:** support@claimsiq.com
- **Phone:** 1-800-CLAIMS-IQ
- **Hours:** 24/7 Support

---

**Last Updated:** January 8, 2026  
**Environment:** Development  
**Security Level:** Test/Demo Credentials Only
