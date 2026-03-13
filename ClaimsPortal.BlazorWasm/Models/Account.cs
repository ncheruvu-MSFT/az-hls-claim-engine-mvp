namespace ClaimsPortal.BlazorWasm.Models;

/// <summary>
/// Represents an employer account with enrolled members
/// </summary>
public class Account
{
    public string AccountId { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string EmployerName { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public string PlanId { get; set; } = string.Empty;
    public int MemberCount { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalPremium { get; set; }
    public DateTime EffectiveDate { get; set; }
}

/// <summary>
/// Represents a member enrolled under an account
/// </summary>
public class AccountMember
{
    public string AccountId { get; set; } = string.Empty;
    public string MemberId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
}
