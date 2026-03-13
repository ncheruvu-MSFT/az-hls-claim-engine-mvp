# Test Identity-Based Authentication
# Run this script to verify your Azure CLI identity can access Cosmos DB and Blob Storage

Write-Host "🔐 Testing Identity-Based Authentication" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

# 1. Verify Azure CLI login
Write-Host "1️⃣  Checking Azure CLI authentication..." -ForegroundColor Yellow
try {
    $currentUser = az account show --query user.name -o tsv
    Write-Host "   ✅ Logged in as: $currentUser" -ForegroundColor Green
} catch {
    Write-Host "   ❌ Not logged in to Azure CLI. Run: az login" -ForegroundColor Red
    exit 1
}

# 2. Check subscription
Write-Host "`n2️⃣  Verifying subscription..." -ForegroundColor Yellow
$subscription = az account show --query name -o tsv
Write-Host "   ✅ Using subscription: $subscription" -ForegroundColor Green

# 3. Test Cosmos DB access
Write-Host "`n3️⃣  Testing Cosmos DB access..." -ForegroundColor Yellow
try {
    $cosmosDb = az cosmosdb sql database show `
        --account-name cosmosclaimstest001ncv `
        --resource-group rg-claims-rules-test `
        --name claims `
        --query id -o tsv 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   ✅ Can access Cosmos DB 'claims' database" -ForegroundColor Green
    } else {
        Write-Host "   ⚠️  Cannot access Cosmos DB (may need RBAC role)" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   ❌ Cosmos DB access failed" -ForegroundColor Red
}

# 4. Test Cosmos DB container
Write-Host "`n4️⃣  Testing Cosmos DB 'audit' container..." -ForegroundColor Yellow
try {
    $container = az cosmosdb sql container show `
        --account-name cosmosclaimstest001ncv `
        --resource-group rg-claims-rules-test `
        --database-name claims `
        --name audit `
        --query id -o tsv 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   ✅ Can access 'audit' container" -ForegroundColor Green
    } else {
        Write-Host "   ⚠️  Cannot access audit container" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   ❌ Container access failed" -ForegroundColor Red
}

# 5. Check RBAC assignments
Write-Host "`n5️⃣  Checking RBAC role assignments..." -ForegroundColor Yellow
$userObjectId = az ad signed-in-user show --query id -o tsv

Write-Host "   Your Object ID: $userObjectId" -ForegroundColor Cyan

# Check Cosmos DB roles
$cosmosRoles = az cosmosdb sql role assignment list `
    --account-name cosmosclaimstest001ncv `
    --resource-group rg-claims-rules-test `
    --query "[?principalId=='$userObjectId'].roleDefinitionId" -o tsv

if ($cosmosRoles) {
    Write-Host "   ✅ Cosmos DB role assigned" -ForegroundColor Green
} else {
    Write-Host "   ❌ No Cosmos DB role assigned" -ForegroundColor Red
    Write-Host "      Run: az cosmosdb sql role assignment create --account-name cosmosclaimstest001ncv --resource-group rg-claims-rules-test --role-definition-name 'Cosmos DB Built-in Data Contributor' --principal-id $userObjectId --scope '/'" -ForegroundColor Gray
}

# Check Storage roles
$storageRoles = az role assignment list `
    --assignee $userObjectId `
    --scope "/subscriptions/64e1939f-6460-4656-ad75-dcc277b155f1/resourceGroups/rg-claims-rules-test/providers/Microsoft.Storage/storageAccounts/ahdsstrtest001ncv" `
    --query "[?roleDefinitionName=='Storage Blob Data Contributor'].roleDefinitionName" -o tsv

if ($storageRoles) {
    Write-Host "   ✅ Storage Blob Data Contributor role assigned" -ForegroundColor Green
} else {
    Write-Host "   ❌ No Storage role assigned" -ForegroundColor Red
    Write-Host "      Run: az role assignment create --assignee $userObjectId --role 'Storage Blob Data Contributor' --scope '/subscriptions/64e1939f-6460-4656-ad75-dcc277b155f1/resourceGroups/rg-claims-rules-test/providers/Microsoft.Storage/storageAccounts/ahdsstrtest001ncv'" -ForegroundColor Gray
}

# 6. Check storage account access
Write-Host "`n6️⃣  Testing Storage Account access..." -ForegroundColor Yellow
$storageAccount = az storage account show --name ahdsstrtest001ncv --resource-group rg-claims-rules-test --query name -o tsv 2>&1

if ($LASTEXITCODE -eq 0) {
    Write-Host "   ✅ Can access storage account metadata" -ForegroundColor Green
    
    # Check network rules
    $publicAccess = az storage account show --name ahdsstrtest001ncv --resource-group rg-claims-rules-test --query publicNetworkAccess -o tsv
    Write-Host "   📊 Public Network Access: $publicAccess" -ForegroundColor Cyan
    
    if ($publicAccess -eq "Disabled") {
        Write-Host "   ⚠️  Public access disabled - use Azure Portal to create containers" -ForegroundColor Yellow
    }
} else {
    Write-Host "   ❌ Storage account access failed" -ForegroundColor Red
}

# 7. Check if blob container exists
Write-Host "`n7️⃣  Checking blob container 'contract-audit-archive'..." -ForegroundColor Yellow
try {
    $containerExists = az storage container exists `
        --name contract-audit-archive `
        --account-name ahdsstrtest001ncv `
        --auth-mode login `
        --query exists -o tsv 2>&1
    
    if ($containerExists -eq "true") {
        Write-Host "   ✅ Container 'contract-audit-archive' exists" -ForegroundColor Green
    } else {
        Write-Host "   ⚠️  Container 'contract-audit-archive' does not exist" -ForegroundColor Yellow
        Write-Host "      Create via Azure Portal: Storage Account → Containers → + Container" -ForegroundColor Gray
    }
} catch {
    Write-Host "   ⚠️  Cannot check container (network restrictions)" -ForegroundColor Yellow
    Write-Host "      Create via Azure Portal: Storage Account → Containers → + Container" -ForegroundColor Gray
}

# 8. Test Function App configuration
Write-Host "`n8️⃣  Checking local.settings.json..." -ForegroundColor Yellow
try {
    $localSettings = Get-Content "C:\Git\AZ\azure-claims-rules-full-bundle\azure-claims-rules-mvp-starter\src\ClaimsRules.Api\local.settings.json" | ConvertFrom-Json

    $requiredSettings = @(
        "Cosmos__AccountEndpoint",
        "Cosmos__Database",
        "Cosmos__AuditContainer",
        "BlobStorage__AccountName"
    )

    $missingSettings = @()
    foreach ($setting in $requiredSettings) {
        $value = $localSettings.Values.$setting
        if ($null -eq $value) {
            $missingSettings += $setting
        } else {
            Write-Host "   ✅ $setting configured" -ForegroundColor Green
        }
    }

    if ($missingSettings.Count -gt 0) {
        Write-Host "   ❌ Missing settings: $($missingSettings -join ', ')" -ForegroundColor Red
    } else {
        Write-Host "   ✅ All required settings present" -ForegroundColor Green
    }

    # 9. Check for connection strings (should be removed)
    Write-Host "`n9️⃣  Verifying no connection strings/keys..." -ForegroundColor Yellow
    $hasConnectionString = $null -ne $localSettings.Values.'CosmosDb__ConnectionString'
    $hasAccountKey = $null -ne $localSettings.Values.'BlobStorage__AccountKey'
    $hasBlobConnectionString = $null -ne $localSettings.Values.'BlobStorage__ConnectionString'

    if ($hasConnectionString -or $hasAccountKey -or $hasBlobConnectionString) {
        Write-Host "   ⚠️  Old connection strings/keys still present (should be removed)" -ForegroundColor Yellow
    } else {
        Write-Host "   ✅ No connection strings/keys found (identity-based auth only)" -ForegroundColor Green
    }
} catch {
    Write-Host "   ❌ Error reading local.settings.json: $_" -ForegroundColor Red
}

# Summary
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "📊 Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

if ($cosmosRoles -and $storageRoles) {
    Write-Host "✅ Identity-based authentication is properly configured!" -ForegroundColor Green
    Write-Host "`nYou can now run:" -ForegroundColor Cyan
    Write-Host "  cd ClaimsRules.Api" -ForegroundColor White
    Write-Host "  func start" -ForegroundColor White
    Write-Host "`nDefaultAzureCredential will automatically use your Azure CLI token." -ForegroundColor Cyan
} else {
    Write-Host "⚠️  Some configuration is missing. Review the errors above." -ForegroundColor Yellow
}

Write-Host "`n📚 For detailed setup instructions, see:" -ForegroundColor Cyan
Write-Host "   IDENTITY_BASED_AUTH_SETUP.md" -ForegroundColor White

Write-Host "`n✨ Benefits of identity-based auth:" -ForegroundColor Cyan
Write-Host "   • No secrets in code or configuration" -ForegroundColor White
Write-Host "   • Automatic token rotation by Azure" -ForegroundColor White
Write-Host "   • Same code works locally (CLI) and production (Managed Identity)" -ForegroundColor White
Write-Host "   • Full audit trail via Azure AD" -ForegroundColor White
Write-Host "   • HIPAA and SOC 2 compliant" -ForegroundColor White
