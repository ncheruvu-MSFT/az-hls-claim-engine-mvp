
using System.Net;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using ClaimsRules.Api.Models;

namespace ClaimsRules.Api.Functions
{
    /// <summary>
    /// Benefits API -查询plans, eligibility, coverage
    /// </summary>
    public class BenefitsFn
    {
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
                CoveredServices = new() { "99213", "99214", "80053", "85025" },
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
            },
            new BenefitPlan
            {
                PlanId = "PLAN003",
                PlanName = "Bronze Catastrophic",
                PlanType = "EPO",
                Deductible = 5000,
                OutOfPocketMax = 8000,
                Copay = 75,
                CoinsuranceRate = 0.4m,
                CoveredServices = new() { "99213" },
                NetworkProviders = new() { "PROV001" }
            }
        };

        [Function("get-plans")]
        public async Task<HttpResponseData> GetPlans(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req,
            FunctionContext ctx)
        {
            var log = ctx.GetLogger<BenefitsFn>();
            log.LogInformation("Fetching benefit plans");

            var res = req.CreateResponse(HttpStatusCode.OK);
            await res.WriteAsJsonAsync(_plans);
            return res;
        }

        [Function("get-plan")]
        public async Task<HttpResponseData> GetPlan(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "plans/{planId}")] HttpRequestData req,
            string planId,
            FunctionContext ctx)
        {
            var log = ctx.GetLogger<BenefitsFn>();
            log.LogInformation($"Fetching plan: {planId}");

            var plan = _plans.FirstOrDefault(p => p.PlanId == planId);
            var res = req.CreateResponse();

            if (plan == null)
            {
                res.StatusCode = HttpStatusCode.NotFound;
                await res.WriteStringAsync($"{{\"error\":\"Plan {planId} not found\"}}");
            }
            else
            {
                res.StatusCode = HttpStatusCode.OK;
                await res.WriteAsJsonAsync(plan);
            }

            return res;
        }

        [Function("check-eligibility")]
        public async Task<HttpResponseData> CheckEligibility(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req,
            FunctionContext ctx)
        {
            var log = ctx.GetLogger<BenefitsFn>();
            
            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            string patientId = query["patientId"] ?? "PAT001";
            string planId = query["planId"] ?? "PLAN001";

            log.LogInformation($"Checking eligibility: Patient={patientId}, Plan={planId}");

            var plan = _plans.FirstOrDefault(p => p.PlanId == planId);
            var result = new EligibilityCheck
            {
                PatientId = patientId,
                PlanId = planId
            };

            if (plan == null)
            {
                result.IsEligible = false;
                result.Reason = "Plan not found";
            }
            else
            {
                // In production, check against enrollment database
                result.IsEligible = true;
                result.Reason = "Active coverage";
                result.RemainingDeductible = plan.Deductible * 0.7m; // Mock: 70% remaining
                result.YearToDateClaims = plan.Deductible * 0.3m;
            }

            var res = req.CreateResponse(HttpStatusCode.OK);
            await res.WriteAsJsonAsync(result);
            return res;
        }
    }
}
