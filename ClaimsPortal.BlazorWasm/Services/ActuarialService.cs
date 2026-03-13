using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using ClaimsPortal.BlazorWasm.Models;

namespace ClaimsPortal.BlazorWasm.Services;

public class ActuarialService
{
    private readonly HttpClient _httpClient;
    private readonly bool _useMockData = false;  // Always use backend APIs

    public ActuarialService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://funcclaimstest001ncv.azurewebsites.net/api/");
    }

    // ===== BASELINE METHODS =====

    public async Task<BaselineMetricsModel> GetBaselineAsync(string segment, string period)
    {
        if (_useMockData) return GetMockBaseline(segment, period);
        
        var response = await _httpClient.GetAsync($"baseline?segment={segment}&period={period}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<BaselineMetricsModel>() ?? new();
    }

    // ===== SCENARIO METHODS =====

    public async Task<ScenarioModel> CreateScenarioAsync(ScenarioModel scenario)
    {
        if (_useMockData) return CreateMockScenario(scenario);
        
        var response = await _httpClient.PostAsJsonAsync("scenarios", scenario);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ScenarioModel>() ?? new();
    }

    public async Task<ScenarioResultModel> RunScenarioAsync(string scenarioId)
    {
        if (_useMockData) return GetMockScenarioResult();
        
        var response = await _httpClient.PostAsync($"scenarios/{scenarioId}/run", null);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ScenarioResultModel>() ?? new();
    }

    public async Task<ScenarioModel> GetScenarioAsync(string scenarioId)
    {
        if (_useMockData) return GetMockScenarios().First(s => s.ScenarioId == scenarioId);
        
        var response = await _httpClient.GetAsync($"scenarios/{scenarioId}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ScenarioModel>() ?? new();
    }

    public async Task<List<ScenarioModel>> ListScenariosAsync(string? owner = null)
    {
        if (_useMockData) return GetMockScenarios();
        
        var url = owner != null ? $"scenarios?owner={owner}" : "scenarios";
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<ScenarioModel>>() ?? new();
    }

    // ===== RISK WEIGHTS METHODS =====

    public async Task<RiskWeightsModel> GetRiskWeightsAsync()
    {
        if (_useMockData) return GetMockRiskWeights();
        
        var response = await _httpClient.GetAsync("weights");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<RiskWeightsModel>() ?? new();
    }

    public async Task UpdateRiskWeightsAsync(RiskWeightsModel weights)
    {
        if (_useMockData) return;
        
        var response = await _httpClient.PutAsJsonAsync("weights", weights);
        response.EnsureSuccessStatusCode();
    }

    // ===== MOCK DATA METHODS =====

    private BaselineMetricsModel GetMockBaseline(string segment, string period)
    {
        return new BaselineMetricsModel
        {
            BaselineId = $"baseline-2025-{segment.ToLower()}-{period}",
            Segment = segment,
            PeriodStart = DateTime.Parse("2025-01-01"),
            PeriodEnd = DateTime.Parse("2025-12-31"),
            
            // Financial
            TotalAllowed = 12_500_000m,
            ProviderAllowed = 9_000_000m,
            PharmacyAllowed = 3_000_000m,
            OtherAllowed = 500_000m,
            
            // Member-months
            MemberMonths = 25_000m,
            UniqueMemberCount = 2_150,
            
            // PMPM
            TotalPMPM = 500.00m,
            ProviderPMPM = 360.00m,
            PharmacyPMPM = 120.00m,
            OtherPMPM = 20.00m,
            
            // Service buckets
            ServiceBuckets = new ServiceBucketMetricsModel
            {
                InpatientAllowed = 4_000_000m,
                InpatientPMPM = 160.00m,
                OutpatientAllowed = 3_500_000m,
                OutpatientPMPM = 140.00m,
                ProfessionalAllowed = 1_500_000m,
                ProfessionalPMPM = 60.00m,
                PharmacyAllowed = 3_000_000m,
                PharmacyPMPM = 120.00m,
                OtherAllowed = 500_000m,
                OtherPMPM = 20.00m
            },
            
            // Risk and utilization
            PopulationRiskIndex = 1.05m,
            UtilizationIndex = 1.00m,
            
            // Utilization rates
            Utilization = new UtilizationRatesModel
            {
                EdVisitsPer1000 = 450m,
                IpAdmitsPer1000 = 85m,
                OpVisitsPer1000 = 3200m,
                OfficeVisitsPer1000 = 8500m,
                TotalEdVisits = 956,
                TotalIpAdmits = 181,
                TotalOpVisits = 6800,
                TotalOfficeVisits = 18063
            },
            
            // Top conditions
            TopConditions = new List<ConditionPrevalenceModel>
            {
                new() { ConditionGroup = "HCC_106_Hypertension", ConditionName = "Hypertension", MemberCount = 645, PrevalenceRate = 30.0m, AvgRiskScore = 2.8m },
                new() { ConditionGroup = "HCC_018_Diabetes_NoComplications", ConditionName = "Diabetes without complications", MemberCount = 387, PrevalenceRate = 18.0m, AvgRiskScore = 3.5m },
                new() { ConditionGroup = "HCC_096_COPD", ConditionName = "COPD", MemberCount = 258, PrevalenceRate = 12.0m, AvgRiskScore = 4.2m },
                new() { ConditionGroup = "HCC_085_CHF", ConditionName = "Congestive Heart Failure", MemberCount = 172, PrevalenceRate = 8.0m, AvgRiskScore = 5.1m },
                new() { ConditionGroup = "HCC_135_CAD", ConditionName = "Coronary Artery Disease", MemberCount = 150, PrevalenceRate = 7.0m, AvgRiskScore = 4.0m },
                new() { ConditionGroup = "HCC_112_Asthma", ConditionName = "Asthma", MemberCount = 129, PrevalenceRate = 6.0m, AvgRiskScore = 2.3m },
                new() { ConditionGroup = "HCC_017_Diabetes_Complications", ConditionName = "Diabetes with complications", MemberCount = 108, PrevalenceRate = 5.0m, AvgRiskScore = 5.8m },
                new() { ConditionGroup = "HCC_114_Depression", ConditionName = "Depression", MemberCount = 86, PrevalenceRate = 4.0m, AvgRiskScore = 2.9m },
                new() { ConditionGroup = "HCC_110_CKD_Stage4", ConditionName = "CKD Stage 4", MemberCount = 65, PrevalenceRate = 3.0m, AvgRiskScore = 6.5m },
                new() { ConditionGroup = "HCC_130_Stroke", ConditionName = "Stroke", MemberCount = 43, PrevalenceRate = 2.0m, AvgRiskScore = 7.2m }
            },
            
            // Risk distribution
            RiskDistribution = new RiskDistributionModel
            {
                MinScore = 0.5m,
                MaxScore = 12.8m,
                AvgScore = 2.1m,
                MedianScore = 1.8m,
                Buckets = new List<RiskBucketModel>
                {
                    new() { Label = "0-1", MinScore = 0m, MaxScore = 1m, MemberCount = 645, Percentage = 30.0m },
                    new() { Label = "1-2", MinScore = 1m, MaxScore = 2m, MemberCount = 538, Percentage = 25.0m },
                    new() { Label = "2-3", MinScore = 2m, MaxScore = 3m, MemberCount = 430, Percentage = 20.0m },
                    new() { Label = "3-4", MinScore = 3m, MaxScore = 4m, MemberCount = 258, Percentage = 12.0m },
                    new() { Label = "4-5", MinScore = 4m, MaxScore = 5m, MemberCount = 150, Percentage = 7.0m },
                    new() { Label = "5+", MinScore = 5m, MaxScore = 15m, MemberCount = 129, Percentage = 6.0m }
                }
            }
        };
    }

    private List<ScenarioModel> GetMockScenarios()
    {
        var baseline = GetMockBaseline("Plan_HMO_East", "last12mo");
        
        return new List<ScenarioModel>
        {
            new()
            {
                ScenarioId = "scenario-001",
                Name = "2026 Budget - Base Case",
                Description = "Conservative baseline with 5% provider trend, 8% pharmacy trend",
                Owner = "john.actuary@claimsiq.com",
                BaselineId = baseline.BaselineId,
                Knobs = new ScenarioKnobsModel
                {
                    ProviderTrendPct = 5.0m,
                    PharmacyTrendPct = 8.0m,
                    MembershipDeltaPct = 0m,
                    UtilizationDeltaPct = 0m,
                    PopulationRiskDeltaPct = 0m
                },
                Result = new ScenarioResultModel
                {
                    ForecastPMPM = 518.80m,
                    DeltaPMPM = 18.80m,
                    DeltaPct = 3.76m,
                    DeltaDrivers = new DeltaDriversModel
                    {
                        ProviderTrendDelta = 12.50m,
                        PharmacyTrendDelta = 8.00m,
                        RiskDelta = 0m,
                        UtilizationDelta = 0m,
                        MembershipDelta = 0m
                    }
                },
                Status = "completed",
                CreatedAt = DateTime.Parse("2026-01-02"),
                ComputedAt = DateTime.Parse("2026-01-02")
            },
            new()
            {
                ScenarioId = "scenario-002",
                Name = "2026 Budget - High Growth",
                Description = "Aggressive membership growth (10%) with moderate trends",
                Owner = "john.actuary@claimsiq.com",
                BaselineId = baseline.BaselineId,
                Knobs = new ScenarioKnobsModel
                {
                    ProviderTrendPct = 5.0m,
                    PharmacyTrendPct = 8.0m,
                    MembershipDeltaPct = 10.0m,
                    UtilizationDeltaPct = 2.0m,
                    PopulationRiskDeltaPct = -1.0m  // Slightly healthier new members
                },
                Result = new ScenarioResultModel
                {
                    ForecastPMPM = 512.65m,
                    DeltaPMPM = 12.65m,
                    DeltaPct = 2.53m
                },
                Status = "completed",
                CreatedAt = DateTime.Parse("2026-01-03"),
                ComputedAt = DateTime.Parse("2026-01-03")
            },
            new()
            {
                ScenarioId = "scenario-003",
                Name = "2026 Budget - Worst Case",
                Description = "High trends, aging population, increased utilization",
                Owner = "john.actuary@claimsiq.com",
                BaselineId = baseline.BaselineId,
                Knobs = new ScenarioKnobsModel
                {
                    ProviderTrendPct = 8.0m,
                    PharmacyTrendPct = 12.0m,
                    MembershipDeltaPct = -5.0m,
                    UtilizationDeltaPct = 5.0m,
                    PopulationRiskDeltaPct = 3.0m,
                    ConditionPrevalenceShiftPct = new Dictionary<string, decimal>
                    {
                        ["HCC_085_CHF"] = 5.0m,
                        ["HCC_017_Diabetes_Complications"] = 3.0m
                    }
                },
                Result = new ScenarioResultModel
                {
                    ForecastPMPM = 587.25m,
                    DeltaPMPM = 87.25m,
                    DeltaPct = 17.45m
                },
                Status = "completed",
                CreatedAt = DateTime.Parse("2026-01-04"),
                ComputedAt = DateTime.Parse("2026-01-04")
            },
            new()
            {
                ScenarioId = "scenario-004",
                Name = "2026 Budget - Care Management Success",
                Description = "Reduced ED utilization, improved chronic disease management",
                Owner = "john.actuary@claimsiq.com",
                BaselineId = baseline.BaselineId,
                Knobs = new ScenarioKnobsModel
                {
                    ProviderTrendPct = 5.0m,
                    PharmacyTrendPct = 8.0m,
                    UtilizationDeltaPct = -5.0m,
                    PopulationRiskDeltaPct = -2.0m,
                    EncounterMixShiftPct = new Dictionary<string, decimal>
                    {
                        ["Emergency"] = -15.0m,
                        ["Office"] = 5.0m
                    }
                },
                Result = new ScenarioResultModel
                {
                    ForecastPMPM = 483.20m,
                    DeltaPMPM = -16.80m,
                    DeltaPct = -3.36m
                },
                Status = "completed",
                CreatedAt = DateTime.Parse("2026-01-05"),
                ComputedAt = DateTime.Parse("2026-01-05")
            }
        };
    }

    private ScenarioModel CreateMockScenario(ScenarioModel scenario)
    {
        scenario.ScenarioId = $"scenario-{Guid.NewGuid().ToString().Substring(0, 8)}";
        scenario.Status = "created";
        scenario.CreatedAt = DateTime.UtcNow;
        return scenario;
    }

    private ScenarioResultModel GetMockScenarioResult()
    {
        return new ScenarioResultModel
        {
            ForecastAllowed = 13_750_000m,
            ForecastMemberMonths = 25_000m,
            ForecastPMPM = 550.00m,
            DeltaAllowed = 1_250_000m,
            DeltaPMPM = 50.00m,
            DeltaPct = 10.00m,
            DeltaDrivers = new DeltaDriversModel
            {
                ProviderTrendDelta = 25.00m,
                PharmacyTrendDelta = 15.00m,
                RiskDelta = 10.00m,
                UtilizationDelta = 5.00m,
                MembershipDelta = -5.00m
            },
            ForecastServiceBuckets = new ServiceBucketMetricsModel
            {
                InpatientPMPM = 176.00m,
                OutpatientPMPM = 154.00m,
                ProfessionalPMPM = 66.00m,
                PharmacyPMPM = 132.00m,
                OtherPMPM = 22.00m
            },
            ForecastUtilization = new UtilizationRatesModel
            {
                EdVisitsPer1000 = 473m,
                IpAdmitsPer1000 = 89m,
                OpVisitsPer1000 = 3360m,
                OfficeVisitsPer1000 = 8925m
            },
            ForecastPopulationRiskIndex = 1.10m
        };
    }

    private RiskWeightsModel GetMockRiskWeights()
    {
        return new RiskWeightsModel
        {
            WeightsId = "risk-weights-v1",
            Version = 1,
            LastModified = DateTime.Parse("2025-12-15"),
            ModifiedBy = "admin@claimsiq.com",
            AgeGenderWeights = new Dictionary<string, decimal>
            {
                ["M_0_34"] = 0.5m,
                ["M_35_44"] = 0.8m,
                ["M_45_54"] = 1.2m,
                ["M_55_64"] = 1.8m,
                ["M_65_74"] = 2.5m,
                ["M_75+"] = 3.5m,
                ["F_0_34"] = 0.6m,
                ["F_35_44"] = 0.9m,
                ["F_45_54"] = 1.3m,
                ["F_55_64"] = 1.9m,
                ["F_65_74"] = 2.6m,
                ["F_75+"] = 3.8m
            },
            ConditionGroupWeights = new Dictionary<string, decimal>
            {
                ["HCC_085_CHF"] = 1.8m,
                ["HCC_017_Diabetes_Complications"] = 1.3m,
                ["HCC_018_Diabetes_NoComplications"] = 0.8m,
                ["HCC_096_COPD"] = 1.2m,
                ["HCC_106_Hypertension"] = 0.6m,
                ["HCC_110_CKD_Stage4"] = 1.7m,
                ["HCC_112_Asthma"] = 0.5m,
                ["HCC_114_Depression"] = 0.4m,
                ["HCC_130_Stroke"] = 1.6m,
                ["HCC_135_CAD"] = 1.1m
            },
            EncounterWeights = new Dictionary<string, decimal>
            {
                ["Emergency"] = 0.3m,
                ["Inpatient"] = 1.0m,
                ["Outpatient"] = 0.1m,
                ["Ambulatory"] = 0.05m
            },
            ObservationSeverityWeights = new Dictionary<string, decimal>
            {
                ["A1c_High"] = 0.2m,
                ["BP_High"] = 0.15m,
                ["BMI_High"] = 0.1m
            }
        };
    }
}
