
using System;
using System.Globalization;
using ClaimsRules.Api.Models;

namespace ClaimsRules.Api.Mapping
{
    public static class CsvToFhirMapper
    {
        public static dynamic ToClaim(ClaimCsv csv)
        {
            // Minimal FHIR R4 Claim JSON
            return new
            {
                resourceType = "Claim",
                status = "active",
                type = new { coding = new[] { new { system = "http://terminology.hl7.org/CodeSystem/claim-type", code = "professional" } } },
                use = "claim",
                patient = new { reference = $"Patient/{csv.MemberId}" },
                provider = new { reference = $"Practitioner/{csv.ProviderId}" },
                insurer = new { reference = $"Organization/{csv.PlanId}" },
                created = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                diagnosis = new[] { new { sequence = 1, diagnosisCodeableConcept = new { coding = new[] { new { system = "http://hl7.org/fhir/sid/icd-10", code = csv.DiagnosisCode } } } } },
                item = new[] { new { sequence = 1, productOrService = new { coding = new[] { new { system = "http://www.ama-assn.org/go/cpt", code = csv.ProcedureCode } } }, quantity = new { value = int.Parse(csv.Units) }, unitPrice = new { value = decimal.Parse(csv.ChargeAmount, CultureInfo.InvariantCulture), currency = "USD" } } }
            };
        }
    }
}
