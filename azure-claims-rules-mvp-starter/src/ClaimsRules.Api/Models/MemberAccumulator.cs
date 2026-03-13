namespace ClaimsRules.Api.Models;

public class MemberAccumulator
{
    public string MemberId { get; set; } = string.Empty;
    public string PlanId { get; set; } = string.Empty;
    public int PlanYear { get; set; }
    public decimal DeductibleMet { get; set; }
    public decimal OutOfPocketMet { get; set; }
    public decimal DeductibleRemaining { get; set; }
    public decimal OutOfPocketRemaining { get; set; }
    public bool DeductibleSatisfied { get; set; }
    public bool OutOfPocketMaxReached { get; set; }
    public DateTime? LastUpdated { get; set; }
}
