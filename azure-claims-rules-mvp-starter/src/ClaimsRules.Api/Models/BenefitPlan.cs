
using System.Collections.Generic;

namespace ClaimsRules.Api.Models
{
    public class BenefitPlan
    {
        public string PlanId { get; set; } = string.Empty;
        public string PlanName { get; set; } = string.Empty;
        public string PlanType { get; set; } = "HMO"; // HMO, PPO, EPO, POS
        public decimal Deductible { get; set; }
        public decimal OutOfPocketMax { get; set; }
        public decimal Copay { get; set; }
        public decimal CoinsuranceRate { get; set; } // 0.2 = 20%
        public List<string> CoveredServices { get; set; } = new();
        public List<string> NetworkProviders { get; set; } = new();
    }

    public class EligibilityCheck
    {
        public string PatientId { get; set; } = string.Empty;
        public string PlanId { get; set; } = string.Empty;
        public bool IsEligible { get; set; }
        public string Reason { get; set; } = string.Empty;
        public decimal RemainingDeductible { get; set; }
        public decimal YearToDateClaims { get; set; }
    }

    public class ClaimValidationResult
    {
        public bool IsApproved { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public decimal PatientResponsibility { get; set; }
        public decimal InsurancePayment { get; set; }
        public string DenialReason { get; set; } = string.Empty;
    }
}
