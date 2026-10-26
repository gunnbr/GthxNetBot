using 'main.bicep'

// Values that differ per deploy, or that shouldn't be in the repo, come from environment variables.
param imageTag = readEnvironmentVariable('IMAGE_TAG', '')
param deployApp = bool(readEnvironmentVariable('DEPLOY_APP', 'true'))
param vaultAdminPrincipalId = readEnvironmentVariable('VAULT_ADMIN_PRINCIPAL_ID', '')
param budgetContactEmail = readEnvironmentVariable('BUDGET_CONTACT_EMAIL', '')

// Until Libera lifts the ban, the bot connects to the home test server (its name is the Key Vault secret
// irc-server), whose certificate comes from the private CA in testing/tls/.
param ircChannels = '#gthxtestchannel'
param useTrustedCa = true

// Warnings and errors are emailed. The mail settings are the Key Vault secrets email-*.
param enableEmail = true

param sqlServerFqdn = 'gthxdb.database.windows.net'
param sqlDatabaseName = 'Gthx'

param budgetStartDate = '2026-11-01'
