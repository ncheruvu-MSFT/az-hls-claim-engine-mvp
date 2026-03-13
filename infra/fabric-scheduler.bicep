// Logic App to suspend/resume Fabric capacity on schedule
// Saves cost by pausing during non-working hours (after 6 PM - before 8 AM EST, and weekends)

param location string
param fabricCapacityName string
param fabricResourceGroup string
param logicAppNamePause string = 'logic-fabric-pause'
param logicAppNameResume string = 'logic-fabric-resume'

// Get reference to existing Fabric capacity in same resource group
resource fabricCapacity 'Microsoft.Fabric/capacities@2023-11-01' existing = {
  name: fabricCapacityName
}

// Managed Identity for Logic Apps to manage Fabric capacity
resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'id-fabric-scheduler'
  location: location
}

// RBAC: Grant Contributor role to managed identity on Fabric capacity
resource fabricRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(fabricCapacity.id, managedIdentity.id, 'Contributor')
  scope: fabricCapacity
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'b24988ac-6180-42a0-ab88-20f7382dd24c') // Contributor
    principalId: managedIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

// Logic App 1: Suspend Fabric at 6 PM EST on weekdays
resource logicAppPause 'Microsoft.Logic/workflows@2019-05-01' = {
  name: logicAppNamePause
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentity.id}': {}
    }
  }
  properties: {
    state: 'Enabled'
    definition: {
      '$schema': 'https://schema.management.azure.com/providers/Microsoft.Logic/schemas/2016-06-01/workflowdefinition.json#'
      contentVersion: '1.0.0.0'
      parameters: {}
      triggers: {
        'Recurrence-6PM-EST': {
          type: 'Recurrence'
          recurrence: {
            frequency: 'Day'
            interval: 1
            timeZone: 'Eastern Standard Time'
            schedule: {
              hours: ['18'] // 6 PM EST
              minutes: [0]
            }
          }
        }
      }
      actions: {
        'Check_If_Weekend': {
          type: 'Compose'
          inputs: '@or(equals(dayOfWeek(utcNow()), 0), equals(dayOfWeek(utcNow()), 6))' // Sunday=0, Saturday=6
          runAfter: {}
        }
        'Condition-Skip_Weekend': {
          type: 'If'
          expression: {
            and: [
              {
                equals: [
                  '@outputs(\'Check_If_Weekend\')'
                  false
                ]
              }
            ]
          }
          actions: {
            'Suspend_Fabric_Capacity': {
              type: 'Http'
              inputs: {
                method: 'POST'
                uri: '${environment().resourceManager}${fabricCapacity.id}/suspend?api-version=2023-11-01'
                authentication: {
                  type: 'ManagedServiceIdentity'
                  identity: managedIdentity.id
                  audience: environment().resourceManager
                }
              }
            }
          }
          runAfter: {
            Check_If_Weekend: ['Succeeded']
          }
        }
      }
      outputs: {}
    }
  }
  dependsOn: [
    fabricRoleAssignment
  ]
}

// Logic App 2: Resume Fabric at 8 AM EST on weekdays
resource logicAppResume 'Microsoft.Logic/workflows@2019-05-01' = {
  name: logicAppNameResume
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentity.id}': {}
    }
  }
  properties: {
    state: 'Enabled'
    definition: {
      '$schema': 'https://schema.management.azure.com/providers/Microsoft.Logic/schemas/2016-06-01/workflowdefinition.json#'
      contentVersion: '1.0.0.0'
      parameters: {}
      triggers: {
        'Recurrence-8AM-EST': {
          type: 'Recurrence'
          recurrence: {
            frequency: 'Day'
            interval: 1
            timeZone: 'Eastern Standard Time'
            schedule: {
              hours: ['8'] // 8 AM EST
              minutes: [0]
            }
          }
        }
      }
      actions: {
        'Check_If_Weekend': {
          type: 'Compose'
          inputs: '@or(equals(dayOfWeek(utcNow()), 0), equals(dayOfWeek(utcNow()), 6))' // Sunday=0, Saturday=6
          runAfter: {}
        }
        'Condition-Skip_Weekend': {
          type: 'If'
          expression: {
            and: [
              {
                equals: [
                  '@outputs(\'Check_If_Weekend\')'
                  false
                ]
              }
            ]
          }
          actions: {
            'Resume_Fabric_Capacity': {
              type: 'Http'
              inputs: {
                method: 'POST'
                uri: '${environment().resourceManager}${fabricCapacity.id}/resume?api-version=2023-11-01'
                authentication: {
                  type: 'ManagedServiceIdentity'
                  identity: managedIdentity.id
                  audience: environment().resourceManager
                }
              }
            }
          }
          runAfter: {
            Check_If_Weekend: ['Succeeded']
          }
        }
      }
      outputs: {}
    }
  }
  dependsOn: [
    fabricRoleAssignment
  ]
}

output managedIdentityId string = managedIdentity.id
output pauseLogicAppId string = logicAppPause.id
output resumeLogicAppId string = logicAppResume.id
output schedule string = 'Pause: 6 PM EST (weekdays), Resume: 8 AM EST (weekdays). Weekends: Always paused.'
output monthlyCost string = 'Logic Apps Consumption: ~$0.10/month (first 4,000 executions free)'
output estimatedSavings string = 'Fabric F2: ~$370/month savings (14hrs/day + weekends paused = 70% savings)'
