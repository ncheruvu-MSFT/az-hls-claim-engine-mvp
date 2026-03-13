
using System.Net;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ClaimsRules.Api.Models;

namespace ClaimsRules.Api.Functions
{
    /// <summary>
    /// Claims Rules Engine - validates claims against benefit plans and coverage rules
    /// </summary>
    public class RulesEngineFn
    {
        // In production, load these from Cosmos DB
        private readonly List<BenefitPlan> _plans = new()
        {
            new BenefitPlan
            {
                PlanId = "PLAN001",
                PlanName = "Gold PPO",
                PlanType = "PPO",
                Deductible = 1000,
                OutOfPocketMax = 5000,
                Copay = 30,
                CoinsuranceRate = 0.2m,
                CoveredServices = new() { "99213", "99214", "80053", "85025" }, // CPT codes
                NetworkProviders = new() { "PROV001", "PROV002" }
            },
            new BenefitPlan
            {
                PlanId = "PLAN002",
                PlanName = "Silver HMO",
                PlanType = "HMO",
                Deductible = 2000,
                OutOfPocketMax = 6500,
                Copay = 50,
                CoinsuranceRate = 0.3m,
                CoveredServices = new() { "99213", "99214" },
                NetworkProviders = new() { "PROV001" }
            }
        };

        [Function("validate-claim")]
        public async Task<HttpResponseData> ValidateClaim(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req,
            FunctionContext ctx)
        {
            var log = ctx.GetLogger<RulesEngineFn>();
            var res = req.CreateResponse();

            try
            {
                var body = await new StreamReader(req.Body).ReadToEndAsync();
                var request = JsonConvert.DeserializeObject<dynamic>(body);
                
                string planId = request?.planId ?? "PLAN001";
                string serviceCode = request?.serviceCode ?? "";
                string providerId = request?.providerId ?? "";
                decimal claimAmount = request?.claimAmount ?? 0;
                decimal ytdSpent = request?.yearToDateSpent ?? 0;

                var plan = _plans.FirstOrDefault(p => p.PlanId == planId);
                var result = new ClaimValidationResult();

                if (plan == null)
                {
                    result.IsApproved = false;
                    result.Errors.Add("Plan not found");
                    await res.WriteAsJsonAsync(result);
                    return res;
                }

                // Rule 1: Check if service is covered
                if (!plan.CoveredServices.Contains(serviceCode))
                {
                    result.IsApproved = false;
                    result.DenialReason = $"Service {serviceCode} not covered under {plan.PlanName}";
                    result.Errors.Add(result.DenialReason);
                }

                // Rule 2: Check if provider is in-network (for HMO)
                if (plan.PlanType == "HMO" && !plan.NetworkProviders.Contains(providerId))
                {
                    result.IsApproved = false;
                    result.DenialReason = "Out-of-network provider not covered under HMO";
                    result.Errors.Add(result.DenialReason);
                }
                else if (plan.PlanType == "PPO" && !plan.NetworkProviders.Contains(providerId))
                {
                    result.Warnings.Add("Out-of-network provider - higher cost-sharing applies");
                }

                // Rule 3: Calculate patient responsibility
                var remainingDeductible = plan.Deductible - ytdSpent;
                if (remainingDeductible > 0)
                {
                    result.PatientResponsibility = System.Math.Min(claimAmount, remainingDeductible);
                    var afterDeductible = claimAmount - result.PatientResponsibility;
                    if (afterDeductible > 0)
                    {
                        result.PatientResponsibility += afterDeductible * plan.CoinsuranceRate;
                    }
                }
                else
                {
                    result.PatientResponsibility = claimAmount * plan.CoinsuranceRate + plan.Copay;
                }

                result.InsurancePayment = claimAmount - result.PatientResponsibility;

                // Rule 4: Check out-of-pocket max
                var totalOOP = ytdSpent + result.PatientResponsibility;
                if (totalOOP > plan.OutOfPocketMax)
                {
                    var excess = totalOOP - plan.OutOfPocketMax;
                    result.PatientResponsibility -= excess;
                    result.InsurancePayment += excess;
                    result.Warnings.Add("Out-of-pocket maximum reached - insurance covers remaining");
                }

                if (result.Errors.Count == 0)
                {
                    result.IsApproved = true;
                }

                log.LogInformation($"Claim validation: {result.IsApproved}, Amount: ${claimAmount}, Patient: ${result.PatientResponsibility:F2}");
                
                res.StatusCode = HttpStatusCode.OK;
                await res.WriteAsJsonAsync(result);
                return res;
            }
            catch (System.Exception ex)
            {
                log.LogError($"Error validating claim: {ex.Message}");
                res.StatusCode = HttpStatusCode.InternalServerError;
                await res.WriteStringAsync($"{{\"error\":\"{ex.Message}\"}}");
                return res;
            }
        }
    }
}
