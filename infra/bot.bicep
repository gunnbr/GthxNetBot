// One bot (gthx, othx, ...) on the shared resources from main.bicep:
//
// - User-assigned managed identity that the bot's container runs as, allowed to pull from the registry, read
//   the Key Vault and (granted in the database, see infra/README.md) use the bot's own database
// - The bot's container app (exactly one replica)

targetScope = 'resourceGroup'

param location string
param tags object

@description('The bot\'s name, used in its resource names (id-<name>-<env>, ca-<name>-<env>).')
param name string
param environmentName string

param registryName string
param vaultName string
param containerEnvironmentId string

@description('Image tag to run. The container app is only deployed when this is set and deployApp is true.')
param imageTag string
param deployApp bool

param ircPort int
param ircChannels string
param ircNick string
param ircRealName string

@description('Key Vault secret holding the server name (shared by the bots on the same network).')
#disable-next-line secure-secrets-in-params // the secret's name, not its value
param ircServerSecret string

@description('Key Vault secret holding this bot\'s NickServ password, used for SASL.')
#disable-next-line secure-secrets-in-params // the secret's name, not its value
param ircSaslPasswordSecret string

param useTrustedCa bool
param enableEmail bool

@description('Subject of the error emails, so each bot\'s are easy to tell apart.')
param emailSubject string

param sqlServerFqdn string
param sqlDatabaseName string
param logLevel string

// Built-in role definition IDs
var acrPullRoleId = '7f951dda-4ed3-4680-a7ca-43fe172d538d'
var keyVaultSecretsUserRoleId = '4633458b-17de-408a-b874-0445c86b69e6'

resource registry 'Microsoft.ContainerRegistry/registries@2023-07-01' existing = {
  name: registryName
}

resource vault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: vaultName
}

resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'id-${name}-${environmentName}'
  location: location
  tags: tags
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

resource vaultSecretsUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(vault.id, identity.id, keyVaultSecretsUserRoleId)
  scope: vault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultSecretsUserRoleId)
    principalId: identity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

// Key Vault secrets the app reads. The app's secret names are the same for every bot; the Key Vault names
// (vaultName) can differ.
var baseSecrets = [
  { name: 'irc-server', vaultName: ircServerSecret }
  { name: 'irc-sasl-password', vaultName: ircSaslPasswordSecret }
]
var caSecrets = useTrustedCa ? [{ name: 'irc-trusted-ca', vaultName: 'irc-trusted-ca' }] : []
var emailSecrets = enableEmail
  ? map(
      ['email-from', 'email-to', 'email-server', 'email-port', 'email-username', 'email-password'],
      n => { name: n, vaultName: n }
    )
  : []
var secrets = concat(baseSecrets, caSecrets, emailSecrets)

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
// Passed as text rather than a mounted file, so it doesn't matter which user the bot runs as.
var caEnv = useTrustedCa ? [{ name: 'IrcInfo__TrustedCaCertificate', secretRef: 'irc-trusted-ca' }] : []
var emailEnv = enableEmail
  ? [
      { name: 'EmailConfiguration__FromName', secretRef: 'email-from' }
      { name: 'EmailConfiguration__ToEmail', secretRef: 'email-to' }
      { name: 'EmailConfiguration__MailServer', secretRef: 'email-server' }
      { name: 'EmailConfiguration__Port', secretRef: 'email-port' }
      { name: 'EmailConfiguration__UserName', secretRef: 'email-username' }
      { name: 'EmailConfiguration__Password', secretRef: 'email-password' }
      { name: 'EmailConfiguration__EmailSubject', value: emailSubject }
    ]
  : []

resource app 'Microsoft.App/containerApps@2024-03-01' = if (deployApp && !empty(imageTag)) {
  name: 'ca-${name}-${environmentName}'
  location: location
  tags: tags
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${identity.id}': {}
    }
  }
  properties: {
    managedEnvironmentId: containerEnvironmentId
    configuration: {
      activeRevisionsMode: 'Single'
      registries: [
        {
          server: registry.properties.loginServer
          identity: identity.id
        }
      ]
      secrets: [
        for secret in secrets: {
          name: secret.name
          keyVaultUrl: '${vault.properties.vaultUri}secrets/${secret.vaultName}'
          identity: identity.id
        }
      ]
    }
    template: {
      containers: [
        {
          name: name
          image: '${registry.properties.loginServer}/gthxnetbot:${imageTag}'
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
          env: concat(baseEnv, caEnv, emailEnv)
        }
      ]
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

output identityClientId string = identity.properties.clientId
output identityPrincipalId string = identity.properties.principalId
output identityName string = identity.name
output containerAppName string = 'ca-${name}-${environmentName}'
