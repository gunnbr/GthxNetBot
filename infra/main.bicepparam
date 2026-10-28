using 'main.bicep'

// Values that differ per deploy, or that shouldn't be in the repo, come from environment variables.
param imageTag = readEnvironmentVariable('IMAGE_TAG', '')
param deployApp = bool(readEnvironmentVariable('DEPLOY_APP', 'true'))
param vaultAdminPrincipalId = readEnvironmentVariable('VAULT_ADMIN_PRINCIPAL_ID', '')
param budgetContactEmail = readEnvironmentVariable('BUDGET_CONTACT_EMAIL', '')

// Both bots run the same image. The server is the Key Vault secret irc-server; each bot's NickServ password
// is its own secret. To point the bots at a test server with a private certificate, set useTrustedCa = true
// and put its CA in the secret irc-trusted-ca.
param bots = [
  {
    name: 'gthx'
    deployApp: true
    ircChannels: '#reprap,#mobious,#smoothieware,#gthxtest,#dlp3dprinting,#slic3r,#siderala,#reprap.de'
    ircNick: 'gthx'
    ircRealName: 'Gthx Reprap Factoid Bot'
    ircSaslPasswordSecret: 'irc-sasl-password'
    sqlDatabaseName: 'Gthx'
    emailSubject: 'Gthx Error'
  }
  {
    name: 'othx'
    deployApp: true
    ircChannels: '#openscad,#othxtest'
    ircNick: 'othx'
    ircRealName: 'OpenSCAD Factoid Bot'
    ircSaslPasswordSecret: 'othx-irc-sasl-password'
    sqlDatabaseName: 'Othx'
    emailSubject: 'Othx Error'
  }
]
param useTrustedCa = false

// Warnings and errors are emailed. The mail settings are the Key Vault secrets email-*.
param enableEmail = true

param sqlServerFqdn = 'gthxdb.database.windows.net'

param budgetStartDate = '2026-11-01'
