using ClaimsRules.Api.Models;

namespace ClaimsRules.Api.Services;

/// <summary>
/// Claim bundling service implementing CCI (Correct Coding Initiative) edits
/// Prevents unbundling and duplicate payment for component services
/// </summary>
public class ClaimBundlingService
{
    // Sample CCI edits (normally loaded from CMS CCI database)
    private static readonly List<CciEdit> _cciEdits = new()
    {
        // Colonoscopy with biopsy - biopsy included in colonoscopy
        new() { ComprehensiveCode = "45380", ComponentCode = "88305", ModifierAllowed = false, 
                EditRationale = "Biopsy pathology included in colonoscopy with biopsy" },
        
        // Upper endoscopy combinations
        new() { ComprehensiveCode = "43239", ComponentCode = "43235", ModifierAllowed = false,
                EditRationale = "Diagnostic endoscopy included when therapeutic endoscopy performed" },
        
        // Arthroscopy procedures
        new() { ComprehensiveCode = "29881", ComponentCode = "29870", ModifierAllowed = false,
                EditRationale = "Diagnostic arthroscopy included in surgical arthroscopy" },
        new() { ComprehensiveCode = "29881", ComponentCode = "29877", ModifierAllowed = true,
                EditRationale = "Component may be separate with modifier 59 for different compartment" },
        
        // E/M with procedures same day
        new() { ComprehensiveCode = "99213", ComponentCode = "96372", ModifierAllowed = true,
                EditRationale = "E/M separate from injection with modifier 25 if significant separately identifiable" },
        
        // Imaging combinations
        new() { ComprehensiveCode = "71046", ComponentCode = "71045", ModifierAllowed = false,
                EditRationale = "2-view chest X-ray includes 1-view" },
        
        // Lab panels
        new() { ComprehensiveCode = "80053", ComponentCode = "82947", ModifierAllowed = false,
                EditRationale = "Glucose included in comprehensive metabolic panel" },
        new() { ComprehensiveCode = "80053", ComponentCode = "84132", ModifierAllowed = false,
                EditRationale = "Potassium included in comprehensive metabolic panel" },
    };

    // Bilateral procedures with typical reimbursement rules
    private static readonly Dictionary<string, BilateralProcedure> _bilateralProcedures = new()
    {
        ["29881"] = new() { ProcedureCode = "29881", ProcedureName = "Knee arthroscopy", RequiresModifier50 = true, SecondSideReduction = 0.50m },
        ["27447"] = new() { ProcedureCode = "27447", ProcedureName = "Total knee arthroplasty", RequiresModifier50 = true, SecondSideReduction = 0.50m },
        ["66984"] = new() { ProcedureCode = "66984", ProcedureName = "Cataract surgery", RequiresModifier50 = true, SecondSideReduction = 0.50m },
    };

    /// <summary>
    /// Apply bundling rules to claim services
    /// </summary>
    public BundlingResult ApplyBundling(List<ClaimService> services)
    {
        var result = new BundlingResult
        {
            ClaimId = "CLAIM-" + DateTime.UtcNow.Ticks
        };

        // Sort by charge amount descending (comprehensive procedures first)
        var sortedServices = services.OrderByDescending(s => s.ChargeAmount).ToList();

        // Check for CCI edit violations
        for (int i = 0; i < sortedServices.Count; i++)
        {
            for (int j = i + 1; j < sortedServices.Count; j++)
            {
                var primary = sortedServices[i];
                var component = sortedServices[j];

                // Check if component bundles into primary
                var edit = FindCciEdit(primary.ProcedureCode, component.ProcedureCode);
                
                if (edit != null)
                {
                    // Check if modifier 59 present (unbundling modifier)
                    bool hasUnbundlingModifier = component.Modifiers.Contains("59") || component.Modifiers.Contains("X{EPSU}");
                    
                    if (hasUnbundlingModifier && edit.ModifierAllowed)
                    {
                        // Modifier appropriately used - services are separate
                        result.Messages.Add($"✓ Modifier 59 appropriate for {component.ProcedureCode} - services not bundled");
                    }
                    else if (hasUnbundlingModifier && !edit.ModifierAllowed)
                    {
                        // Modifier incorrectly used
                        result.Violations.Add(new UnbundlingViolation
                        {
                            ViolationType = "Modifier Abuse",
                            InvolvedCodes = new() { primary.ProcedureCode, component.ProcedureCode },
                            Description = $"Modifier 59 not allowed for CCI edit {primary.ProcedureCode}/{component.ProcedureCode}",
                            Severity = "Error",
                            Recommendation = "Remove modifier 59 or provide documentation for separate encounter"
                        });
                    }
                    else
                    {
                        // Component should bundle into comprehensive
                        result.BundledServices.Add(new BundledItem
                        {
                            ComponentLineNumber = component.LineNumber,
                            ComponentCode = component.ProcedureCode,
                            ComponentDescription = component.Description,
                            PrimaryLineNumber = primary.LineNumber,
                            PrimaryCode = primary.ProcedureCode,
                            PrimaryDescription = primary.Description,
                            BundlingReason = edit.EditRationale,
                            CciEditType = "Comprehensive/Component",
                            AdjustedAmount = component.ChargeAmount * component.Quantity
                        });
                        
                        result.TotalAdjustment += component.ChargeAmount * component.Quantity;
                        result.Messages.Add($"Bundled: {component.ProcedureCode} into {primary.ProcedureCode} - ${component.ChargeAmount:F2} adjusted");
                    }
                }
            }
        }

        // Check for bilateral procedure issues
        var bilateralIssues = DetectBilateralIssues(services);
        result.Violations.AddRange(bilateralIssues);

        // Check for component unbundling patterns
        var unbundlingPatterns = DetectUnbundlingPatterns(services);
        result.Violations.AddRange(unbundlingPatterns);

        // Summary message
        if (result.BundledServices.Any())
        {
            result.Messages.Add($"⚠️ {result.BundledServices.Count} service(s) bundled - total adjustment ${result.TotalAdjustment:F2}");
        }
        else
        {
            result.Messages.Add("✓ No bundling adjustments required");
        }

        if (result.Violations.Any())
        {
            result.Messages.Add($"⚠️ {result.Violations.Count} potential coding violation(s) detected");
        }

        return result;
    }

    /// <summary>
    /// Find CCI edit between two codes
    /// </summary>
    private CciEdit? FindCciEdit(string code1, string code2)
    {
        // Check if code2 is component of code1
        var edit = _cciEdits.FirstOrDefault(e => 
            e.ComprehensiveCode == code1 && e.ComponentCode == code2);
        
        if (edit != null) return edit;

        // Check reverse (code1 might be component of code2)
        return _cciEdits.FirstOrDefault(e => 
            e.ComprehensiveCode == code2 && e.ComponentCode == code1);
    }

    /// <summary>
    /// Detect bilateral procedure issues
    /// </summary>
    private List<UnbundlingViolation> DetectBilateralIssues(List<ClaimService> services)
    {
        var violations = new List<UnbundlingViolation>();

        // Group services by procedure code and date
        var procedureGroups = services
            .GroupBy(s => new { s.ProcedureCode, s.ServiceDate })
            .Where(g => g.Count() > 1);

        foreach (var group in procedureGroups)
        {
            if (_bilateralProcedures.TryGetValue(group.Key.ProcedureCode, out var bilateral))
            {
                // Check if modifier 50 or RT/LT used
                bool hasModifier50 = group.Any(s => s.Modifiers.Contains("50"));
                bool hasRTLT = group.Any(s => s.Modifiers.Contains("RT") || s.Modifiers.Contains("LT"));

                if (!hasModifier50 && !hasRTLT)
                {
                    violations.Add(new UnbundlingViolation
                    {
                        ViolationType = "Bilateral Incorrect",
                        InvolvedCodes = new() { group.Key.ProcedureCode },
                        Description = $"Bilateral procedure {bilateral.ProcedureName} ({group.Key.ProcedureCode}) billed twice without modifier 50 or RT/LT",
                        Severity = "Warning",
                        Recommendation = "Use modifier 50 for bilateral procedures or RT/LT for separate sides"
                    });
                }
            }
        }

        return violations;
    }

    /// <summary>
    /// Detect unbundling patterns
    /// </summary>
    private List<UnbundlingViolation> DetectUnbundlingPatterns(List<ClaimService> services)
    {
        var violations = new List<UnbundlingViolation>();

        // Pattern 1: Multiple E/M codes same day (usually not appropriate)
        var emCodes = services.Where(s => s.ProcedureCode.StartsWith("992") && s.ProcedureCode.Length == 5)
            .GroupBy(s => s.ServiceDate)
            .Where(g => g.Count() > 1)
            .ToList();

        foreach (var group in emCodes)
        {
            violations.Add(new UnbundlingViolation
            {
                ViolationType = "Component Unbundling",
                InvolvedCodes = group.Select(s => s.ProcedureCode).ToList(),
                Description = $"Multiple E/M codes billed on {group.Key:yyyy-MM-dd}",
                Severity = "Warning",
                Recommendation = "Verify separate encounters or use appropriate modifier 25"
            });
        }

        // Pattern 2: Lab components of panels
        var labServices = services.Where(s => IsLabCode(s.ProcedureCode)).ToList();
        if (labServices.Any(s => s.ProcedureCode == "80053")) // CMP panel
        {
            var cmpComponents = labServices.Where(s => new[] { "82947", "84132", "82565", "84520" }.Contains(s.ProcedureCode)).ToList();
            
            if (cmpComponents.Any())
            {
                violations.Add(new UnbundlingViolation
                {
                    ViolationType = "Component Unbundling",
                    InvolvedCodes = new List<string> { "80053" }.Concat(cmpComponents.Select(c => c.ProcedureCode)).ToList(),
                    Description = "Individual lab tests billed with comprehensive metabolic panel",
                    Severity = "Error",
                    Recommendation = "Remove individual tests included in panel (glucose, potassium, creatinine, urea)"
                });
            }
        }

        return violations;
    }

    /// <summary>
    /// Check if code is a lab test
    /// </summary>
    private bool IsLabCode(string code)
    {
        // Lab codes typically in range 80047-89398
        if (code.Length == 5 && int.TryParse(code, out int numericCode))
        {
            return numericCode >= 80000 && numericCode <= 89999;
        }
        return false;
    }

    /// <summary>
    /// Validate modifiers
    /// </summary>
    public List<ModifierValidation> ValidateModifiers(List<ClaimService> services)
    {
        var validations = new List<ModifierValidation>();

        foreach (var service in services)
        {
            foreach (var modifier in service.Modifiers)
            {
                var validation = new ModifierValidation
                {
                    LineNumber = service.LineNumber,
                    ServiceCode = service.ProcedureCode,
                    Modifier = modifier,
                    IsValid = true
                };

                switch (modifier)
                {
                    case "25": // Significant, separately identifiable E/M
                        if (!service.ProcedureCode.StartsWith("992"))
                        {
                            validation.IsValid = false;
                            validation.ValidationMessage = "Modifier 25 only applicable to E/M codes";
                        }
                        break;

                    case "50": // Bilateral procedure
                        if (!_bilateralProcedures.ContainsKey(service.ProcedureCode))
                        {
                            validation.IsValid = false;
                            validation.ValidationMessage = $"Procedure {service.ProcedureCode} not typically bilateral";
                        }
                        break;

                    case "59": // Distinct procedural service
                        // Check if modifier 59 is supported by CCI edit
                        validation.ValidationMessage = "Verify modifier 59 appropriateness with documentation";
                        break;

                    case "RT": // Right side
                    case "LT": // Left side
                        validation.ValidationMessage = "Anatomical modifier - verify correct side";
                        break;

                    case "76": // Repeat procedure same physician
                    case "77": // Repeat procedure different physician
                        validation.ValidationMessage = "Verify medical necessity documentation for repeat procedure";
                        break;
                }

                validations.Add(validation);
            }
        }

        return validations;
    }

    /// <summary>
    /// Get bundling summary for claim
    /// </summary>
    public Dictionary<string, object> GetBundlingSummary(BundlingResult result)
    {
        return new Dictionary<string, object>
        {
            ["Total Services"] = result.BundledServices.Count + result.Violations.Count,
            ["Services Bundled"] = result.BundledServices.Count,
            ["Bundling Adjustment"] = result.TotalAdjustment,
            ["Violations Detected"] = result.Violations.Count,
            ["Critical Violations"] = result.Violations.Count(v => v.Severity == "Error"),
            ["Warnings"] = result.Violations.Count(v => v.Severity == "Warning")
        };
    }
}
