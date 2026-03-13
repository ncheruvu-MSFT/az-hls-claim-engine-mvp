
# Scripts — external tenant service principal

Creates an app registration/service principal in the **external tenant**, grants RBAC on the AHDS FHIR service, and outputs values for local dev.

Usage:
```powershell
pwsh scripts/setup-external-tenant-sp.ps1   -TenantId <TENANT_GUID>   -SubscriptionId <SUB_ID>   -ResourceGroup <RG>   -WorkspaceName <WS_NAME>   -FhirServiceName <FHIR_NAME>   -RoleName "Healthcare APIs Data Reader"
```

> Choose a role appropriate for your scenario (Reader/Writer/Contributor). Review Azure Healthcare APIs built-in roles in the portal.
