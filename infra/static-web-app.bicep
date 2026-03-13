// Static Web App for Claims Portal
param location string
param staticWebAppName string
param functionAppName string

resource staticWebApp 'Microsoft.Web/staticSites@2023-01-01' = {
  name: staticWebAppName
  location: location
  sku: {
    name: 'Free'
    tier: 'Free'
  }
  properties: {
    allowConfigFileUpdates: true
    buildProperties: {
      skipGithubActionWorkflowGeneration: true
    }
  }
}

// Note: Linked backends require Standard tier. For Free tier,
// the Blazor WASM app calls the Function App directly via HTTP/CORS.

output staticWebAppUrl string = staticWebApp.properties.defaultHostname
output staticWebAppId string = staticWebApp.id
