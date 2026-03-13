namespace ClaimsPortal.BlazorWasm.Models;

/// <summary>
/// Represents a claim record from FHIR Claim resource
/// </summary>
public class ClaimRecord
{
    public string ClaimId { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string ProviderId { get; set; } = string.Empty;
    public DateTime ServiceDate { get; set; }
    public DateTime SubmittedDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal ApprovedAmount { get; set; }
    public List<string> DiagnosisCodes { get; set; } = new();
    public List<string> ProcedureCodes { get; set; } = new();
    public string ClaimType { get; set; } = string.Empty;
}
