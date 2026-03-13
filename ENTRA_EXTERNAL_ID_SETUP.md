# Entra External ID Setup Guide

This guide explains how to configure Microsoft Entra External ID for separate Member and Provider portal authentication.

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│           Microsoft Entra External ID Setup                  │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  ┌──────────────────────┐      ┌──────────────────────┐    │
│  │  Member Portal Auth  │      │ Provider Portal Auth │    │
│  │  (Customer Identity) │      │ (Workforce/B2B)      │    │
│  └──────────────────────┘      └──────────────────────┘    │
│           │                              │                   │
│           │                              │                   │
│  ┌────────▼───────────┐        ┌────────▼───────────┐      │
│  │  External ID       │        │  External ID       │      │
│  │  Tenant (Members)  │        │  Tenant (Providers)│      │
│  │  ciamlogin.com     │        │  ciamlogin.com     │      │
│  └────────────────────┘        └────────────────────┘      │
│                                                               │
└─────────────────────────────────────────────────────────────┘
```

## Step 1: Create Entra External ID Tenants

### Option A: Single Tenant with User Flows (Recommended for Testing)
Use one Entra External ID tenant with two separate user flows:
- **Member User Flow**: For patients/subscribers
- **Provider User Flow**: For healthcare providers

### Option B: Separate Tenants (Recommended for Production)
Create two separate Entra External ID tenants:
- **Member Tenant**: `members-healthcare.ciamlogin.com`
- **Provider Tenant**: `providers-healthcare.ciamlogin.com`

## Step 2: Configure Member Portal External ID

### 2.1 Create Member Portal App Registration

1. Navigate to [Microsoft Entra admin center](https://entra.microsoft.com)
2. Go to **Applications** > **App registrations** > **New registration**
3. Configure:
   - **Name**: `Healthcare Member Portal`
   - **Supported account types**: `Accounts in this organizational directory only (External ID)`
   - **Redirect URI**: 
     - Platform: `Single-page application (SPA)`
     - URI: `https://localhost:5090/authentication/login-callback`

### 2.2 Configure Member Portal Authentication

1. Go to **Authentication** blade
2. Add redirect URIs:
   ```
   https://localhost:5090/authentication/login-callback
   https://<your-domain>.azurestaticapps.net/authentication/login-callback
   ```
3. Enable **ID tokens** and **Access tokens**
4. Set **Logout URL**: `https://localhost:5090/`

### 2.3 Define Member Custom Attributes

1. Go to **User attributes** (in External ID)
2. Add custom attributes:
   - `DateOfBirth` (Date)
   - `PlanId` (String)
   - `SubscriberIdassignId` (String)
   - `LastFourSSN` (String - for verification only)

### 2.4 Configure API Permissions

1. Go to **API permissions**
2. Add permissions:
   - `Microsoft Graph` > `User.Read`
   - `Microsoft Graph` > `openid`
   - `Microsoft Graph` > `profile`
   - `Microsoft Graph` > `email`
   - `Microsoft Graph` > `offline_access`

### 2.5 Copy Configuration Values

```json
{
  "Authority": "https://<member-tenant>.ciamlogin.com/<member-tenant>.onmicrosoft.com",
  "ClientId": "<your-member-app-client-id>",
  "TenantId": "<your-tenant-id>"
}
```

## Step 3: Configure Provider Portal External ID (EPIC-Style)

### 3.1 Create Provider Portal App Registration

1. Create new app registration:
   - **Name**: `Healthcare Provider Portal (EPIC)`
   - **Redirect URI**: `https://localhost:5090/authentication/provider-login-callback`

### 3.2 Configure Provider Portal Authentication

Same process as Member Portal, with provider-specific redirect URIs.

### 3.3 Define Provider Custom Attributes (Critical for EPIC Integration)

1. Add custom attributes:
   - `NPI` (String) - **REQUIRED** - National Provider Identifier
   - `FacilityId` (String) - Facility/Organization ID
   - `Specialty` (String) - Medical specialty
   - `DEANumber` (String) - DEA registration
   - `StateLicense` (String) - State medical license
   - `CredentialType` (String) - MD, DO, NP, PA, etc.

### 3.4 Configure Provider API Scopes

Create custom scope for provider access:
1. Go to **Expose an API**
2. Add scope:
   - **Scope name**: `npi_access`
   - **Who can consent**: Admins only
   - **Display name**: Access provider information
   - **Description**: Allows the application to read provider NPI and credentials

## Step 4: Update Blazor Application Configuration

### 4.1 Install NuGet Packages

```bash
cd ClaimsPortal.BlazorWasm
dotnet add package Microsoft.Authentication.WebAssembly.Msal --version 8.0.0
dotnet add package Microsoft.AspNetCore.Components.WebAssembly.Authentication --version 8.0.0
```

### 4.2 Update Program.cs

```csharp
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using ClaimsPortal.BlazorWasm;
using ClaimsPortal.BlazorWasm.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure HttpClient
builder.Services.AddScoped(sp => new HttpClient { 
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) 
});

// Register Member Portal Authentication (Primary)
builder.Services.AddMsalAuthentication(options =>
{
    builder.Configuration.Bind("AzureAdExternalId:MemberPortal", options.ProviderOptions.Authentication);
    options.ProviderOptions.DefaultAccessTokenScopes.Add("openid");
    options.ProviderOptions.DefaultAccessTokenScopes.Add("profile");
    options.ProviderOptions.DefaultAccessTokenScopes.Add("email");
    options.ProviderOptions.DefaultAccessTokenScopes.Add("offline_access");
});

// Register services
builder.Services.AddScoped<AuthenticationService>();
builder.Services.AddScoped<ClaimsApiService>();
builder.Services.AddScoped<NaturalLanguageService>();
builder.Services.AddScoped<HedisCalculationService>();

await builder.Build().RunAsync();
```

### 4.3 Update appsettings.json

Replace placeholders in `wwwroot/appsettings.json`:

```json
{
  "AzureAdExternalId": {
    "MemberPortal": {
      "Authority": "https://memberportal.ciamlogin.com/memberportal.onmicrosoft.com",
      "ClientId": "12345678-1234-1234-1234-123456789abc",
      "ValidateAuthority": true,
      "KnownAuthorities": ["memberportal.ciamlogin.com"]
    },
    "ProviderPortal": {
      "Authority": "https://providerportal.ciamlogin.com/providerportal.onmicrosoft.com",
      "ClientId": "87654321-4321-4321-4321-cba987654321",
      "ValidateAuthority": true,
      "KnownAuthorities": ["providerportal.ciamlogin.com"]
    }
  }
}
```

## Step 5: Update Portal Pages with Authentication

### 5.1 Member Portal with Entra External ID

Add to `Pages/MemberPortal.razor`:

```razor
@using Microsoft.AspNetCore.Components.WebAssembly.Authentication
@using System.Security.Claims
@inject AuthenticationService AuthService
@inject AuthenticationStateProvider AuthenticationStateProvider
@attribute [Authorize]

<AuthorizeView>
    <Authorized>
        <!-- Member portal content -->
        <div class="member-welcome">
            <h1>Welcome, @GetMemberName(context.User)</h1>
            <p>Member ID: @GetMemberId(context.User)</p>
        </div>
    </Authorized>
    <NotAuthorized>
        <RedirectToLogin />
    </NotAuthorized>
</AuthorizeView>

@code {
    private string GetMemberName(ClaimsPrincipal user)
    {
        return user.FindFirst("name")?.Value ?? "Member";
    }
    
    private string GetMemberId(ClaimsPrincipal user)
    {
        return user.FindFirst("extension_SubscriberId")?.Value ?? "N/A";
    }
}
```

### 5.2 Provider Portal (EPIC-Style) with NPI Validation

Add to `Pages/ProviderPortal.razor`:

```razor
@using Microsoft.AspNetCore.Components.WebAssembly.Authentication
@inject AuthenticationService AuthService
@inject AuthenticationStateProvider AuthenticationStateProvider
@attribute [Authorize]

<AuthorizeView>
    <Authorized>
        @if (ValidateNPI(context.User))
        {
            <!-- EPIC-style provider portal content -->
            <div class="epic-header">
                <div class="epic-user-info">
                    <p class="epic-user-name">@GetProviderName(context.User)</p>
                    <p class="epic-user-npi">NPI: @GetProviderNPI(context.User)</p>
                </div>
            </div>
        }
        else
        {
            <div class="error-message">
                <h3>Access Denied</h3>
                <p>Valid NPI required for provider portal access.</p>
            </div>
        }
    </Authorized>
    <NotAuthorized>
        <RedirectToLogin />
    </NotAuthorized>
</AuthorizeView>

@code {
    private bool ValidateNPI(ClaimsPrincipal user)
    {
        var npi = user.FindFirst("extension_NPI")?.Value;
        return !string.IsNullOrEmpty(npi) && npi.Length == 10;
    }
    
    private string GetProviderName(ClaimsPrincipal user)
    {
        return user.FindFirst("name")?.Value ?? "Provider";
    }
    
    private string GetProviderNPI(ClaimsPrincipal user)
    {
        return user.FindFirst("extension_NPI")?.Value ?? "N/A";
    }
}
```

## Step 6: Configure User Sign-Up Flows

### 6.1 Member Portal Sign-Up Flow

1. In Entra External ID portal, go to **User flows**
2. Create new sign-up flow:
   - **Name**: `MemberSignUp`
   - **Identity providers**: Email/Password
   - **Collect attributes**:
     - Email (required)
     - Display Name (required)
     - Date of Birth (required)
     - Plan ID (optional - can be assigned post-registration)

### 6.2 Provider Portal Sign-Up Flow (Admin-Approved)

1. Create new sign-up flow:
   - **Name**: `ProviderSignUp`
   - **Identity providers**: Email/Password
   - **Collect attributes**:
     - Email (required)
     - Display Name (required)
     - NPI (required - with validation)
     - Facility ID (required)
     - Specialty (required)
2. Enable **Admin approval required**
3. Configure approval workflow:
   - Requires NPI verification via NPPES API
   - Facility validation
   - Credentialing check

## Step 7: Testing Authentication

### 7.1 Test Member Portal

```bash
# Run application
dotnet run --project ClaimsPortal.BlazorWasm

# Navigate to Member Portal
# https://localhost:5090/member-portal

# Sign in with test member account
Email: testmember@healthcare.com
Password: (your test password)
```

### 7.2 Test Provider Portal

```bash
# Navigate to Provider Portal
# https://localhost:5090/provider-portal

# Sign in with test provider account
Email: drjohnson@northridge.com
NPI: 1234567890
```

## Step 8: EPIC Integration Features

### 8.1 Single Sign-On (SSO) Configuration

For EPIC EMR integration:
1. Configure SAML 2.0 in Entra External ID
2. Register EPIC as relying party
3. Map EPIC user attributes to Entra claims:
   - `EPIC_USER_ID` → `extension_EpicUserId`
   - `DEPARTMENT` → `extension_FacilityId`
   - `PROVIDER_ID` → `extension_NPI`

### 8.2 Patient Context Sharing (SMART on FHIR)

Enable SMART App Launch:
```json
{
  "smartConfiguration": {
    "authorizationEndpoint": "https://<tenant>.ciamlogin.com/oauth2/v2.0/authorize",
    "tokenEndpoint": "https://<tenant>.ciamlogin.com/oauth2/v2.0/token",
    "capabilities": [
      "launch-standalone",
      "client-confidential-symmetric",
      "context-patient",
      "permission-patient"
    ]
  }
}
```

## Security Best Practices

### Multi-Factor Authentication (MFA)
1. Enable MFA for Provider Portal (required)
2. Optional MFA for Member Portal
3. Configure Conditional Access policies:
   - Require MFA from unknown locations
   - Block legacy authentication
   - Require compliant devices for provider access

### Token Management
- Access token lifetime: 60 minutes
- Refresh token lifetime: 90 days (members), 30 days (providers)
- Enable token encryption in transit

### Audit Logging
Enable Entra ID logs:
- Sign-in logs
- Audit logs
- Risky sign-ins

Export to Azure Monitor/Log Analytics for compliance (HIPAA).

## Troubleshooting

### Common Issues

1. **"AADSTS50011: The reply URL specified in the request does not match"**
   - Verify redirect URIs in app registration
   - Check localhost vs deployed URL

2. **"NPI claim not found"**
   - Ensure custom attribute `extension_NPI` is configured
   - Verify user flow includes NPI attribute

3. **"Correlation ID mismatch"**
   - Clear browser cache
   - Check Authority URL format

### Support Resources

- [Entra External ID Documentation](https://learn.microsoft.com/entra/external-id/)
- [MSAL.js Guide](https://github.com/AzureAD/microsoft-authentication-library-for-js)
- [SMART on FHIR](https://docs.smarthealthit.org/)

## Next Steps

1. Configure production tenants
2. Set up Azure Key Vault for secrets
3. Enable Application Insights for monitoring
4. Configure Azure Front Door for DDoS protection
5. Set up HIPAA compliance logging
