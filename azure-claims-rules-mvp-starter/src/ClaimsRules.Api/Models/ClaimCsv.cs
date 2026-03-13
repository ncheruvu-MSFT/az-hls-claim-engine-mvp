
namespace ClaimsRules.Api.Models
{
    public record ClaimCsv(
        string ClaimId,
        string MemberId,
        string ProviderId,
        string PlanId,
        string ServiceDate,
        string DiagnosisCode,
        string ProcedureCode,
        string Units,
        string ChargeAmount
    );
}
