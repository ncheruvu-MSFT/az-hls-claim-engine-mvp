
param(
  [Parameter(Mandatory=$true)][string]$TenantId,
  [Parameter(Mandatory=$true)][string]$SubscriptionId,
  [Parameter(Mandatory=$true)][string]$ResourceGroup,
  [Parameter(Mandatory=$true)][string]$WorkspaceName,
  [Parameter(Mandatory=$true)][string]$FhirServiceName,
  [Parameter(Mandatory=$false)][string]$AppDisplayName = "claims-rules-external-sp",
  [Parameter(Mandatory=$false)][string]$RoleName = "Healthcare APIs Data Reader"  # choose appropriate role
)

Write-Host "Login to external tenant..."
az login --tenant $TenantId | Out-Null
az account set --subscription $SubscriptionId

# Create app registration + service principal
$app = az ad app create --display-name $AppDisplayName --query '{appId:appId, objectId:id}' -o json | ConvertFrom-Json

$secret = az ad app credential reset --id $app.appId --append --display-name "dev-secret" --query '{clientSecret:password}' -o json | ConvertFrom-Json

# Ensure service principal exists
az ad sp create --id $app.appId | Out-Null

# Resolve FHIR resource id
$fhirId = az resource show --resource-group $ResourceGroup --namespace Microsoft.HealthcareApis --resource-type workspaces/fhirServices --name "$WorkspaceName/$FhirServiceName" --query id -o tsv

# Assign RBAC at FHIR resource scope (adjust RoleName to reader/writer as needed)
az role assignment create --assignee $app.appId --role "$RoleName" --scope $fhirId | Out-Null

# Output JSON for local.settings.json wiring
$endpoint = "https://$FhirServiceName.azurehealthcareapis.com"
$env = @{ 
  FHIR__Endpoint = $endpoint; 
  FHIR__Audience = "https://azurehealthcareapis.com/.default"; 
  AZURE_CLIENT_ID = $app.appId; 
  AZURE_TENANT_ID = $TenantId; 
  CLIENT_SECRET = $secret.clientSecret 
} | ConvertTo-Json -Depth 3

$env | Out-File -FilePath "sp-output.json" -Encoding utf8
Write-Host "Done. See scripts/sp-output.json for values."
