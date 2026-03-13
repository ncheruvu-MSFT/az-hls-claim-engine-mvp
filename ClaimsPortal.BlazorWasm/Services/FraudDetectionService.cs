using ClaimsPortal.BlazorWasm.Models;

namespace ClaimsPortal.BlazorWasm.Services;

/// <summary>
/// AI-powered fraud detection service
/// Uses pattern matching, statistical analysis, and anomaly detection
/// </summary>
public class FraudDetectionService
{
    private readonly ClaimsApiService _apiService;
    private readonly Random _random = new();

    public FraudDetectionService(ClaimsApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<FraudDetectionResult> AnalyzeClaimAsync(FraudDetectionRequest request)
    {
        var result = new FraudDetectionResult
        {
            ClaimId = request.ClaimId,
            PatientId = request.PatientId,
            ProviderId = request.ProviderId,
            ServiceDate = request.ServiceDate,
            ClaimAmount = request.BilledAmount,
            ClaimSource = request.ClaimSource,
            ServiceLocation = request.ServiceLocation
        };

        // Simulate historical data analysis
        var historicalClaims = await GetHistoricalClaimsAsync(request.PatientId, request.ProviderId);
        result.SimilarClaimsCount = historicalClaims.Count;
        result.AverageClaimAmount = historicalClaims.Any() ? historicalClaims.Average() : 0;
        result.StandardDeviation = CalculateStandardDeviation(historicalClaims, result.AverageClaimAmount);

        // Run fraud detection rules
        await CheckClaimSourceAnomalies(request, result);
        await CheckGeographicAnomalies(request, result);
        await CheckAmountAnomalies(request, result, historicalClaims);
        await CheckTimingAnomalies(request, result);
        await CheckProviderPatterns(request, result);
        await CheckMemberBehavior(request, result);

        // Calculate overall risk score
        result.RiskScore = CalculateRiskScore(result);
        result.RiskLevel = DetermineRiskLevel(result.RiskScore);
        result.RequiresReview = result.RiskScore >= 50;
        result.AutoDenied = result.RiskScore >= 90;

        // Generate AI explanation
        result.AiExplanation = GenerateAiExplanation(result);
        result.RecommendedActions = GenerateRecommendedActions(result);

        return result;
    }

    private async Task<List<decimal>> GetHistoricalClaimsAsync(string patientId, string providerId)
    {
        // Simulate historical data - in production, query from Cosmos DB
        await Task.Delay(100);
        
        return new List<decimal> { 250, 300, 275, 320, 290, 310, 280 };
    }

    private async Task CheckClaimSourceAnomalies(FraudDetectionRequest request, FraudDetectionResult result)
    {
        await Task.Delay(50);
        
        // Check for unusual claim sources
        if (request.ClaimSource.Equals("Fax", StringComparison.OrdinalIgnoreCase) && request.BilledAmount > 5000)
        {
            result.FraudFlags.Add(new FraudFlag
            {
                FlagType = "UnusualClaimSource",
                Description = "High-value claim submitted via fax instead of EDI",
                Severity = "Medium",
                ContributionToRiskScore = 15
            });
            
            result.Anomalies.Add(new AnomalyDetection
            {
                AnomalyType = "SubmissionChannel",
                Description = "Large claims typically submitted via EDI, not fax",
                Confidence = 0.75m,
                BaselineValue = "EDI for claims > $5000",
                ActualValue = "Fax",
                DeviationPercentage = 100
            });
        }
        
        // Check for manual portal submissions of complex procedures
        if (request.ClaimSource == "Portal" && request.ServiceCode.StartsWith("99"))
        {
            var complexProcedures = new[] { "99291", "99292", "99223" };
            if (complexProcedures.Contains(request.ServiceCode))
            {
                result.FraudFlags.Add(new FraudFlag
                {
                    FlagType = "ManualComplexProcedure",
                    Description = "Complex procedure code submitted manually through portal",
                    Severity = "Low",
                    ContributionToRiskScore = 10
                });
            }
        }
    }

    private async Task CheckGeographicAnomalies(FraudDetectionRequest request, FraudDetectionResult result)
    {
        await Task.Delay(50);
        
        // Simulate geolocation data
        result.ServiceGeoLocation = new GeoLocation
        {
            Latitude = 34.0522m,
            Longitude = -118.2437m,
            City = "Los Angeles",
            State = "CA",
            ZipCode = "90001"
        };
        
        result.MemberLastLoginLocation = new GeoLocation
        {
            Latitude = 40.7128m,
            Longitude = -74.0060m,
            City = "New York",
            State = "NY",
            ZipCode = "10001"
        };
        
        result.MemberLastLogin = DateTime.UtcNow.AddDays(-1);
        
        var distance = result.ServiceGeoLocation.DistanceFrom(result.MemberLastLoginLocation);
        
        if (distance > 500) // More than 500 miles
        {
            result.FraudFlags.Add(new FraudFlag
            {
                FlagType = "GeographicAnomaly",
                Description = $"Service location is {distance:N0} miles from member's last known location",
                Severity = "High",
                ContributionToRiskScore = 25
            });
            
            result.Anomalies.Add(new AnomalyDetection
            {
                AnomalyType = "Geographic",
                Description = "Service received far from member's typical location",
                Confidence = 0.85m,
                BaselineValue = "Within 50 miles of member location",
                ActualValue = $"{distance:N0} miles away",
                DeviationPercentage = (distance / 50) * 100
            });
        }
        
        // Check for impossible travel
        if (result.MemberLastLogin.HasValue)
        {
            var hoursSinceLastLogin = (DateTime.UtcNow - result.MemberLastLogin.Value).TotalHours;
            var requiredTravelTime = (double)distance / 60; // Assume 60 mph average
            
            if (hoursSinceLastLogin < requiredTravelTime)
            {
                result.FraudFlags.Add(new FraudFlag
                {
                    FlagType = "ImpossibleTravel",
                    Description = $"Member logged in {hoursSinceLastLogin:N1} hours ago, but service is {distance:N0} miles away (requires {requiredTravelTime:N1} hours travel)",
                    Severity = "Critical",
                    ContributionToRiskScore = 40
                });
            }
        }
    }

    private async Task CheckAmountAnomalies(FraudDetectionRequest request, FraudDetectionResult result, List<decimal> historicalClaims)
    {
        await Task.Delay(50);
        
        if (!historicalClaims.Any()) return;
        
        var avgAmount = result.AverageClaimAmount;
        var stdDev = result.StandardDeviation;
        
        // Check if claim is significantly higher than historical average
        if (request.BilledAmount > avgAmount + (2 * stdDev))
        {
            var deviationPct = ((request.BilledAmount - avgAmount) / avgAmount) * 100;
            
            result.FraudFlags.Add(new FraudFlag
            {
                FlagType = "AmountAnomaly",
                Description = $"Claim amount is {deviationPct:N0}% higher than patient's average",
                Severity = deviationPct > 200 ? "High" : "Medium",
                ContributionToRiskScore = deviationPct > 200 ? 30 : 15
            });
            
            result.Anomalies.Add(new AnomalyDetection
            {
                AnomalyType = "ClaimAmount",
                Description = "Claim amount significantly exceeds historical pattern",
                Confidence = 0.80m,
                BaselineValue = $"${avgAmount:N2} ± ${stdDev:N2}",
                ActualValue = $"${request.BilledAmount:N2}",
                DeviationPercentage = deviationPct
            });
        }
        
        // Check for rounded amounts (potential fraud indicator)
        if (request.BilledAmount % 100 == 0 && request.BilledAmount >= 1000)
        {
            result.FraudFlags.Add(new FraudFlag
            {
                FlagType = "RoundedAmount",
                Description = "Claim amount is an exact round number, which is statistically unusual",
                Severity = "Low",
                ContributionToRiskScore = 5
            });
        }
    }

    private async Task CheckTimingAnomalies(FraudDetectionRequest request, FraudDetectionResult result)
    {
        await Task.Delay(50);
        
        // Check for weekend/holiday services
        if (request.ServiceDate.DayOfWeek == DayOfWeek.Saturday || request.ServiceDate.DayOfWeek == DayOfWeek.Sunday)
        {
            if (!request.ServiceCode.StartsWith("99281")) // Not emergency services
            {
                result.FraudFlags.Add(new FraudFlag
                {
                    FlagType = "WeekendService",
                    Description = "Non-emergency service performed on weekend",
                    Severity = "Low",
                    ContributionToRiskScore = 8
                });
            }
        }
        
        // Check for multiple claims on same day
        result.SimilarClaimIds.Add("CLM-20260105-001");
        result.SimilarClaimIds.Add("CLM-20260105-002");
        
        if (result.SimilarClaimIds.Count > 3)
        {
            result.FraudFlags.Add(new FraudFlag
            {
                FlagType = "MultipleClaimsSameDay",
                Description = $"Member has {result.SimilarClaimIds.Count} claims on the same day",
                Severity = "Medium",
                ContributionToRiskScore = 15
            });
            
            result.Anomalies.Add(new AnomalyDetection
            {
                AnomalyType = "ClaimFrequency",
                Description = "Unusually high number of claims on single day",
                Confidence = 0.70m,
                BaselineValue = "1-2 claims per day",
                ActualValue = $"{result.SimilarClaimIds.Count} claims",
                DeviationPercentage = (result.SimilarClaimIds.Count / 2m) * 100
            });
        }
    }

    private async Task CheckProviderPatterns(FraudDetectionRequest request, FraudDetectionResult result)
    {
        await Task.Delay(50);
        
        // Simulate provider risk profile check
        var knownFraudProviders = new[] { "PROV999", "PROV666" };
        
        if (knownFraudProviders.Contains(request.ProviderId))
        {
            result.FraudFlags.Add(new FraudFlag
            {
                FlagType = "HighRiskProvider",
                Description = "Provider has been flagged for previous suspicious activity",
                Severity = "Critical",
                ContributionToRiskScore = 45
            });
        }
        
        // Check for provider billing patterns
        if (request.BilledAmount > 10000 && request.ServiceCode.StartsWith("99"))
        {
            result.FraudFlags.Add(new FraudFlag
            {
                FlagType = "UnusualProviderBilling",
                Description = "Provider billing high amounts for routine procedure codes",
                Severity = "High",
                ContributionToRiskScore = 20
            });
        }
    }

    private async Task CheckMemberBehavior(FraudDetectionRequest request, FraudDetectionResult result)
    {
        await Task.Delay(50);
        
        // Check for member identity verification
        if (!result.MemberLastLogin.HasValue || (DateTime.UtcNow - result.MemberLastLogin.Value).TotalDays > 180)
        {
            result.FraudFlags.Add(new FraudFlag
            {
                FlagType = "MemberInactivity",
                Description = "Member has not logged in for over 180 days",
                Severity = "Medium",
                ContributionToRiskScore = 12
            });
        }
    }

    private decimal CalculateRiskScore(FraudDetectionResult result)
    {
        var totalScore = result.FraudFlags.Sum(f => f.ContributionToRiskScore);
        return Math.Min(totalScore, 100); // Cap at 100
    }

    private string DetermineRiskLevel(decimal riskScore)
    {
        return riskScore switch
        {
            >= 90 => "Critical",
            >= 70 => "High",
            >= 40 => "Medium",
            _ => "Low"
        };
    }

    private decimal CalculateStandardDeviation(List<decimal> values, decimal mean)
    {
        if (values.Count < 2) return 0;
        
        var variance = values.Sum(v => (v - mean) * (v - mean)) / values.Count;
        return (decimal)Math.Sqrt((double)variance);
    }

    private string GenerateAiExplanation(FraudDetectionResult result)
    {
        var criticalFlags = result.FraudFlags.Count(f => f.Severity == "Critical");
        var highFlags = result.FraudFlags.Count(f => f.Severity == "High");
        
        if (result.RiskScore >= 90)
        {
            return $"⚠️ CRITICAL RISK: This claim exhibits {criticalFlags + highFlags} severe fraud indicators. " +
                   $"AI analysis detected {result.Anomalies.Count} significant anomalies including impossible travel patterns, " +
                   $"high-risk provider involvement, or statistical outliers. Automatic denial recommended pending investigation.";
        }
        
        if (result.RiskScore >= 70)
        {
            return $"🔴 HIGH RISK: AI models identified {result.FraudFlags.Count} fraud indicators. " +
                   $"The claim deviates significantly from expected patterns in {result.Anomalies.Count} areas. " +
                   $"Manual review by fraud investigation team is required before processing.";
        }
        
        if (result.RiskScore >= 40)
        {
            return $"🟡 MEDIUM RISK: {result.FraudFlags.Count} potential concerns detected through AI analysis. " +
                   $"While not severe, the claim shows unusual patterns that warrant closer examination. " +
                   $"Consider secondary review or request additional documentation.";
        }
        
        return $"✅ LOW RISK: Claim appears legitimate based on AI pattern analysis. " +
               $"{result.FraudFlags.Count} minor flags detected, but all within normal variance. " +
               $"Proceed with standard adjudication workflow.";
    }

    private List<string> GenerateRecommendedActions(FraudDetectionResult result)
    {
        var actions = new List<string>();
        
        if (result.RiskScore >= 90)
        {
            actions.Add("Place claim on hold and escalate to Special Investigations Unit (SIU)");
            actions.Add("Request member identity verification through photo ID and video call");
            actions.Add("Contact provider to verify service was actually performed");
            actions.Add("Review all recent claims from this member and provider");
            actions.Add("Consider temporary suspension of member benefits pending investigation");
        }
        else if (result.RiskScore >= 70)
        {
            actions.Add("Flag for manual review by experienced claims adjuster");
            actions.Add("Request additional supporting documentation (medical records, EOB)");
            actions.Add("Verify member and provider information through external databases");
            actions.Add("Contact member to confirm service details");
        }
        else if (result.RiskScore >= 40)
        {
            actions.Add("Perform secondary audit of claim details");
            actions.Add("Review provider's recent billing patterns");
            actions.Add("Send automated verification request to member");
        }
        else
        {
            actions.Add("Process through standard adjudication workflow");
            actions.Add("Monitor for patterns if similar flags appear in future claims");
        }
        
        // Add specific actions based on flag types
        if (result.FraudFlags.Any(f => f.FlagType == "ImpossibleTravel"))
        {
            actions.Add("⚠️ PRIORITY: Verify member's physical location on service date");
        }
        
        if (result.FraudFlags.Any(f => f.FlagType == "HighRiskProvider"))
        {
            actions.Add("⚠️ PRIORITY: Cross-reference with provider watchlist database");
        }
        
        return actions;
    }
}
