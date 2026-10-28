// Azure resources for the IRC bots (gthx, othx), deployed into one resource group. Shared by the bots:
//
// - Log Analytics workspace (with a daily cap) that receives the containers' console output
// - Container registry with the bot image (no registry password)
// - Key Vault holding the bots' secrets (RBAC, no access keys)
// - Container Apps environment
// - Monthly cost budgets with email alerts
//
// And for each bot in `bots`, from bot.bicep: its managed identity and its container app (exactly one replica).
//
// The bots' secrets are never in this file or its parameters. Set them in the Key Vault before deploying a
// bot's app (see infra/README.md); the container apps read them through Key Vault references.

targetScope = 'resourceGroup'

@description('Azure region for all resources.')
param location string = resourceGroup().location

@description('Short name used in the shared resources\' names.')
param appName string = 'gthx'

@description('Environment name used in resource names, e.g. prod.')
param environmentName string = 'prod'

@description('Image tag to run (the git commit SHA). A bot\'s container app is only deployed when this is set and deployApp is true for both the deploy and the bot.')
param imageTag string = ''

@description('Deploy the container apps. Set false for the first deploy, before the image and Key Vault secrets exist.')
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

@description('The bots, each with its own identity, container app and database.')
param bots botSettings[]

// IRC settings that aren't secret, the same for every bot. The server name is a Key Vault secret (irc-server).
param ircPort int = 6697

@description('Trust the CA certificate in the Key Vault secret irc-trusted-ca (PEM text), for a test server with a private certificate. Leave false for Libera.')
param useTrustedCa bool = false

@description('Send warnings and errors by email. Needs the email-* Key Vault secrets.')
param enableEmail bool = false

@description('Fully qualified name of the Azure SQL server. Each bot signs in with its managed identity.')
param sqlServerFqdn string

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

type botSettings = {
  @description('Used in the bot\'s resource names: id-<name>-<env>, ca-<name>-<env>.')
  name: string

  @description('Deploy this bot\'s container app. False creates only its identity (needed before its database user).')
  deployApp: bool

  ircChannels: string
  ircNick: string
  ircRealName: string

  @description('Key Vault secret with the bot\'s NickServ password.')
  ircSaslPasswordSecret: string

  @description('The bot\'s database on the Azure SQL server.')
  sqlDatabaseName: string

  @description('Subject of the bot\'s error emails.')
  emailSubject: string
}

var suffix = uniqueString(resourceGroup().id)
var tags = {
  app: appName
  environment: environmentName
}

// Built-in role definition IDs
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

module bot 'bot.bicep' = [
  for b in bots: {
    name: 'bot-${b.name}'
    params: {
      location: location
      tags: tags
      name: b.name
      environmentName: environmentName
      registryName: registry.name
      vaultName: vault.name
      containerEnvironmentId: containerEnvironment.id
      imageTag: imageTag
      deployApp: deployApp && b.deployApp
      ircPort: ircPort
      ircChannels: b.ircChannels
      ircNick: b.ircNick
      ircRealName: b.ircRealName
      ircServerSecret: 'irc-server'
      ircSaslPasswordSecret: b.ircSaslPasswordSecret
      useTrustedCa: useTrustedCa
      enableEmail: enableEmail
      emailSubject: b.emailSubject
      sqlServerFqdn: sqlServerFqdn
      sqlDatabaseName: b.sqlDatabaseName
      logLevel: logLevel
    }
  }
]

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
output bots array = [
  for (b, i) in bots: {
    name: b.name
    identityName: bot[i].outputs.identityName
    identityClientId: bot[i].outputs.identityClientId
    identityPrincipalId: bot[i].outputs.identityPrincipalId
    containerAppName: bot[i].outputs.containerAppName
  }
]
