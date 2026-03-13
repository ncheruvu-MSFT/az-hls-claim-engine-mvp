using System;
using System.Collections.Generic;
using System.Linq;
using ClaimsRules.Api.Models;

namespace ClaimsRules.Api.Services;

/// <summary>
/// Deterministic forecast engine for actuarial scenarios
/// Pure functions for testability
/// </summary>
public class ForecastEngine
{
    /// <summary>
    /// Compute forecast from baseline and scenario knobs
    /// </summary>
    public ScenarioResult ComputeForecast(BaselineMetrics baseline, ScenarioKnobs knobs)
    {
        // 1. Apply financial trends to cost components
        var providerComponent = baseline.ProviderAllowed * (1 + knobs.ProviderTrendPct / 100m);
        var pharmacyComponent = baseline.PharmacyAllowed * (1 + knobs.PharmacyTrendPct / 100m);
        var otherComponent = baseline.OtherAllowed * (1 + knobs.OtherTrendPct / 100m);

        var baseTrendedAllowed = providerComponent + pharmacyComponent + otherComponent;

        // 2. Apply utilization factor
        var utilizationFactor = 1 + (knobs.UtilizationDeltaPct / 100m);

        // 3. Apply risk factor
        var riskFactor = 1 + (knobs.PopulationRiskDeltaPct / 100m);

        // 4. Calculate total forecast allowed
        var forecastAllowed = baseTrendedAllowed * utilizationFactor * riskFactor;

        // 5. Apply membership change (affects member-months)
        var forecastMemberMonths = baseline.MemberMonths * (1 + knobs.MembershipDeltaPct / 100m);

        // 6. Calculate forecast PMPM
        var forecastPMPM = forecastMemberMonths > 0 ? forecastAllowed / forecastMemberMonths : 0m;

        // 7. Calculate service bucket forecasts
        var forecastServiceBuckets = CalculateServiceBucketForecast(baseline, knobs, utilizationFactor, riskFactor);

        // 8. Calculate forecast utilization rates
        var forecastUtilization = CalculateUtilizationForecast(baseline, knobs, forecastMemberMonths);

        // 9. Calculate delta drivers for waterfall
        var deltaDrivers = CalculateDeltaDrivers(baseline, knobs, forecastAllowed, forecastMemberMonths);

        // 10. Build result
        return new ScenarioResult
        {
            ForecastAllowed = forecastAllowed,
            ForecastMemberMonths = forecastMemberMonths,
            ForecastPMPM = forecastPMPM,
            ForecastServiceBuckets = forecastServiceBuckets,
            ForecastUtilization = forecastUtilization,
            ForecastPopulationRiskIndex = baseline.PopulationRiskIndex * riskFactor,
            
            DeltaAllowed = forecastAllowed - baseline.TotalAllowed,
            DeltaPMPM = forecastPMPM - baseline.TotalPMPM,
            DeltaPct = baseline.TotalPMPM > 0 ? ((forecastPMPM - baseline.TotalPMPM) / baseline.TotalPMPM) * 100m : 0m,
            
            DeltaDrivers = deltaDrivers
        };
    }

    /// <summary>
    /// Calculate service bucket forecasts
    /// </summary>
    private ServiceBucketMetrics CalculateServiceBucketForecast(
        BaselineMetrics baseline, 
        ScenarioKnobs knobs, 
        decimal utilizationFactor, 
        decimal riskFactor)
    {
        var membershipFactor = 1 + (knobs.MembershipDeltaPct / 100m);
        var providerTrendFactor = 1 + (knobs.ProviderTrendPct / 100m);
        var pharmacyTrendFactor = 1 + (knobs.PharmacyTrendPct / 100m);
        var otherTrendFactor = 1 + (knobs.OtherTrendPct / 100m);

        // Apply encounter mix shifts if specified
        var ipMixFactor = 1 + (knobs.EncounterMixShiftPct.GetValueOrDefault("Inpatient", 0m) / 100m);
        var opMixFactor = 1 + (knobs.EncounterMixShiftPct.GetValueOrDefault("Outpatient", 0m) / 100m);
        var profMixFactor = 1 + (knobs.EncounterMixShiftPct.GetValueOrDefault("Professional", 0m) / 100m);

        // Forecast each service bucket
        var inpatientAllowed = baseline.ServiceBuckets.InpatientAllowed 
            * providerTrendFactor * utilizationFactor * riskFactor * ipMixFactor;
        
        var outpatientAllowed = baseline.ServiceBuckets.OutpatientAllowed 
            * providerTrendFactor * utilizationFactor * riskFactor * opMixFactor;
        
        var professionalAllowed = baseline.ServiceBuckets.ProfessionalAllowed 
            * providerTrendFactor * utilizationFactor * riskFactor * profMixFactor;
        
        var pharmacyAllowed = baseline.ServiceBuckets.PharmacyAllowed 
            * pharmacyTrendFactor * utilizationFactor * riskFactor;
        
        var otherAllowed = baseline.ServiceBuckets.OtherAllowed 
            * otherTrendFactor * utilizationFactor * riskFactor;

        var forecastMemberMonths = baseline.MemberMonths * membershipFactor;

        return new ServiceBucketMetrics
        {
            InpatientAllowed = inpatientAllowed,
            InpatientPMPM = forecastMemberMonths > 0 ? inpatientAllowed / forecastMemberMonths : 0m,
            
            OutpatientAllowed = outpatientAllowed,
            OutpatientPMPM = forecastMemberMonths > 0 ? outpatientAllowed / forecastMemberMonths : 0m,
            
            ProfessionalAllowed = professionalAllowed,
            ProfessionalPMPM = forecastMemberMonths > 0 ? professionalAllowed / forecastMemberMonths : 0m,
            
            PharmacyAllowed = pharmacyAllowed,
            PharmacyPMPM = forecastMemberMonths > 0 ? pharmacyAllowed / forecastMemberMonths : 0m,
            
            OtherAllowed = otherAllowed,
            OtherPMPM = forecastMemberMonths > 0 ? otherAllowed / forecastMemberMonths : 0m
        };
    }

    /// <summary>
    /// Calculate forecast utilization rates
    /// </summary>
    private UtilizationRates CalculateUtilizationForecast(
        BaselineMetrics baseline, 
        ScenarioKnobs knobs, 
        decimal forecastMemberMonths)
    {
        var utilizationFactor = 1 + (knobs.UtilizationDeltaPct / 100m);
        
        // Apply encounter mix shifts
        var edMixFactor = 1 + (knobs.EncounterMixShiftPct.GetValueOrDefault("Emergency", 0m) / 100m);
        var ipMixFactor = 1 + (knobs.EncounterMixShiftPct.GetValueOrDefault("Inpatient", 0m) / 100m);
        var opMixFactor = 1 + (knobs.EncounterMixShiftPct.GetValueOrDefault("Outpatient", 0m) / 100m);
        var officeMixFactor = 1 + (knobs.EncounterMixShiftPct.GetValueOrDefault("Office", 0m) / 100m);

        var totalEdVisits = (int)(baseline.Utilization.TotalEdVisits * utilizationFactor * edMixFactor);
        var totalIpAdmits = (int)(baseline.Utilization.TotalIpAdmits * utilizationFactor * ipMixFactor);
        var totalOpVisits = (int)(baseline.Utilization.TotalOpVisits * utilizationFactor * opMixFactor);
        var totalOfficeVisits = (int)(baseline.Utilization.TotalOfficeVisits * utilizationFactor * officeMixFactor);

        // Calculate per 1000 rates
        var memberYears = forecastMemberMonths / 12m;
        var per1000Factor = memberYears > 0 ? 1000m / memberYears : 0m;

        return new UtilizationRates
        {
            EdVisitsPer1000 = totalEdVisits * per1000Factor,
            IpAdmitsPer1000 = totalIpAdmits * per1000Factor,
            OpVisitsPer1000 = totalOpVisits * per1000Factor,
            OfficeVisitsPer1000 = totalOfficeVisits * per1000Factor,
            
            TotalEdVisits = totalEdVisits,
            TotalIpAdmits = totalIpAdmits,
            TotalOpVisits = totalOpVisits,
            TotalOfficeVisits = totalOfficeVisits
        };
    }

    /// <summary>
    /// Calculate delta drivers for waterfall chart
    /// </summary>
    private DeltaDrivers CalculateDeltaDrivers(
        BaselineMetrics baseline, 
        ScenarioKnobs knobs, 
        decimal forecastAllowed, 
        decimal forecastMemberMonths)
    {
        var baseMemberMonths = baseline.MemberMonths;
        var basePMPM = baseline.TotalPMPM;

        // Provider trend delta (PMPM impact)
        var providerTrendDelta = baseline.ProviderAllowed * (knobs.ProviderTrendPct / 100m) / baseMemberMonths;

        // Pharmacy trend delta
        var pharmacyTrendDelta = baseline.PharmacyAllowed * (knobs.PharmacyTrendPct / 100m) / baseMemberMonths;

        // Other trend delta
        var otherTrendDelta = baseline.OtherAllowed * (knobs.OtherTrendPct / 100m) / baseMemberMonths;

        // Risk delta (after trends applied)
        var trendedAllowed = baseline.TotalAllowed 
            * (1 + (knobs.ProviderTrendPct + knobs.PharmacyTrendPct + knobs.OtherTrendPct) / 300m);  // Weighted avg
        var riskDelta = trendedAllowed * (knobs.PopulationRiskDeltaPct / 100m) / baseMemberMonths;

        // Utilization delta (after trends and risk)
        var riskAdjustedAllowed = trendedAllowed * (1 + knobs.PopulationRiskDeltaPct / 100m);
        var utilizationDelta = riskAdjustedAllowed * (knobs.UtilizationDeltaPct / 100m) / baseMemberMonths;

        // Membership delta (denominator effect on PMPM)
        // When membership increases, PMPM may decrease slightly due to fixed costs spread
        var forecastPMPM = forecastMemberMonths > 0 ? forecastAllowed / forecastMemberMonths : 0m;
        var membershipImpactPMPM = forecastPMPM - (forecastAllowed / baseMemberMonths);

        // Condition-specific impacts (optional detail)
        var conditionImpacts = CalculateConditionImpacts(baseline, knobs);

        // Encounter mix impacts
        var encounterMixImpacts = CalculateEncounterMixImpacts(baseline, knobs);

        return new DeltaDrivers
        {
            ProviderTrendDelta = providerTrendDelta,
            PharmacyTrendDelta = pharmacyTrendDelta,
            OtherTrendDelta = otherTrendDelta,
            RiskDelta = riskDelta,
            UtilizationDelta = utilizationDelta,
            MembershipDelta = membershipImpactPMPM,
            ConditionImpacts = conditionImpacts,
            EncounterMixImpacts = encounterMixImpacts
        };
    }

    /// <summary>
    /// Calculate condition-specific PMPM impacts
    /// </summary>
    private Dictionary<string, decimal> CalculateConditionImpacts(
        BaselineMetrics baseline, 
        ScenarioKnobs knobs)
    {
        var impacts = new Dictionary<string, decimal>();

        if (!knobs.ConditionPrevalenceShiftPct.Any()) return impacts;

        // Estimate PMPM impact of each condition prevalence shift
        // Assumption: Each condition group has avg cost differential
        foreach (var kvp in knobs.ConditionPrevalenceShiftPct)
        {
            var conditionGroup = kvp.Key;
            var shiftPct = kvp.Value;

            // Find condition in baseline
            var baselineCondition = baseline.TopConditions
                .FirstOrDefault(c => c.ConditionGroup == conditionGroup);

            if (baselineCondition != null)
            {
                // Estimate: prevalence shift * avg risk score * base PMPM * cost multiplier
                var costMultiplier = 2.0m;  // Members with this condition cost 2x average
                var impact = (shiftPct / 100m) * baselineCondition.AvgRiskScore * baseline.TotalPMPM * costMultiplier;
                impacts[conditionGroup] = impact;
            }
        }

        return impacts;
    }

    /// <summary>
    /// Calculate encounter mix PMPM impacts
    /// </summary>
    private Dictionary<string, decimal> CalculateEncounterMixImpacts(
        BaselineMetrics baseline, 
        ScenarioKnobs knobs)
    {
        var impacts = new Dictionary<string, decimal>();

        if (!knobs.EncounterMixShiftPct.Any()) return impacts;

        // Estimate PMPM impact of encounter mix shifts
        // Assumption: Different encounter types have different avg costs
        var encounterCosts = new Dictionary<string, decimal>
        {
            ["Emergency"] = 1500m,      // Avg ED visit cost
            ["Inpatient"] = 15000m,     // Avg IP admit cost
            ["Outpatient"] = 800m,      // Avg OP visit cost
            ["Office"] = 200m           // Avg office visit cost
        };

        foreach (var kvp in knobs.EncounterMixShiftPct)
        {
            var encounterType = kvp.Key;
            var shiftPct = kvp.Value;

            if (encounterCosts.TryGetValue(encounterType, out var avgCost))
            {
                // Get baseline encounter count
                var baselineCount = encounterType switch
                {
                    "Emergency" => baseline.Utilization.TotalEdVisits,
                    "Inpatient" => baseline.Utilization.TotalIpAdmits,
                    "Outpatient" => baseline.Utilization.TotalOpVisits,
                    "Office" => baseline.Utilization.TotalOfficeVisits,
                    _ => 0
                };

                // Calculate cost impact
                var deltaCount = baselineCount * (shiftPct / 100m);
                var totalCostDelta = deltaCount * avgCost;
                var pmpmImpact = baseline.MemberMonths > 0 ? totalCostDelta / baseline.MemberMonths : 0m;

                impacts[encounterType] = pmpmImpact;
            }
        }

        return impacts;
    }

    /// <summary>
    /// Run sensitivity analysis across multiple scenarios
    /// </summary>
    public List<SensitivityResult> RunSensitivityAnalysis(
        BaselineMetrics baseline,
        ScenarioKnobs baseKnobs,
        string variableKnob,
        List<decimal> testValues)
    {
        var results = new List<SensitivityResult>();

        foreach (var testValue in testValues)
        {
            // Clone knobs and adjust variable
            var testKnobs = CloneKnobs(baseKnobs);
            ApplyTestValue(testKnobs, variableKnob, testValue);

            // Run forecast
            var result = ComputeForecast(baseline, testKnobs);

            results.Add(new SensitivityResult
            {
                VariableKnob = variableKnob,
                TestValue = testValue,
                ForecastPMPM = result.ForecastPMPM,
                DeltaPMPM = result.DeltaPMPM,
                DeltaPct = result.DeltaPct
            });
        }

        return results;
    }

    private ScenarioKnobs CloneKnobs(ScenarioKnobs source)
    {
        return new ScenarioKnobs
        {
            ProviderTrendPct = source.ProviderTrendPct,
            PharmacyTrendPct = source.PharmacyTrendPct,
            OtherTrendPct = source.OtherTrendPct,
            MembershipDeltaPct = source.MembershipDeltaPct,
            UtilizationDeltaPct = source.UtilizationDeltaPct,
            PopulationRiskDeltaPct = source.PopulationRiskDeltaPct,
            ConditionPrevalenceShiftPct = new Dictionary<string, decimal>(source.ConditionPrevalenceShiftPct),
            EncounterMixShiftPct = new Dictionary<string, decimal>(source.EncounterMixShiftPct),
            ObservationSeverityShiftPct = new Dictionary<string, decimal>(source.ObservationSeverityShiftPct)
        };
    }

    private void ApplyTestValue(ScenarioKnobs knobs, string variableKnob, decimal testValue)
    {
        switch (variableKnob)
        {
            case "ProviderTrendPct":
                knobs.ProviderTrendPct = testValue;
                break;
            case "PharmacyTrendPct":
                knobs.PharmacyTrendPct = testValue;
                break;
            case "MembershipDeltaPct":
                knobs.MembershipDeltaPct = testValue;
                break;
            case "UtilizationDeltaPct":
                knobs.UtilizationDeltaPct = testValue;
                break;
            case "PopulationRiskDeltaPct":
                knobs.PopulationRiskDeltaPct = testValue;
                break;
        }
    }
}

/// <summary>
/// Sensitivity analysis result
/// </summary>
public class SensitivityResult
{
    public string VariableKnob { get; set; } = string.Empty;
    public decimal TestValue { get; set; }
    public decimal ForecastPMPM { get; set; }
    public decimal DeltaPMPM { get; set; }
    public decimal DeltaPct { get; set; }
}
