using System.Net.Http.Json;
using ClaimsPortal.BlazorWasm.Models;

namespace ClaimsPortal.BlazorWasm.Services;

/// <summary>
/// Service for SDOH (Social Determinants of Health) data
/// Based on HL7 FHIR SDOH Clinical Care IG (Gravity Project)
/// https://www.hl7.org/fhir/us/sdoh-clinicalcare/
/// </summary>
public class SdohService
{
    private readonly HttpClient _httpClient;
    private readonly bool _useMockData = true; // Toggle for dev/prod

    public SdohService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://funcclaimstest001ncv.azurewebsites.net/api/");
    }

    #region Public API Methods

    /// <summary>
    /// Get SDOH dashboard for a patient
    /// </summary>
    public async Task<SdohDashboardModel> GetDashboardAsync(string patientId)
    {
        if (_useMockData)
            return GetMockDashboard(patientId);

        try
        {
            var response = await _httpClient.GetAsync($"sdoh/dashboard/{patientId}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<SdohDashboardModel>() 
                   ?? new SdohDashboardModel();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching SDOH dashboard: {ex.Message}");
            return GetMockDashboard(patientId);
        }
    }

    /// <summary>
    /// Get SDOH risk score for a patient
    /// </summary>
    public async Task<SdohRiskScoreModel> GetRiskScoreAsync(string patientId)
    {
        if (_useMockData)
            return GetMockRiskScore(patientId);

        try
        {
            var response = await _httpClient.GetAsync($"sdoh/risk-score/{patientId}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<SdohRiskScoreModel>() 
                   ?? new SdohRiskScoreModel();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching risk score: {ex.Message}");
            return GetMockRiskScore(patientId);
        }
    }

    /// <summary>
    /// Get SDOH conditions for a patient
    /// </summary>
    public async Task<List<SdohConditionModel>> GetConditionsAsync(string patientId)
    {
        if (_useMockData)
            return GetMockConditions(patientId);

        try
        {
            var response = await _httpClient.GetAsync($"sdoh/conditions/{patientId}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<SdohConditionModel>>() 
                   ?? new List<SdohConditionModel>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching conditions: {ex.Message}");
            return GetMockConditions(patientId);
        }
    }

    /// <summary>
    /// Get SDOH goals for a patient
    /// </summary>
    public async Task<List<SdohGoalModel>> GetGoalsAsync(string patientId)
    {
        if (_useMockData)
            return GetMockGoals(patientId);

        try
        {
            var response = await _httpClient.GetAsync($"sdoh/goals/{patientId}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<SdohGoalModel>>() 
                   ?? new List<SdohGoalModel>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching goals: {ex.Message}");
            return GetMockGoals(patientId);
        }
    }

    /// <summary>
    /// Get SDOH referrals for a patient
    /// </summary>
    public async Task<List<SdohReferralModel>> GetReferralsAsync(string patientId)
    {
        if (_useMockData)
            return GetMockReferrals(patientId);

        try
        {
            var response = await _httpClient.GetAsync($"sdoh/referrals/{patientId}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<SdohReferralModel>>() 
                   ?? new List<SdohReferralModel>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching referrals: {ex.Message}");
            return GetMockReferrals(patientId);
        }
    }

    /// <summary>
    /// Get SDOH interventions for a patient
    /// </summary>
    public async Task<List<SdohInterventionModel>> GetInterventionsAsync(string patientId)
    {
        if (_useMockData)
            return GetMockInterventions(patientId);

        try
        {
            var response = await _httpClient.GetAsync($"sdoh/interventions/{patientId}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<SdohInterventionModel>>() 
                   ?? new List<SdohInterventionModel>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching interventions: {ex.Message}");
            return GetMockInterventions(patientId);
        }
    }

    /// <summary>
    /// Get SDOH screening history for a patient
    /// </summary>
    public async Task<List<SdohScreeningModel>> GetScreeningHistoryAsync(string patientId)
    {
        if (_useMockData)
            return GetMockScreeningHistory(patientId);

        try
        {
            var response = await _httpClient.GetAsync($"sdoh/screening/{patientId}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<SdohScreeningModel>>() 
                   ?? new List<SdohScreeningModel>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching screening history: {ex.Message}");
            return GetMockScreeningHistory(patientId);
        }
    }

    /// <summary>
    /// Search for community resources by domain and location
    /// </summary>
    public async Task<List<CommunityResourceModel>> SearchCommunityResourcesAsync(
        string domain, 
        string zipCode, 
        int radiusMiles = 10)
    {
        if (_useMockData)
            return GetMockCommunityResources(domain, zipCode);

        try
        {
            var response = await _httpClient.GetAsync(
                $"sdoh/resources?domain={domain}&zip={zipCode}&radius={radiusMiles}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<CommunityResourceModel>>() 
                   ?? new List<CommunityResourceModel>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error searching community resources: {ex.Message}");
            return GetMockCommunityResources(domain, zipCode);
        }
    }

    /// <summary>
    /// Create a referral to a community resource
    /// </summary>
    public async Task<SdohReferralModel> CreateReferralAsync(SdohReferralModel referral)
    {
        if (_useMockData)
        {
            referral.ServiceRequestId = $"sr-{Guid.NewGuid():N}";
            referral.Status = "active";
            referral.RequestedDate = DateTime.UtcNow;
            return referral;
        }

        try
        {
            var response = await _httpClient.PostAsJsonAsync("sdoh/referrals", referral);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<SdohReferralModel>() ?? referral;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating referral: {ex.Message}");
            return referral;
        }
    }

    /// <summary>
    /// Complete an SDOH assessment questionnaire
    /// </summary>
    public async Task<SdohAssessmentModel> SubmitAssessmentAsync(SdohAssessmentModel assessment)
    {
        if (_useMockData)
        {
            assessment.Status = "completed";
            assessment.CompletedDate = DateTime.UtcNow;
            return assessment;
        }

        try
        {
            var response = await _httpClient.PostAsJsonAsync("sdoh/assessments", assessment);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<SdohAssessmentModel>() ?? assessment;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error submitting assessment: {ex.Message}");
            return assessment;
        }
    }

    #endregion

    #region Mock Data Methods

    private SdohDashboardModel GetMockDashboard(string patientId)
    {
        return new SdohDashboardModel
        {
            PatientId = patientId,
            PatientName = "Sarah Johnson",
            RiskScore = GetMockRiskScore(patientId),
            ActiveConditions = GetMockConditions(patientId),
            ActiveGoals = GetMockGoals(patientId),
            ActiveReferrals = GetMockReferrals(patientId),
            RecentInterventions = GetMockInterventions(patientId).Take(5).ToList(),
            LatestScreening = GetMockScreeningHistory(patientId).Take(12).ToList()
        };
    }

    private SdohRiskScoreModel GetMockRiskScore(string patientId)
    {
        return new SdohRiskScoreModel
        {
            PatientId = patientId,
            OverallRiskScore = 62,
            DomainScores = new Dictionary<string, decimal>
            {
                [SdohDomains.FoodInsecurity] = 75,
                [SdohDomains.HousingInstability] = 45,
                [SdohDomains.Transportation] = 80,
                [SdohDomains.UtilityInsecurity] = 30,
                [SdohDomains.InterpersonalSafety] = 20,
                [SdohDomains.Employment] = 85,
                [SdohDomains.Education] = 40,
                [SdohDomains.FinancialInsecurity] = 70,
                [SdohDomains.SocialConnection] = 55
            },
            DomainRiskLevels = new Dictionary<string, string>
            {
                [SdohDomains.FoodInsecurity] = "High",
                [SdohDomains.HousingInstability] = "Moderate",
                [SdohDomains.Transportation] = "High",
                [SdohDomains.UtilityInsecurity] = "Low",
                [SdohDomains.InterpersonalSafety] = "Low",
                [SdohDomains.Employment] = "High",
                [SdohDomains.Education] = "Moderate",
                [SdohDomains.FinancialInsecurity] = "High",
                [SdohDomains.SocialConnection] = "Moderate"
            },
            LastAssessmentDate = DateTime.Now.AddDays(-14),
            ActiveConditionsCount = 4,
            ActiveGoalsCount = 3,
            ActiveReferralsCount = 2,
            CompletedInterventionsCount = 5
        };
    }

    private List<SdohConditionModel> GetMockConditions(string patientId)
    {
        return new List<SdohConditionModel>
        {
            new() {
                ConditionId = "cond-001",
                PatientId = patientId,
                Domain = SdohDomains.FoodInsecurity,
                Code = "733423003",
                Description = "Food insecurity",
                ClinicalStatus = "active",
                VerificationStatus = "confirmed",
                Severity = "severe",
                OnsetDate = DateTime.Now.AddMonths(-6)
            },
            new() {
                ConditionId = "cond-002",
                PatientId = patientId,
                Domain = SdohDomains.Transportation,
                Code = "160695008",
                Description = "Lack of access to transportation",
                ClinicalStatus = "active",
                VerificationStatus = "confirmed",
                Severity = "moderate",
                OnsetDate = DateTime.Now.AddMonths(-3)
            },
            new() {
                ConditionId = "cond-003",
                PatientId = patientId,
                Domain = SdohDomains.Employment,
                Code = "73438004",
                Description = "Unemployed",
                ClinicalStatus = "active",
                VerificationStatus = "confirmed",
                Severity = "moderate",
                OnsetDate = DateTime.Now.AddMonths(-8)
            },
            new() {
                ConditionId = "cond-004",
                PatientId = patientId,
                Domain = SdohDomains.FinancialInsecurity,
                Code = "263126000",
                Description = "Financial problem",
                ClinicalStatus = "active",
                VerificationStatus = "confirmed",
                Severity = "severe",
                OnsetDate = DateTime.Now.AddMonths(-10)
            }
        };
    }

    private List<SdohGoalModel> GetMockGoals(string patientId)
    {
        return new List<SdohGoalModel>
        {
            new() {
                GoalId = "goal-001",
                PatientId = patientId,
                Domain = SdohDomains.FoodInsecurity,
                Description = "Access to nutritious food daily",
                LifecycleStatus = "active",
                AchievementStatus = "in-progress",
                StartDate = DateTime.Now.AddMonths(-2),
                TargetDate = DateTime.Now.AddMonths(4),
                Addresses = new List<string> { "cond-001" }
            },
            new() {
                GoalId = "goal-002",
                PatientId = patientId,
                Domain = SdohDomains.Employment,
                Description = "Obtain full-time employment",
                LifecycleStatus = "active",
                AchievementStatus = "in-progress",
                StartDate = DateTime.Now.AddMonths(-1),
                TargetDate = DateTime.Now.AddMonths(6),
                Addresses = new List<string> { "cond-003", "cond-004" }
            },
            new() {
                GoalId = "goal-003",
                PatientId = patientId,
                Domain = SdohDomains.Transportation,
                Description = "Reliable transportation to medical appointments",
                LifecycleStatus = "active",
                AchievementStatus = "in-progress",
                StartDate = DateTime.Now.AddDays(-30),
                TargetDate = DateTime.Now.AddMonths(3),
                Addresses = new List<string> { "cond-002" }
            }
        };
    }

    private List<SdohReferralModel> GetMockReferrals(string patientId)
    {
        return new List<SdohReferralModel>
        {
            new() {
                ServiceRequestId = "sr-001",
                PatientId = patientId,
                Domain = SdohDomains.FoodInsecurity,
                ServiceType = "Food assistance program",
                ServiceDescription = "Weekly food delivery from local food bank",
                Status = "active",
                Priority = "urgent",
                RequestedDate = DateTime.Now.AddDays(-10),
                ReferralSource = "Dr. Emily Chen",
                ReferralTarget = "City Food Bank",
                ReferralTargetId = "org-foodbank-001",
                Addresses = new List<string> { "cond-001", "goal-001" }
            },
            new() {
                ServiceRequestId = "sr-002",
                PatientId = patientId,
                Domain = SdohDomains.Employment,
                ServiceType = "Job training program",
                ServiceDescription = "6-week job skills training and placement assistance",
                Status = "active",
                Priority = "routine",
                RequestedDate = DateTime.Now.AddDays(-5),
                ReferralSource = "Sarah Miller, LCSW",
                ReferralTarget = "Workforce Development Center",
                ReferralTargetId = "org-workforce-001",
                Addresses = new List<string> { "cond-003", "goal-002" }
            }
        };
    }

    private List<SdohInterventionModel> GetMockInterventions(string patientId)
    {
        return new List<SdohInterventionModel>
        {
            new() {
                ProcedureId = "proc-001",
                PatientId = patientId,
                Domain = SdohDomains.Transportation,
                Code = "410410006",
                Description = "Transportation voucher provided",
                Status = "completed",
                PerformedDate = DateTime.Now.AddDays(-7),
                Performer = "Community Health Network",
                Outcome = "Patient received 10 ride vouchers for medical appointments",
                Addresses = new List<string> { "cond-002" }
            },
            new() {
                ProcedureId = "proc-002",
                PatientId = patientId,
                Domain = SdohDomains.FoodInsecurity,
                Code = "710925007",
                Description = "Food assistance enrollment",
                Status = "completed",
                PerformedDate = DateTime.Now.AddDays(-12),
                Performer = "City Food Bank",
                Outcome = "Enrolled in weekly food delivery program",
                Addresses = new List<string> { "cond-001" }
            }
        };
    }

    private List<SdohScreeningModel> GetMockScreeningHistory(string patientId)
    {
        return new List<SdohScreeningModel>
        {
            new() {
                ObservationId = "obs-001",
                PatientId = patientId,
                Domain = SdohDomains.FoodInsecurity,
                QuestionCode = "88122-7",
                QuestionText = "Within the past 12 months, were you worried that your food would run out before you got money to buy more?",
                Answer = "Often true",
                AnswerCode = "LA28397-0",
                RiskLevel = "High",
                AssessedDate = DateTime.Now.AddDays(-14),
                AssessmentTool = "AHC-HRSN"
            },
            new() {
                ObservationId = "obs-002",
                PatientId = patientId,
                Domain = SdohDomains.FoodInsecurity,
                QuestionCode = "88123-5",
                QuestionText = "Within the past 12 months, did the food you bought not last and you didn't have money to get more?",
                Answer = "Often true",
                AnswerCode = "LA28397-0",
                RiskLevel = "High",
                AssessedDate = DateTime.Now.AddDays(-14),
                AssessmentTool = "AHC-HRSN"
            },
            new() {
                ObservationId = "obs-003",
                PatientId = patientId,
                Domain = SdohDomains.HousingInstability,
                QuestionCode = "71802-3",
                QuestionText = "What is your housing situation today?",
                Answer = "I have housing",
                AnswerCode = "LA31993-1",
                RiskLevel = "Low",
                AssessedDate = DateTime.Now.AddDays(-14),
                AssessmentTool = "AHC-HRSN"
            },
            new() {
                ObservationId = "obs-004",
                PatientId = patientId,
                Domain = SdohDomains.Transportation,
                QuestionCode = "93030-5",
                QuestionText = "In the past 12 months, has lack of reliable transportation kept you from medical appointments, meetings, work, or from getting things needed for daily living?",
                Answer = "Yes",
                AnswerCode = "LA33-6",
                RiskLevel = "High",
                AssessedDate = DateTime.Now.AddDays(-14),
                AssessmentTool = "AHC-HRSN"
            }
        };
    }

    private List<CommunityResourceModel> GetMockCommunityResources(string domain, string zipCode)
    {
        var allResources = new List<CommunityResourceModel>
        {
            new() {
                OrganizationId = "org-foodbank-001",
                Name = "City Food Bank",
                Type = "Food Bank",
                ServicesDomains = new List<string> { SdohDomains.FoodInsecurity },
                Phone = "(555) 123-4567",
                Website = "www.cityfoodbank.org",
                AddressLine = "123 Main Street",
                City = "Seattle",
                State = "WA",
                ZipCode = "98101",
                Description = "Provides free groceries and hot meals. Weekly food distribution on Tuesdays and Fridays.",
                AvailabilityHours = "Tue/Fri 10am-2pm",
                AcceptsReferrals = true,
                DistanceMiles = 2.3m
            },
            new() {
                OrganizationId = "org-shelter-001",
                Name = "Family Emergency Shelter",
                Type = "Homeless Shelter",
                ServicesDomains = new List<string> { SdohDomains.HousingInstability },
                Phone = "(555) 234-5678",
                Website = "www.familyshelter.org",
                AddressLine = "456 Oak Avenue",
                City = "Seattle",
                State = "WA",
                ZipCode = "98102",
                Description = "Emergency shelter for families. Case management and housing placement assistance available.",
                AvailabilityHours = "24/7",
                AcceptsReferrals = true,
                DistanceMiles = 3.7m
            },
            new() {
                OrganizationId = "org-workforce-001",
                Name = "Workforce Development Center",
                Type = "Job Training",
                ServicesDomains = new List<string> { SdohDomains.Employment, SdohDomains.Education },
                Phone = "(555) 345-6789",
                Website = "www.workforcecenter.org",
                AddressLine = "789 Pine Street",
                City = "Seattle",
                State = "WA",
                ZipCode = "98103",
                Description = "Free job training, resume assistance, and job placement services. Computer skills classes available.",
                AvailabilityHours = "Mon-Fri 9am-5pm",
                AcceptsReferrals = true,
                DistanceMiles = 1.8m
            },
            new() {
                OrganizationId = "org-transport-001",
                Name = "Community Rides",
                Type = "Transportation Service",
                ServicesDomains = new List<string> { SdohDomains.Transportation },
                Phone = "(555) 456-7890",
                Website = "www.communityrides.org",
                AddressLine = "321 Elm Drive",
                City = "Seattle",
                State = "WA",
                ZipCode = "98104",
                Description = "Free and low-cost transportation to medical appointments. Wheelchair accessible vehicles available.",
                AvailabilityHours = "Mon-Sat 7am-6pm",
                AcceptsReferrals = true,
                DistanceMiles = 4.2m
            }
        };

        // Filter by domain if specified
        if (!string.IsNullOrEmpty(domain))
        {
            return allResources.Where(r => r.ServicesDomains.Contains(domain)).ToList();
        }

        return allResources;
    }

    #endregion
}
