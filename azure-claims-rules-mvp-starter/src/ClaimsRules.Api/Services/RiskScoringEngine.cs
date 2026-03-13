using System;
using System.Collections.Generic;
using System.Linq;
using ClaimsRules.Api.Models;

namespace ClaimsRules.Api.Services;

/// <summary>
/// Risk scoring engine with configurable weights for demographic, condition, encounter, and observation-based risk
/// </summary>
public class RiskScoringEngine
{
    private RiskWeights _weights;

    public RiskScoringEngine()
    {
        // Initialize with default weights
        _weights = GetDefaultWeights();
    }

    public void LoadWeights(RiskWeights weights)
    {
        _weights = weights;
    }

    /// <summary>
    /// Calculate risk score for a single member
    /// </summary>
    public MemberRiskScore CalculateMemberRiskScore(
        string patientId,
        DateTime birthDate,
        string gender,
        List<string> conditionCodes,  // ICD-10 codes
        Dictionary<string, int> encounterCounts,
        Dictionary<string, decimal> observations = null)
    {
        var score = new MemberRiskScore
        {
            PatientId = patientId
        };

        // A. Demographic risk
        var ageGenderBand = GetAgeGenderBand(birthDate, gender);
        score.AgeGenderBand = ageGenderBand;
        score.DemographicScore = _weights.AgeGenderWeights.GetValueOrDefault(ageGenderBand, 1.0m);

        // B. Condition-based risk (HCC mapping)
        var conditionGroups = conditionCodes
            .Select(icd10 => MapToHCC(icd10))
            .Where(hcc => hcc != null)
            .Distinct()
            .ToList();

        score.ActiveConditionGroups = conditionGroups!;
        score.ConditionScore = conditionGroups
            .Sum(hcc => _weights.ConditionGroupWeights.GetValueOrDefault(hcc, 0m));

        // C. Encounter-based risk
        score.EncounterCounts = encounterCounts;
        score.EncounterScore = encounterCounts
            .Sum(kvp => _weights.EncounterWeights.GetValueOrDefault(kvp.Key, 0m) * kvp.Value);

        // D. Observation severity adjustment
        score.SeverityAdjustment = CalculateSeverityAdjustment(observations);

        // Total risk score
        score.TotalRiskScore = score.DemographicScore 
                             + score.ConditionScore 
                             + score.EncounterScore 
                             + score.SeverityAdjustment;

        return score;
    }

    /// <summary>
    /// Calculate population risk index from member scores
    /// </summary>
    public PopulationRiskIndex CalculatePopulationRiskIndex(List<MemberRiskScore> memberScores)
    {
        if (!memberScores.Any())
        {
            return new PopulationRiskIndex
            {
                Index = 1.0m,
                AvgMemberRiskScore = 0m,
                PopulationAvgCalibration = 2.0m,
                TotalMembers = 0
            };
        }

        var avgScore = memberScores.Average(m => m.TotalRiskScore);
        var calibrationBase = 2.0m;  // Calibrated baseline (configurable)

        return new PopulationRiskIndex
        {
            Index = avgScore / calibrationBase,
            AvgMemberRiskScore = avgScore,
            PopulationAvgCalibration = calibrationBase,
            TotalMembers = memberScores.Count,
            TopRiskMembers = memberScores
                .OrderByDescending(m => m.TotalRiskScore)
                .Take(10)
                .ToList()
        };
    }

    /// <summary>
    /// Get age/gender band for demographic scoring
    /// </summary>
    private string GetAgeGenderBand(DateTime birthDate, string gender)
    {
        var age = DateTime.UtcNow.Year - birthDate.Year;
        if (DateTime.UtcNow < birthDate.AddYears(age)) age--;

        var genderCode = gender?.ToUpper() switch
        {
            "MALE" or "M" => "M",
            "FEMALE" or "F" => "F",
            _ => "U"
        };

        var ageBand = age switch
        {
            < 35 => "0_34",
            < 45 => "35_44",
            < 55 => "45_54",
            < 65 => "55_64",
            < 75 => "65_74",
            _ => "75+"
        };

        return $"{genderCode}_{ageBand}";
    }

    /// <summary>
    /// Map ICD-10 code to HCC group
    /// </summary>
    private string? MapToHCC(string icd10Code)
    {
        if (string.IsNullOrEmpty(icd10Code)) return null;
        
        // Normalize code (remove dots, uppercase)
        var normalized = icd10Code.Replace(".", "").ToUpper();
        
        return _weights.Icd10Mapping.GetValueOrDefault(normalized);
    }

    /// <summary>
    /// Calculate severity adjustment from observations
    /// </summary>
    private decimal CalculateSeverityAdjustment(Dictionary<string, decimal>? observations)
    {
        if (observations == null || !observations.Any()) return 0m;

        decimal adjustment = 0m;

        // A1c severity (diabetes control)
        if (observations.TryGetValue("A1c", out var a1c))
        {
            if (a1c > 9.0m)  // High A1c
                adjustment += _weights.ObservationSeverityWeights.GetValueOrDefault("A1c_High", 0.2m);
            else if (a1c > 7.0m)  // Moderate
                adjustment += _weights.ObservationSeverityWeights.GetValueOrDefault("A1c_Moderate", 0.1m);
        }

        // Blood pressure severity
        if (observations.TryGetValue("BP_Systolic", out var sbp))
        {
            if (sbp > 160m)  // Stage 2 hypertension
                adjustment += _weights.ObservationSeverityWeights.GetValueOrDefault("BP_High", 0.15m);
            else if (sbp > 140m)  // Stage 1
                adjustment += _weights.ObservationSeverityWeights.GetValueOrDefault("BP_Moderate", 0.08m);
        }

        // BMI severity
        if (observations.TryGetValue("BMI", out var bmi))
        {
            if (bmi > 35m)  // Class 2 obesity
                adjustment += _weights.ObservationSeverityWeights.GetValueOrDefault("BMI_High", 0.1m);
            else if (bmi > 30m)  // Class 1 obesity
                adjustment += _weights.ObservationSeverityWeights.GetValueOrDefault("BMI_Moderate", 0.05m);
        }

        return adjustment;
    }

    /// <summary>
    /// Get default risk weights (CMS HCC-like)
    /// </summary>
    public static RiskWeights GetDefaultWeights()
    {
        return new RiskWeights
        {
            WeightsId = "risk-weights-default-v1",
            Version = 1,
            LastModified = DateTime.UtcNow,
            ModifiedBy = "system",

            // Age/Gender demographic weights
            AgeGenderWeights = new Dictionary<string, decimal>
            {
                // Male
                ["M_0_34"] = 0.5m,
                ["M_35_44"] = 0.8m,
                ["M_45_54"] = 1.2m,
                ["M_55_64"] = 1.8m,
                ["M_65_74"] = 2.5m,
                ["M_75+"] = 3.5m,
                
                // Female
                ["F_0_34"] = 0.6m,
                ["F_35_44"] = 0.9m,
                ["F_45_54"] = 1.3m,
                ["F_55_64"] = 1.9m,
                ["F_65_74"] = 2.6m,
                ["F_75+"] = 3.8m,
                
                // Unknown
                ["U_0_34"] = 0.55m,
                ["U_35_44"] = 0.85m,
                ["U_45_54"] = 1.25m,
                ["U_55_64"] = 1.85m,
                ["U_65_74"] = 2.55m,
                ["U_75+"] = 3.65m
            },

            // HCC-like condition group weights
            ConditionGroupWeights = new Dictionary<string, decimal>
            {
                ["HCC_001_HIV"] = 1.5m,
                ["HCC_008_MetastaticCancer"] = 2.5m,
                ["HCC_009_Lung_Cancer"] = 2.0m,
                ["HCC_017_Diabetes_Complications"] = 1.3m,
                ["HCC_018_Diabetes_NoComplications"] = 0.8m,
                ["HCC_019_Diabetes_OphthalComp"] = 1.1m,
                ["HCC_085_CHF"] = 1.8m,
                ["HCC_096_COPD"] = 1.2m,
                ["HCC_106_Hypertension"] = 0.6m,
                ["HCC_107_Hypertension_Complications"] = 1.0m,
                ["HCC_110_CKD_Stage4"] = 1.7m,
                ["HCC_111_CKD_Stage5"] = 2.3m,
                ["HCC_112_Asthma"] = 0.5m,
                ["HCC_114_Depression"] = 0.4m,
                ["HCC_130_Stroke"] = 1.6m,
                ["HCC_135_CAD"] = 1.1m
            },

            // ICD-10 to HCC mapping (sample - would be much larger in production)
            Icd10Mapping = new Dictionary<string, string>
            {
                // Diabetes
                ["E1165"] = "HCC_017_Diabetes_Complications",
                ["E1169"] = "HCC_017_Diabetes_Complications",
                ["E119"] = "HCC_018_Diabetes_NoComplications",
                ["E1039"] = "HCC_019_Diabetes_OphthalComp",
                
                // CHF
                ["I509"] = "HCC_085_CHF",
                ["I5020"] = "HCC_085_CHF",
                ["I5030"] = "HCC_085_CHF",
                ["I5040"] = "HCC_085_CHF",
                
                // COPD
                ["J449"] = "HCC_096_COPD",
                ["J440"] = "HCC_096_COPD",
                ["J441"] = "HCC_096_COPD",
                
                // Hypertension
                ["I10"] = "HCC_106_Hypertension",
                ["I110"] = "HCC_107_Hypertension_Complications",
                ["I119"] = "HCC_107_Hypertension_Complications",
                
                // CKD
                ["N184"] = "HCC_110_CKD_Stage4",
                ["N185"] = "HCC_111_CKD_Stage5",
                
                // Asthma
                ["J45909"] = "HCC_112_Asthma",
                ["J4520"] = "HCC_112_Asthma",
                
                // Depression
                ["F329"] = "HCC_114_Depression",
                ["F3310"] = "HCC_114_Depression",
                
                // Stroke
                ["I6350"] = "HCC_130_Stroke",
                ["I6352"] = "HCC_130_Stroke",
                
                // CAD
                ["I2510"] = "HCC_135_CAD",
                ["I252"] = "HCC_135_CAD",
                
                // Cancer
                ["C7800"] = "HCC_008_MetastaticCancer",
                ["C3490"] = "HCC_009_Lung_Cancer",
                
                // HIV
                ["B20"] = "HCC_001_HIV"
            },

            // Encounter-based weights
            EncounterWeights = new Dictionary<string, decimal>
            {
                ["Emergency"] = 0.3m,      // Each ED visit adds 0.3
                ["Inpatient"] = 1.0m,      // Each IP admit adds 1.0
                ["Outpatient"] = 0.1m,     // Each OP visit adds 0.1
                ["Ambulatory"] = 0.05m,    // Each office visit adds 0.05
                ["Other"] = 0.02m
            },

            // Observation severity weights
            ObservationSeverityWeights = new Dictionary<string, decimal>
            {
                ["A1c_High"] = 0.2m,       // A1c > 9.0%
                ["A1c_Moderate"] = 0.1m,   // A1c 7.0-9.0%
                ["BP_High"] = 0.15m,       // SBP > 160
                ["BP_Moderate"] = 0.08m,   // SBP 140-160
                ["BMI_High"] = 0.1m,       // BMI > 35
                ["BMI_Moderate"] = 0.05m   // BMI 30-35
            },

            // Disease interaction multipliers (optional - not yet implemented)
            DiseaseInteractionMultipliers = new Dictionary<string, decimal>
            {
                ["Diabetes_CHF"] = 1.2m,           // Diabetes + CHF: 20% higher risk
                ["COPD_CHF"] = 1.15m,              // COPD + CHF: 15% higher risk
                ["CKD_Diabetes"] = 1.25m           // CKD + Diabetes: 25% higher risk
            }
        };
    }
}
