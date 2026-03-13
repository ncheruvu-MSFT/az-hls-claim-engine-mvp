using System.Net.Http.Json;

namespace ClaimsPortal.BlazorWasm.Services;

/// <summary>
/// Service for generating and managing Explanation of Benefits (EOB) PDF documents
/// Stores PDFs as FHIR DocumentReference resources
/// </summary>
public class EOBDocumentService
{
    private readonly HttpClient _http;
    private readonly string _apiBaseUrl;
    private readonly FhirDataService _fhirService;

    public EOBDocumentService(HttpClient http, IConfiguration configuration, FhirDataService fhirService)
    {
        _http = http;
        _apiBaseUrl = configuration["ApiBaseUrl"] ?? "http://localhost:7071/api";
        _fhirService = fhirService;
    }

    /// <summary>
    /// Generate EOB PDF for a claim and store in FHIR as DocumentReference
    /// </summary>
    public async Task<EOBDocument?> GenerateEOBPdfAsync(string claimId, string memberId)
    {
        try
        {
            // Call backend API to generate PDF
            var request = new { claimId, memberId };
            var response = await _http.PostAsJsonAsync($"{_apiBaseUrl}/eob/generate", request);
            response.EnsureSuccessStatusCode();
            
            var eobDocument = await response.Content.ReadFromJsonAsync<EOBDocument>();
            return eobDocument;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error generating EOB PDF: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Get all EOB documents for a member
    /// </summary>
    public async Task<List<EOBDocument>> GetMemberEOBsAsync(string memberId)
    {
        try
        {
            return await _http.GetFromJsonAsync<List<EOBDocument>>($"{_apiBaseUrl}/eob/member/{memberId}") ?? new();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting member EOBs: {ex.Message}");
            return new();
        }
    }

    /// <summary>
    /// Get EOB document by ID
    /// </summary>
    public async Task<EOBDocument?> GetEOBByIdAsync(string documentId)
    {
        try
        {
            return await _http.GetFromJsonAsync<EOBDocument>($"{_apiBaseUrl}/eob/{documentId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting EOB document: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Download EOB PDF content
    /// </summary>
    public async Task<byte[]?> DownloadEOBPdfAsync(string documentId)
    {
        try
        {
            var response = await _http.GetAsync($"{_apiBaseUrl}/eob/{documentId}/pdf");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsByteArrayAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error downloading EOB PDF: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Generate preview URL for PDF (data URI)
    /// </summary>
    public string GetPdfPreviewUrl(byte[] pdfContent)
    {
        var base64 = Convert.ToBase64String(pdfContent);
        return $"data:application/pdf;base64,{base64}";
    }
}

/// <summary>
/// EOB Document model
/// </summary>
public class EOBDocument
{
    public string DocumentId { get; set; } = string.Empty;
    public string ClaimId { get; set; } = string.Empty;
    public string MemberId { get; set; } = string.Empty;
    public string MemberName { get; set; } = string.Empty;
    public string ClaimNumber { get; set; } = string.Empty;
    public DateTime ServiceDate { get; set; }
    public DateTime GeneratedDate { get; set; }
    public string Status { get; set; } = "Final";
    public decimal TotalBilled { get; set; }
    public decimal TotalAllowed { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal MemberResponsibility { get; set; }
    public string FhirDocumentReferenceId { get; set; } = string.Empty;
    public string PdfUrl { get; set; } = string.Empty;
    public int PageCount { get; set; }
    public long FileSizeBytes { get; set; }
    public string ContentType { get; set; } = "application/pdf";
    
    // Claim details for display
    public List<EOBLineItem> LineItems { get; set; } = new();
    public string ProviderName { get; set; } = string.Empty;
    public string ProviderNPI { get; set; } = string.Empty;
}

public class EOBLineItem
{
    public string ServiceDescription { get; set; } = string.Empty;
    public string ProcedureCode { get; set; } = string.Empty;
    public DateTime ServiceDate { get; set; }
    public decimal BilledAmount { get; set; }
    public decimal AllowedAmount { get; set; }
    public decimal Deductible { get; set; }
    public decimal Coinsurance { get; set; }
    public decimal Copay { get; set; }
    public decimal PaidByPlan { get; set; }
    public decimal MemberOwes { get; set; }
    public string Notes { get; set; } = string.Empty;
}
