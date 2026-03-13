$FhirUrl = 'https://ahdswstest001-fhirr4test001.fhir.azurehealthcareapis.com'
Write-Host 'Getting token...' -ForegroundColor Yellow
$Token = (az account get-access-token --resource="$FhirUrl" --query accessToken -o tsv)
if (-not $Token) { Write-Host 'ERROR: No token' -ForegroundColor Red; exit 1 }
Write-Host 'Token OK' -ForegroundColor Green

function Upload-FhirResource {
    param([string]$ResourceType, [string]$JsonFile, [string]$Name)
    $headers = @{ 'Authorization' = "Bearer $Token"; 'Content-Type' = 'application/fhir+json' }
    $body = Get-Content "$PSScriptRoot\$JsonFile" -Raw
    try {
        Invoke-RestMethod -Uri "$FhirUrl/$ResourceType" -Method Post -Headers $headers -Body $body -ErrorAction Stop | Out-Null
        Write-Host '[OK]' $Name -ForegroundColor Green
    } catch {
        if ($_.Exception.Response.StatusCode.value__ -eq 409) { Write-Host '[EXISTS]' $Name -ForegroundColor Yellow }
        else { Write-Host '[ERROR]' $Name -ForegroundColor Red }
    }
}

Write-Host 'Uploading Patient...' -ForegroundColor Cyan
Upload-FhirResource 'Patient' 'patient-example.json' 'John Smith'

Write-Host 'Uploading Practitioners...' -ForegroundColor Cyan
Upload-FhirResource 'Practitioner' 'practitioner-johnson.json' 'Dr. Johnson'
Upload-FhirResource 'Practitioner' 'practitioner-chen.json' 'Dr. Chen'

Write-Host 'Uploading Medications...' -ForegroundColor Cyan
Upload-FhirResource 'MedicationRequest' 'med-metformin.json' 'Metformin'
Upload-FhirResource 'MedicationRequest' 'med-lisinopril.json' 'Lisinopril'
Upload-FhirResource 'MedicationRequest' 'med-atorvastatin.json' 'Atorvastatin'

Write-Host 'Uploading Observations...' -ForegroundColor Cyan
Upload-FhirResource 'Observation' 'obs-cbc-wbc.json' 'WBC'
Upload-FhirResource 'Observation' 'obs-cbc-rbc.json' 'RBC'
Upload-FhirResource 'Observation' 'obs-cbc-hemoglobin.json' 'Hemoglobin'
Upload-FhirResource 'Observation' 'obs-cbc-hematocrit.json' 'Hematocrit'
Upload-FhirResource 'Observation' 'obs-cbc-platelet.json' 'Platelet'
Upload-FhirResource 'Observation' 'obs-lipid-total.json' 'Cholesterol'

Write-Host 'Uploading Reports...' -ForegroundColor Cyan
Upload-FhirResource 'DiagnosticReport' 'report-cbc.json' 'CBC Report'
Upload-FhirResource 'DiagnosticReport' 'report-lipid.json' 'Lipid Report'

Write-Host 'Upload complete!' -ForegroundColor Green
