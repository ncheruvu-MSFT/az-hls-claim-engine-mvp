# Install required NuGet packages for Entra External ID authentication
# Run this script from the ClaimsPortal.BlazorWasm directory

Write-Host "Installing Microsoft Entra External ID authentication packages..." -ForegroundColor Cyan

# Navigate to project directory
$projectPath = "c:\Git\AZ\azure-claims-rules-full-bundle\ClaimsPortal.BlazorWasm"
Set-Location $projectPath

Write-Host "`nInstalling Microsoft.Authentication.WebAssembly.Msal..." -ForegroundColor Yellow
dotnet add package Microsoft.Authentication.WebAssembly.Msal --version 8.0.0

Write-Host "`nInstalling Microsoft.AspNetCore.Components.WebAssembly.Authentication..." -ForegroundColor Yellow
dotnet add package Microsoft.AspNetCore.Components.WebAssembly.Authentication --version 8.0.0

Write-Host "`nRestoring packages..." -ForegroundColor Yellow
dotnet restore

Write-Host "`nBuilding project to verify installation..." -ForegroundColor Yellow
dotnet build

Write-Host "`n✅ Authentication packages installed successfully!" -ForegroundColor Green
Write-Host "`nNext steps:" -ForegroundColor Cyan
Write-Host "1. Configure your Entra External ID tenants (see ENTRA_EXTERNAL_ID_SETUP.md)" -ForegroundColor White
Write-Host "2. Update appsettings.json with your tenant details" -ForegroundColor White
Write-Host "3. Update Program.cs to register authentication services" -ForegroundColor White
Write-Host "4. Test authentication flows" -ForegroundColor White
