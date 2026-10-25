# Azure deployment

`main.bicep` describes everything the bot needs in one resource group (`rg-gthx-prod`):

| Resource | Name | Purpose |
|---|---|---|
| Log Analytics workspace | `log-gthx-prod` | Receives the container's console output (one JSON object per line). Daily cap 0.15 GB, inside the free 5 GB/month. |
| Managed identity | `id-gthx-prod` | What the container runs as. It pulls the image, reads Key Vault and signs in to Azure SQL, so none of those need a password. |
| Container registry | `crgthx<suffix>` (Basic) | Holds the bot's images. No admin user; the identity has AcrPull. |
| Key Vault | `kv-gthx-<suffix>` | The bot's secrets (RBAC; the identity has Key Vault Secrets User). |
| Container Apps environment and app | `cae-gthx-prod`, `ca-gthx-prod` | Runs exactly one copy of the bot (0.25 vCPU, 0.5 GiB). |
| Budgets | `budget-gthx-prod`, `budget-gthx-prod-logging` | Email alerts for the resource group ($50/month) and for logging ($25/month). |

`main.bicepparam` holds the non-secret settings. Values that change per deploy or shouldn't be in the repo
come from environment variables (`IMAGE_TAG`, `DEPLOY_APP`, `VAULT_ADMIN_PRINCIPAL_ID`, `BUDGET_CONTACT_EMAIL`).

The deploy workflow (`.github/workflows/deploy.yml`) runs the tests, builds and pushes the image, and
deploys the template, on every push to `master`, after approval for the `production` environment.

## Key Vault secrets

The container app reads these through Key Vault references. They must exist before the app is deployed.

| Secret | Setting | Notes |
|---|---|---|
| `irc-server` | `IrcInfo__Server` | Kept out of the repo. |
| `irc-sasl-password` | `IrcInfo__SaslPassword` | The NickServ password for the bot's nick on that server. |
| `irc-trusted-ca` | file `/certs/ca.pem` | Only when `useTrustedCa` is true: the PEM CA certificate of a test server with a private certificate (`testing/tls/out/ca.pem`). |
| `email-from`, `email-to`, `email-server`, `email-port`, `email-username`, `email-password` | `EmailConfiguration__*` | Only when `enableEmail` is true. |

To change one, set a new version and restart the app; it picks up the latest version:

    az keyvault secret set --vault-name <vault> -n irc-sasl-password --value '<new password>'
    az containerapp revision restart -g rg-gthx-prod -n ca-gthx-prod --revision $(az containerapp revision list -g rg-gthx-prod -n ca-gthx-prod --query '[0].name' -o tsv)

## Reading the logs

    scripts/gthx-logs                  # last hour, Information and above
    scripts/gthx-logs -s 1d -l Warning # last day, warnings and errors
    scripts/gthx-logs -f               # stream live

Or open the workspace's **Logs** page in the Azure portal and query `ContainerAppConsoleLogs_CL`, e.g.

    ContainerAppConsoleLogs_CL
    | extend e = parse_json(Log_s)
    | where tostring(e['@l']) in ('Warning', 'Error')
    | project TimeGenerated, Level = tostring(e['@l']), Message = tostring(e['@m']), Exception = tostring(e['@x'])

## One-time setup

1. Register the resource provider and create the resource group:

       az provider register -n Microsoft.App --wait
       az group create -n rg-gthx-prod -l westus --tags app=gthx environment=prod

2. Deploy everything except the app, giving yourself permission to set secrets:

       DEPLOY_APP=false \
       VAULT_ADMIN_PRINCIPAL_ID=$(az ad signed-in-user show --query id -o tsv) \
       BUDGET_CONTACT_EMAIL=<email> \
       az deployment group create -g rg-gthx-prod -n gthx-bootstrap -p infra/main.bicepparam

3. Set the Key Vault secrets (see above). For the CA certificate:

       az keyvault secret set --vault-name <vault> -n irc-trusted-ca --file testing/tls/out/ca.pem

4. Let the app's identity into the database. Make yourself the server's Microsoft Entra admin, then, signed
   in to the database as that admin (for example in the portal's query editor), create a user for the
   identity. The bot applies EF Core migrations on startup, so it needs `db_ddladmin` as well.

       az sql server ad-admin create -g <sql resource group> -s gthxdb \
           --display-name "$(az ad signed-in-user show --query userPrincipalName -o tsv)" \
           --object-id "$(az ad signed-in-user show --query id -o tsv)"

       CREATE USER [id-gthx-prod] FROM EXTERNAL PROVIDER;
       ALTER ROLE db_datareader ADD MEMBER [id-gthx-prod];
       ALTER ROLE db_datawriter ADD MEMBER [id-gthx-prod];
       ALTER ROLE db_ddladmin ADD MEMBER [id-gthx-prod];

5. Let GitHub Actions deploy, with OpenID Connect: `infra/setup-github-oidc.sh` creates an app registration
   that trusts only this repo's `master` branch and `production` environment, and gives it Contributor on
   the resource group, AcrPush on the registry, and the right to assign only the roles the template uses.
   Then set the repository variables and secret it prints, and add yourself as a required reviewer of the
   `production` environment.

6. Run the **Build and deploy to Azure** workflow (or push to `master`).

## Switching to Libera

In `main.bicepparam`, set `ircChannels` to the production channel list and `useTrustedCa = false`. Set the
`irc-server` secret to `irc.libera.chat` and `irc-sasl-password` to the Libera NickServ password, then
deploy.
