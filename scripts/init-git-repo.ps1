# Initialize Git repository and prepare for GitHub
# Run this script from the root of the workspace

Write-Host "=== Azure Claims Rules - GitHub Setup ===" -ForegroundColor Cyan
Write-Host ""

# Check if git is installed
if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
    Write-Host "ERROR: Git is not installed. Please install Git first." -ForegroundColor Red
    exit 1
}

# Check if already initialized
if (Test-Path ".git") {
    Write-Host "Git repository already initialized." -ForegroundColor Yellow
    $continue = Read-Host "Do you want to continue? (y/n)"
    if ($continue -ne 'y') {
        exit 0
    }
} else {
    # Initialize git repository
    Write-Host "Initializing Git repository..." -ForegroundColor Green
    git init
    Write-Host "✓ Git initialized" -ForegroundColor Green
}

# Configure git
Write-Host ""
Write-Host "Configuring Git..." -ForegroundColor Green
$userName = git config user.name
$userEmail = git config user.email

if (-not $userName) {
    $userName = Read-Host "Enter your Git username"
    git config user.name $userName
}

if (-not $userEmail) {
    $userEmail = Read-Host "Enter your Git email"
    git config user.email $userEmail
}

Write-Host "✓ Git configured for: $userName <$userEmail>" -ForegroundColor Green

# Check for local.settings.json (should be ignored)
$localSettings = Get-ChildItem -Recurse -Filter "local.settings.json" -ErrorAction SilentlyContinue
if ($localSettings.Count -gt 0) {
    Write-Host ""
    Write-Host "WARNING: Found local.settings.json files. These contain secrets and will be ignored by Git." -ForegroundColor Yellow
    foreach ($file in $localSettings) {
        Write-Host "  - $($file.FullName)" -ForegroundColor Yellow
    }
}

# Add all files
Write-Host ""
Write-Host "Staging files for commit..." -ForegroundColor Green
git add .

# Check status
Write-Host ""
Write-Host "Git status:" -ForegroundColor Cyan
git status --short

# Create initial commit
Write-Host ""
$commitMessage = "Initial commit: Claims Rules MVP with infra, API, React portal, CI/CD workflows"
Write-Host "Creating initial commit..." -ForegroundColor Green
git commit -m $commitMessage

if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ Initial commit created" -ForegroundColor Green
} else {
    Write-Host "No changes to commit or commit failed" -ForegroundColor Yellow
}

# Get current branch
$currentBranch = git branch --show-current
if ($currentBranch -ne "main") {
    Write-Host ""
    Write-Host "Renaming branch to 'main'..." -ForegroundColor Green
    git branch -M main
}

Write-Host ""
Write-Host "=== Next Steps ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. Create a PRIVATE GitHub repository:"
Write-Host "   - Go to: https://github.com/new"
Write-Host "   - Name: azure-claims-rules-full-bundle"
Write-Host "   - Visibility: PRIVATE"
Write-Host ""
Write-Host "2. Add remote and push:"
Write-Host '   git remote add origin https://github.com/YOUR_USERNAME/azure-claims-rules-full-bundle.git'
Write-Host '   git push -u origin main'
Write-Host ""
Write-Host "3. Setup Azure OIDC authentication:"
Write-Host "   - Follow instructions in SETUP-GITHUB.md"
Write-Host "   - Run the PowerShell script to create service principal"
Write-Host "   - Add secrets to GitHub repository"
Write-Host ""
Write-Host "4. (Optional) Test workflows:"
Write-Host "   - Go to Actions tab in GitHub"
Write-Host "   - Manually run 'CI - Build & Test' workflow"
Write-Host ""
Write-Host "For detailed instructions, see: SETUP-GITHUB.md" -ForegroundColor Green
Write-Host ""
