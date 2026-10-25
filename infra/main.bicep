// Azure resources for the gthx IRC bot, deployed into one resource group:
//
// - Log Analytics workspace (with a daily cap) that receives the container's console output
// - User-assigned managed identity that the container runs as
// - Container registry, pulled from by that identity (no registry password)
// - Key Vault holding the bot's secrets, read by that identity (RBAC, no access keys)
// - Container Apps environment and the bot's container app (exactly one replica)
// - Monthly cost budgets with email alerts
//
// The bot's secrets are never in this file or its parameters. Set them in the Key Vault before the first
// deploy with deployApp = true (see infra/README.md); the container app reads them through Key Vault
// references.

targetScope = 'resourceGroup'

@description('Azure region for all resources.')
param location string = resourceGroup().location

@description('Short name used in resource names.')
param appName string = 'gthx'

@description('Environment name used in resource names, e.g. prod.')
param environmentName string = 'prod'

@description('Image tag to run (the git commit SHA). The container app is only deployed when this is set and deployApp is true.')
param imageTag string = ''

@description('Deploy the container app. Set false for the first deploy, before the image and Key Vault secrets exist.')
param deployApp bool = true

@description('Object ID of a user or group that may manage Key Vault secrets (e.g. the person setting them). Optional.')
param vaultAdminPrincipalId string = ''

@description('Principal type of vaultAdminPrincipalId.')
@allowed([
  'User'
  'Group'
  'ServicePrincipal'
])
param vaultAdminPrincipalType string = 'User'

// IRC settings that aren't secret. The server name is a Key Vault secret (irc-server).
param ircPort int = 6697
param ircChannels string
param ircNick string = 'gthx'
param ircRealName string = 'Gthx Reprap Factoid Bot'

@description('Mount the Key Vault secret irc-trusted-ca (a PEM CA certificate) and trust it, for a test server with a private certificate. Leave false for Libera.')
param useTrustedCa bool = false

@description('Send warnings and errors by email. Needs the email-* Key Vault secrets.')
param enableEmail bool = false

@description('Fully qualified name of the Azure SQL server. The app signs in with its managed identity.')
param sqlServerFqdn string

@description('Database name on the Azure SQL server.')
param sqlDatabaseName string

@description('Minimum Serilog level written to the console (and so to Log Analytics).')
@allowed([
  'Verbose'
  'Debug'
  'Information'
  'Warning'
  'Error'
])
param logLevel string = 'Information'

@description('Daily Log Analytics ingestion cap in GB. 0.15 GB/day stays inside the 5 GB/month free allowance.')
param logDailyCapGb string = '0.15'

@description('Log retention in days (up to 31 days is included in the ingestion price).')
param logRetentionDays int = 30

@description('First day of the month the budgets start, yyyy-MM-dd. Must not change after the first deploy.')
param budgetStartDate string

@description('Monthly budget in USD for everything in this resource group.')
param monthlyBudget int = 50

@description('Monthly budget in USD for logging (the Log Analytics workspace).')
param loggingBudget int = 25

@description('Email address for budget alerts. No budgets are created when empty.')
param budgetContactEmail string = ''

var suffix = uniqueString(resourceGroup().id)
var tags = {
  app: appName
  environment: environmentName
}

// Built-in role definition IDs
var acrPullRoleId = '7f951dda-4ed3-4680-a7ca-43fe172d538d'
var keyVaultSecretsUserRoleId = '4633458b-17de-408a-b874-0445c86b69e6'
var keyVaultSecretsOfficerRoleId = 'b86a8fe4-44ce-4948-aee5-eccb2c155cd7'

resource workspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: 'log-${appName}-${environmentName}'
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: logRetentionDays
    workspaceCapping: {
      dailyQuotaGb: json(logDailyCapGb)
    }
  }
}

resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'id-${appName}-${environmentName}'
  location: location
  tags: tags
}

resource registry 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: 'cr${appName}${suffix}'
  location: location
  tags: tags
  sku: {
    name: 'Basic'
  }
  properties: {
    adminUserEnabled: false
  }
}

resource registryPull 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(registry.id, identity.id, acrPullRoleId)
  scope: registry
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', acrPullRoleId)
    principalId: identity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

resource vault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: 'kv-${appName}-${suffix}'
  location: location
  tags: tags
  properties: {
    tenantId: subscription().tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 90
  }
}

resource vaultSecretsUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(vault.id, identity.id, keyVaultSecretsUserRoleId)
  scope: vault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultSecretsUserRoleId)
    principalId: identity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

resource vaultSecretsOfficer 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(vaultAdminPrincipalId)) {
  name: guid(vault.id, vaultAdminPrincipalId, keyVaultSecretsOfficerRoleId)
  scope: vault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultSecretsOfficerRoleId)
    principalId: vaultAdminPrincipalId
    principalType: vaultAdminPrincipalType
  }
}

resource containerEnvironment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: 'cae-${appName}-${environmentName}'
  location: location
  tags: tags
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: workspace.properties.customerId
        sharedKey: workspace.listKeys().primarySharedKey
      }
    }
  }
}

// Key Vault secrets the app reads. Each becomes a container app secret of the same name.
var baseSecretNames = [
  'irc-server'
  'irc-sasl-password'
]
var caSecretNames = useTrustedCa ? ['irc-trusted-ca'] : []
var emailSecretNames = enableEmail
  ? [
      'email-from'
      'email-to'
      'email-server'
      'email-port'
      'email-username'
      'email-password'
    ]
  : []
var secretNames = concat(baseSecretNames, caSecretNames, emailSecretNames)

var baseEnv = [
  { name: 'IrcInfo__Server', secretRef: 'irc-server' }
  { name: 'IrcInfo__Port', value: string(ircPort) }
  { name: 'IrcInfo__UseSsl', value: 'true' }
  { name: 'IrcInfo__Channels', value: ircChannels }
  { name: 'IrcInfo__Nick', value: ircNick }
  { name: 'IrcInfo__RealName', value: ircRealName }
  { name: 'IrcInfo__SaslPassword', secretRef: 'irc-sasl-password' }
  {
    name: 'ConnectionStrings__GthxDb'
    value: 'Server=tcp:${sqlServerFqdn},1433;Initial Catalog=${sqlDatabaseName};Authentication=Active Directory Managed Identity;User Id=${identity.properties.clientId};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
  }
  { name: 'ConnectionStrings__GthxDb_Type', value: 'sqlserver' }
  // One JSON object per line, so Log Analytics queries can filter on level and properties
  { name: 'Serilog__MinimumLevel__Default', value: logLevel }
  { name: 'Serilog__WriteTo__0__Name', value: 'Console' }
  {
    name: 'Serilog__WriteTo__0__Args__formatter'
    value: 'Serilog.Formatting.Compact.RenderedCompactJsonFormatter, Serilog.Formatting.Compact'
  }
]
var caEnv = useTrustedCa ? [{ name: 'IrcInfo__TrustedCaCertificate', value: '/certs/ca.pem' }] : []
var emailEnv = enableEmail
  ? [
      { name: 'EmailConfiguration__FromName', secretRef: 'email-from' }
      { name: 'EmailConfiguration__ToEmail', secretRef: 'email-to' }
      { name: 'EmailConfiguration__MailServer', secretRef: 'email-server' }
      { name: 'EmailConfiguration__Port', secretRef: 'email-port' }
      { name: 'EmailConfiguration__UserName', secretRef: 'email-username' }
      { name: 'EmailConfiguration__Password', secretRef: 'email-password' }
      { name: 'EmailConfiguration__EmailSubject', value: 'Gthx Error' }
    ]
  : []

resource app 'Microsoft.App/containerApps@2024-03-01' = if (deployApp && !empty(imageTag)) {
  name: 'ca-${appName}-${environmentName}'
  location: location
  tags: tags
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${identity.id}': {}
    }
  }
  properties: {
    managedEnvironmentId: containerEnvironment.id
    configuration: {
      activeRevisionsMode: 'Single'
      registries: [
        {
          server: registry.properties.loginServer
          identity: identity.id
        }
      ]
      secrets: [
        for name in secretNames: {
          name: name
          keyVaultUrl: '${vault.properties.vaultUri}secrets/${name}'
          identity: identity.id
        }
      ]
    }
    template: {
      containers: [
        {
          name: appName
          image: '${registry.properties.loginServer}/gthxnetbot:${imageTag}'
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
          env: concat(baseEnv, caEnv, emailEnv)
          volumeMounts: useTrustedCa
            ? [
                {
                  volumeName: 'certs'
                  mountPath: '/certs'
                }
              ]
            : []
        }
      ]
      volumes: useTrustedCa
        ? [
            {
              name: 'certs'
              storageType: 'Secret'
              secrets: [
                {
                  secretRef: 'irc-trusted-ca'
                  path: 'ca.pem'
                }
              ]
            }
          ]
        : []
      // An IRC bot must run as exactly one copy: two would fight over the nick.
      scale: {
        minReplicas: 1
        maxReplicas: 1
      }
    }
  }
  dependsOn: [
    registryPull
    vaultSecretsUser
  ]
}

resource budget 'Microsoft.Consumption/budgets@2023-11-01' = if (!empty(budgetContactEmail)) {
  name: 'budget-${appName}-${environmentName}'
  properties: {
    category: 'Cost'
    amount: monthlyBudget
    timeGrain: 'Monthly'
    timePeriod: {
      startDate: budgetStartDate
    }
    notifications: {
      actual80: {
        enabled: true
        operator: 'GreaterThan'
        threshold: 80
        thresholdType: 'Actual'
        contactEmails: [budgetContactEmail]
      }
      forecast100: {
        enabled: true
        operator: 'GreaterThan'
        threshold: 100
        thresholdType: 'Forecasted'
        contactEmails: [budgetContactEmail]
      }
    }
  }
}

resource loggingBudgetResource 'Microsoft.Consumption/budgets@2023-11-01' = if (!empty(budgetContactEmail)) {
  name: 'budget-${appName}-${environmentName}-logging'
  properties: {
    category: 'Cost'
    amount: loggingBudget
    timeGrain: 'Monthly'
    timePeriod: {
      startDate: budgetStartDate
    }
    filter: {
      dimensions: {
        name: 'ResourceId'
        operator: 'In'
        values: [workspace.id]
      }
    }
    notifications: {
      actual50: {
        enabled: true
        operator: 'GreaterThan'
        threshold: 50
        thresholdType: 'Actual'
        contactEmails: [budgetContactEmail]
      }
      forecast100: {
        enabled: true
        operator: 'GreaterThan'
        threshold: 100
        thresholdType: 'Forecasted'
        contactEmails: [budgetContactEmail]
      }
    }
  }
}

output registryLoginServer string = registry.properties.loginServer
output registryName string = registry.name
output keyVaultName string = vault.name
output workspaceName string = workspace.name
output workspaceCustomerId string = workspace.properties.customerId
output identityClientId string = identity.properties.clientId
output identityPrincipalId string = identity.properties.principalId
output identityName string = identity.name
output containerAppName string = 'ca-${appName}-${environmentName}'
