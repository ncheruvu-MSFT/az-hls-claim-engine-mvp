# GitHub Repository Setup Guide

This guide walks through setting up a private GitHub repository with CI/CD workflows for the Azure Claims Rules platform.

## Prerequisites

- GitHub account
- Azure subscription with Contributor access
- Azure CLI installed locally
- Git installed locally

## Step 1: Create Private GitHub Repository

```bash
# Login to GitHub CLI (if available)
gh auth login

# Create private repository
gh repo create azure-claims-rules-full-bundle --private --description "Healthcare claims ingestion, rules engine, and analytics platform"

# Or create manually at: https://github.com/new
# Set visibility to PRIVATE
```

## Step 2: Push Code to GitHub

```powershell
# Initialize git repository (if not already done)
cd c:\Git\AZ\azure-claims-rules-full-bundle
git init
git add .
git commit -m "Initial commit: Claims Rules MVP with infra, API, React portal"

# Add remote and push
git remote add origin https://github.com/YOUR_USERNAME/azure-claims-rules-full-bundle.git
git branch -M main
git push -u origin main
```

## Step 3: Setup Azure OIDC Authentication for GitHub Actions

This enables GitHub Actions to deploy to Azure without storing credentials.

```powershell
# Set variables
$subscriptionId = "64e1939f-6460-4656-ad75-dcc277b155f1"
$resourceGroup = "rg-claims-rules-test"
$appName = "github-actions-claims-rules"
$githubOrg = "YOUR_USERNAME"  # Replace with your GitHub username
$githubRepo = "azure-claims-rules-full-bundle"

# Login to Azure
az login
az account set --subscription $subscriptionId

# Create Azure AD application
$appId = az ad app create --display-name $appName --query appId -o tsv

# Create service principal
$spId = az ad sp create --id $appId --query id -o tsv

# Assign Contributor role to resource group
az role assignment create `
  --assignee $appId `
  --role Contributor `
  --scope "/subscriptions/$subscriptionId/resourceGroups/$resourceGroup"

# Create federated credentials for GitHub Actions
# For main branch deployments
az ad app federated-credential create `
  --id $appId `
  --parameters "{
    `"name`": `"github-main-branch`",
    `"issuer`": `"https://token.actions.githubusercontent.com`",
    `"subject`": `"repo:$githubOrg/${githubRepo}:ref:refs/heads/main`",
    `"audiences`": [`"api://AzureADTokenExchange`"]
  }"

# For pull request deployments
az ad app federated-credential create `
  --id $appId `
  --parameters "{
    `"name`": `"github-pull-requests`",
    `"issuer`": `"https://token.actions.githubusercontent.com`",
    `"subject`": `"repo:$githubOrg/${githubRepo}:pull_request`",
    `"audiences`": [`"api://AzureADTokenExchange`"]
  }"

# Get tenant ID
$tenantId = az account show --query tenantId -o tsv

# Display credentials for GitHub Secrets
Write-Host "`n=== Add these secrets to GitHub ===" -ForegroundColor Green
Write-Host "AZURE_CLIENT_ID: $appId"
Write-Host "AZURE_TENANT_ID: $tenantId"
Write-Host "AZURE_SUBSCRIPTION_ID: $subscriptionId"
```

## Step 4: Configure GitHub Secrets

Go to your GitHub repository → Settings → Secrets and variables → Actions

Add these **Repository secrets**:

1. **AZURE_CLIENT_ID**: (from Step 3 output)
2. **AZURE_TENANT_ID**: (from Step 3 output)
3. **AZURE_SUBSCRIPTION_ID**: `64e1939f-6460-4656-ad75-dcc277b155f1`
4. **FABRIC_ADMIN_PRINCIPAL_ID**: (optional, for Fabric capacity deployment)

## Step 5: Update CODEOWNERS

Edit `.github/CODEOWNERS` and replace `@OWNER_USERNAME` with your GitHub username:

```bash
* @YOUR_GITHUB_USERNAME
```

## Step 6: Test Workflows Locally (Optional)

Install Act to test GitHub Actions locally:

```powershell
# Install Act (requires Docker)
choco install act-cli

# Test CI workflow
act pull_request -W .github/workflows/ci.yml
```

## Available Workflows

### 1. **CI - Build & Test** (`ci.yml`)
- **Trigger**: Push/PR to main
- **Actions**: 
  - Builds .NET Function App
  - Runs unit tests
  - Lints Bicep templates
  - Builds React portal
- **Use**: Automated on every commit

### 2. **Deploy Infrastructure** (`deploy-infra.yml`)
- **Trigger**: Manual workflow dispatch
- **Parameters**:
  - Environment: nonprod/prod
  - Deploy Fabric: true/false
- **Actions**: Deploys Bicep templates to Azure
- **Use**: Run when infrastructure changes needed

### 3. **Deploy Function App** (`deploy-function-app.yml`)
- **Trigger**: 
  - Manual dispatch
  - Auto on push to main (Function App changes)
- **Actions**: 
  - Builds .NET app
  - Runs tests
  - Deploys to Azure Functions
  - Tests health endpoint
- **Use**: Auto-deploys on Function App code changes

### 4. **Deploy Static Web App** (`deploy-static-web-app.yml`)
- **Trigger**:
  - Manual dispatch
  - Auto on push to main (React portal changes)
- **Actions**:
  - Builds React app with API URL
  - Deploys to Azure Static Web Apps
  - Tests deployment
- **Use**: Auto-deploys on React code changes

## Running Workflows Manually

1. Go to: `https://github.com/YOUR_USERNAME/azure-claims-rules-full-bundle/actions`
2. Select workflow (e.g., "Deploy Infrastructure")
3. Click "Run workflow"
4. Select environment and options
5. Click green "Run workflow" button

## Local Development (Current Approach)

For now, continue using local deployment:

```powershell
# Deploy infrastructure
cd infra
az deployment group create -g rg-claims-rules-test -f main.bicep -p '@params.json'

# Deploy Function App
cd ..\azure-claims-rules-mvp-starter\src\ClaimsRules.Api
dotnet publish -c Release -o ./output
Compress-Archive -Path ./output/* -DestinationPath function-app.zip -Force
az functionapp deployment source config-zip -g rg-claims-rules-test -n funcclaimstest001ncv --src function-app.zip

# Build React Portal (requires Node.js)
cd ..\..\..\claims-portal
npm install
npm run build
```

## Security Best Practices

1. ✓ Private repository
2. ✓ OIDC authentication (no long-lived credentials)
3. ✓ Branch protection rules (configure in Settings → Branches)
4. ✓ Required reviews for PRs
5. ✓ Secret scanning enabled
6. ✓ Dependabot alerts enabled

## Next Steps

- [ ] Install Node.js locally to complete React deployment
- [ ] Resolve Fabric capacity principal validation issue
- [ ] Test workflows manually once infrastructure is stable
- [ ] Enable branch protection on `main` branch
- [ ] Setup environment-specific deployments (nonprod → prod promotion)

## Troubleshooting

**Workflow fails with "The workflow is requesting a JWT but the GitHub App is not installed"**
- Verify federated credentials are created correctly
- Check subject matches: `repo:ORG/REPO:ref:refs/heads/main`

**Deployment fails with "Authorization failed"**
- Verify service principal has Contributor role on resource group
- Check AZURE_CLIENT_ID secret matches the app registration

**Static Web App deployment fails**
- Verify Node.js version matches (18.x)
- Check build output directory is `build/`
- Ensure API URL environment variable is set correctly

## Resources

- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [Azure OIDC with GitHub](https://learn.microsoft.com/en-us/azure/developer/github/connect-from-azure)
- [Azure Static Web Apps Deploy Action](https://github.com/Azure/static-web-apps-deploy)
