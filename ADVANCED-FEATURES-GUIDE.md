# Healthcare Claims Management - Advanced Features Guide

## Current System Overview

### ✅ **What's Currently Implemented**

**Rules Engine** ([RulesEngineFn.cs](../azure-claims-rules-mvp-starter/src/ClaimsRules.Api/Functions/RulesEngineFn.cs)):
1. **Coverage Check** — Validates service code is in plan's covered services
2. **Network Validation** — Checks provider is in-network (strict for HMO, warning for PPO)
3. **Deductible Calculation** — Applies patient responsibility up to deductible amount
4. **Out-of-Pocket Maximum** — Stops patient cost-sharing after OOP max reached

**Data Model**:
```csharp
BenefitPlan {
    PlanId, PlanName, PlanType (PPO/HMO)
    Deductible, OutOfPocketMax, Copay, CoinsuranceRate
    CoveredServices (List<string> CPT codes)
    NetworkProviders (List<string> Provider IDs)
}
```

---

## 🏥 **Provider Network Management**

### Architecture

Provider networks should be managed separately from plans, with many-to-many relationships:

```
Plan ──┬─> PlanNetworkMapping ──> Network ──> NetworkProvider ──> Provider
       │
       └─> Can have multiple networks (in-network, preferred, out-of-network)
```

### FHIR Resources
- **Organization** — Health plans, provider groups, networks
- **PractitionerRole** — Links practitioners to organizations/networks
- **Location** — Service delivery locations
- **HealthcareService** — Services offered by providers

### Enhanced Data Model

```csharp
public class ProviderNetwork
{
    public string NetworkId { get; set; } = string.Empty;
    public string NetworkName { get; set; } = string.Empty; // "Preferred Provider Network", "Regional Network"
    public string NetworkType { get; set; } = string.Empty; // "Tier1", "Tier2", "OutOfNetwork"
    public decimal ReimbursementRate { get; set; } // 100%, 80%, 60%
    public List<string> ProviderIds { get; set; } = new();
    public List<string> CoveredStates { get; set; } = new(); // Geographic coverage
    public DateTime EffectiveDate { get; set; }
    public DateTime? TerminationDate { get; set; }
}

public class Provider
{
    public string ProviderId { get; set; } = string.Empty;
    public string Npi { get; set; } = string.Empty; // National Provider Identifier
    public string TaxId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ProviderType { get; set; } = string.Empty; // "Hospital", "Physician", "Lab"
    public List<string> Specialties { get; set; } = new();
    public List<string> TaxonomyCodes { get; set; } = new();
    public Address ServiceAddress { get; set; } = new();
    public List<ProviderCredential> Credentials { get; set; } = new();
    public decimal QualityScore { get; set; } // 0-100
    public bool AcceptingNewPatients { get; set; }
}

public class PlanNetworkMapping
{
    public string PlanId { get; set; } = string.Empty;
    public string NetworkId { get; set; } = string.Empty;
    public string NetworkTier { get; set; } = string.Empty; // "In-Network", "Preferred", "Out-of-Network"
    public decimal CostShareMultiplier { get; set; } = 1.0m; // 1.0 = full coverage, 1.5 = 50% higher cost
    public bool RequiresPriorAuth { get; set; }
}
```

### Network Validation Rules

```csharp
public class NetworkValidator
{
    public NetworkValidationResult ValidateProvider(string providerId, string planId, DateTime serviceDate)
    {
        var result = new NetworkValidationResult();
        
        // 1. Check provider exists and is active
        var provider = GetProvider(providerId);
        if (provider == null)
        {
            result.IsValid = false;
            result.Errors.Add("Provider not found");
            return result;
        }
        
        // 2. Check provider credentials are current
        if (!provider.Credentials.Any(c => c.IsActive && c.ExpirationDate > serviceDate))
        {
            result.IsValid = false;
            result.Errors.Add("Provider credentials expired or invalid");
        }
        
        // 3. Check provider is in plan's network
        var networkMappings = GetNetworkMappings(planId);
        var providerNetworks = GetProviderNetworks(providerId, serviceDate);
        
        var matchingNetwork = networkMappings
            .Join(providerNetworks, m => m.NetworkId, n => n.NetworkId, (m, n) => new { Mapping = m, Network = n })
            .OrderBy(x => x.Mapping.CostShareMultiplier) // Prefer lower cost networks
            .FirstOrDefault();
        
        if (matchingNetwork == null)
        {
            result.IsValid = false;
            result.NetworkStatus = "Out-of-Network";
            result.CostShareMultiplier = 2.0m; // Out-of-network penalty
            result.Warnings.Add("Provider is out-of-network. Higher costs apply.");
        }
        else
        {
            result.IsValid = true;
            result.NetworkStatus = matchingNetwork.Mapping.NetworkTier;
            result.NetworkId = matchingNetwork.Network.NetworkId;
            result.NetworkName = matchingNetwork.Network.NetworkName;
            result.CostShareMultiplier = matchingNetwork.Mapping.CostShareMultiplier;
            result.ReimbursementRate = matchingNetwork.Network.ReimbursementRate;
            result.RequiresPriorAuth = matchingNetwork.Mapping.RequiresPriorAuth;
        }
        
        return result;
    }
}
```

### FHIR Mapping

```json
{
  "resourceType": "Organization",
  "id": "network-preferred-001",
  "type": [{
    "coding": [{
      "system": "http://terminology.hl7.org/CodeSystem/organization-type",
      "code": "prov",
      "display": "Healthcare Provider"
    }]
  }],
  "name": "Preferred Provider Network",
  "partOf": {
    "reference": "Organization/plan-gold-ppo"
  }
}
```

---

## 💰 **Payment & Remittance Processing**

### Payment Workflow

```
Claim Adjudication → Payment Calculation → Remittance Generation → Payment Processing → EOB/ERA
```

### Data Models

```csharp
public class PaymentCalculation
{
    public string ClaimId { get; set; } = string.Empty;
    public decimal BilledAmount { get; set; }
    public decimal AllowedAmount { get; set; } // Contracted rate
    public decimal PlanPaid { get; set; }
    public decimal PatientResponsibility { get; set; }
    public decimal Adjustment { get; set; } // Billed - Allowed
    
    public List<PaymentLineItem> LineItems { get; set; } = new();
    
    // CARC/RARC codes for denials/adjustments
    public List<AdjustmentCode> AdjustmentReasons { get; set; } = new();
}

public class PaymentLineItem
{
    public int LineNumber { get; set; }
    public string ServiceCode { get; set; } = string.Empty; // CPT/HCPCS
    public DateTime ServiceDate { get; set; }
    public decimal Quantity { get; set; }
    public decimal BilledAmount { get; set; }
    public decimal AllowedAmount { get; set; }
    public decimal Deductible { get; set; }
    public decimal Coinsurance { get; set; }
    public decimal Copay { get; set; }
    public decimal PlanPaid { get; set; }
    public string AdjustmentGroupCode { get; set; } = string.Empty; // CO, PR, OA
    public List<AdjustmentCode> Adjustments { get; set; } = new();
}

public class AdjustmentCode
{
    public string GroupCode { get; set; } = string.Empty; // CO=Contractual, PR=Patient Responsibility
    public string ReasonCode { get; set; } = string.Empty; // CARC (Claim Adjustment Reason Code)
    public string RemarkCode { get; set; } = string.Empty; // RARC (Remittance Advice Remark Code)
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class RemittanceAdvice
{
    public string RemittanceId { get; set; } = string.Empty; // 835 Transaction ID
    public string PayerId { get; set; } = string.Empty;
    public string PayerName { get; set; } = string.Empty;
    public string PayeeNpi { get; set; } = string.Empty; // Provider receiving payment
    public DateTime PaymentDate { get; set; }
    public string CheckNumber { get; set; } = string.Empty;
    public decimal TotalPaid { get; set; }
    
    public List<ClaimPayment> Claims { get; set; } = new();
}

public class ClaimPayment
{
    public string ClaimId { get; set; } = string.Empty;
    public string PatientAccountNumber { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public DateTime ServiceDate { get; set; }
    public decimal BilledAmount { get; set; }
    public decimal AllowedAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal PatientResponsibility { get; set; }
    public string ClaimStatus { get; set; } = string.Empty; // Paid, Partial, Denied
    public List<PaymentLineItem> ServiceLines { get; set; } = new();
}
```

### Payment Calculation Rules

```csharp
public class PaymentCalculator
{
    public PaymentCalculation CalculatePayment(Claim claim, BenefitPlan plan, MemberAccumulator accumulator)
    {
        var payment = new PaymentCalculation { ClaimId = claim.ClaimId };
        
        foreach (var service in claim.Services)
        {
            var lineItem = new PaymentLineItem
            {
                LineNumber = service.LineNumber,
                ServiceCode = service.ProcedureCode,
                ServiceDate = service.ServiceDate,
                Quantity = service.Quantity,
                BilledAmount = service.ChargeAmount
            };
            
            // Step 1: Apply contracted/allowed amount (fee schedule)
            var feeSchedule = GetFeeSchedule(plan.PlanId, service.ProcedureCode, service.PlaceOfService);
            lineItem.AllowedAmount = feeSchedule?.AllowedAmount ?? service.ChargeAmount * 0.8m; // Default 80% of billed
            
            // Step 2: Calculate adjustment (contractual write-off)
            var adjustment = service.ChargeAmount - lineItem.AllowedAmount;
            if (adjustment > 0)
            {
                lineItem.Adjustments.Add(new AdjustmentCode
                {
                    GroupCode = "CO", // Contractual Obligation
                    ReasonCode = "45", // Charge exceeds fee schedule
                    Amount = adjustment,
                    Description = "Charge exceeds contracted amount"
                });
            }
            
            // Step 3: Apply deductible
            if (accumulator.IndividualDeductibleRemaining > 0)
            {
                lineItem.Deductible = Math.Min(lineItem.AllowedAmount, accumulator.IndividualDeductibleRemaining);
                accumulator.IndividualDeductibleMet += lineItem.Deductible;
                accumulator.IndividualDeductibleRemaining -= lineItem.Deductible;
            }
            
            // Step 4: Apply coinsurance
            var afterDeductible = lineItem.AllowedAmount - lineItem.Deductible;
            if (afterDeductible > 0)
            {
                lineItem.Coinsurance = afterDeductible * plan.CoinsuranceRate;
            }
            
            // Step 5: Apply copay (if applicable)
            if (IsCopayService(service.ProcedureCode))
            {
                lineItem.Copay = plan.Copay;
            }
            
            // Step 6: Check OOP maximum
            var totalPatientCost = lineItem.Deductible + lineItem.Coinsurance + lineItem.Copay;
            if (accumulator.IndividualOopMet + totalPatientCost > accumulator.IndividualOopMet + accumulator.IndividualOopRemaining)
            {
                var excess = (accumulator.IndividualOopMet + totalPatientCost) - 
                            (accumulator.IndividualOopMet + accumulator.IndividualOopRemaining);
                totalPatientCost -= excess;
                lineItem.Adjustments.Add(new AdjustmentCode
                {
                    GroupCode = "PR", // Patient Responsibility
                    ReasonCode = "119", // OOP maximum reached
                    Amount = excess,
                    Description = "Out-of-pocket maximum reached"
                });
            }
            
            // Step 7: Calculate plan payment
            lineItem.PlanPaid = lineItem.AllowedAmount - totalPatientCost;
            
            payment.LineItems.Add(lineItem);
            payment.PlanPaid += lineItem.PlanPaid;
            payment.PatientResponsibility += totalPatientCost;
        }
        
        payment.BilledAmount = claim.Services.Sum(s => s.ChargeAmount);
        payment.AllowedAmount = payment.LineItems.Sum(l => l.AllowedAmount);
        payment.Adjustment = payment.BilledAmount - payment.AllowedAmount;
        
        return payment;
    }
}
```

### 835 EDI Remittance Format

```csharp
public class Edi835Generator
{
    public string GenerateRemittance(RemittanceAdvice remittance)
    {
        var segments = new List<string>();
        
        // ISA - Interchange Control Header
        segments.Add("ISA*00*          *00*          *ZZ*SENDERNPI      *ZZ*RECEIVERNPI    *260107*1200*^*00501*000000001*0*P*:~");
        
        // GS - Functional Group Header
        segments.Add("GS*HP*SENDERID*RECEIVERID*20260107*1200*1*X*005010X221A1~");
        
        // ST - Transaction Set Header (835)
        segments.Add("ST*835*0001*005010X221A1~");
        
        // BPR - Financial Information
        segments.Add($"BPR*I*{remittance.TotalPaid:F2}*C*ACH*CCD*01*999999999*DA*123456789*{remittance.PayerId}**01*999999999*DA*123456789*{remittance.PaymentDate:yyyyMMdd}~");
        
        // TRN - Reassociation Trace Number
        segments.Add($"TRN*1*{remittance.RemittanceId}*{remittance.PayerId}~");
        
        foreach (var claim in remittance.Claims)
        {
            // CLP - Claim Payment Information
            segments.Add($"CLP*{claim.ClaimId}*1*{claim.BilledAmount:F2}*{claim.PaidAmount:F2}*{claim.PatientResponsibility:F2}*12*{claim.PatientAccountNumber}*11~");
            
            // NM1 - Patient Name
            segments.Add($"NM1*QC*1*{claim.PatientName}****MI*{claim.PatientId}~");
            
            foreach (var line in claim.ServiceLines)
            {
                // SVC - Service Payment Information
                segments.Add($"SVC*HC:{line.ServiceCode}*{line.BilledAmount:F2}*{line.PlanPaid:F2}**{line.Quantity}~");
                
                // DTM - Service Date
                segments.Add($"DTM*472*{line.ServiceDate:yyyyMMdd}~");
                
                // CAS - Claim Adjustment Segments
                foreach (var adj in line.Adjustments)
                {
                    segments.Add($"CAS*{adj.GroupCode}*{adj.ReasonCode}*{adj.Amount:F2}~");
                }
            }
        }
        
        // SE - Transaction Set Trailer
        segments.Add($"SE*{segments.Count + 1}*0001~");
        segments.Add("GE*1*1~");
        segments.Add("IEA*1*000000001~");
        
        return string.Join("\n", segments);
    }
}
```

---

## 📦 **Claim Bundling**

### Bundling Rules

Claim bundling groups related services to prevent duplicate payment:

```csharp
public class ClaimBundlingEngine
{
    // CCI (Correct Coding Initiative) edits
    private readonly Dictionary<string, List<string>> _cciEdits = new()
    {
        ["99213"] = new() { "99212", "99211" }, // Can't bill multiple office visit levels same day
        ["29881"] = new() { "29880", "29870" }, // Arthroscopy bundling
        ["45380"] = new() { "45378" }, // Colonoscopy with biopsy includes diagnostic colonoscopy
    };
    
    public BundlingResult ApplyBundling(List<ClaimService> services)
    {
        var result = new BundlingResult();
        var bundledServices = new List<ClaimService>();
        
        // Group services by date and provider
        var serviceGroups = services
            .GroupBy(s => new { s.ServiceDate, s.ProviderId })
            .ToList();
        
        foreach (var group in serviceGroups)
        {
            var groupServices = group.OrderByDescending(s => s.ChargeAmount).ToList();
            
            for (int i = 0; i < groupServices.Count; i++)
            {
                var primaryService = groupServices[i];
                
                // Check if this service has bundling rules
                if (_cciEdits.TryGetValue(primaryService.ProcedureCode, out var bundledCodes))
                {
                    // Find services that should be bundled
                    var toBeBundled = groupServices
                        .Skip(i + 1)
                        .Where(s => bundledCodes.Contains(s.ProcedureCode))
                        .ToList();
                    
                    foreach (var bundledService in toBeBundled)
                    {
                        result.BundledItems.Add(new BundledItem
                        {
                            PrimaryCode = primaryService.ProcedureCode,
                            BundledCode = bundledService.ProcedureCode,
                            Reason = "CCI Edit - Component of comprehensive service",
                            DeniedAmount = bundledService.ChargeAmount
                        });
                        
                        bundledService.IsBundled = true;
                        bundledService.DenialReason = $"Bundled into {primaryService.ProcedureCode}";
                    }
                }
            }
            
            // Check for same-day bilateral procedures (modifier 50)
            var bilateralGroups = groupServices
                .Where(s => !s.IsBundled)
                .GroupBy(s => s.ProcedureCode)
                .Where(g => g.Count() > 1)
                .ToList();
            
            foreach (var bilateralGroup in bilateralGroups)
            {
                var procedures = bilateralGroup.ToList();
                if (procedures.Count == 2 && IsBilateralProcedure(procedures[0].ProcedureCode))
                {
                    // Pay first at 100%, second at 50%
                    procedures[1].AllowedAmount *= 0.5m;
                    result.Warnings.Add($"Bilateral procedure {procedures[0].ProcedureCode} - second side paid at 50%");
                }
            }
        }
        
        result.TotalDenied = result.BundledItems.Sum(b => b.DeniedAmount);
        return result;
    }
}

public class BundledItem
{
    public string PrimaryCode { get; set; } = string.Empty;
    public string BundledCode { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public decimal DeniedAmount { get; set; }
}
```

---

## 📜 **State Rules Compliance**

### State-Specific Rules Engine

```csharp
public class StateRulesEngine
{
    public StateComplianceResult ValidateStateRules(Claim claim, string state)
    {
        var result = new StateComplianceResult { State = state };
        
        // California-specific rules
        if (state == "CA")
        {
            // CA requires mental health parity
            if (IsMentalHealthService(claim.PrimaryDiagnosis))
            {
                result.Rules.Add(new StateRule
                {
                    RuleId = "CA-MH-001",
                    Description = "Mental health services must have same cost-sharing as medical",
                    IsCompliant = ValidateMentalHealthParity(claim)
                });
            }
            
            // CA Surprise Billing Protection (AB 72)
            if (claim.IsEmergency && claim.IsOutOfNetwork)
            {
                result.Rules.Add(new StateRule
                {
                    RuleId = "CA-SB-001",
                    Description = "Emergency services from out-of-network providers billed at in-network rates",
                    RequiresAdjustment = true,
                    AdjustmentInstructions = "Apply in-network cost-sharing"
                });
            }
        }
        
        // New York-specific rules
        if (state == "NY")
        {
            // NY requires coverage for autism therapy (ABA)
            if (claim.Services.Any(s => s.ProcedureCode.StartsWith("97"))) // ABA codes
            {
                result.Rules.Add(new StateRule
                {
                    RuleId = "NY-AUT-001",
                    Description = "ABA therapy mandated benefit under NY mental health parity law",
                    IsCompliant = true
                });
            }
        }
        
        // Texas-specific rules
        if (state == "TX")
        {
            // TX has network adequacy requirements
            if (claim.IsOutOfNetwork)
            {
                result.Rules.Add(new StateRule
                {
                    RuleId = "TX-NET-001",
                    Description = "Check network adequacy - may require in-network rates",
                    RequiresReview = true
                });
            }
        }
        
        // Prompt payment laws (varies by state)
        var promptPaymentDays = GetPromptPaymentDays(state);
        var claimAge = (DateTime.Today - claim.ReceivedDate).Days;
        
        result.Rules.Add(new StateRule
        {
            RuleId = $"{state}-PROMPT-001",
            Description = $"{state} requires payment within {promptPaymentDays} days",
            IsCompliant = claimAge <= promptPaymentDays,
            DaysRemaining = promptPaymentDays - claimAge
        });
        
        return result;
    }
    
    private int GetPromptPaymentDays(string state)
    {
        // State prompt payment laws
        return state switch
        {
            "CA" => 30, // California - 30 working days
            "NY" => 45, // New York - 45 days
            "TX" => 30, // Texas - 30 days
            "FL" => 45, // Florida - 45 days
            _ => 30    // Default 30 days
        };
    }
}
```

---

## 🚨 **Upcoding Detection**

### Upcoding Rules

```csharp
public class UpcodingDetector
{
    public UpcodingAnalysis DetectUpcoding(List<Claim> historicalClaims, Claim currentClaim)
    {
        var analysis = new UpcodingAnalysis { ClaimId = currentClaim.ClaimId };
        
        // 1. Check for unusual service level progression
        var officeVisitCodes = new[] { "99211", "99212", "99213", "99214", "99215" };
        var currentLevel = Array.IndexOf(officeVisitCodes, currentClaim.PrimaryServiceCode);
        
        if (currentLevel >= 0)
        {
            var historicalLevels = historicalClaims
                .Where(c => c.ProviderId == currentClaim.ProviderId)
                .Where(c => officeVisitCodes.Contains(c.PrimaryServiceCode))
                .Select(c => Array.IndexOf(officeVisitCodes, c.PrimaryServiceCode))
                .ToList();
            
            if (historicalLevels.Any())
            {
                var avgLevel = historicalLevels.Average();
                if (currentLevel > avgLevel + 1.5) // More than 1.5 levels above average
                {
                    analysis.Flags.Add(new UpcodingFlag
                    {
                        FlagType = "ServiceLevelEscalation",
                        Severity = "High",
                        Description = $"Provider typically bills level {avgLevel:F1}, current claim is level {currentLevel}",
                        ExpectedCode = officeVisitCodes[(int)Math.Round(avgLevel)],
                        ActualCode = currentClaim.PrimaryServiceCode,
                        PotentialOvercharge = GetReimbursementDifference(
                            officeVisitCodes[(int)Math.Round(avgLevel)], 
                            currentClaim.PrimaryServiceCode)
                    });
                }
            }
        }
        
        // 2. Check for modifier abuse (modifier 25 with E/M)
        if (currentClaim.Services.Any(s => s.Modifiers.Contains("25")))
        {
            var emService = currentClaim.Services.FirstOrDefault(s => officeVisitCodes.Contains(s.ProcedureCode));
            var procedureService = currentClaim.Services.FirstOrDefault(s => !officeVisitCodes.Contains(s.ProcedureCode));
            
            if (emService != null && procedureService == null)
            {
                analysis.Flags.Add(new UpcodingFlag
                {
                    FlagType = "ModifierAbuse",
                    Severity = "Medium",
                    Description = "Modifier 25 used without separate significant procedure",
                    ExpectedCode = emService.ProcedureCode,
                    ActualCode = $"{emService.ProcedureCode}-25"
                });
            }
        }
        
        // 3. Check for unbundling (billing components instead of comprehensive code)
        var unbundlingPatterns = DetectUnbundling(currentClaim.Services);
        analysis.Flags.AddRange(unbundlingPatterns);
        
        // 4. Check for diagnosis-procedure mismatch
        if (!IsDiagnosisAppropriate(currentClaim.PrimaryDiagnosis, currentClaim.PrimaryServiceCode))
        {
            analysis.Flags.Add(new UpcodingFlag
            {
                FlagType = "DiagnosisMismatch",
                Severity = "High",
                Description = $"Diagnosis {currentClaim.PrimaryDiagnosis} doesn't support service level {currentClaim.PrimaryServiceCode}",
                RequiresReview = true
            });
        }
        
        // 5. Compare to regional benchmarks
        var regionalAvg = GetRegionalAverage(currentClaim.ProviderId, currentClaim.PrimaryServiceCode);
        if (currentClaim.TotalCharges > regionalAvg * 1.5m)
        {
            analysis.Flags.Add(new UpcodingFlag
            {
                FlagType = "PricingOutlier",
                Severity = "Medium",
                Description = $"Charges 50% above regional average (${regionalAvg:F2})",
                PotentialOvercharge = currentClaim.TotalCharges - regionalAvg
            });
        }
        
        analysis.RiskScore = CalculateUpcodingRiskScore(analysis.Flags);
        analysis.RequiresAudit = analysis.RiskScore > 70;
        
        return analysis;
    }
}

public class UpcodingFlag
{
    public string FlagType { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ExpectedCode { get; set; } = string.Empty;
    public string ActualCode { get; set; } = string.Empty;
    public decimal PotentialOvercharge { get; set; }
    public bool RequiresReview { get; set; }
}
```

---

## 📊 **Accumulator Calculation from Claims**

### How Accumulators Work

Accumulators track member spending across the enrollment period. They're updated with each claim adjudication:

```csharp
public class AccumulatorService
{
    public async Task<MemberAccumulator> UpdateAccumulators(
        MemberAccumulator accumulator, 
        PaymentCalculation payment,
        Claim claim)
    {
        // Update deductible accumulators
        var deductibleApplied = payment.LineItems.Sum(l => l.Deductible);
        accumulator.IndividualDeductibleMet += deductibleApplied;
        accumulator.IndividualDeductibleRemaining -= deductibleApplied;
        
        // Update OOP accumulators
        var patientCosts = payment.LineItems.Sum(l => 
            l.Deductible + l.Coinsurance + l.Copay);
        accumulator.IndividualOopMet += patientCosts;
        accumulator.IndividualOopRemaining -= patientCosts;
        
        // Update service-specific accumulators
        foreach (var service in claim.Services)
        {
            if (IsOfficeVisit(service.ProcedureCode))
            {
                accumulator.OfficeVisitsUsed++;
                accumulator.OfficeVisitsRemaining--;
            }
            else if (IsInpatientDay(service.ProcedureCode))
            {
                accumulator.HospitalDaysUsed += (int)service.Quantity;
                accumulator.HospitalDaysRemaining -= (int)service.Quantity;
            }
            else if (IsPrescription(service.ProcedureCode))
            {
                accumulator.PrescriptionSpend += payment.LineItems
                    .Where(l => l.ServiceCode == service.ProcedureCode)
                    .Sum(l => l.PlanPaid + l.Deductible + l.Coinsurance);
            }
        }
        
        // Update family accumulators if applicable
        if (claim.IsFamilyMember && accumulator.FamilyDeductibleRemaining > 0)
        {
            accumulator.FamilyDeductibleMet += deductibleApplied;
            accumulator.FamilyDeductibleRemaining -= deductibleApplied;
        }
        
        // Update timestamps
        accumulator.LastClaimDate = claim.ServiceDate;
        accumulator.LastUpdated = DateTime.UtcNow;
        
        // Persist to Cosmos DB
        await SaveAccumulator(accumulator);
        
        return accumulator;
    }
    
    // Reset accumulators at end of enrollment period
    public async Task ResetAccumulators(string memberId, string planId)
    {
        var accumulator = await GetAccumulator(memberId, planId);
        var plan = await GetPlan(planId);
        
        // Check if enrollment period has ended
        if (DateTime.Today > accumulator.EnrollmentEndDate)
        {
            // Create new accumulator for new period
            var newAccumulator = new MemberAccumulator
            {
                MemberId = memberId,
                PlanId = planId,
                EnrollmentStartDate = accumulator.EnrollmentEndDate.AddDays(1),
                EnrollmentEndDate = accumulator.EnrollmentEndDate.AddYears(1),
                AccumulatorPeriod = $"Calendar Year {accumulator.EnrollmentEndDate.Year + 1}",
                
                // Set limits from plan
                IndividualDeductibleRemaining = plan.Deductible,
                FamilyDeductibleRemaining = plan.FamilyDeductible,
                IndividualOopRemaining = plan.OutOfPocketMax,
                FamilyOopRemaining = plan.FamilyOopMax,
                
                // Reset service counts
                OfficeVisitsRemaining = plan.MaxOfficeVisits,
                HospitalDaysRemaining = plan.MaxHospitalDays,
                
                // Initialize dates
                LastUpdated = DateTime.UtcNow,
                DaysInPeriod = 365,
                DaysRemaining = 365
            };
            
            await SaveAccumulator(newAccumulator);
        }
    }
}
```

### Accumulator Update Trigger

Claims adjudication automatically updates accumulators:

```csharp
public async Task<ClaimAdjudicationResult> AdjudicateClaim(Claim claim)
{
    // 1. Load member accumulators
    var accumulator = await _accumulatorService.GetAccumulator(claim.MemberId, claim.PlanId);
    
    // 2. Load benefit plan
    var plan = await _planService.GetPlan(claim.PlanId);
    
    // 3. Calculate payment
    var payment = _paymentCalculator.CalculatePayment(claim, plan, accumulator);
    
    // 4. Update accumulators with this claim's impact
    accumulator = await _accumulatorService.UpdateAccumulators(accumulator, payment, claim);
    
    // 5. Generate remittance
    var remittance = _remittanceService.GenerateRemittance(payment);
    
    // 6. Return complete adjudication result
    return new ClaimAdjudicationResult
    {
        ClaimId = claim.ClaimId,
        Status = "Adjudicated",
        Payment = payment,
        UpdatedAccumulators = accumulator,
        RemittanceId = remittance.RemittanceId
    };
}
```

---

## 🔄 **Integration Architecture**

```
┌─────────────────────────────────────────────────────────────┐
│                     Claim Ingestion                          │
│  (EDI 837, CSV, Portal, HL7 FHIR Claim)                    │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│              Claim Validation Pipeline                       │
│  • Provider Network Check                                    │
│  • Bundling Rules                                           │
│  • Upcoding Detection                                       │
│  • State Rules Compliance                                   │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│              Rules Engine Adjudication                       │
│  • Coverage Verification                                     │
│  • Prior Auth Check                                         │
│  • Medical Necessity                                        │
│  • Service Limits                                           │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│              Payment Calculation                             │
│  • Load Accumulators                                        │
│  • Calculate Deductible/Coinsurance/Copay                  │
│  • Apply Fee Schedule                                       │
│  • Update Accumulators                                      │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│              Output Generation                               │
│  • 835 EDI Remittance                                       │
│  • EOB (Explanation of Benefits)                            │
│  • Provider Payment                                         │
│  • FHIR ClaimResponse                                       │
└─────────────────────────────────────────────────────────────┘
```

---

## 📝 **Implementation Recommendations**

### Phase 1: Provider Network Management
1. Create Provider and ProviderNetwork tables in Cosmos DB
2. Build network validation API endpoint
3. Integrate network checks into rules engine
4. Add network display to portal

### Phase 2: Payment & Remittance
1. Implement PaymentCalculator service
2. Create fee schedule storage (Cosmos DB)
3. Build 835 EDI generator
4. Add payment history view to portal

### Phase 3: Advanced Rules
1. Implement claim bundling engine
2. Add state rules configuration
3. Build upcoding detection service
4. Integrate with fraud detection

### Phase 4: Accumulator Enhancement
1. Real-time accumulator updates on claim adjudication
2. Family accumulator aggregation
3. Period rollover automation
4. Historical trending dashboard

All these features map cleanly to **FHIR R4 resources** and can be deployed as additional Azure Functions with Cosmos DB storage.
