
using ClaimsRules.Api.Models;
using ClaimsRules.Api.Mapping;
using Xunit;

public class CsvToFhirMapperTests
{
    [Fact]
    public void MapsMinimalFields()
    {
        var csv = new ClaimCsv("C1","M1","P1","PlanA","2025-11-01","H52.4","99213","1","125.00");
        dynamic claim = CsvToFhirMapper.ToClaim(csv);
        Assert.Equal("Claim", (string)claim.resourceType);
        Assert.Equal("Patient/M1", (string)claim.patient.reference);
    }
}
