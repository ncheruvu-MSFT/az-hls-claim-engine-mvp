using System.Net.Http.Json;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using System.Text.Json;

namespace ClaimsPortal.BlazorWasm.Services;

public class MemberPortalService
{
    private readonly HttpClient _httpClient;
    private readonly string _fhirBaseUrl;
    private readonly string _apiBaseUrl;

    public MemberPortalService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _fhirBaseUrl = configuration["FhirServerUrl"] ?? "http://localhost:8080/fhir";
        _apiBaseUrl = configuration["ApiBaseUrl"] ?? "https://funcclaimstest001ncv.azurewebsites.net/api";
    }

    // Authenticate member and get FHIR Patient resource
    public async Task<MemberAuthResult?> AuthenticateMemberAsync(string memberId, string ssn)
    {
        try
        {
            // Search for Patient by identifier (member ID)
            var searchUrl = $"{_fhirBaseUrl}/Patient?identifier={memberId}";
            var response = await _httpClient.GetAsync(searchUrl);
            
            if (!response.IsSuccessStatusCode)
                return null;

            var bundleJson = await response.Content.ReadAsStringAsync();
            var parser = new FhirJsonParser();
            var bundle = parser.Parse<Bundle>(bundleJson);

            if (bundle.Entry?.Count > 0)
            {
                var patient = bundle.Entry[0].Resource as Patient;
                if (patient != null)
                {
                    return new MemberAuthResult
                    {
                        PatientId = patient.Id,
                        MemberId = memberId,
                        FullName = $"{patient.Name[0].Given.FirstOrDefault()} {patient.Name[0].Family}",
                        DateOfBirth = patient.BirthDate,
                        IsAuthenticated = true
                    };
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    // Get member's coverage information (deductibles, OOP max)
    public async Task<MemberCoverageInfo?> GetCoverageInfoAsync(string patientId)
    {
        try
        {
            // Search for active Coverage resources for this patient
            var searchUrl = $"{_fhirBaseUrl}/Coverage?beneficiary=Patient/{patientId}&status=active";
            var response = await _httpClient.GetAsync(searchUrl);
            
            if (!response.IsSuccessStatusCode)
                return null;

            var bundleJson = await response.Content.ReadAsStringAsync();
            var parser = new FhirJsonParser();
            var bundle = parser.Parse<Bundle>(bundleJson);

            if (bundle.Entry?.Count > 0)
            {
                var coverage = bundle.Entry[0].Resource as Coverage;
                if (coverage != null)
                {
                    return new MemberCoverageInfo
                    {
                        CoverageId = coverage.Id,
                        PlanName = coverage.Class?.FirstOrDefault(c => c.Type.Coding[0].Code == "plan")?.Value ?? "Unknown Plan",
                        Status = coverage.Status?.ToString() ?? "Unknown",
                        PeriodStart = coverage.Period?.StartElement?.ToString(),
                        PeriodEnd = coverage.Period?.EndElement?.ToString(),
                        // Extract cost-to-beneficiary values
                        DeductibleValue = GetCostValue(coverage, "deductible"),
                        OutOfPocketMaxValue = GetCostValue(coverage, "maxoutofpocket")
                    };
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    // Get member's claims from ExplanationOfBenefit resources
    public async Task<List<MemberClaim>> GetMemberClaimsAsync(string patientId)
    {
        try
        {
            var searchUrl = $"{_fhirBaseUrl}/ExplanationOfBenefit?patient=Patient/{patientId}&_sort=-created&_count=20";
            var response = await _httpClient.GetAsync(searchUrl);
            
            if (!response.IsSuccessStatusCode)
                return new List<MemberClaim>();

            var bundleJson = await response.Content.ReadAsStringAsync();
            var parser = new FhirJsonParser();
            var bundle = parser.Parse<Bundle>(bundleJson);

            var claims = new List<MemberClaim>();

            if (bundle.Entry != null)
            {
                foreach (var entry in bundle.Entry)
                {
                    if (entry.Resource is ExplanationOfBenefit eob)
                    {
                        claims.Add(new MemberClaim
                        {
                            ClaimId = eob.Id,
                            ServiceDate = eob.Created,
                            ProviderName = eob.Provider?.Display ?? "Unknown Provider",
                            ServiceDescription = eob.Type?.Coding?[0]?.Display ?? "Medical Service",
                            Status = eob.Status?.ToString() ?? "Unknown",
                            BilledAmount = eob.Total?.FirstOrDefault(t => t.Category.Coding[0].Code == "submitted")?.Amount?.Value ?? 0,
                            PatientResponsibility = eob.Total?.FirstOrDefault(t => t.Category.Coding[0].Code == "copay")?.Amount?.Value ?? 0
                        });
                    }
                }
            }

            return claims;
        }
        catch
        {
            return new List<MemberClaim>();
        }
    }

    // Get accumulators (YTD spending) from custom API
    public async Task<MemberAccumulators?> GetAccumulatorsAsync(string memberId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/accumulators/{memberId}");
            
            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<MemberAccumulators>();
        }
        catch
        {
            return null;
        }
    }

    // Get providers from PractitionerRole resources
    public async Task<List<ProviderInfo>> SearchProvidersAsync(string searchTerm, string? specialty = null)
    {
        try
        {
            var searchUrl = $"{_fhirBaseUrl}/PractitionerRole?_include=PractitionerRole:practitioner&_count=10";
            
            if (!string.IsNullOrEmpty(specialty))
            {
                searchUrl += $"&specialty={specialty}";
            }

            var response = await _httpClient.GetAsync(searchUrl);
            
            if (!response.IsSuccessStatusCode)
                return new List<ProviderInfo>();

            var bundleJson = await response.Content.ReadAsStringAsync();
            var parser = new FhirJsonParser();
            var bundle = parser.Parse<Bundle>(bundleJson);

            var providers = new List<ProviderInfo>();

            if (bundle.Entry != null)
            {
                foreach (var entry in bundle.Entry)
                {
                    if (entry.Resource is PractitionerRole role)
                    {
                        // Find the included Practitioner
                        var practitioner = bundle.Entry
                            .FirstOrDefault(e => e.FullUrl == role.Practitioner.Reference)?
                            .Resource as Practitioner;

                        if (practitioner != null)
                        {
                            providers.Add(new ProviderInfo
                            {
                                ProviderId = practitioner.Id,
                                Name = $"{practitioner.Name[0].Given.FirstOrDefault()} {practitioner.Name[0].Family}",
                                Specialty = role.Specialty?[0]?.Coding?[0]?.Display ?? "General Practice",
                                LocationName = role.Location?[0]?.Display ?? "Unknown Location",
                                IsAcceptingPatients = role.Active ?? true
                            });
                        }
                    }
                }
            }

            return providers;
        }
        catch
        {
            return new List<ProviderInfo>();
        }
    }

    // Get care gaps from Condition and Observation resources
    public async Task<List<CareGapItem>> GetCareGapsAsync(string patientId)
    {
        try
        {
            // This would typically use HEDIS/quality measure APIs
            // For now, query for Conditions marked as needing follow-up
            var searchUrl = $"{_fhirBaseUrl}/Condition?patient=Patient/{patientId}&clinical-status=active";
            var response = await _httpClient.GetAsync(searchUrl);
            
            if (!response.IsSuccessStatusCode)
                return new List<CareGapItem>();

            var bundleJson = await response.Content.ReadAsStringAsync();
            var parser = new FhirJsonParser();
            var bundle = parser.Parse<Bundle>(bundleJson);

            var careGaps = new List<CareGapItem>();

            if (bundle.Entry != null)
            {
                foreach (var entry in bundle.Entry)
                {
                    if (entry.Resource is Condition condition)
                    {
                        // Check if there are unmet care requirements
                        careGaps.Add(new CareGapItem
                        {
                            GapType = "condition-follow-up",
                            Title = $"Follow-up for {condition.Code?.Coding?[0]?.Display}",
                            Description = "Regular monitoring recommended",
                            Priority = "medium",
                            DueDate = DateTime.Now.AddDays(30)
                        });
                    }
                }
            }

            return careGaps;
        }
        catch
        {
            return new List<CareGapItem>();
        }
    }

    // Get documents from DocumentReference resources
    public async Task<List<MemberDocument>> GetDocumentsAsync(string patientId)
    {
        try
        {
            var searchUrl = $"{_fhirBaseUrl}/DocumentReference?patient=Patient/{patientId}&_sort=-date&_count=20";
            var response = await _httpClient.GetAsync(searchUrl);
            
            if (!response.IsSuccessStatusCode)
                return new List<MemberDocument>();

            var bundleJson = await response.Content.ReadAsStringAsync();
            var parser = new FhirJsonParser();
            var bundle = parser.Parse<Bundle>(bundleJson);

            var documents = new List<MemberDocument>();

            if (bundle.Entry != null)
            {
                foreach (var entry in bundle.Entry)
                {
                    if (entry.Resource is DocumentReference docRef)
                    {
                        documents.Add(new MemberDocument
                        {
                            DocumentId = docRef.Id,
                            Title = docRef.Type?.Coding?[0]?.Display ?? "Document",
                            DocumentType = docRef.Type?.Coding?[0]?.Code ?? "unknown",
                            Date = docRef.Date?.ToString(),
                            ContentUrl = docRef.Content?[0]?.Attachment?.Url
                        });
                    }
                }
            }

            return documents;
        }
        catch
        {
            return new List<MemberDocument>();
        }
    }

    // Get detailed medications for Epic-style medication screen
    public async Task<List<MedicationDetail>> GetMedicationsDetailedAsync(string patientId)
    {
        try
        {
            var searchUrl = $"{_fhirBaseUrl}/MedicationRequest?patient=Patient/{patientId}&status=active&_include=MedicationRequest:requester&_count=50";
            var response = await _httpClient.GetAsync(searchUrl);
            
            if (!response.IsSuccessStatusCode)
                return new List<MedicationDetail>();

            var bundleJson = await response.Content.ReadAsStringAsync();
            var parser = new FhirJsonParser();
            var bundle = parser.Parse<Bundle>(bundleJson);

            var medications = new List<MedicationDetail>();

            if (bundle.Entry != null)
            {
                foreach (var entry in bundle.Entry)
                {
                    if (entry.Resource is MedicationRequest medReq)
                    {
                        var dosage = medReq.DosageInstruction?.FirstOrDefault();
                        var timing = dosage?.Timing?.Repeat;
                        
                        var frequency = "As directed";
                        if (timing != null && timing.Frequency.HasValue)
                        {
                            frequency = $"{timing.Frequency} time(s) per {timing.Period} {timing.PeriodUnit}";
                        }

                        // Get medication name - can be CodeableConcept or Reference
                        var medName = "Unknown Medication";
                        if (medReq.Medication is CodeableConcept concept)
                        {
                            medName = concept.Coding?.FirstOrDefault()?.Display ?? concept.Text ?? "Unknown Medication";
                        }
                        else if (medReq.Medication is ResourceReference reference)
                        {
                            medName = reference.Display ?? "Medication (Reference)";
                        }

                        medications.Add(new MedicationDetail
                        {
                            MedicationId = medReq.Id,
                            Name = medName,
                            Dosage = dosage?.Text ?? "As directed",
                            Frequency = frequency,
                            LastFilled = medReq.DispenseRequest?.ValidityPeriod?.StartElement?.ToString() ?? "N/A",
                            RefillsRemaining = medReq.DispenseRequest?.NumberOfRepeatsAllowed,
                            Prescriber = medReq.Requester?.Display ?? "Unknown Provider",
                            Status = medReq.Status?.ToString() ?? "active",
                            Instructions = dosage?.PatientInstruction ?? dosage?.Text ?? "Take as directed"
                        });
                    }
                }
            }

            return medications;
        }
        catch
        {
            return new List<MedicationDetail>();
        }
    }

    // Get test results for Epic-style test results screen
    public async Task<List<TestResult>> GetTestResultsAsync(string patientId)
    {
        try
        {
            var searchUrl = $"{_fhirBaseUrl}/DiagnosticReport?patient=Patient/{patientId}&_sort=-date&_count=30&_include=DiagnosticReport:result";
            var response = await _httpClient.GetAsync(searchUrl);
            
            if (!response.IsSuccessStatusCode)
                return new List<TestResult>();

            var bundleJson = await response.Content.ReadAsStringAsync();
            var parser = new FhirJsonParser();
            var bundle = parser.Parse<Bundle>(bundleJson);

            var testResults = new List<TestResult>();

            if (bundle.Entry != null)
            {
                foreach (var entry in bundle.Entry)
                {
                    if (entry.Resource is DiagnosticReport report)
                    {
                        var isNew = report.Issued.HasValue && (DateTime.Now - report.Issued.Value.DateTime).TotalDays <= 7;
                        var hasAbnormal = false;
                        
                        // Check if any observations are abnormal
                        if (report.Result != null)
                        {
                            foreach (var resultRef in report.Result)
                            {
                                var obsEntry = bundle.Entry.FirstOrDefault(e => 
                                    e.FullUrl == resultRef.Reference || 
                                    e.Resource?.Id == resultRef.Reference.Replace("Observation/", ""));
                                    
                                if (obsEntry?.Resource is Observation obs)
                                {
                                    var interpretation = obs.Interpretation?.FirstOrDefault()?.Coding?.FirstOrDefault()?.Code;
                                    if (interpretation == "H" || interpretation == "L" || interpretation == "A")
                                    {
                                        hasAbnormal = true;
                                        break;
                                    }
                                }
                            }
                        }
                        
                        testResults.Add(new TestResult
                        {
                            ReportId = report.Id,
                            TestName = report.Code?.Coding?.FirstOrDefault()?.Display ?? "Lab Test",
                            CollectionDate = report.Effective?.ToString() ?? "N/A",
                            OrderedBy = report.Performer?.FirstOrDefault()?.Display ?? "Unknown Provider",
                            Status = report.Status?.ToString() ?? "final",
                            IsNew = isNew,
                            HasAbnormal = hasAbnormal,
                            ResultCount = report.Result?.Count ?? 0
                        });
                    }
                }
            }

            return testResults;
        }
        catch
        {
            return new List<TestResult>();
        }
    }

    // Get detailed diagnostic report with observations
    public async Task<DiagnosticReportDetail?> GetDiagnosticReportDetailAsync(string reportId)
    {
        try
        {
            var reportUrl = $"{_fhirBaseUrl}/DiagnosticReport/{reportId}?_include=DiagnosticReport:result";
            var response = await _httpClient.GetAsync(reportUrl);
            
            if (!response.IsSuccessStatusCode)
                return null;

            var bundleJson = await response.Content.ReadAsStringAsync();
            var parser = new FhirJsonParser();
            var bundle = parser.Parse<Bundle>(bundleJson);
            
            var report = bundle.Entry?.FirstOrDefault(e => e.Resource is DiagnosticReport)?.Resource as DiagnosticReport;
            if (report == null) return null;
            
            var observations = new List<ObservationResult>();
            
            if (report.Result != null)
            {
                foreach (var resultRef in report.Result)
                {
                    // Try to fetch each observation
                    var obsUrl = resultRef.Reference.StartsWith("http") ? resultRef.Reference : $"{_fhirBaseUrl}/{resultRef.Reference}";
                    var obsResponse = await _httpClient.GetAsync(obsUrl);
                    
                    if (obsResponse.IsSuccessStatusCode)
                    {
                        var obsJson = await obsResponse.Content.ReadAsStringAsync();
                        var obs = parser.Parse<Observation>(obsJson);
                        
                        var value = "N/A";
                        if (obs.Value is Quantity qty)
                        {
                            value = $"{qty.Value} {qty.Unit}";
                        }
                        else if (obs.Value is FhirString str)
                        {
                            value = str.Value;
                        }
                        
                        var refRange = "N/A";
                        if (obs.ReferenceRange?.FirstOrDefault() != null)
                        {
                            var range = obs.ReferenceRange.First();
                            refRange = $"{range.Low?.Value}-{range.High?.Value} {range.Low?.Unit}";
                        }
                        
                        var interpretation = obs.Interpretation?.FirstOrDefault()?.Coding?.FirstOrDefault()?.Code ?? "N";
                        
                        observations.Add(new ObservationResult
                        {
                            Name = obs.Code?.Coding?.FirstOrDefault()?.Display ?? "Test",
                            Value = value,
                            ReferenceRange = refRange,
                            Flag = interpretation,
                            IsAbnormal = interpretation == "H" || interpretation == "L" || interpretation == "A"
                        });
                    }
                }
            }
            
            return new DiagnosticReportDetail
            {
                ReportId = report.Id,
                TestName = report.Code?.Coding?.FirstOrDefault()?.Display ?? "Lab Test",
                CollectionDateTime = report.Effective?.ToString() ?? "N/A",
                OrderedBy = report.Performer?.FirstOrDefault()?.Display ?? "Unknown Provider",
                Status = report.Status?.ToString() ?? "final",
                Observations = observations,
                Comments = report.Conclusion ?? ""
            };
        }
        catch
        {
            return null;
        }
    }

    private decimal GetCostValue(Coverage coverage, string costType)
    {
        var cost = coverage.CostToBeneficiary?
            .FirstOrDefault(c => c.Type?.Coding?[0]?.Code == costType);
        
        if (cost?.Value is Money money)
        {
            return money.Value ?? 0;
        }
        
        return 0;
    }
}

// DTOs for Member Portal
public class MemberAuthResult
{
    public string PatientId { get; set; } = string.Empty;
    public string MemberId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? DateOfBirth { get; set; }
    public bool IsAuthenticated { get; set; }
}

public class MemberCoverageInfo
{
    public string CoverageId { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? PeriodStart { get; set; }
    public string? PeriodEnd { get; set; }
    public decimal DeductibleValue { get; set; }
    public decimal OutOfPocketMaxValue { get; set; }
}

public class MemberClaim
{
    public string ClaimId { get; set; } = string.Empty;
    public string? ServiceDate { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string ServiceDescription { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal BilledAmount { get; set; }
    public decimal PatientResponsibility { get; set; }
}

public class MemberAccumulators
{
    public string MemberId { get; set; } = string.Empty;
    public decimal MedicalDeductibleYTD { get; set; }
    public decimal MedicalDeductibleMax { get; set; }
    public decimal MedicalOOPYTD { get; set; }
    public decimal MedicalOOPMax { get; set; }
    public decimal DentalSpentYTD { get; set; }
    public decimal DentalMax { get; set; }
    public decimal PharmacyOOPYTD { get; set; }
    public decimal PharmacyOOPMax { get; set; }
    public int QualityScore { get; set; }
}

public class ProviderInfo
{
    public string ProviderId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Specialty { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public bool IsAcceptingPatients { get; set; }
}

public class CareGapItem
{
    public string GapType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
}

public class MemberDocument
{
    public string DocumentId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string? Date { get; set; }
    public string? ContentUrl { get; set; }
}

public class MedicationDetail
{
    public string MedicationId { get; set; } = string.Empty;
    public string MedicationName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
    public string Frequency { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public string? StartDate { get; set; }
    public string? LastFilled { get; set; }
    public int? RefillsRemaining { get; set; }
    public int? Refills { get; set; }
    public string? RxNumber { get; set; }
    public string Prescriber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Instructions { get; set; } = string.Empty;
}

public class TestResult
{
    public string Id { get; set; } = string.Empty;
    public string ReportId { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? OrderedDate { get; set; }
    public string? ResultDate { get; set; }
    public string? CollectionDate { get; set; }
    public string OrderedBy { get; set; } = string.Empty;
    public string OrderingProvider { get; set; } = string.Empty;
    public string PerformingLab { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsNew { get; set; }
    public bool HasAbnormal { get; set; }
    public int ResultCount { get; set; }
}

public class DiagnosticReportDetail
{
    public string ReportId { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public string? CollectionDateTime { get; set; }
    public string OrderedBy { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public List<ObservationResult> Observations { get; set; } = new();
    public string Comments { get; set; } = string.Empty;
}

public class ObservationResult
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string ReferenceRange { get; set; } = string.Empty;
    public string Flag { get; set; } = string.Empty;
    public bool IsAbnormal { get; set; }
}
