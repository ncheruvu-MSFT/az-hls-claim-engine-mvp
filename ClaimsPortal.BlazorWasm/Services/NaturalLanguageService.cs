using System.Text.RegularExpressions;
using ClaimsPortal.BlazorWasm.Models;

namespace ClaimsPortal.BlazorWasm.Services;

/// <summary>
/// Natural language AI assistant service for healthcare concepts and benefit configuration
/// Provides context-aware answers about benefits, claims, FHIR, fraud detection, and processes benefit configuration requests
/// </summary>
public class NaturalLanguageService
{
    private static readonly Dictionary<string, string> CPTCodeDescriptions = new()
    {
        { "99213", "Office/Outpatient Visit - Established Patient (Low-Moderate Complexity)" },
        { "99214", "Office/Outpatient Visit - Established Patient (Moderate Complexity)" },
        { "99215", "Office/Outpatient Visit - Established Patient (High Complexity)" },
        { "99203", "Office/Outpatient Visit - New Patient (Low-Moderate Complexity)" },
        { "99204", "Office/Outpatient Visit - New Patient (Moderate Complexity)" },
        { "99205", "Office/Outpatient Visit - New Patient (High Complexity)" },
        { "80053", "Comprehensive Metabolic Panel" },
        { "85025", "Complete Blood Count with Differential" },
        { "93000", "Electrocardiogram (ECG)" },
        { "70450", "CT Scan - Head/Brain without Contrast" },
        { "72148", "MRI - Lumbar Spine without Contrast" },
        { "99285", "Emergency Department Visit (High Severity)" }
    };

    private static readonly Dictionary<string, string> StateCMSRegulations = new()
    {
        { "California", "CA State requires prior authorization for imaging studies over $500 (CA Health & Safety Code §1367.01)" },
        { "Texas", "TX mandates 24-hour emergency coverage with no prior auth (TX Insurance Code §1271.155)" },
        { "Florida", "FL requires specialist referrals within 5 days for HMO plans (FL Statute §641.31)" },
        { "New York", "NY mandates coverage for preventive services with no cost-sharing (NY Insurance Law §3216)" },
        { "CMS", "CMS Medicare requires adherence to National Coverage Determinations (NCDs) and Local Coverage Determinations (LCDs)" }
    };

    public async Task<string> GetAnswerAsync(string question)
    {
        await Task.Delay(100); // Simulate API call
        
        var lowerQuestion = question.ToLowerInvariant();
        
        // Deductible questions
        if (lowerQuestion.Contains("deductible"))
        {
            return "The deductible is the amount a patient must pay out-of-pocket before insurance coverage begins. " +
                   "In FHIR, this maps to Coverage.costToBeneficiary with type 'deductible'. Accumulators track how much has been met during the enrollment period.";
        }
        
        // Coinsurance questions
        if (lowerQuestion.Contains("coinsurance"))
        {
            return "Coinsurance is the percentage of costs a patient pays after meeting their deductible. " +
                   "For example, 20% coinsurance means the patient pays 20% and insurance pays 80%. " +
                   "In FHIR, this is represented in Coverage.costToBeneficiary.valueMoney and ClaimResponse.payment.";
        }
        
        // Copay questions
        if (lowerQuestion.Contains("copay") || lowerQuestion.Contains("co-pay"))
        {
            return "A copay (copayment) is a fixed amount a patient pays for a covered service, typically at the time of service. " +
                   "Common copays include $20 for office visits or $10 for generic prescriptions. " +
                   "In FHIR, copays map to Coverage.costToBeneficiary with type 'copay'.";
        }
        
        // Out-of-pocket maximum
        if (lowerQuestion.Contains("out of pocket") || lowerQuestion.Contains("oop") || lowerQuestion.Contains("maximum"))
        {
            return "The out-of-pocket maximum is the most a patient will pay during a plan year. After reaching this limit, " +
                   "insurance pays 100% of covered services. OOP max includes deductibles, coinsurance, and copays. " +
                   "In FHIR, this is Coverage.costToBeneficiary with type 'oopmax'. Accumulators track YTD spending toward this limit.";
        }
        
        // Prior authorization
        if (lowerQuestion.Contains("prior auth") || lowerQuestion.Contains("preauth"))
        {
            return "Prior authorization is approval from insurance required before certain services or medications. " +
                   "Without prior auth, claims may be denied. Common for expensive procedures, specialty drugs, and advanced imaging. " +
                   "In FHIR, this is tracked using CoverageEligibilityRequest and CoverageEligibilityResponse resources.";
        }
        
        // Network providers
        if (lowerQuestion.Contains("network") || lowerQuestion.Contains("in-network") || lowerQuestion.Contains("out-of-network"))
        {
            return "In-network providers have contracts with insurance companies offering discounted rates. Out-of-network providers " +
                   "don't have contracts and typically cost more. PPO plans offer both options; HMO plans usually require in-network only. " +
                   "In FHIR, provider networks are defined in Organization and PractitionerRole resources.";
        }
        
        // CPT codes
        if (lowerQuestion.Contains("cpt") || lowerQuestion.Contains("procedure code"))
        {
            return "CPT (Current Procedural Terminology) codes identify medical procedures and services for billing. " +
                   "Examples: 99213 (office visit), 99291 (critical care), 85025 (blood count). " +
                   "In FHIR, CPT codes map to Claim.item.productOrService and ClaimResponse.item.adjudication.";
        }
        
        // FHIR mapping
        if (lowerQuestion.Contains("fhir") || lowerQuestion.Contains("hl7"))
        {
            return "FHIR (Fast Healthcare Interoperability Resources) is the HL7 standard for healthcare data exchange. " +
                   "Key resources: Coverage (benefits), Claim (submitted claims), ClaimResponse (adjudication results), " +
                   "Patient (member demographics), CoverageEligibilityResponse (eligibility checks). All data in this portal maps to FHIR R4 resources.";
        }
        
        // Claim status
        if (lowerQuestion.Contains("claim status") || lowerQuestion.Contains("adjudicat"))
        {
            return "Claim adjudication is the process of reviewing and processing a claim to determine payment. " +
                   "Statuses include: Submitted, In Review, Approved, Denied, Pending Additional Info. " +
                   "The Claims Engine validates claims against plan rules, provider networks, deductibles, and OOP maximums before approval.";
        }
        
        // Accumulators
        if (lowerQuestion.Contains("accumulator"))
        {
            return "Accumulators track member utilization over an enrollment period (typically calendar year or plan year). " +
                   "They include: deductible met/remaining, OOP met/remaining, service utilization (office visits, hospital days), " +
                   "and prescription spending. Accumulators reset at the start of each new enrollment period. " +
                   "In FHIR, accumulators are represented in CoverageEligibilityResponse.insurance.item.benefit fields.";
        }
        
        // Fraud detection
        if (lowerQuestion.Contains("fraud"))
        {
            return "AI-powered fraud detection analyzes claims for suspicious patterns: unusual amounts (statistical outliers), " +
                   "impossible travel (service location far from member location), high-risk providers, multiple claims same day, " +
                   "geographic anomalies, and claim source inconsistencies. Risk scores range 0-100 with Critical (90+), High (70-89), " +
                   "Medium (40-69), and Low (<40) levels. In FHIR, fraud flags are represented as Flag resources linked to claims.";
        }
        
        // Default response
        return "I can help you understand healthcare benefits, claims processing, FHIR resources, and fraud detection. " +
               "Try asking about deductibles, copays, coinsurance, prior authorization, claims status, or accumulators.";
    }
    
    /// <summary>
    /// Process natural language query and generate benefit configuration
    /// </summary>
    public async Task<BenefitConfigResponse> GetBenefitConfigAsync(string query)
    {
        await Task.Delay(500); // Simulate API call

        var response = new BenefitConfigResponse
        {
            OriginalQuery = query,
            ParsedIntent = DetermineIntent(query),
            ConfigurationSuggestion = GenerateConfiguration(query),
            CPTCodesIdentified = ExtractCPTCodes(query),
            StateRegulations = ExtractStateRegulations(query),
            CMSGuidance = ExtractCMSGuidance(query),
            Confidence = CalculateConfidence(query)
        };

        return response;
    }

    /// <summary>
    /// Determine the intent of the user query
    /// </summary>
    private string DetermineIntent(string query)
    {
        query = query.ToLower();

        if (query.Contains("add") && query.Contains("benefit"))
            return "Add New Benefit";
        if (query.Contains("modify") || query.Contains("update") || query.Contains("change"))
            return "Modify Existing Benefit";
        if (query.Contains("prior auth") || query.Contains("authorization"))
            return "Configure Prior Authorization";
        if (query.Contains("copay") || query.Contains("co-pay"))
            return "Set Copayment";
        if (query.Contains("coinsurance") || query.Contains("co-insurance"))
            return "Set Coinsurance";
        if (query.Contains("deductible"))
            return "Configure Deductible";
        if (query.Contains("state") || query.Contains("regulation"))
            return "Apply State Regulations";
        if (query.Contains("cms") || query.Contains("medicare"))
            return "Apply CMS/Medicare Requirements";

        return "General Benefit Configuration";
    }

    /// <summary>
    /// Generate benefit configuration from natural language
    /// </summary>
    private string GenerateConfiguration(string query)
    {
        var config = new List<string>();

        // Extract copay amounts
        var copayMatch = Regex.Match(query, @"\$(\d+)\s*copay", RegexOptions.IgnoreCase);
        if (copayMatch.Success)
        {
            config.Add($"Copayment: ${copayMatch.Groups[1].Value}");
        }

        // Extract coinsurance percentages
        var coinsuranceMatch = Regex.Match(query, @"(\d+)%\s*coinsurance", RegexOptions.IgnoreCase);
        if (coinsuranceMatch.Success)
        {
            config.Add($"Coinsurance: {coinsuranceMatch.Groups[1].Value}%");
        }

        // Extract deductible amounts
        var deductibleMatch = Regex.Match(query, @"\$?(\d+)\s*deductible", RegexOptions.IgnoreCase);
        if (deductibleMatch.Success)
        {
            config.Add($"Deductible: ${deductibleMatch.Groups[1].Value}");
        }

        // Check for prior authorization
        if (query.Contains("prior auth", StringComparison.OrdinalIgnoreCase) || 
            query.Contains("pre-auth", StringComparison.OrdinalIgnoreCase))
        {
            config.Add("Prior Authorization: Required");
        }

        // Check for benefit types
        if (query.Contains("office visit", StringComparison.OrdinalIgnoreCase))
        {
            config.Add("Service Type: Office Visit");
        }
        if (query.Contains("emergency", StringComparison.OrdinalIgnoreCase))
        {
            config.Add("Service Type: Emergency Department");
        }
        if (query.Contains("imaging", StringComparison.OrdinalIgnoreCase) || 
            query.Contains("mri", StringComparison.OrdinalIgnoreCase) || 
            query.Contains("ct scan", StringComparison.OrdinalIgnoreCase))
        {
            config.Add("Service Type: Diagnostic Imaging");
        }
        if (query.Contains("lab", StringComparison.OrdinalIgnoreCase) || 
            query.Contains("blood test", StringComparison.OrdinalIgnoreCase))
        {
            config.Add("Service Type: Laboratory Services");
        }

        return string.Join("\n", config);
    }

    /// <summary>
    /// Extract CPT codes from query
    /// </summary>
    private List<string> ExtractCPTCodes(string query)
    {
        var codes = new List<string>();
        
        // Match CPT code patterns (5 digits)
        var matches = Regex.Matches(query, @"\b(\d{5})\b");
        
        foreach (Match match in matches)
        {
            var code = match.Groups[1].Value;
            if (CPTCodeDescriptions.ContainsKey(code))
            {
                codes.Add($"{code} - {CPTCodeDescriptions[code]}");
            }
            else
            {
                codes.Add($"{code} - (Verify CPT code validity)");
            }
        }

        // Also check for text descriptions that match known CPT codes
        if (query.Contains("office visit", StringComparison.OrdinalIgnoreCase))
        {
            if (!codes.Any(c => c.Contains("99213")))
                codes.Add("99213 - Office/Outpatient Visit - Established Patient (Low-Moderate Complexity)");
        }

        return codes;
    }

    /// <summary>
    /// Extract state regulations from query
    /// </summary>
    private List<string> ExtractStateRegulations(string query)
    {
        var regulations = new List<string>();

        foreach (var state in StateCMSRegulations.Keys)
        {
            if (query.Contains(state, StringComparison.OrdinalIgnoreCase))
            {
                regulations.Add($"{state}: {StateCMSRegulations[state]}");
            }
        }

        return regulations;
    }

    /// <summary>
    /// Extract CMS guidance from query
    /// </summary>
    private string ExtractCMSGuidance(string query)
    {
        if (query.Contains("cms", StringComparison.OrdinalIgnoreCase) || 
            query.Contains("medicare", StringComparison.OrdinalIgnoreCase))
        {
            return StateCMSRegulations["CMS"];
        }

        // Default CMS guidance for common scenarios
        if (query.Contains("prior auth", StringComparison.OrdinalIgnoreCase))
        {
            return "CMS requires prior authorization decisions within 72 hours for standard requests and 24 hours for expedited requests per 42 CFR §438.210";
        }

        if (query.Contains("imaging", StringComparison.OrdinalIgnoreCase))
        {
            return "CMS National Coverage Determination (NCD) 220.13 applies to diagnostic imaging services";
        }

        return "Refer to CMS National Coverage Determinations (NCDs) and Local Coverage Determinations (LCDs) for specific guidance";
    }

    /// <summary>
    /// Calculate confidence score for the configuration
    /// </summary>
    private double CalculateConfidence(string query)
    {
        double confidence = 0.5; // Base confidence

        // Increase confidence if specific values are found
        if (Regex.IsMatch(query, @"\$\d+"))
            confidence += 0.15; // Dollar amounts

        if (Regex.IsMatch(query, @"\d+%"))
            confidence += 0.15; // Percentages

        if (Regex.IsMatch(query, @"\b\d{5}\b"))
            confidence += 0.1; // CPT codes

        if (query.Contains("state") || query.Contains("cms"))
            confidence += 0.1; // Regulatory references

        return Math.Min(confidence, 0.95); // Cap at 95%
    }

    /// <summary>
    /// Get CPT code suggestions
    /// </summary>
    public List<string> GetCPTCodeSuggestions(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return CPTCodeDescriptions.Select(kvp => $"{kvp.Key} - {kvp.Value}").ToList();

        searchTerm = searchTerm.ToLower();
        return CPTCodeDescriptions
            .Where(kvp => kvp.Key.Contains(searchTerm) || kvp.Value.ToLower().Contains(searchTerm))
            .Select(kvp => $"{kvp.Key} - {kvp.Value}")
            .ToList();
    }

    /// <summary>
    /// Get state regulation information
    /// </summary>
    public string GetStateRegulation(string state)
    {
        return StateCMSRegulations.TryGetValue(state, out var regulation) 
            ? regulation 
            : "No specific state regulation information available";
    }
}
