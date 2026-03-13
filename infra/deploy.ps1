# Claims Rules Infrastructure Deployment Script
# Usage: .\deploy.ps1 -ResourceGroup "rg-name" [-Location "westus2"] [-WhatIf]

param(
    [Parameter(Mandatory=$true)]
    [string]$ResourceGroup,
    
    [Parameter(Mandatory=$false)]
    [string]$Location = "westus2",
    
    [Parameter(Mandatory=$false)]
    [switch]$WhatIf,
    
    [Parameter(Mandatory=$false)]
    [string]$ParametersFile = "params.json"
)

$ErrorActionPreference = "Stop"

# Generate deployment name with timestamp
$timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$deploymentName = "claims-rules-infra-$timestamp"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Claims Rules Infrastructure Deployment" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Deployment Name : $deploymentName" -ForegroundColor Yellow
Write-Host "Resource Group  : $ResourceGroup" -ForegroundColor Yellow
Write-Host "Location        : $Location" -ForegroundColor Yellow
Write-Host "Parameters File : $ParametersFile" -ForegroundColor Yellow
Write-Host ""

# Check if resource group exists
$rgExists = az group exists -n $ResourceGroup
if ($rgExists -eq "false") {
    Write-Host "Resource group does not exist. Creating..." -ForegroundColor Green
    az group create -n $ResourceGroup -l $Location
    Write-Host "Resource group created successfully." -ForegroundColor Green
} else {
    Write-Host "Resource group already exists." -ForegroundColor Green
}

Write-Host ""

# Validate template
Write-Host "Validating Bicep template..." -ForegroundColor Green
az deployment group validate `
    -g $ResourceGroup `
    -f main.bicep `
    -p $ParametersFile

if ($LASTEXITCODE -ne 0) {
    Write-Host "Template validation failed!" -ForegroundColor Red
    exit 1
}

Write-Host "Template validation successful." -ForegroundColor Green
Write-Host ""

# Run what-if or actual deployment
if ($WhatIf) {
    Write-Host "Running what-if analysis..." -ForegroundColor Cyan
    az deployment group what-if `
        -g $ResourceGroup `
        -n $deploymentName `
        -f main.bicep `
        -p $ParametersFile
} else {
    Write-Host "Starting deployment..." -ForegroundColor Cyan
    Write-Host "This may take 15-20 minutes. Please wait..." -ForegroundColor Yellow
    Write-Host ""
    
    az deployment group create `
        -g $ResourceGroup `
        -n $deploymentName `
        -f main.bicep `
        -p $ParametersFile
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "========================================" -ForegroundColor Green
        Write-Host "Deployment completed successfully!" -ForegroundColor Green
        Write-Host "========================================" -ForegroundColor Green
        Write-Host ""
        Write-Host "Fetching deployment outputs..." -ForegroundColor Cyan
        
        az deployment group show `
            -g $ResourceGroup `
            -n $deploymentName `
            --query "properties.outputs" `
            -o json | ConvertFrom-Json | ConvertTo-Json -Depth 10
        
        Write-Host ""
        Write-Host "View resources in Azure Portal:" -ForegroundColor Cyan
        Write-Host "https://portal.azure.com/#@/resource/subscriptions/$(az account show --query id -o tsv)/resourceGroups/$ResourceGroup" -ForegroundColor Yellow
    } else {
        Write-Host ""
        Write-Host "Deployment failed! Checking errors..." -ForegroundColor Red
        
        az deployment operation group list `
            -g $ResourceGroup `
            -n $deploymentName `
            --query "[?properties.provisioningState=='Failed'].{Resource:properties.targetResource.resourceName, Error:properties.statusMessage.error.message}" `
            -o table
    }
}

Write-Host ""
Write-Host "Deployment Name: $deploymentName" -ForegroundColor Yellow
Write-Host "To check status: az deployment group show -g $ResourceGroup -n $deploymentName" -ForegroundColor Gray
