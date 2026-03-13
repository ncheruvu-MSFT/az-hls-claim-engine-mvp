# Quick Start Guide: Testing Portals with Demo Authentication
# Before implementing Entra External ID

## Current Demo Authentication

Both Member and Provider portals currently use **demo authentication** for testing purposes.

### Member Portal Demo Login
- **URL**: http://localhost:5090/member-portal
- **Credentials**:
  ```
  Member ID: SUB001
  Last 4 SSN: 1234
  ```
- **Test Features**:
  - Overview Dashboard (spending, activity, care gaps)
  - Benefits Details with FHIR Coverage
  - Claims History
  - Provider Search
  - Documents & Messages

### Provider Portal Demo Login (EPIC-Style)
- **URL**: http://localhost:5090/provider-portal
- **Credentials**:
  ```
  NPI: 1234567890
  Facility: NORTH001
  Access Code: demo123
  ```
- **Test Features**:
  - Patient List Sidebar
  - EPIC-Style Patient Banner
  - Coverage & Eligibility
  - Claims History
  - Medications & Problems
  - FHIR Resource Viewer (5 tabs)

## Migration Path to Entra External ID

### Phase 1: Current State (Demo Auth) ✅
- Simple credential validation
- No token management
- Local session state
- Perfect for UI/UX testing

### Phase 2: Entra External ID Integration (Next Steps)

#### Step 1: Install Authentication Packages
```powershell
cd c:\Git\AZ\azure-claims-rules-full-bundle
.\scripts\install-auth-packages.ps1
```

#### Step 2: Configure Tenants
Follow the comprehensive guide: `ENTRA_EXTERNAL_ID_SETUP.md`

**Quick Configuration Checklist**:
- [ ] Create Member Portal app registration
- [ ] Create Provider Portal app registration  
- [ ] Configure redirect URIs
- [ ] Set up custom attributes (NPI for providers)
- [ ] Update appsettings.json with tenant URLs
- [ ] Update Program.cs with MSAL configuration

#### Step 3: Update Portal Pages

**Member Portal** - Add authentication:
```razor
@attribute [Authorize]
@using Microsoft.AspNetCore.Components.Authorization

<AuthorizeView>
    <Authorized>
        <!-- Existing member portal content -->
    </Authorized>
    <NotAuthorized>
        <RedirectToLogin />
    </NotAuthorized>
</AuthorizeView>
```

**Provider Portal** - Add authentication with NPI validation:
```razor
@attribute [Authorize]
@inject AuthenticationService AuthService

<AuthorizeView>
    <Authorized>
        @if (ValidateProviderNPI(context.User))
        {
            <!-- Existing EPIC-style provider content -->
        }
        else
        {
            <div class="access-denied">
                <h3>⚠️ Access Denied</h3>
                <p>Valid NPI required for provider portal access.</p>
            </div>
        }
    </Authorized>
    <NotAuthorized>
        <RedirectToLogin />
    </NotAuthorized>
</AuthorizeView>
```

#### Step 4: Testing Strategy

**Member Portal Testing**:
1. Register test member account in Entra External ID
2. Add custom attributes (PlanId, SubscriberId)
3. Test sign-in flow
4. Verify claims in user profile
5. Test benefits and claims views

**Provider Portal Testing**:
1. Register test provider account
2. Add NPI custom attribute (10-digit number)
3. Test NPI validation on login
4. Verify patient list access
5. Test FHIR resource viewer

### Phase 3: Production Deployment

#### Security Enhancements:
- [ ] Enable MFA for provider portal (required)
- [ ] Configure Conditional Access policies
- [ ] Set up Azure Key Vault for secrets
- [ ] Enable HIPAA audit logging
- [ ] Configure token lifetimes
- [ ] Set up API authentication

#### Monitoring & Compliance:
- [ ] Azure Application Insights
- [ ] Sign-in logs to Log Analytics
- [ ] Failed authentication alerts
- [ ] Token refresh monitoring
- [ ] HIPAA compliance reporting

## Architecture Comparison

### Demo Authentication (Current)
```
User Input → Simple Validation → Session State → Portal Access
```

### Entra External ID (Production)
```
User Input → Entra Auth → Token Validation → Claims Mapping → 
Custom Attributes (NPI) → Authorization → Portal Access → FHIR API
```

## Benefits of Entra External ID

### For Member Portal:
✅ Self-service account management
✅ Password reset flows
✅ Social login options (Google, Facebook)
✅ Multi-device support
✅ Secure token management
✅ HIPAA-compliant identity storage

### For Provider Portal (EPIC Integration):
✅ NPI-based authentication
✅ Facility/Organization grouping
✅ Role-based access (MD, NP, PA)
✅ SAML/OIDC for EPIC EMR SSO
✅ SMART on FHIR launch support
✅ Credential validation workflows
✅ DEA number verification
✅ State license validation

## EPIC Integration Specifics

### EPIC User Context Mapping
```
EPIC EMR User → Entra External ID
├─ EPIC_USER_ID → extension_EpicUserId
├─ NPI → extension_NPI
├─ DEPARTMENT → extension_FacilityId
├─ PROVIDER_TYPE → extension_CredentialType
└─ SPECIALTY → extension_Specialty
```

### SMART App Launch Flow
1. EPIC EMR launches app with launch token
2. App requests authorization from Entra External ID
3. User authenticates (if not already)
4. App receives access token with patient context
5. FHIR API calls include patient scope

### Sample EPIC SSO Configuration
```xml
<!-- Entra External ID as SAML Identity Provider for EPIC -->
<EntityDescriptor>
  <IDPSSODescriptor>
    <SingleSignOnService 
      Binding="urn:oasis:names:tc:SAML:2.0:bindings:HTTP-Redirect"
      Location="https://<tenant>.ciamlogin.com/saml2" />
  </IDPSSODescriptor>
</EntityDescriptor>
```

## Timeline Estimate

| Phase | Duration | Effort |
|-------|----------|--------|
| Install packages | 10 min | Low |
| Configure Entra tenants | 2-4 hours | Medium |
| Update Portal code | 4-6 hours | Medium |
| Testing & validation | 4-8 hours | High |
| EPIC integration | 8-16 hours | High |
| Production deployment | 2-4 hours | Medium |
| **Total** | **2-3 days** | **Medium-High** |

## Support & Resources

- 📖 **Full Setup Guide**: `ENTRA_EXTERNAL_ID_SETUP.md`
- 🔧 **Installation Script**: `scripts/install-auth-packages.ps1`
- 🏥 **EPIC Documentation**: https://fhir.epic.com/
- 🔐 **Entra External ID Docs**: https://learn.microsoft.com/entra/external-id/

## Current vs. Future State

### Current Demo Features ✅
- Visual design completed
- FHIR resource displays working
- Patient/member data models
- Navigation flows
- EPIC-style UI components

### Pending Implementation 🚧
- Real authentication tokens
- API authorization
- User claims mapping
- NPI validation service
- FHIR API integration with auth headers
- Audit logging
- Token refresh logic

## Quick Decision Matrix

**Use Demo Auth If**:
- Testing UI/UX
- Demo purposes
- Local development
- Proof of concept

**Use Entra External ID If**:
- Production deployment
- Real patient/provider data
- HIPAA compliance required
- EPIC EMR integration needed
- Multi-tenant requirements
- Enterprise security required

---

**Current Status**: Demo authentication active, Entra External ID configuration files created and ready for implementation when needed.
