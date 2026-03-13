namespace ClaimsPortal.BlazorWasm.Models;

/// <summary>
/// Accumulator tracking for member benefit utilization
/// Aligned with FHIR Coverage and CoverageEligibilityResponse resources
/// </summary>
public class MemberAccumulator
{
    public string MemberId { get; set; } = string.Empty;
    public string PlanId { get; set; } = string.Empty;
    public DateTime EnrollmentStartDate { get; set; }
    public DateTime EnrollmentEndDate { get; set; }
    public string AccumulatorPeriod { get; set; } = "Calendar Year"; // Calendar Year, Plan Year, Lifetime
    
    // Deductible accumulators
    public decimal IndividualDeductibleMet { get; set; }
    public decimal IndividualDeductibleRemaining { get; set; }
    public decimal FamilyDeductibleMet { get; set; }
    public decimal FamilyDeductibleRemaining { get; set; }
    
    // Out-of-Pocket accumulators
    public decimal IndividualOopMet { get; set; }
    public decimal IndividualOopRemaining { get; set; }
    public decimal FamilyOopMet { get; set; }
    public decimal FamilyOopRemaining { get; set; }
    
    // Service-specific accumulators
    public int OfficeVisitsUsed { get; set; }
    public int OfficeVisitsRemaining { get; set; }
    public decimal PrescriptionSpend { get; set; }
    public int HospitalDaysUsed { get; set; }
    public int HospitalDaysRemaining { get; set; }
    
    // Time-based tracking
    public DateTime LastClaimDate { get; set; }
    public DateTime LastUpdated { get; set; }
    public int DaysInPeriod { get; set; }
    public int DaysRemaining { get; set; }
    
    /// <summary>
    /// Maps to FHIR CoverageEligibilityResponse.insurance.item.benefit structure
    /// </summary>
    public string ToFhirBenefitJson()
    {
        return $@"{{
  ""insurance"": [{{
    ""coverage"": {{ ""reference"": ""Coverage/{PlanId}"" }},
    ""item"": [
      {{
        ""category"": {{ ""coding"": [{{ ""code"": ""30"", ""display"": ""Health Benefit Plan Coverage"" }}] }},
        ""benefit"": [
          {{
            ""type"": {{ ""coding"": [{{ ""code"": ""deductible"" }}] }},
            ""usedMoney"": {{ ""value"": {IndividualDeductibleMet}, ""currency"": ""USD"" }},
            ""allowedMoney"": {{ ""value"": {IndividualDeductibleMet + IndividualDeductibleRemaining}, ""currency"": ""USD"" }}
          }},
          {{
            ""type"": {{ ""coding"": [{{ ""code"": ""oopmax"" }}] }},
            ""usedMoney"": {{ ""value"": {IndividualOopMet}, ""currency"": ""USD"" }},
            ""allowedMoney"": {{ ""value"": {IndividualOopMet + IndividualOopRemaining}, ""currency"": ""USD"" }}
          }}
        ]
      }}
    ]
  }}]
}}";
    }
}

/// <summary>
/// Fraud detection result with AI-powered anomaly scoring
/// </summary>
public class FraudDetectionResult
{
    public string ClaimId { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string ProviderId { get; set; } = string.Empty;
    public DateTime ServiceDate { get; set; }
    public decimal ClaimAmount { get; set; }
    
    // Overall risk assessment
    public decimal RiskScore { get; set; } // 0-100
    public string RiskLevel { get; set; } = "Low"; // Low, Medium, High, Critical
    public bool RequiresReview { get; set; }
    public bool AutoDenied { get; set; }
    
    // Fraud indicators
    public List<FraudFlag> FraudFlags { get; set; } = new();
    public List<AnomalyDetection> Anomalies { get; set; } = new();
    
    // Supporting evidence
    public string ClaimSource { get; set; } = string.Empty; // EDI, Portal, Mobile, Fax
    public DateTime? MemberLastLogin { get; set; }
    public string ServiceLocation { get; set; } = string.Empty;
    public GeoLocation? ServiceGeoLocation { get; set; }
    public GeoLocation? MemberLastLoginLocation { get; set; }
    
    // Pattern analysis
    public int SimilarClaimsCount { get; set; }
    public List<string> SimilarClaimIds { get; set; } = new();
    public decimal AverageClaimAmount { get; set; }
    public decimal StandardDeviation { get; set; }
    
    // AI insights
    public string AiExplanation { get; set; } = string.Empty;
    public List<string> RecommendedActions { get; set; } = new();
}

public class FraudFlag
{
    public string FlagType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = "Low"; // Low, Medium, High, Critical
    public decimal ContributionToRiskScore { get; set; }
}

public class AnomalyDetection
{
    public string AnomalyType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Confidence { get; set; } // 0-1
    public string BaselineValue { get; set; } = string.Empty;
    public string ActualValue { get; set; } = string.Empty;
    public decimal DeviationPercentage { get; set; }
}

public class GeoLocation
{
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    
    public decimal DistanceFrom(GeoLocation other)
    {
        // Haversine formula for distance calculation
        var R = 3959; // Earth radius in miles
        var dLat = ToRadians(other.Latitude - Latitude);
        var dLon = ToRadians(other.Longitude - Longitude);
        
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(Latitude)) * Math.Cos(ToRadians(other.Latitude)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return (decimal)(R * c);
    }
    
    private double ToRadians(decimal degrees) => (double)degrees * Math.PI / 180;
}

public class FraudDetectionRequest
{
    public string ClaimId { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string ProviderId { get; set; } = string.Empty;
    public DateTime ServiceDate { get; set; }
    public string ServiceCode { get; set; } = string.Empty;
    public decimal BilledAmount { get; set; }
    public string ClaimSource { get; set; } = string.Empty;
    public string ServiceLocation { get; set; } = string.Empty;
}
