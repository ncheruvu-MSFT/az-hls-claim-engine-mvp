
// Azure Health Data Services (FHIR R4) + minimal dependencies
// FREE/CONSUMPTION TIER for nonprod testing
// Deploy at resource-group scope: az deployment group create -g <rg> -f infra/main.bicep -p @infra/params.json

param location string
param workspaceName string
param fhirServiceName string
param storageName string
param cosmosAccountName string
param eventGridTopicName string
param functionAppName string
param appInsightsName string
param fabricCapacityName string
param staticWebAppName string = 'claims-portal-swa'
param openAIAccountName string
@description('Optional: Object ID of user/service principal to be Fabric capacity admin. Leave empty to skip Fabric deployment.')
param fabricCapacityAdminId string = ''
@description('Deployment timestamp for forcing new GUIDs on role assignments')
param deploymentTimestamp string = utcNow('yyyyMMddHHmmss')

param fabricLocation string = 'centralus'  // Fabric F2 in Central US

// Workspace
resource workspace 'Microsoft.HealthcareApis/workspaces@2022-12-01' = {
  name: workspaceName
  location: location
}

// FHIR service (R4)
resource fhir 'Microsoft.HealthcareApis/workspaces/fhirServices@2022-12-01' = {
  parent: workspace
  name: fhirServiceName
  location: location
  kind: 'fhir-R4'
  properties: {
    authenticationConfiguration: {
      authority: '${environment().authentication.loginEndpoint}${subscription().tenantId}'
      audience: 'https://${workspaceName}-${fhirServiceName}.fhir.azurehealthcareapis.com'
      smartProxyEnabled: false
    }
    publicNetworkAccess: 'Enabled'
    exportConfiguration: {
      storageAccountName: storageName
    }
  }
}

// Storage account for $export (Data Lake Gen2) + Analytics
resource sa 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: storageName
  location: location
  sku: { name: 'Standard_LRS' }
  kind: 'StorageV2'
  properties: {
    isHnsEnabled: true
    allowBlobPublicAccess: false
    minimumTlsVersion: 'TLS1_2'
  }
}

// Storage containers for analytics/Power BI
resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-01-01' = {
  parent: sa
  name: 'default'
}

resource fhirExportContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-01-01' = {
  parent: blobService
  name: 'fhir-export'
}

resource analyticsContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-01-01' = {
  parent: blobService
  name: 'analytics'
}

resource auditArchiveContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-01-01' = {
  parent: blobService
  name: 'contract-audit-archive'
  properties: {
    publicAccess: 'None'
  }
}

// Cosmos DB for rules/audit (serverless + Synapse Link for Power BI)
resource cosmos 'Microsoft.DocumentDB/databaseAccounts@2023-04-15' = {
  name: cosmosAccountName
  location: location
  kind: 'GlobalDocumentDB'
  properties: {
    databaseAccountOfferType: 'Standard'
    enableFreeTier: false  // Free tier not supported in internal subscriptions
    enableAnalyticalStorage: true  // Enable for Power BI DirectQuery via Synapse Link
    capabilities: [ { name: 'EnableServerless' } ]
    locations: [ { locationName: location } ]
  }
}

// Cosmos database + container
resource cosmosDb 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2023-04-15' = {
  parent: cosmos
  name: 'claims'
  properties: {
    resource: { id: 'claims' }
  }
}

resource cosmosContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2023-04-15' = {
  parent: cosmosDb
  name: 'audit'
  properties: {
    resource: {
      id: 'audit'
      partitionKey: { 
        paths: [ '/id' ]
        kind: 'Hash' 
      }
      analyticalStorageTtl: -1  // Enable Synapse Link for Power BI (infinite retention)
    }
  }
}

// Azure AI Services (Multi-Service) for AI MDM matching
// Includes language models (Phi-3), text analytics, entity extraction
// Cost: Pay-as-you-go, lower cost than OpenAI for patient matching
// Estimated: <$3/month for 1,000 patient matches/month
resource openai 'Microsoft.CognitiveServices/accounts@2023-05-01' = {
  name: openAIAccountName
  location: location  // AIServices available in westus2
  kind: 'AIServices'  // Multi-service includes language models
  sku: {
    name: 'S0'  // Standard tier (pay-as-you-go)
  }
  properties: {
    customSubDomainName: openAIAccountName
    publicNetworkAccess: 'Enabled'
    networkAcls: {
      defaultAction: 'Allow'
    }
  }
}

// Note: AIServices includes built-in language models (Phi-3, text analytics)
// No separate model deployment needed - ready to use via REST API

// Event Grid topic (optional events)
resource topic 'Microsoft.EventGrid/topics@2023-06-01-preview' = {
  name: eventGridTopicName
  location: location
}

// Log Analytics Workspace (use our own to avoid managed workspace deny assignment issues)
resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: '${appInsightsName}-law'
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'  // Pay-as-you-go (free up to 5GB/month for App Insights)
    }
    retentionInDays: 30
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

// Application Insights (FREE tier - first 5GB/month free) linked to our Log Analytics workspace
resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id  // Link to our own workspace (avoids managed workspace issues)
    IngestionMode: 'LogAnalytics'
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

// Hosting plan - Consumption (Y1) - FREE tier (Windows for easier deployment)
resource hostingPlan 'Microsoft.Web/serverfarms@2023-01-01' = {
  name: '${functionAppName}-plan'
  location: location
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
  properties: {
    reserved: false  // Windows
  }
}

// Function App - CONSUMPTION PLAN (FREE tier: 1M executions + 400K GB-s/month)
resource functionApp 'Microsoft.Web/sites@2023-01-01' = {
  name: functionAppName
  location: location
  kind: 'functionapp'  // Windows
  dependsOn: [
    sa
    fhirExportContainer
    analyticsContainer
  ]
  properties: {
    serverFarmId: hostingPlan.id
    httpsOnly: true
    siteConfig: {
      appSettings: [
        { name: 'AzureWebJobsStorage', value: 'DefaultEndpointsProtocol=https;AccountName=${sa.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${sa.listKeys().keys[0].value}' }
        { name: 'FUNCTIONS_EXTENSION_VERSION', value: '~4' }
        { name: 'FUNCTIONS_WORKER_RUNTIME', value: 'dotnet-isolated' }
        { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: appInsights.properties.ConnectionString }
        { name: 'FHIR__Endpoint', value: 'https://${fhirServiceName}.azurehealthcareapis.com' }
        { name: 'FHIR__Audience', value: 'https://azurehealthcareapis.com' }
        { name: 'Cosmos__AccountEndpoint', value: cosmos.properties.documentEndpoint }
        { name: 'Cosmos__Database', value: 'claims' }
        { name: 'Cosmos__AuditContainer', value: 'audit' }
        { name: 'BlobStorage__AccountName', value: sa.name }
        { name: 'EventGrid__TopicEndpoint', value: topic.properties.endpoint }
        { name: 'EventGrid__AccessKey', value: topic.listKeys().key1 }
        { name: 'AzureOpenAI__Endpoint', value: openai.properties.endpoint }
        { name: 'AzureOpenAI__DeploymentName', value: 'gpt-35-turbo' }
      ]
      netFrameworkVersion: 'v10.0'
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
    }
  }
  identity: {
    type: 'SystemAssigned'
  }
}

// RBAC: Grant Function App managed identity access to FHIR
resource fhirRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(fhir.id, functionApp.id, deploymentTimestamp)
  scope: fhir
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '5a1fc7df-4bf1-4951-a576-89034ee01acd') // FHIR Data Contributor
    principalId: functionApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

// RBAC: Grant Function App managed identity access to Cosmos
resource cosmosRoleAssignment 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2023-04-15' = {
  name: guid(cosmos.id, functionApp.id, deploymentTimestamp)
  parent: cosmos
  properties: {
    roleDefinitionId: '${cosmos.id}/sqlRoleDefinitions/00000000-0000-0000-0000-000000000002' // Cosmos DB Built-in Data Contributor
    principalId: functionApp.identity.principalId
    scope: cosmos.id
  }
}

// RBAC: Grant Function App managed identity access to Storage (for file share and blobs)
resource storageRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(sa.id, functionApp.id, deploymentTimestamp, 'StorageBlobDataContributor')
  scope: sa
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe') // Storage Blob Data Contributor
    principalId: functionApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

// RBAC: Grant Function App managed identity Storage Blob Data Owner (required for secrets management with identity-based connections)
resource storageOwnerRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(sa.id, functionApp.id, deploymentTimestamp, 'StorageBlobDataOwner')
  scope: sa
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'b7e6dc6d-f1e8-4753-8033-0f276bb0955b') // Storage Blob Data Owner
    principalId: functionApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

// RBAC: Grant Function App managed identity Storage Account Contributor (for file share access on Windows consumption)
resource storageAccountContributorRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(sa.id, functionApp.id, deploymentTimestamp, 'StorageAccountContributor')
  scope: sa
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '17d1049b-9a84-46fb-8f53-869881c3d3ab') // Storage Account Contributor
    principalId: functionApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

// RBAC: Grant Function App managed identity Storage Queue Data Contributor (required for Azure Functions runtime with identity-based connections)
resource storageQueueRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(sa.id, functionApp.id, deploymentTimestamp, 'StorageQueueDataContributor')
  scope: sa
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '974c5e8b-45b9-4653-ba55-5f855dd0fb88') // Storage Queue Data Contributor
    principalId: functionApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

// RBAC: Grant Function App managed identity Storage Table Data Contributor (for Tables access)
resource storageTableRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(sa.id, functionApp.id, deploymentTimestamp, 'StorageTableDataContributor')
  scope: sa
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3') // Storage Table Data Contributor
    principalId: functionApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

// RBAC: Grant Function App managed identity Cognitive Services OpenAI User (for AI MDM)
resource openaiRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(openai.id, functionApp.id, deploymentTimestamp, 'CognitiveServicesOpenAIUser')
  scope: openai
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd') // Cognitive Services OpenAI User
    principalId: functionApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

// Microsoft Fabric Capacity (F2 SKU - lowest tier ~$524/month)
// NOTE: Use Fabric Trial (60 days free) or existing workspace for testing
// OneLake is included with Fabric capacity - unified data lake across all Fabric workloads
resource fabricCapacity 'Microsoft.Fabric/capacities@2023-11-01' = if (!empty(fabricCapacityAdminId)) {
  name: fabricCapacityName
  location: fabricLocation
  sku: {
    name: 'F2'
    tier: 'Fabric'
  }
  properties: {
    administration: {
      members: [
        fabricCapacityAdminId  // Fabric capacity administrator object ID from parameter
      ]
    }
  }
}

// Fabric Auto-Suspend/Resume Scheduler (saves ~70% on Fabric costs)
// Suspends Fabric at 6 PM EST, resumes at 8 AM EST (weekdays only)
module fabricScheduler 'fabric-scheduler.bicep' = if (!empty(fabricCapacityAdminId)) {
  name: 'fabric-scheduler-deployment'
  params: {
    location: location
    fabricCapacityName: fabricCapacity.name
    fabricResourceGroup: resourceGroup().name
  }
}

// Auto-Shutdown Logic App (stops ALL services at 6 PM EST to avoid costs)
// Stops: Function App, Fabric Capacity (if deployed)
resource shutdownIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'id-shutdown-scheduler'
  location: location
}

// Grant Contributor role to shutdown identity on resource group
resource shutdownRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().id, shutdownIdentity.id, 'Contributor')
  scope: resourceGroup()
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'b24988ac-6180-42a0-ab88-20f7382dd24c') // Contributor
    principalId: shutdownIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

resource shutdownLogicApp 'Microsoft.Logic/workflows@2019-05-01' = {
  name: 'logic-shutdown-services'
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${shutdownIdentity.id}': {}
    }
  }
  properties: {
    state: 'Enabled'
    definition: {
      '$schema': 'https://schema.management.azure.com/providers/Microsoft.Logic/schemas/2016-06-01/workflowdefinition.json#'
      contentVersion: '1.0.0.0'
      triggers: {
        Recurrence: {
          type: 'Recurrence'
          recurrence: {
            frequency: 'Day'
            interval: 1
            schedule: {
              hours: ['18']  // 6 PM EST
              minutes: [0]
            }
            timeZone: 'Eastern Standard Time'
          }
        }
      }
      actions: {
        Check_If_Weekend: {
          type: 'If'
          expression: {
            or: [
              { equals: ['@dayOfWeek(utcNow())', 0] }  // Sunday
              { equals: ['@dayOfWeek(utcNow())', 6] }  // Saturday
            ]
          }
          actions: {
            Terminate_Weekend: {
              type: 'Terminate'
              inputs: {
                runStatus: 'Cancelled'
              }
            }
          }
          runAfter: {}
        }
        Stop_Function_App: {
          type: 'Http'
          inputs: {
            method: 'POST'
            uri: '${environment().resourceManager}subscriptions/${subscription().subscriptionId}/resourceGroups/${resourceGroup().name}/providers/Microsoft.Web/sites/${functionApp.name}/stop?api-version=2023-01-01'
            authentication: {
              type: 'ManagedServiceIdentity'
              identity: shutdownIdentity.id
            }
          }
          runAfter: {
            Check_If_Weekend: ['Succeeded']
          }
        }
        Stop_Fabric_If_Exists: {
          type: 'If'
          expression: {
            not: {
              equals: ['${!empty(fabricCapacityAdminId)}', 'false']
            }
          }
          actions: {
            Stop_Fabric_Capacity: {
              type: 'Http'
              inputs: {
                method: 'POST'
                uri: '${environment().resourceManager}subscriptions/${subscription().subscriptionId}/resourceGroups/${resourceGroup().name}/providers/Microsoft.Fabric/capacities/${fabricCapacityName}/suspend?api-version=2023-11-01'
                authentication: {
                  type: 'ManagedServiceIdentity'
                  identity: shutdownIdentity.id
                }
              }
            }
          }
          runAfter: {
            Stop_Function_App: ['Succeeded']
          }
        }
      }
    }
  }
  dependsOn: [
    shutdownRoleAssignment
  ]
}

// Static Web App for Blazor WASM UI (Free tier)
module staticWebApp 'static-web-app.bicep' = {
  name: 'static-web-app-deployment'
  params: {
    location: location
    staticWebAppName: staticWebAppName
    functionAppName: functionApp.name
  }
}

// Outputs for local development + Power BI/Fabric OneLake
output fhirEndpoint string = 'https://${fhirServiceName}.azurehealthcareapis.com'
output functionAppName string = functionApp.name
output functionAppUrl string = 'https://${functionApp.properties.defaultHostName}'
output staticWebAppUrl string = staticWebApp.outputs.staticWebAppUrl
output cosmosEndpoint string = cosmos.properties.documentEndpoint
output eventGridEndpoint string = topic.properties.endpoint
output eventGridKey string = topic.listKeys().key1
output appInsightsConnectionString string = appInsights.properties.ConnectionString
output storageAccountName string = sa.name
output fabricCapacityName string = !empty(fabricCapacityAdminId) ? fabricCapacity.name : 'Not deployed - set fabricCapacityAdminId parameter to enable'
output fabricCapacityId string = !empty(fabricCapacityAdminId) ? fabricCapacity.id : ''
output fabricScheduler object = !empty(fabricCapacityAdminId) ? {
  pauseLogicAppId: fabricScheduler.outputs.pauseLogicAppId
  resumeLogicAppId: fabricScheduler.outputs.resumeLogicAppId
  schedule: fabricScheduler.outputs.schedule
  monthlyCost: fabricScheduler.outputs.monthlyCost
  estimatedSavings: fabricScheduler.outputs.estimatedSavings
} : {
  pauseLogicAppId: ''
  resumeLogicAppId: ''
  schedule: 'Not deployed'
  monthlyCost: '$0'
  estimatedSavings: '$0'
}
output oneLakeIntegration object = {
  cosmos: {
    endpoint: cosmos.properties.documentEndpoint
    database: 'claims'
    container: 'audit'
    analyticalStore: 'Enabled - use Fabric mirroring or shortcuts'
  }
  azureStorage: {
    accountName: sa.name
    fhirExportContainer: 'fhir-export'
    analyticsContainer: 'analytics'
    connectionString: 'Create OneLake shortcut to these containers'
  }
  fabricWorkspace: {
    capacity: !empty(fabricCapacityAdminId) ? fabricCapacity.name : 'Not deployed'
    dataLake: 'OneLake (unified across all Fabric workloads)'
    orchestration: 'Use Fabric Data Pipelines (not ADF)'
    compute: 'Azure Functions (already deployed)'
  }
}
