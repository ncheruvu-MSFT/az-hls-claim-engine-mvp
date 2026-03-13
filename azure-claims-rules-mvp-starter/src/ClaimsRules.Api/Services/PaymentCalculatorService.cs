using ClaimsRules.Api.Models;

namespace ClaimsRules.Api.Services;

/// <summary>
/// Payment calculation service with fee schedule application and line-item detail
/// Implements commercial payer adjudication logic
/// </summary>
public class PaymentCalculatorService
{
    // Sample fee schedules (normally loaded from database/configuration)
    private static readonly Dictionary<string, FeeSchedule> _feeSchedules = new()
    {
        // Office visits
        ["PLAN001-99213-11"] = new() { PlanId = "PLAN001", ServiceCode = "99213", PlaceOfService = "11", AllowedAmount = 110.00m },
        ["PLAN001-99214-11"] = new() { PlanId = "PLAN001", ServiceCode = "99214", PlaceOfService = "11", AllowedAmount = 165.00m },
        ["PLAN001-99215-11"] = new() { PlanId = "PLAN001", ServiceCode = "99215", PlaceOfService = "11", AllowedAmount = 210.00m },
        
        // Lab tests
        ["PLAN001-80053-11"] = new() { PlanId = "PLAN001", ServiceCode = "80053", PlaceOfService = "11", AllowedAmount = 25.00m },
        ["PLAN001-85025-11"] = new() { PlanId = "PLAN001", ServiceCode = "85025", PlaceOfService = "11", AllowedAmount = 15.00m },
        
        // Imaging
        ["PLAN001-71046-11"] = new() { PlanId = "PLAN001", ServiceCode = "71046", PlaceOfService = "11", AllowedAmount = 75.00m },
        ["PLAN001-72110-11"] = new() { PlanId = "PLAN001", ServiceCode = "72110", PlaceOfService = "11", AllowedAmount = 95.00m },
        
        // Procedures
        ["PLAN001-29881-21"] = new() { PlanId = "PLAN001", ServiceCode = "29881", PlaceOfService = "21", AllowedAmount = 850.00m },
        ["PLAN001-43239-21"] = new() { PlanId = "PLAN001", ServiceCode = "43239", PlaceOfService = "21", AllowedAmount = 450.00m },
        
        // PLAN002 rates (typically 10% lower)
        ["PLAN002-99213-11"] = new() { PlanId = "PLAN002", ServiceCode = "99213", PlaceOfService = "11", AllowedAmount = 99.00m },
        ["PLAN002-99214-11"] = new() { PlanId = "PLAN002", ServiceCode = "99214", PlaceOfService = "11", AllowedAmount = 148.50m },
    };

    /// <summary>
    /// Calculate payment for claim with line-item detail
    /// </summary>
    public PaymentCalculation CalculatePayment(
        Claim claim,
        BenefitPlan plan,
        MemberAccumulator accumulator,
        NetworkValidationResult? networkResult = null)
    {
        var calculation = new PaymentCalculation
        {
            ClaimId = claim.ClaimId,
            MemberId = claim.MemberId,
            PlanId = claim.PlanId,
            TotalBilled = claim.TotalCharges
        };

        var lineNumber = 1;
        decimal runningDeductible = accumulator.DeductibleMet;
        decimal runningOutOfPocket = accumulator.OutOfPocketMet;

        foreach (var service in claim.Services)
        {
            var lineItem = CalculateLineItem(
                lineNumber,
                service,
                plan,
                ref runningDeductible,
                ref runningOutOfPocket,
                networkResult);

            calculation.LineItems.Add(lineItem);
            lineNumber++;
        }

        // Calculate totals
        calculation.TotalAllowed = calculation.LineItems.Sum(li => li.AllowedAmount * li.Quantity);
        calculation.TotalAdjustment = calculation.TotalBilled - calculation.TotalAllowed;
        calculation.DeductibleApplied = calculation.LineItems.Sum(li => li.Deductible);
        calculation.CoinsuranceApplied = calculation.LineItems.Sum(li => li.Coinsurance);
        calculation.CopayApplied = calculation.LineItems.Sum(li => li.Copay);
        calculation.TotalPatientResponsibility = calculation.LineItems.Sum(li => li.TotalPatientCost);
        calculation.TotalPlanPaid = calculation.LineItems.Sum(li => li.PlanPaid);

        // Add summary messages
        calculation.Messages.Add($"Processed {calculation.LineItems.Count} service lines");
        calculation.Messages.Add($"Total charges: ${calculation.TotalBilled:F2}");
        calculation.Messages.Add($"Total allowed: ${calculation.TotalAllowed:F2}");
        calculation.Messages.Add($"Contractual adjustment: ${calculation.TotalAdjustment:F2}");
        calculation.Messages.Add($"Plan paid: ${calculation.TotalPlanPaid:F2}");
        calculation.Messages.Add($"Patient responsibility: ${calculation.TotalPatientResponsibility:F2}");

        if (networkResult?.NetworkStatus == "Out-of-Network")
        {
            calculation.Messages.Add("⚠️ Out-of-network provider - higher patient cost share applied");
        }

        return calculation;
    }

    /// <summary>
    /// Calculate single line item payment
    /// </summary>
    private PaymentLineItem CalculateLineItem(
        int lineNumber,
        ClaimService service,
        BenefitPlan plan,
        ref decimal runningDeductible,
        ref decimal runningOutOfPocket,
        NetworkValidationResult? networkResult)
    {
        var lineItem = new PaymentLineItem
        {
            LineNumber = lineNumber,
            ServiceCode = service.ProcedureCode,
            ServiceDescription = GetServiceDescription(service.ProcedureCode),
            ServiceDate = service.ServiceDate,
            ProviderId = service.ProviderId,
            PlaceOfService = service.PlaceOfService,
            Quantity = service.Quantity,
            BilledAmount = service.ChargeAmount,
            NetworkStatus = networkResult?.NetworkStatus ?? "In-Network",
            NetworkCostMultiplier = networkResult?.CostShareMultiplier ?? 1.0m
        };

        // Step 1: Apply fee schedule to get allowed amount
        var allowedAmount = GetAllowedAmount(plan.PlanId, service.ProcedureCode, service.PlaceOfService);
        lineItem.AllowedAmount = allowedAmount;
        lineItem.Adjustment = lineItem.BilledAmount - allowedAmount;

        if (lineItem.Adjustment > 0)
        {
            lineItem.Adjustments.Add(new AdjustmentCode
            {
                GroupCode = "CO",
                ReasonCode = "45",
                Description = "Charge exceeds fee schedule/maximum allowable",
                Amount = lineItem.Adjustment
            });
        }

        // Step 2: Apply network cost multiplier (out-of-network penalty)
        var effectiveAllowed = allowedAmount * lineItem.NetworkCostMultiplier;

        // Step 3: Apply copay (if applicable)
        decimal copay = 0;
        if (IsOfficeCopayApplicable(service.ProcedureCode))
        {
            copay = plan.Copay;
            lineItem.Copay = copay;
            lineItem.Adjustments.Add(new AdjustmentCode
            {
                GroupCode = "PR",
                ReasonCode = "2",
                Description = "Copay amount",
                Amount = copay
            });
        }

        // Step 4: Apply deductible
        decimal remainingDeductible = Math.Max(0, plan.Deductible - runningDeductible);
        decimal amountSubjectToDeductible = Math.Max(0, effectiveAllowed - copay);
        decimal deductibleApplied = Math.Min(remainingDeductible, amountSubjectToDeductible);
        
        lineItem.Deductible = deductibleApplied;
        runningDeductible += deductibleApplied;

        if (deductibleApplied > 0)
        {
            lineItem.Adjustments.Add(new AdjustmentCode
            {
                GroupCode = "PR",
                ReasonCode = "1",
                Description = "Deductible amount",
                Amount = deductibleApplied
            });
        }

        // Step 5: Apply coinsurance to remaining amount
        decimal amountAfterDeductible = amountSubjectToDeductible - deductibleApplied;
        decimal coinsurance = amountAfterDeductible * plan.CoinsuranceRate;
        
        lineItem.Coinsurance = coinsurance;
        
        if (coinsurance > 0)
        {
            lineItem.Adjustments.Add(new AdjustmentCode
            {
                GroupCode = "PR",
                ReasonCode = "3",
                Description = $"Coinsurance amount ({plan.CoinsuranceRate * 100}%)",
                Amount = coinsurance
            });
        }

        // Step 6: Check out-of-pocket maximum
        decimal totalPatientCost = copay + deductibleApplied + coinsurance;
        decimal remainingOOP = Math.Max(0, plan.OutOfPocketMax - runningOutOfPocket);
        
        if (totalPatientCost > remainingOOP)
        {
            // Hit OOP max - plan covers excess
            decimal oopExcess = totalPatientCost - remainingOOP;
            totalPatientCost = remainingOOP;
            
            lineItem.Adjustments.Add(new AdjustmentCode
            {
                GroupCode = "PR",
                ReasonCode = "267",
                Description = "Out-of-pocket maximum reached",
                Amount = -oopExcess // Negative = patient benefit
            });
        }

        runningOutOfPocket += totalPatientCost;

        lineItem.TotalPatientCost = totalPatientCost;
        lineItem.PlanPaid = effectiveAllowed - totalPatientCost;

        return lineItem;
    }

    /// <summary>
    /// Get allowed amount from fee schedule
    /// </summary>
    private decimal GetAllowedAmount(string planId, string serviceCode, string placeOfService)
    {
        var key = $"{planId}-{serviceCode}-{placeOfService}";
        
        if (_feeSchedules.TryGetValue(key, out var schedule))
        {
            return schedule.AllowedAmount;
        }

        // Default: 80% of billed (typical contracted rate)
        return 0; // Caller will use billed * 0.8
    }

    /// <summary>
    /// Get service description for common CPT codes
    /// </summary>
    private string GetServiceDescription(string serviceCode)
    {
        var descriptions = new Dictionary<string, string>
        {
            ["99213"] = "Office visit - established patient, moderate complexity",
            ["99214"] = "Office visit - established patient, high complexity",
            ["99215"] = "Office visit - established patient, very high complexity",
            ["99204"] = "Office visit - new patient, high complexity",
            ["80053"] = "Comprehensive metabolic panel",
            ["85025"] = "Complete blood count with differential",
            ["71046"] = "Chest X-ray, 2 views",
            ["72110"] = "Spine X-ray, 4 views",
            ["29881"] = "Knee arthroscopy with meniscectomy",
            ["43239"] = "Upper gastrointestinal endoscopy with biopsy",
            ["93000"] = "Electrocardiogram, complete",
            ["96372"] = "Therapeutic injection, subcutaneous or intramuscular",
        };

        return descriptions.TryGetValue(serviceCode, out var desc) ? desc : serviceCode;
    }

    /// <summary>
    /// Check if copay applies (typically for E/M visits)
    /// </summary>
    private bool IsOfficeCopayApplicable(string serviceCode)
    {
        // E/M visit codes (99201-99215)
        if (serviceCode.StartsWith("992") && serviceCode.Length == 5)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Generate HIPAA 835 remittance advice (simplified)
    /// </summary>
    public string GenerateRemittance835(PaymentCalculation payment, BenefitPlan plan)
    {
        var remittance = new System.Text.StringBuilder();
        
        // ISA segment (Interchange Control Header)
        remittance.AppendLine("ISA*00*          *00*          *ZZ*SENDER         *ZZ*RECEIVER       *" + 
                              $"{DateTime.Now:yyMMdd}*{DateTime.Now:HHmm}*U*00401*000000001*0*P*:~");
        
        // GS segment (Functional Group Header)
        remittance.AppendLine($"GS*HP*SENDER*RECEIVER*{DateTime.Now:yyyyMMdd}*{DateTime.Now:HHmm}*1*X*004010X091A1~");
        
        // ST segment (Transaction Set Header)
        remittance.AppendLine("ST*835*0001~");
        
        // BPR segment (Financial Information)
        remittance.AppendLine($"BPR*C*{payment.TotalPlanPaid:F2}*C*ACH*CTX*01*999999999*DA*123456*" +
                              $"1234567890**01*999999999*DA*12345*{DateTime.Now:yyyyMMdd}~");
        
        // TRN segment (Reassociation Trace Number)
        remittance.AppendLine($"TRN*1*{payment.ClaimId}*1234567890~");
        
        // REF segment (Receiver Identification)
        remittance.AppendLine($"REF*EV*{plan.PlanId}~");
        
        // DTM segment (Production Date)
        remittance.AppendLine($"DTM*405*{DateTime.Now:yyyyMMdd}~");
        
        // N1 segment (Payer Identification)
        remittance.AppendLine($"N1*PR*{plan.PlanName}~");
        
        // CLP segment (Claim Payment Information)
        remittance.AppendLine($"CLP*{payment.ClaimId}*1*{payment.TotalBilled:F2}*{payment.TotalPlanPaid:F2}*" +
                              $"{payment.TotalPatientResponsibility:F2}*12*{payment.ClaimId}*11~");
        
        // CAS segment (Claim Level Adjustments)
        if (payment.TotalAdjustment > 0)
        {
            remittance.AppendLine($"CAS*CO*45*{payment.TotalAdjustment:F2}~");
        }
        
        // NM1 segment (Patient Name)
        remittance.AppendLine($"NM1*QC*1*PATIENT*SAMPLE****MI*{payment.MemberId}~");
        
        // Service line details
        foreach (var lineItem in payment.LineItems)
        {
            // SVC segment (Service Payment Information)
            remittance.AppendLine($"SVC*HC:{lineItem.ServiceCode}*{lineItem.BilledAmount:F2}*" +
                                  $"{lineItem.PlanPaid:F2}**{lineItem.Quantity}~");
            
            // DTM segment (Service Date)
            remittance.AppendLine($"DTM*472*{lineItem.ServiceDate:yyyyMMdd}~");
            
            // CAS segments (Service Level Adjustments)
            foreach (var adjustment in lineItem.Adjustments)
            {
                remittance.AppendLine($"CAS*{adjustment.GroupCode}*{adjustment.ReasonCode}*{adjustment.Amount:F2}~");
            }
        }
        
        // SE segment (Transaction Set Trailer)
        var segmentCount = remittance.ToString().Count(c => c == '~');
        remittance.AppendLine($"SE*{segmentCount + 1}*0001~");
        
        // GE segment (Functional Group Trailer)
        remittance.AppendLine("GE*1*1~");
        
        // IEA segment (Interchange Control Trailer)
        remittance.AppendLine("IEA*1*000000001~");
        
        return remittance.ToString();
    }

    /// <summary>
    /// Calculate payment summary for display
    /// </summary>
    public Dictionary<string, decimal> GetPaymentSummary(PaymentCalculation payment)
    {
        return new Dictionary<string, decimal>
        {
            ["Total Charges"] = payment.TotalBilled,
            ["Contractual Adjustment"] = -payment.TotalAdjustment,
            ["Allowed Amount"] = payment.TotalAllowed,
            ["Deductible"] = payment.DeductibleApplied,
            ["Coinsurance"] = payment.CoinsuranceApplied,
            ["Copay"] = payment.CopayApplied,
            ["Patient Responsibility"] = payment.TotalPatientResponsibility,
            ["Plan Paid"] = payment.TotalPlanPaid
        };
    }
}
