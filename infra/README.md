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
| `irc-trusted-ca` | `IrcInfo__TrustedCaCertificate` | Only when `useTrustedCa` is true: the PEM text of the CA certificate of a test server with a private certificate (`testing/tls/out/ca.pem`). |
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

## Backups

The database `Gthx` on the Azure SQL server `gthxdb` (in `rg-gthx-prod`, Basic tier, about 45 MB) is backed
up in three ways:

| Backup | Kept | Restores to |
|---|---|---|
| Azure point-in-time restore (automatic: full weekly, differential daily, log every few minutes) | 7 days, the most Basic allows | Any moment in those 7 days |
| Azure long-term retention, monthly (set on 2026-10-28) | 12 months | The time of that backup |
| `.bacpac` export to the Linux PC, nightly (`scripts/gthx-backup`) | 30 days, plus the backup from the 1st of each month for a year | The time of that backup |

Azure's backups are geo-redundant (also copied to the paired region). A `.bacpac` holds the schema and all
the data; one is about 2.5 MB.

### Restoring from Azure

To a new database, then check it before switching anything over (a restore can't overwrite `Gthx`):

    az sql db restore -g rg-gthx-prod -s gthxdb -n Gthx --dest-name Gthx-restored \
        --time 2026-10-28T16:00:00Z --service-objective Basic

From a long-term backup instead: `az sql db ltr-backup list -l westus -s gthxdb -d Gthx`, then
`az sql db ltr-backup restore --backup-id <id> --dest-database Gthx-restored --dest-server gthxdb
--dest-resource-group rg-gthx-prod --service-objective Basic`. A point-in-time restore of `Gthx` took about 50
minutes (2026-10-28), and the new database doesn't appear until it's done.

To use it, either point the bot at it (`sqlDatabaseName` in `main.bicepparam`, then deploy) or rename the
databases in the portal (rename `Gthx` aside first). A restored database has the same users as the
original, so `id-gthx-prod`, `id-gthx-web` and `gthx-backup` keep working. Delete databases you don't keep:
each costs about $5 a month.

### Restoring a local backup

Into the local SQL Server container (`compose.yaml`, port 14333), to test or to look at old data:

    sqlpackage /Action:Import /SourceFile:$HOME/backups/gthx/Gthx-<time>.bacpac \
        /TargetConnectionString:"Server=localhost,14333;Initial Catalog=GthxRestored;User Id=sa;Password=<SQL_SA_PASSWORD>;TrustServerCertificate=True"

Into Azure, as a new database (the import creates it; signed in as the server's Entra admin):

    sqlpackage /Action:Import /SourceFile:$HOME/backups/gthx/Gthx-<time>.bacpac \
        /TargetConnectionString:"Server=tcp:gthxdb.database.windows.net,1433;Initial Catalog=Gthx-restored;Authentication=Active Directory Default;Encrypt=True" \
        /p:DatabaseEdition=Basic /p:DatabaseServiceObjective=Basic

An import doesn't bring the Entra users with it, so create them again (see step 4 of the one-time setup,
and the setup below).

### Setting up the nightly backup

It runs as the app registration `gthx-backup`, which signs in with a certificate kept on the PC. The app
has no Azure roles; in the database it can only read the data and the schema.

1. Install SqlPackage: `dotnet tool install -g microsoft.sqlpackage`.
2. Make a certificate and the app (the private key stays in `~/.config/gthx-backup/`):

       mkdir -p ~/.config/gthx-backup && chmod 700 ~/.config/gthx-backup && cd ~/.config/gthx-backup
       openssl req -x509 -newkey rsa:3072 -nodes -keyout key.pem -out cert.pem -days 730 -subj "/CN=gthx-backup"
       cat key.pem cert.pem > gthx-backup.pem && rm key.pem && chmod 600 gthx-backup.pem
       APP=$(az ad app create --display-name gthx-backup --query appId -o tsv)
       az ad sp create --id $APP
       az ad app credential reset --id $APP --cert @cert.pem --append

   The certificate expires after two years (the current one on 2028-10-27). Make a new one the same way
   and add it with `--append` before then.
3. In the `Gthx` database, as the Entra admin:

       CREATE USER [gthx-backup] FROM EXTERNAL PROVIDER;
       ALTER ROLE db_datareader ADD MEMBER [gthx-backup];
       GRANT VIEW DEFINITION TO [gthx-backup];

4. Write `~/.config/gthx-backup/backup.env` (mode 600):

       BACKUP_APP_ID=<the app's client ID>
       BACKUP_TENANT_ID=<the tenant ID>
       BACKUP_DIR=$HOME/backups/gthx
       # Failure emails, through the bot's SMTP server. Leave SMTP_URL empty to only log.
       SMTP_URL=smtps://<server>:465
       SMTP_USER=<user>
       SMTP_PASSWORD='<password>'   # quote values with spaces or characters such as | # < >
       ALERT_FROM='Name <address>'
       ALERT_TO=<admin address>

5. Run `scripts/gthx-backup` once to check it (to test the failure email, run it with a copy of the
   settings that sets `SQL_DATABASE=NoSuchDb`, through `GTHX_BACKUP_CONFIG`), then turn on the timer (nightly at 03:30, catching up after
   the PC was off):

       mkdir -p ~/.config/systemd/user
       ln -sf "$PWD/scripts/systemd/gthx-backup.service" "$PWD/scripts/systemd/gthx-backup.timer" ~/.config/systemd/user/
       systemctl --user daemon-reload
       systemctl --user enable --now gthx-backup.timer
       sudo loginctl enable-linger "$USER"   # so it runs while nobody is logged in

   The service expects the repo at `~/source/repos/GthxNetBot`. Check on it with
   `systemctl --user list-timers gthx-backup.timer` and `journalctl --user -u gthx-backup`.

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

5. Let GitHub Actions deploy, with OpenID Connect: `infra/setup-github-oidc.sh` creates two app
   registrations. `gthx-github-deploy` trusts only this repo's `production` environment (the approved
   deploy job) and gets Contributor on the resource group and the right to assign only the roles the
   template uses. `gthx-github-build` trusts only the `master` branch (the build job, which runs without
   approval) and gets only AcrPush and Reader on the registry, so nothing unapproved can change Azure.
   Then set the repository variables and secret it prints, and add yourself as a required reviewer of the
   `production` environment.

6. Run the **Build and deploy to Azure** workflow (or push to `master`).

## Switching to Libera

In `main.bicepparam`, set `ircChannels` to the production channel list and `useTrustedCa = false`. Set the
`irc-server` secret to `irc.libera.chat` and `irc-sasl-password` to the Libera NickServ password, then
deploy.
