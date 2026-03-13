namespace ClaimsPortal.BlazorWasm.Models;

/// <summary>
/// Response from natural language benefit configuration processing
/// </summary>
public class BenefitConfigResponse
{
    public string OriginalQuery { get; set; } = string.Empty;
    public string ParsedIntent { get; set; } = string.Empty;
    public string ConfigurationSuggestion { get; set; } = string.Empty;
    public List<string> CPTCodesIdentified { get; set; } = new();
    public List<string> StateRegulations { get; set; } = new();
    public string CMSGuidance { get; set; } = string.Empty;
    public double Confidence { get; set; }
}
