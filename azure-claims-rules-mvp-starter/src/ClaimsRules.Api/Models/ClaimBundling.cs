namespace ClaimsRules.Api.Models;

/// <summary>
/// Result of claim bundling analysis
/// </summary>
public class BundlingResult
{
    public string ClaimId { get; set; } = string.Empty;
    public List<BundledItem> BundledServices { get; set; } = new();
    public List<UnbundlingViolation> Violations { get; set; } = new();
    public decimal TotalAdjustment { get; set; }
    public List<string> Messages { get; set; } = new();
    public bool HasBundling => BundledServices.Any();
    public bool HasViolations => Violations.Any();
}

/// <summary>
/// Service bundled into another service
/// </summary>
public class BundledItem
{
    public int ComponentLineNumber { get; set; }
    public string ComponentCode { get; set; } = string.Empty;
    public string ComponentDescription { get; set; } = string.Empty;
    public int PrimaryLineNumber { get; set; }
    public string PrimaryCode { get; set; } = string.Empty;
    public string PrimaryDescription { get; set; } = string.Empty;
    public string BundlingReason { get; set; } = string.Empty;
    public decimal AdjustedAmount { get; set; }
    public string CciEditType { get; set; } = string.Empty; // "Comprehensive/Component", "Mutually Exclusive", "Bilateral"
}

/// <summary>
/// Detected unbundling violation
/// </summary>
public class UnbundlingViolation
{
    public string ViolationType { get; set; } = string.Empty; // "Component Unbundling", "Bilateral Incorrect", "Modifier Abuse"
    public List<string> InvolvedCodes { get; set; } = new();
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = "Warning"; // "Info", "Warning", "Error"
    public string Recommendation { get; set; } = string.Empty;
}

/// <summary>
/// CCI (Correct Coding Initiative) edit rule
/// </summary>
public class CciEdit
{
    public string ComprehensiveCode { get; set; } = string.Empty; // The "parent" procedure
    public string ComponentCode { get; set; } = string.Empty; // The "child" procedure that bundles
    public bool ModifierAllowed { get; set; } // Can modifier 59 override bundling?
    public string EditRationale { get; set; } = string.Empty;
    public DateTime EffectiveDate { get; set; }
    public DateTime? EndDate { get; set; }
}

/// <summary>
/// Bilateral procedure tracking
/// </summary>
public class BilateralProcedure
{
    public string ProcedureCode { get; set; } = string.Empty;
    public string ProcedureName { get; set; } = string.Empty;
    public bool RequiresModifier50 { get; set; } = true; // Most bilateral procedures require modifier 50
    public decimal SecondSideReduction { get; set; } = 0.50m; // Typically 50% reduction for second side
}

/// <summary>
/// Modifier validation result
/// </summary>
public class ModifierValidation
{
    public int LineNumber { get; set; }
    public string ServiceCode { get; set; } = string.Empty;
    public string Modifier { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public string ValidationMessage { get; set; } = string.Empty;
}
