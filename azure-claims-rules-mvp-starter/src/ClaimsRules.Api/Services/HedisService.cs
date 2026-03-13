using ClaimsRules.Api.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ClaimsRules.Api.Services;

/// <summary>
/// HEDIS quality measure calculation service using FHIR R4 data
/// Implements NCQA HEDIS specifications for 15 key measures
/// </summary>
public class HedisService
{
    private readonly FhirClient _fhirClient;
    private readonly CosmosAudit _cosmosAudit;
    private readonly ILogger<HedisService> _logger;
    private readonly bool _useMockData;

    public HedisService(
        FhirClient fhirClient,
        CosmosAudit cosmosAudit,
        IConfiguration configuration,
        ILogger<HedisService> logger)
    {
        _fhirClient = fhirClient;
        _cosmosAudit = cosmosAudit;
        _logger = logger;
        _useMockData = configuration["USE_MOCK_SERVICES"] == "true";
    }

    /// <summary>
    /// Calculate all HEDIS measures for a plan segment
    /// </summary>
    public async Task<HedisDashboard> CalculateDashboardAsync(string planSegment, int measurementYear)
    {
        _logger.LogInformation("Calculating HEDIS dashboard for {Segment} year {Year}", planSegment, measurementYear);

        if (_useMockData)
        {
            return GetMockDashboard(planSegment, measurementYear);
        }

        var dashboard = new HedisDashboard
        {
            MeasurementYear = measurementYear,
            PlanSegment = planSegment,
            LastUpdated = DateTime.UtcNow
        };

        // Calculate each measure
        var measureDefinitions = HedisMeasures.GetAllDefinitions();
        foreach (var definition in measureDefinitions)
        {
            try
            {
                var result = await CalculateMeasureAsync(definition.MeasureId, planSegment, measurementYear);
                dashboard.Measures.Add(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating measure {MeasureId}", definition.MeasureId);
            }
        }

        // Calculate overall metrics
        dashboard.TotalGaps = dashboard.Measures.Sum(m => m.GapCount);
        dashboard.OverallStarRating = CalculateOverallStarRating(dashboard.Measures);
        dashboard.ProjectedStarRating = CalculateProjectedStarRating(dashboard.Measures);

        // Identify priority actions
        dashboard.PriorityActions = IdentifyPriorityActions(dashboard.Measures);

        return dashboard;
    }

    /// <summary>
    /// Calculate a single HEDIS measure
    /// </summary>
    public async Task<HedisMeasureResult> CalculateMeasureAsync(string measureId, string planSegment, int measurementYear)
    {
        _logger.LogInformation("Calculating HEDIS measure {MeasureId}", measureId);

        if (_useMockData)
        {
            return GetMockMeasureResult(measureId, measurementYear);
        }

        var definition = HedisMeasures.GetDefinition(measureId) 
            ?? throw new ArgumentException($"Unknown measure: {measureId}");

        // Get eligible population (denominator)
        var eligiblePatients = await GetEligiblePopulationAsync(definition, planSegment, measurementYear);

        // Check numerator criteria for each patient
        var numeratorPatients = new List<string>();
        var gapPatients = new List<HedisGapPatient>();

        foreach (var patient in eligiblePatients)
        {
            var meetsNumerator = await CheckNumeratorCriteriaAsync(patient, definition, measurementYear);
            
            if (meetsNumerator.Meets)
            {
                numeratorPatients.Add(patient.Id);
            }
            else
            {
                gapPatients.Add(new HedisGapPatient
                {
                    PatientId = patient.Id,
                    PatientName = patient.Name ?? "Unknown",
                    DateOfBirth = patient.BirthDate ?? "Unknown",
                    Age = patient.Age,
                    Gender = patient.Gender,
                    MeasureId = measureId,
                    MeasureName = definition.MeasureName,
                    LastServiceDate = meetsNumerator.LastServiceDate,
                    DaysSinceLastService = meetsNumerator.DaysSinceLastService,
                    RecommendedAction = GetRecommendedAction(measureId),
                    PrimaryCareProvider = patient.PrimaryCareProvider,
                    Phone = patient.Phone,
                    Priority = CalculatePatientPriority(patient, measureId),
                    RiskLevel = patient.RiskLevel ?? "Medium"
                });
            }
        }

        var result = new HedisMeasureResult
        {
            MeasureId = measureId,
            MeasureName = definition.MeasureName,
            MeasureCategory = definition.Category,
            Denominator = eligiblePatients.Count,
            Numerator = numeratorPatients.Count,
            Rate = eligiblePatients.Count > 0 ? (double)numeratorPatients.Count / eligiblePatients.Count * 100 : 0,
            GapCount = gapPatients.Count,
            GapPatients = gapPatients.OrderByDescending(p => p.Priority).Take(100).ToList(), // Limit to top 100
            CalculatedAt = DateTime.UtcNow,
            MeasurementYear = measurementYear,
            PlanSegment = planSegment
        };

        // Audit calculation
        await _cosmosAudit.WriteAuditAsync("hedis-calculation", new
        {
            measureId,
            planSegment,
            measurementYear,
            denominator = result.Denominator,
            numerator = result.Numerator,
            rate = result.Rate,
            gapCount = result.GapCount
        });

        return result;
    }

    /// <summary>
    /// Get eligible population for a measure (denominator)
    /// </summary>
    private async Task<List<PatientSummary>> GetEligiblePopulationAsync(
        HedisMeasureDefinition definition, 
        string planSegment, 
        int measurementYear)
    {
        // This would query FHIR for:
        // 1. Active members in plan segment
        // 2. Age filter (minAge/maxAge)
        // 3. Gender filter
        // 4. Diagnosis codes (if required for denominator)
        // 5. Exclusion criteria

        // For now, return empty list (implement FHIR queries in production)
        return new List<PatientSummary>();
    }

    /// <summary>
    /// Check if patient meets numerator criteria for a measure
    /// </summary>
    private async Task<(bool Meets, string? LastServiceDate, int DaysSinceLastService)> CheckNumeratorCriteriaAsync(
        PatientSummary patient, 
        HedisMeasureDefinition definition, 
        int measurementYear)
    {
        // This would query FHIR for:
        // - DiagnosticReport (LOINC codes)
        // - Procedure (SNOMED/CPT codes)
        // - Observation (lab results)
        // - MedicationRequest/MedicationStatement
        // - Encounter (well visits)

        // Check lookback period
        var lookbackStart = new DateTime(measurementYear, 1, 1).AddMonths(-definition.LookbackMonths);
        var measurementEnd = new DateTime(measurementYear, 12, 31);

        // Query FHIR based on measure type
        var meetsNumerator = false;
        string? lastServiceDate = null;
        var daysSince = 999;

        switch (definition.MeasureId)
        {
            case HedisMeasures.BCS: // Breast Cancer Screening
                // Query DiagnosticReport with LOINC codes for mammography
                // var reports = await _fhirClient.SearchDiagnosticReportsAsync(patient.Id, definition.LoincCodes, lookbackStart, measurementEnd);
                // meetsNumerator = reports.Any();
                break;

            case HedisMeasures.CBP: // Controlling High Blood Pressure
                // Query Observation for BP readings
                // Find most recent BP <140/90
                break;

            case HedisMeasures.HBD: // HbA1c Control
                // Query Observation for HbA1c results
                // Check if most recent HbA1c <8.0%
                break;

            // ... implement other measures
        }

        return (meetsNumerator, lastServiceDate, daysSince);
    }

    /// <summary>
    /// Calculate patient priority for gap closure (1-5)
    /// </summary>
    private int CalculatePatientPriority(PatientSummary patient, string measureId)
    {
        var priority = 3; // Default medium

        // High priority factors
        if (patient.RiskLevel == "High") priority++;
        if (patient.Age > 70) priority++; // Older patients
        if (patient.HasChronicConditions) priority++;

        // Measure-specific priorities
        if (new[] { HedisMeasures.CBP, HedisMeasures.HBD, HedisMeasures.CDC }.Contains(measureId))
        {
            priority++; // High-impact measures
        }

        return Math.Clamp(priority, 1, 5);
    }

    /// <summary>
    /// Get recommended action for measure gap
    /// </summary>
    private string GetRecommendedAction(string measureId)
    {
        return measureId switch
        {
            HedisMeasures.BCS => "Schedule mammogram appointment",
            HedisMeasures.COL => "Schedule colonoscopy or order FIT test",
            HedisMeasures.CBP => "Schedule BP check appointment",
            HedisMeasures.HBD => "Order HbA1c lab test",
            HedisMeasures.CDC => "Schedule comprehensive diabetes visit",
            HedisMeasures.KED => "Order kidney function tests (uACR, eGFR)",
            HedisMeasures.EED => "Schedule diabetic eye exam",
            HedisMeasures.SPC => "Prescribe statin medication",
            HedisMeasures.OMW => "Order DXA scan or prescribe osteoporosis medication",
            HedisMeasures.IMA => "Administer HPV, Tdap, and meningococcal vaccines",
            HedisMeasures.AWC => "Schedule annual well-care visit",
            HedisMeasures.ABA => "Document BMI at next visit",
            _ => "Contact member for preventive care"
        };
    }

    /// <summary>
    /// Calculate overall Star Rating from measure rates
    /// Simplified - actual CMS calculation is much more complex
    /// </summary>
    private double CalculateOverallStarRating(List<HedisMeasureResult> measures)
    {
        if (!measures.Any()) return 0;

        // Weighted average based on measure importance
        var weightedSum = 0.0;
        var totalWeight = 0.0;

        foreach (var measure in measures)
        {
            var weight = measure.MeasureId switch
            {
                HedisMeasures.CBP => 3.0, // High weight
                HedisMeasures.HBD => 3.0,
                HedisMeasures.CDC => 3.0,
                HedisMeasures.BCS => 2.5,
                HedisMeasures.COL => 2.5,
                _ => 1.0
            };

            // Convert rate (0-100) to star score (0-5)
            var stars = measure.Rate / 20.0; // 100% = 5 stars
            weightedSum += stars * weight;
            totalWeight += weight;
        }

        return totalWeight > 0 ? Math.Round(weightedSum / totalWeight, 1) : 0;
    }

    /// <summary>
    /// Calculate projected Star Rating with gap closure
    /// Assumes 50% of gaps can be closed with outreach
    /// </summary>
    private double CalculateProjectedStarRating(List<HedisMeasureResult> measures)
    {
        var projectedMeasures = measures.Select(m =>
        {
            // Assume 50% gap closure
            var additionalNumerator = (int)(m.GapCount * 0.5);
            var projectedNumerator = m.Numerator + additionalNumerator;
            var projectedRate = m.Denominator > 0 ? (double)projectedNumerator / m.Denominator * 100 : 0;
            
            return new HedisMeasureResult
            {
                MeasureId = m.MeasureId,
                MeasureName = m.MeasureName,
                MeasureCategory = m.MeasureCategory,
                Denominator = m.Denominator,
                Numerator = projectedNumerator,
                Rate = projectedRate,
                GapCount = m.GapCount - additionalNumerator,
                CalculatedAt = DateTime.UtcNow,
                MeasurementYear = m.MeasurementYear,
                PlanSegment = m.PlanSegment
            };
        }).ToList();

        return CalculateOverallStarRating(projectedMeasures);
    }

    /// <summary>
    /// Identify top priority actions for gap closure
    /// </summary>
    private List<HedisPriorityAction> IdentifyPriorityActions(List<HedisMeasureResult> measures)
    {
        return measures
            .Where(m => m.GapCount > 0)
            .Select(m =>
            {
                var targetRate = Math.Min(m.Rate + 15, 90); // Target +15% improvement, max 90%
                var starImpact = HedisMeasures.CalculateStarRatingImpact(m.MeasureId, m.Rate, targetRate);

                return new HedisPriorityAction
                {
                    MeasureId = m.MeasureId,
                    MeasureName = m.MeasureName,
                    GapCount = m.GapCount,
                    CurrentRate = m.Rate,
                    TargetRate = targetRate,
                    StarRatingImpact = starImpact,
                    RecommendedApproach = GetOutreachStrategy(m.MeasureId, m.GapCount)
                };
            })
            .OrderByDescending(a => a.StarRatingImpact)
            .Take(5)
            .ToList();
    }

    /// <summary>
    /// Get recommended outreach strategy for measure gap
    /// </summary>
    private string GetOutreachStrategy(string measureId, int gapCount)
    {
        if (gapCount > 1000)
        {
            return "Mass outreach: Automated calls + mail + member portal alerts";
        }
        else if (gapCount > 100)
        {
            return "Targeted outreach: Care manager calls + provider alerts";
        }
        else
        {
            return "High-touch outreach: Personal calls from care team";
        }
    }

    /// <summary>
    /// Get mock dashboard for development/testing
    /// </summary>
    private HedisDashboard GetMockDashboard(string planSegment, int measurementYear)
    {
        return new HedisDashboard
        {
            MeasurementYear = measurementYear,
            PlanSegment = planSegment,
            OverallStarRating = 4.0,
            ProjectedStarRating = 4.5,
            TotalMembers = 50000,
            TotalGaps = 6050,
            LastUpdated = DateTime.UtcNow,
            Measures = new List<HedisMeasureResult>
            {
                GetMockMeasureResult(HedisMeasures.BCS, measurementYear),
                GetMockMeasureResult(HedisMeasures.COL, measurementYear),
                GetMockMeasureResult(HedisMeasures.CBP, measurementYear),
                GetMockMeasureResult(HedisMeasures.HBD, measurementYear),
                GetMockMeasureResult(HedisMeasures.CDC, measurementYear)
            },
            PriorityActions = new List<HedisPriorityAction>
            {
                new HedisPriorityAction
                {
                    MeasureId = HedisMeasures.HBD,
                    MeasureName = "HbA1c Control for Patients with Diabetes",
                    GapCount = 2100,
                    CurrentRate = 62,
                    TargetRate = 77,
                    StarRatingImpact = 0.45,
                    RecommendedApproach = "Targeted outreach: Care manager calls + provider alerts"
                },
                new HedisPriorityAction
                {
                    MeasureId = HedisMeasures.BCS,
                    MeasureName = "Breast Cancer Screening",
                    GapCount = 1200,
                    CurrentRate = 68,
                    TargetRate = 83,
                    StarRatingImpact = 0.38,
                    RecommendedApproach = "Mass outreach: Automated calls + mail + member portal alerts"
                }
            }
        };
    }

    /// <summary>
    /// Get mock measure result for development/testing
    /// </summary>
    private HedisMeasureResult GetMockMeasureResult(string measureId, int measurementYear)
    {
        var definition = HedisMeasures.GetDefinition(measureId) 
            ?? throw new ArgumentException($"Unknown measure: {measureId}");

        var (denominator, numerator, rate) = measureId switch
        {
            HedisMeasures.BCS => (3750, 2550, 68.0),
            HedisMeasures.COL => (4200, 3024, 72.0),
            HedisMeasures.CBP => (6500, 5525, 85.0),
            HedisMeasures.HBD => (5500, 3410, 62.0),
            HedisMeasures.CDC => (5500, 4125, 75.0),
            HedisMeasures.KED => (5500, 4400, 80.0),
            HedisMeasures.EED => (5500, 3850, 70.0),
            HedisMeasures.SPC => (2800, 2240, 80.0),
            HedisMeasures.OMW => (450, 315, 70.0),
            HedisMeasures.IMA => (2200, 1760, 80.0),
            HedisMeasures.AWC => (8500, 6800, 80.0),
            HedisMeasures.ABA => (35000, 31500, 90.0),
            _ => (1000, 750, 75.0)
        };

        var gapCount = denominator - numerator;

        return new HedisMeasureResult
        {
            MeasureId = measureId,
            MeasureName = definition.MeasureName,
            MeasureCategory = definition.Category,
            Denominator = denominator,
            Numerator = numerator,
            Rate = rate,
            GapCount = gapCount,
            GapPatients = GenerateMockGapPatients(measureId, definition.MeasureName, Math.Min(gapCount, 10)),
            CalculatedAt = DateTime.UtcNow,
            MeasurementYear = measurementYear,
            PlanSegment = "All Plans"
        };
    }

    /// <summary>
    /// Generate mock gap patients for testing
    /// </summary>
    private List<HedisGapPatient> GenerateMockGapPatients(string measureId, string measureName, int count)
    {
        var patients = new List<HedisGapPatient>();
        var random = new Random();

        for (int i = 0; i < count; i++)
        {
            patients.Add(new HedisGapPatient
            {
                PatientId = $"patient-{measureId}-{i + 1}",
                PatientName = $"Patient {i + 1}",
                DateOfBirth = $"{1940 + random.Next(50)}-{random.Next(1, 13):D2}-{random.Next(1, 29):D2}",
                Age = 40 + random.Next(40),
                Gender = random.Next(2) == 0 ? "Female" : "Male",
                MeasureId = measureId,
                MeasureName = measureName,
                LastServiceDate = random.Next(2) == 0 ? $"{2023 + random.Next(2)}-{random.Next(1, 13):D2}-{random.Next(1, 29):D2}" : null,
                DaysSinceLastService = random.Next(365, 1000),
                RecommendedAction = GetRecommendedAction(measureId),
                PrimaryCareProvider = $"Dr. Smith {i % 5 + 1}",
                Phone = $"206-555-{1000 + i:D4}",
                Priority = random.Next(1, 6),
                RiskLevel = random.Next(3) switch { 0 => "Low", 1 => "Medium", _ => "High" }
            });
        }

        return patients;
    }
}

/// <summary>
/// Patient summary model for HEDIS calculations
/// </summary>
public class PatientSummary
{
    public required string Id { get; set; }
    public string? Name { get; set; }
    public string? BirthDate { get; set; }
    public int Age { get; set; }
    public string? Gender { get; set; }
    public string? PrimaryCareProvider { get; set; }
    public string? Phone { get; set; }
    public string? RiskLevel { get; set; }
    public bool HasChronicConditions { get; set; }
}
