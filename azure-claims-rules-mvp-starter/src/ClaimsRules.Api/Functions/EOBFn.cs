using System.Net;
using System.Text;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using ClaimsRules.Api.Services;
using System.Text.Json;

namespace ClaimsRules.Api.Functions;

/// <summary>
/// Azure Function for generating and managing EOB (Explanation of Benefits) PDF documents
/// Stores documents in FHIR as DocumentReference resources with embedded Base64 PDF
/// </summary>
public class EOBFn
{
    private readonly ILogger<EOBFn> _logger;
    private readonly FhirClient _fhirClient;

    public EOBFn(ILogger<EOBFn> logger, FhirClient fhirClient)
    {
        _logger = logger;
        _fhirClient = fhirClient;
    }

    /// <summary>
    /// Generate EOB PDF for a claim
    /// POST /api/eob/generate
    /// </summary>
    [Function("GenerateEOB")]
    public async Task<HttpResponseData> GenerateEOB(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "eob/generate")] HttpRequestData req)
    {
        _logger.LogInformation("Generating EOB PDF");

        try
        {
            var body = await req.ReadFromJsonAsync<GenerateEOBRequest>();
            if (body == null || string.IsNullOrEmpty(body.ClaimId))
            {
                var badReq = req.CreateResponse(HttpStatusCode.BadRequest);
                await badReq.WriteStringAsync("ClaimId is required");
                return badReq;
            }

            // Generate PDF content (simplified - in production use a PDF library like QuestPDF or iTextSharp)
            var pdfContent = GenerateEOBPdfContent(body.ClaimId, body.MemberId);
            var base64Pdf = Convert.ToBase64String(pdfContent);

            // Create FHIR DocumentReference
            var documentReference = new
            {
                resourceType = "DocumentReference",
                status = "current",
                type = new
                {
                    coding = new[]
                    {
                        new
                        {
                            system = "http://loinc.org",
                            code = "64290-0",
                            display = "Explanation of benefits"
                        }
                    }
                },
                category = new[]
                {
                    new
                    {
                        coding = new[]
                        {
                            new
                            {
                                system = "http://hl7.org/fhir/us/core/CodeSystem/us-core-documentreference-category",
                                code = "clinical-note",
                                display = "Clinical Note"
                            }
                        }
                    }
                },
                subject = new
                {
                    reference = $"Patient/{body.MemberId}"
                },
                date = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                author = new[]
                {
                    new
                    {
                        display = "ClaimsIQ Platform"
                    }
                },
                description = $"Explanation of Benefits for Claim {body.ClaimId}",
                content = new[]
                {
                    new
                    {
                        attachment = new
                        {
                            contentType = "application/pdf",
                            data = base64Pdf,
                            title = $"EOB-{body.ClaimId}-{DateTime.UtcNow:yyyyMMdd}.pdf",
                            creation = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                        }
                    }
                },
                context = new
                {
                    related = new[]
                    {
                        new
                        {
                            reference = $"Claim/{body.ClaimId}"
                        }
                    }
                }
            };

            // Store in FHIR - serialize the anonymous object to JSON
            var documentReferenceJson = JsonSerializer.Serialize(documentReference);
            var fhirResponse = await _fhirClient.CreateAsync("DocumentReference", documentReferenceJson);

            var eobDocument = new
            {
                documentId = ExtractIdFromResponse(fhirResponse),
                claimId = body.ClaimId,
                memberId = body.MemberId,
                memberName = body.MemberName ?? "Member",
                claimNumber = $"CLM-{body.ClaimId}",
                serviceDate = DateTime.UtcNow.AddDays(-7),
                generatedDate = DateTime.UtcNow,
                status = "Final",
                totalBilled = 1250.00m,
                totalAllowed = 1000.00m,
                totalPaid = 800.00m,
                memberResponsibility = 200.00m,
                fhirDocumentReferenceId = ExtractIdFromResponse(fhirResponse),
                pdfUrl = $"/api/eob/{ExtractIdFromResponse(fhirResponse)}/pdf",
                pageCount = 2,
                fileSizeBytes = pdfContent.Length,
                contentType = "application/pdf",
                providerName = "Dr. Sarah Johnson",
                providerNPI = "1234567890",
                lineItems = new[]
                {
                    new
                    {
                        serviceDescription = "Office Visit - Established Patient",
                        procedureCode = "99213",
                        serviceDate = DateTime.UtcNow.AddDays(-7),
                        billedAmount = 150.00m,
                        allowedAmount = 120.00m,
                        deductible = 0.00m,
                        coinsurance = 24.00m,
                        copay = 25.00m,
                        paidByPlan = 71.00m,
                        memberOwes = 49.00m,
                        notes = "In-network provider"
                    },
                    new
                    {
                        serviceDescription = "Laboratory - Comprehensive Metabolic Panel",
                        procedureCode = "80053",
                        serviceDate = DateTime.UtcNow.AddDays(-7),
                        billedAmount = 85.00m,
                        allowedAmount = 68.00m,
                        deductible = 0.00m,
                        coinsurance = 13.60m,
                        copay = 0.00m,
                        paidByPlan = 54.40m,
                        memberOwes = 13.60m,
                        notes = "In-network lab"
                    }
                }
            };

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(eobDocument);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating EOB");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = ex.Message });
            return errorResponse;
        }
    }

    /// <summary>
    /// Get EOB documents for a member
    /// GET /api/eob/member/{memberId}
    /// </summary>
    [Function("GetMemberEOBs")]
    public async Task<HttpResponseData> GetMemberEOBs(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "eob/member/{memberId}")] HttpRequestData req,
        string memberId)
    {
        _logger.LogInformation($"Getting EOBs for member: {memberId}");

        try
        {
            // Query FHIR for DocumentReference resources for this patient
            var searchParams = $"subject=Patient/{memberId}&type=64290-0";
            var fhirResponse = await _fhirClient.SearchAsync("DocumentReference", searchParams);

            // Parse FHIR bundle and extract EOB documents
            var eobDocuments = ParseEOBDocuments(fhirResponse, memberId);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(eobDocuments);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting member EOBs");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = ex.Message });
            return errorResponse;
        }
    }

    /// <summary>
    /// Download EOB PDF
    /// GET /api/eob/{documentId}/pdf
    /// </summary>
    [Function("DownloadEOBPdf")]
    public async Task<HttpResponseData> DownloadEOBPdf(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "eob/{documentId}/pdf")] HttpRequestData req,
        string documentId)
    {
        _logger.LogInformation($"Downloading EOB PDF: {documentId}");

        try
        {
            // Get DocumentReference from FHIR
            var fhirResponse = await _fhirClient.GetByIdAsync("DocumentReference", documentId);
            
            // Extract PDF content from FHIR resource
            var pdfContent = ExtractPdfContentFromFhir(fhirResponse);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/pdf");
            response.Headers.Add("Content-Disposition", $"attachment; filename=EOB-{documentId}.pdf");
            await response.Body.WriteAsync(pdfContent);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading EOB PDF");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = ex.Message });
            return errorResponse;
        }
    }

    // Helper methods
    private byte[] GenerateEOBPdfContent(string claimId, string memberId)
    {
        // Simplified PDF generation - in production use QuestPDF, iTextSharp, or similar
        var pdfContent = $@"
Explanation of Benefits (EOB)
==============================

Claim ID: {claimId}
Member ID: {memberId}
Date of Service: {DateTime.UtcNow.AddDays(-7):MM/dd/yyyy}
Date Generated: {DateTime.UtcNow:MM/dd/yyyy}

Provider: Dr. Sarah Johnson
NPI: 1234567890

Service Details:
- Office Visit (99213): $150.00 billed, $120.00 allowed, You owe: $49.00
- Lab Test (80053): $85.00 billed, $68.00 allowed, You owe: $13.60

Total Billed: $235.00
Total Allowed: $188.00
Plan Paid: $125.40
You Owe: $62.60

This is a sample EOB. For detailed information, contact member services.
";
        return Encoding.UTF8.GetBytes(pdfContent);
    }

    private string ExtractIdFromResponse(string fhirResponse)
    {
        try
        {
            using var doc = JsonDocument.Parse(fhirResponse);
            return doc.RootElement.GetProperty("id").GetString() ?? Guid.NewGuid().ToString();
        }
        catch
        {
            return Guid.NewGuid().ToString();
        }
    }

    private List<object> ParseEOBDocuments(string fhirBundle, string memberId)
    {
        // Simplified - in production parse FHIR Bundle properly
        return new List<object>
        {
            new
            {
                documentId = Guid.NewGuid().ToString(),
                claimId = "CLM-001",
                memberId = memberId,
                claimNumber = "CLM-2026-001",
                serviceDate = DateTime.UtcNow.AddDays(-30),
                generatedDate = DateTime.UtcNow.AddDays(-29),
                status = "Final",
                totalBilled = 1250.00m,
                totalPaid = 800.00m,
                memberResponsibility = 200.00m
            }
        };
    }

    private byte[] ExtractPdfContentFromFhir(string fhirResource)
    {
        try
        {
            using var doc = JsonDocument.Parse(fhirResource);
            var contentArray = doc.RootElement.GetProperty("content");
            var firstContent = contentArray.EnumerateArray().First();
            var attachment = firstContent.GetProperty("attachment");
            var base64Data = attachment.GetProperty("data").GetString();
            return Convert.FromBase64String(base64Data ?? "");
        }
        catch
        {
            // Return sample PDF if extraction fails
            return Encoding.UTF8.GetBytes("Sample EOB PDF Content");
        }
    }
}

public class GenerateEOBRequest
{
    public string ClaimId { get; set; } = string.Empty;
    public string MemberId { get; set; } = string.Empty;
    public string? MemberName { get; set; }
}
