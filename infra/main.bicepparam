using 'main.bicep'

// Values that differ per deploy, or that shouldn't be in the repo, come from environment variables.
param imageTag = readEnvironmentVariable('IMAGE_TAG', '')
param deployApp = bool(readEnvironmentVariable('DEPLOY_APP', 'true'))
param vaultAdminPrincipalId = readEnvironmentVariable('VAULT_ADMIN_PRINCIPAL_ID', '')
param budgetContactEmail = readEnvironmentVariable('BUDGET_CONTACT_EMAIL', '')

// The server is the Key Vault secret irc-server. To point the bot at a test server with a
// private certificate, set useTrustedCa = true and put its CA in the secret irc-trusted-ca.
param ircChannels = '#reprap,#mobious,#smoothieware,#gthxtest,#dlp3dprinting,#slic3r,#siderala,#reprap.de'
param useTrustedCa = false

// Warnings and errors are emailed. The mail settings are the Key Vault secrets email-*.
param enableEmail = true

param sqlServerFqdn = 'gthxdb.database.windows.net'
param sqlDatabaseName = 'Gthx'

param budgetStartDate = '2026-11-01'
