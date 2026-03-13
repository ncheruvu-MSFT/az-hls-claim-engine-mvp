namespace ClaimsPortal.BlazorWasm.Models;

// Based on HL7 FHIR SDOH Clinical Care IG (Gravity Project)
// https://www.hl7.org/fhir/us/sdoh-clinicalcare/

/// <summary>
/// SDOH screening assessment result
/// Maps to FHIR Observation (SDOHCC-ObservationScreeningResponse)
/// </summary>
public class SdohScreeningModel
{
    public string ObservationId { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty; // Food, Housing, Transportation, etc.
    public string QuestionCode { get; set; } = string.Empty; // LOINC code
    public string QuestionText { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public string AnswerCode { get; set; } = string.Empty;
    public string RiskLevel { get; set; } = string.Empty; // None, Low, Moderate, High
    public DateTime AssessedDate { get; set; }
    public string AssessmentTool { get; set; } = string.Empty; // PRAPARE, AHC-HRSN, etc.
}

/// <summary>
/// SDOH condition/problem
/// Maps to FHIR Condition (SDOHCC-Condition)
/// </summary>
public class SdohConditionModel
{
    public string ConditionId { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty; // SNOMED-CT or ICD-10
    public string Description { get; set; } = string.Empty;
    public string ClinicalStatus { get; set; } = string.Empty; // active, resolved
    public string VerificationStatus { get; set; } = string.Empty; // confirmed, provisional
    public string Severity { get; set; } = string.Empty; // mild, moderate, severe
    public DateTime OnsetDate { get; set; }
    public DateTime? AbatementDate { get; set; }
}

/// <summary>
/// SDOH goal
/// Maps to FHIR Goal (SDOHCC-Goal)
/// </summary>
public class SdohGoalModel
{
    public string GoalId { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string LifecycleStatus { get; set; } = string.Empty; // active, completed, cancelled
    public string AchievementStatus { get; set; } = string.Empty; // in-progress, achieved, not-achieved
    public DateTime StartDate { get; set; }
    public DateTime? TargetDate { get; set; }
    public List<string> Addresses { get; set; } = new(); // Related condition IDs
}

/// <summary>
/// SDOH referral/service request
/// Maps to FHIR ServiceRequest (SDOHCC-ServiceRequest)
/// </summary>
public class SdohReferralModel
{
    public string ServiceRequestId { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string ServiceType { get; set; } = string.Empty;
    public string ServiceDescription { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // active, completed, cancelled
    public string Priority { get; set; } = string.Empty; // routine, urgent, asap
    public DateTime RequestedDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public string ReferralSource { get; set; } = string.Empty; // Provider name
    public string ReferralTarget { get; set; } = string.Empty; // CBO name
    public string ReferralTargetId { get; set; } = string.Empty; // Organization ID
    public List<string> Addresses { get; set; } = new(); // Related condition/goal IDs
}

/// <summary>
/// SDOH intervention/procedure
/// Maps to FHIR Procedure (SDOHCC-Procedure)
/// </summary>
public class SdohInterventionModel
{
    public string ProcedureId { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty; // SNOMED-CT
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // completed, in-progress, not-done
    public DateTime PerformedDate { get; set; }
    public string Performer { get; set; } = string.Empty; // Provider/CBO name
    public string Outcome { get; set; } = string.Empty;
    public List<string> Addresses { get; set; } = new(); // Related condition/goal IDs
}

/// <summary>
/// Community resource (CBO)
/// Maps to FHIR Organization and HealthcareService
/// </summary>
public class CommunityResourceModel
{
    public string OrganizationId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // Food bank, shelter, etc.
    public List<string> ServicesDomains { get; set; } = new(); // Domains served
    public string Phone { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string AvailabilityHours { get; set; } = string.Empty;
    public bool AcceptsReferrals { get; set; }
    public decimal DistanceMiles { get; set; }
}

/// <summary>
/// SDOH risk score summary
/// </summary>
public class SdohRiskScoreModel
{
    public string PatientId { get; set; } = string.Empty;
    public decimal OverallRiskScore { get; set; } // 0-100
    public Dictionary<string, decimal> DomainScores { get; set; } = new(); // Domain -> Score
    public Dictionary<string, string> DomainRiskLevels { get; set; } = new(); // Domain -> Low/Moderate/High
    public DateTime LastAssessmentDate { get; set; }
    public int ActiveConditionsCount { get; set; }
    public int ActiveGoalsCount { get; set; }
    public int ActiveReferralsCount { get; set; }
    public int CompletedInterventionsCount { get; set; }
}

/// <summary>
/// SDOH dashboard summary
/// </summary>
public class SdohDashboardModel
{
    public string PatientId { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public SdohRiskScoreModel RiskScore { get; set; } = new();
    public List<SdohConditionModel> ActiveConditions { get; set; } = new();
    public List<SdohGoalModel> ActiveGoals { get; set; } = new();
    public List<SdohReferralModel> ActiveReferrals { get; set; } = new();
    public List<SdohInterventionModel> RecentInterventions { get; set; } = new();
    public List<SdohScreeningModel> LatestScreening { get; set; } = new();
}

/// <summary>
/// SDOH assessment questionnaire (PRAPARE, AHC-HRSN)
/// Maps to FHIR Questionnaire and QuestionnaireResponse
/// </summary>
public class SdohAssessmentModel
{
    public string QuestionnaireId { get; set; } = string.Empty;
    public string QuestionnaireResponseId { get; set; } = string.Empty;
    public string AssessmentName { get; set; } = string.Empty; // PRAPARE, AHC-HRSN
    public string PatientId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // in-progress, completed
    public DateTime StartedDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public List<QuestionResponseModel> Questions { get; set; } = new();
}

public class QuestionResponseModel
{
    public string LinkId { get; set; } = string.Empty;
    public string QuestionText { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string AnswerType { get; set; } = string.Empty; // choice, boolean, string
    public string Answer { get; set; } = string.Empty;
    public string AnswerCode { get; set; } = string.Empty;
}

/// <summary>
/// SDOH domain categories per Gravity Project
/// </summary>
public static class SdohDomains
{
    public const string FoodInsecurity = "Food Insecurity";
    public const string HousingInstability = "Housing Instability";
    public const string Transportation = "Transportation Insecurity";
    public const string UtilityInsecurity = "Utility Insecurity";
    public const string InterpersonalSafety = "Interpersonal Safety";
    public const string Employment = "Employment";
    public const string Education = "Educational Attainment";
    public const string FinancialInsecurity = "Financial Insecurity";
    public const string HealthLiteracy = "Health Literacy";
    public const string SocialConnection = "Social Connection";
    public const string StressAnxiety = "Stress/Anxiety";
    public const string VeteranStatus = "Veteran Status";

    public static List<string> AllDomains => new()
    {
        FoodInsecurity,
        HousingInstability,
        Transportation,
        UtilityInsecurity,
        InterpersonalSafety,
        Employment,
        Education,
        FinancialInsecurity,
        HealthLiteracy,
        SocialConnection,
        StressAnxiety,
        VeteranStatus
    };

    public static string GetDomainIcon(string domain) => domain switch
    {
        FoodInsecurity => "🍎",
        HousingInstability => "🏠",
        Transportation => "🚗",
        UtilityInsecurity => "💡",
        InterpersonalSafety => "🛡️",
        Employment => "💼",
        Education => "🎓",
        FinancialInsecurity => "💰",
        HealthLiteracy => "📚",
        SocialConnection => "👥",
        StressAnxiety => "🧘",
        VeteranStatus => "🎖️",
        _ => "📋"
    };

    public static string GetRiskColor(string riskLevel) => riskLevel switch
    {
        "None" or "Low" => "var(--success-color)",
        "Moderate" => "var(--warning-color)",
        "High" or "Critical" => "var(--error-color)",
        _ => "var(--text-secondary)"
    };
}
