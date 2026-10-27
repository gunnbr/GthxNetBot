# GthxNetBot Roadmap

Status legend: ⬜ not started · 🟡 in progress · ✅ done

| # | Phase | Status |
|---|-------|--------|
| 1 | Finish updating to .NET 10 | ✅ |
| 2 | Add SASL support | ✅ |
| 3 | Deploy as a locally running Docker container on the Linux PC | ✅ |
| 4 | Logging that's accessible once deployed to Azure | ✅ |
| 5 | Switch to deploy to Azure | ✅ |
| 6 | Automatic version numbers suited to CI/CD | ✅ |
| 7 | Build on GitHub with build/test status badges on the main page | ✅ |
| 8 | Automatically deploy to Azure from GitHub | ✅ |
| 9 | Fix known security problems | 🟡 |
| 10 | Fix reported bugs | 🟡 |
| 11 | Implement new features | 🟡 |
| 12 | Look after the Gthx.Data repository (`gunnbr/GthxNetData`) | 🟡 |
| 13 | Bring the documentation up to date | 🟡 |
| 14 | Verify the database backups, and back up to the Linux PC on a schedule | ⬜ |
| 15 | Clean up the old branches in this repo | ⬜ |
| 16 | Move othx to Azure | ⬜ |

**Where development happens:** phases 1–2 on the Windows dev machine; phase 3 onward on the Linux PC (which has Docker).

**Next up:** phase 5 is done: since 2026-10-27 the bot runs in Azure Container Apps as `gthx` on Libera, and the 2022 `gthx2.26` container is retired. Next are phase 14 (check the backups and keep a copy at home), the security settings in phase 9 that are the owner's call, and the feature requests in phase 11. The soak traffic (`testing/traffic/`) is stopped; to use it again, point it at a test bot on the local server, never at Libera.

**Testing rule:** never run test bots or probe clients against Libera (we were banned there on 2026-10-24 after phase 3 test runs; the ban was lifted on 2026-10-26). Only the production bot connects to Libera. Use the local InspIRCd test server on sakura: `10.0.0.125:6667` plaintext, or `10.0.0.125:6697` TLS (certificate from the private CA in `testing/tls/`).

## 1. Finish updating to .NET 10 — ✅

Done:
- All projects target `net10.0`; Dockerfile uses the 10.0 SDK/runtime images; solution is `.slnx`.
- Solution builds (0 errors). All 30 tests pass, including the SQL Server Testcontainers tests (verified with Podman on Windows using the `docker_engine` pipe).
- `podman build` of the Dockerfile succeeds.

Merged in [#41](https://github.com/gunnbr/GthxNetBot/pull/41) and [GthxNetData#3](https://github.com/gunnbr/GthxNetData/pull/3) on 2026-10-24:
- [x] Packages updated: EF Core 10.0.13, `Microsoft.Extensions.*` 10.0.13, `Microsoft.AspNetCore.TestHost` 10.0.13, Serilog 4.4 and its 10.x integrations and current sinks, Aspire 13.6.4, `Microsoft.NET.Test.Sdk` 18.11, NUnit 4.6.1 + adapter 6.3.1, coverlet 10.1. Tests use `ClassicAssert`.
- [x] `DotNet.Testcontainers` 1.6.0 replaced with `Testcontainers.MsSql` 4.16.0.
- [x] **MariaDB support dropped** (decided 2026-10-24) because Pomelo has no EF Core 10 release. Removed the `MariaDbMigrations` project and Pomelo; `GthxDb_Type` values other than `sqlserver` now fail at startup with a clear error.
- [x] `System.Security.Cryptography.Pkcs` pinned to 10.0.13 (EF Core 10 / Aspire pulled in vulnerable versions).
- [x] Removed the redundant `System.Threading.Tasks.Dataflow` reference; replaced the obsolete `WebHostBuilder` in tests and `IrcRawMessageEventArgs.Message` in the bot.
- [x] Builds with 0 errors; all 30 tests pass.
- Remaining minor warnings, left alone: lowercase `init` migration class name (renaming a migration is risky), and an unused mock event.

**Follow-up — othx:** the `othx` deployment uses MariaDB. It must stay on the current release until its data is ported to SQL Server (plan this before or during phase 3/5).

## 2. Add SASL support — ✅

- `GthxIrcClient`: SASL PLAIN using the nick plus `SaslPassword` (falls back to `Password`), TLS on by default (port 6697), CAP/SASL diagnostic logging, faster connect-failure handling.
- AppHost passes `sasl-password` as a secret parameter; the bot has a `UserSecretsId`.
- SASL support lives in the `gunnbr/IrcDotNet` fork ([#2](https://github.com/gunnbr/IrcDotNet/pull/2)), included as a git submodule and project reference. Upstream IrcDotNet looks abandoned, so the fork is the source of truth.
- Login verified against Libera as `gthxtest`. Merged in [#40](https://github.com/gunnbr/GthxNetBot/pull/40) on 2026-10-24.

## 3. Local Docker container on the Linux PC — ✅
The local container did its job: it proved the new build on Linux and in Docker, against the real data, over SASL and TLS, with a long soak. Production goes to Azure (phase 5) instead of a local container, so the Libera cutover is tracked there.

- Clone with `git clone --recurse-submodules` (or run `git submodule update --init`) so that `Gthx.Data` and `IrcDotNet` are present before building.

Done on the Linux PC (2026-10-24):
- Solution builds and all 30 tests pass on Linux with Docker (Testcontainers SQL Server included).
- **Fixed: the phase 1 image couldn't start** ("Microsoft.AspNetCore.App not found"). `Serilog.AspNetCore` pulled the ASP.NET Core shared framework into a plain generic-host app; replaced it with `Serilog.Extensions.Hosting` + `Serilog.Sinks.Console` and referenced `Microsoft.Extensions.Hosting` directly, so the `dotnet/runtime` image works.
- Missing required `IrcInfo` settings now fail at startup, naming the keys, instead of leaving a running container that never connects.
- Added `.dockerignore` (host `bin/`/`obj/` and the secret-holding `gthx.env`/`.env` were being sent into the build context) and git-ignored `gthx.env`/`.env`.
- Added `compose.yaml`: bot + SQL Server 2022 container (persistent `gthx-sql-data` volume, health-checked, on `127.0.0.1:14333`).
- Verified in the container: EF migrations create the database; over IRC on the local InspIRCd server the bot joins, stores and returns factoids, and answers `status`. Console mode (`--console`) works against the same database.

Decisions (2026-10-24):
- **Production database is Azure SQL** for both gthx and othx. gthx's catalog should lose "Test" from its name (today it's `GthxTest`).
- **gthx first; othx later.** othx keeps running its existing `othx2.26` container and MariaDB data until its data is ported to Azure SQL.
- **Don't touch `gthx2.26` while testing.** The new build runs as a separate `gthxtestbot` container with its own nick (`compose.gthxtestbot.yaml`, `gthxtestbot.env.example`).

To do:
- [x] Test database: `Gthx` created on 2026-10-24 as a Basic-tier copy of `GthxTest` on server `gthxdb` (then in resource group `FirstTests`, moved to `rg-gthx-prod` on 2026-10-26). `gthxtestbot` gets real data without writing to production, and `Gthx` becomes the renamed catalog at cutover.
- [x] `gthxtestbot` runs on the local InspIRCd server against `Gthx` (2026-10-24). Verified over IRC: existing factoids and `info` history from the copied data; set, `is also`, and forget; tell (acknowledged and delivered); seen; status. Replies take about 0.16 s (about 1.2 s for the first query after startup).
- [x] NickServ on the local test server (2026-10-24): Anope 2.0.20 in Docker (`testing/services/`), linked to InspIRCd 4 over `127.0.0.1:7000`. InspIRCd 4 requires TLS for server links from non-loopback addresses, so the link must stay on loopback. `gthxtestbot` is registered and logs in with SASL PLAIN (WHOIS shows it logged in).
- [x] **No rapid reconnect loops** (2026-10-24). The bot exits whenever the connection ends and `restart: unless-stopped` restarts it at once (Docker backs off only up to 1 minute, and resets after a run of 10 s or more), so failures used to mean reconnecting about once a minute, or every ~12 s when the connection failed. Libera reportedly also drops the connection when SASL fails for an IP that requires it. Now every exit waits first, chosen by cause: reported ban (465 or a K/G/Z/D-line `ERROR`) 60 min; SASL failure 15 min (and no channels are joined); connection failed or ended within 5 min of registering 5 min; disconnect after running normally 30 s. All are configurable (`IrcInfo:*ExitDelayMinutes`, `IrcInfo:StableConnectionMinutes`). Each path verified, using the local test server and a fake IRC server for the ban and drop cases.
- [x] **Cutover rehearsal** (2026-10-25): `compose.gthx.yaml` runs the production container `gthx` (nick `gthx`) against the `Gthx` copy on the local test server, with SASL over TLS on 6697, the way it will connect to Libera. The local server got a TLS listener with a certificate from a private CA (`testing/tls/`), and `gthx` is registered with the local NickServ. `gthxtestbot` is stopped, and the soak traffic now runs against `gthx` (`BOT_NICK=gthx`).
- [x] Soak results (2026-10-25): after the fixes in [#45](https://github.com/gunnbr/GthxNetBot/pull/45) (lurkers? crash, capitalized tell and google), 11+ hours of traffic with no restarts or errors, every request answered (median 0.18 s), and flat memory (about 62 MB plus about 45 MB of .NET code memory).
- [x] The local `gthx` container was stopped on 2026-10-26 when the Azure bot took over the nick. `compose.gthx.yaml` stays for local rehearsals.

## 4. Logging accessible after Azure deploy — ✅
Decided 2026-10-25 (budget: at most $25/month for logging):
- The container's console output goes to a Log Analytics workspace through Azure Container Apps. This needs no logging code, and it also captures crashes printed outside Serilog. In Azure the console writes one compact JSON object per line (`Serilog.Formatting.Compact`, switched on by environment variables in `infra/main.bicep`), so queries can filter on level and properties.
- Cost: a daily ingestion cap of 0.15 GB keeps it inside the free 5 GB/month, so about $0. A $25/month budget alert on the workspace is a backstop.
- Reading from home: `scripts/gthx-logs` (Log Analytics query, or `-f` to stream live) or the portal's Logs page. Error emails stay, including the deliberate one on every startup.
- Rejected: hosted Seq (a second always-on container), shipping to the home Seq (opens a port, and logs are lost when home is down), and a new Application Insights resource (same billing, more code, misses crashes).
- [x] Verified after the first Azure deploy (2026-10-26): the logs reach `log-gthx-prod`, and `scripts/gthx-logs` reads them. Lines show up in Log Analytics a minute or two after they're written; `-f` streams them live.

## 5. Switch to deploy to Azure — ✅
Rebuilt from scratch on 2026-10-25 using current practice. The 2020 setup (Container Instances, password-based secrets in GitHub) never worked and was removed.
- `infra/main.bicep`: resource group `rg-gthx-prod` (westus) holding Log Analytics, a user-assigned managed identity, Azure Container Registry (Basic, pulled with the identity), Key Vault (RBAC), and a Container Apps environment and app (one replica, 0.25 vCPU / 0.5 GiB), plus budgets. Estimated total about $25/month, including the SQL database.
- Secrets: the IRC server name, the NickServ password and the email settings live in Key Vault and reach the container as Key Vault references. The database uses the managed identity (Entra authentication), so it has no password.
- `.github/workflows/ci.yml` runs the tests on PRs. `deploy.yml` tests, builds and pushes the image, and deploys the template on pushes to `master`, after approval for the `production` environment. Azure sign-in uses OpenID Connect (`infra/setup-github-oidc.sh`), so no Azure password is stored in GitHub.
- Until the Libera ban is lifted, the Azure bot connects to the home test server, whose name is a Key Vault secret and never appears in the repo. Its certificate comes from the private CA in `testing/tls/`.
- [x] One-time setup (`infra/README.md`), done 2026-10-25: resource group and bootstrap deploy, Key Vault secrets, SQL Entra admin and database user, OIDC app, GitHub variables and the `production` environment (the owner must approve each deploy).
- [x] First deploy ([#48](https://github.com/gunnbr/GthxNetBot/pull/48), 2026-10-26). The bot connects to the home test server over TLS with SASL, reads and writes `Gthx` through its managed identity, and answers the soak traffic as quickly as it did at home.
- [x] YouTube titles from Azure ([#50](https://github.com/gunnbr/GthxNetBot/pull/50)): YouTube serves the Azure host a page whose `<title>` is empty, so new links got "unknown title". The bot now asks YouTube's oEmbed endpoint first, and the page parsing skips empty titles and no longer captures the rest of the page.
- [x] **Taking the nick back when the old copy quits unseen** ([#65](https://github.com/gunnbr/GthxNetBot/pull/65), 2026-10-27). At the Libera cutover the old copy was only in `#gthxtestchannel`, which the new copy never joined, so the new copy didn't see its QUIT and stayed `gthx_`. Its 2-minute WHOIS check got 401 "No such nick", but IrcDotNet then raised its WHOIS-reply event with the details cached from the first check, so the bot thought the nick was still taken. The bot now takes the nick back on that 401, and stops the WHOIS checks once it has it. Verified on the test server with a client holding the nick from outside every channel.
- [x] Nick handoff during deploys ([#51](https://github.com/gunnbr/GthxNetBot/pull/51)): Container Apps starts the new copy before it stops the old one, so the new copy comes up as `gthx_`. It used to take `gthx` back only at its 2-minute WHOIS check, and the old copy was killed without quitting. Now the old copy quits IRC on SIGTERM, and the new one reclaims the nick as soon as it sees the QUIT. On #51's own deploy the new copy had `gthx` back 20 s after starting, the moment the old copy disconnected.
- [x] **Switch to Libera** (done 2026-10-27; the ban was lifted on 2026-10-26):
  1. ✅ Stop the `soaktraffic` container, so test traffic doesn't land in the production data (stopped 2026-10-27; its last hour had every request answered, median 0.08 s).
  2. ✅ Database: keep `Gthx` as it is, with the testing changes (decided 2026-10-27).
  3. ✅ In `infra/main.bicepparam` (done in [#64](https://github.com/gunnbr/GthxNetBot/pull/64)) set `ircChannels` to `#reprap,#mobious,#smoothieware,#gthxtest,#dlp3dprinting,#slic3r,#siderala,#reprap.de` (`#bigdelta` dropped: the channel is long dead) and `useTrustedCa = false`.
  4. ✅ Set the Key Vault secrets `irc-server` to `irc.libera.chat` and `irc-sasl-password` to the Libera NickServ password for `gthx`.
  5. ✅ Deploy (a PR with the bicepparam change, then approve the deploy), and check with `scripts/gthx-logs` that the bot logs in with SASL and joins every channel. Deployed 2026-10-27 04:24 UTC: SASL login on Libera and all eight channels joined, answering in `#reprap`. It came up as `gthx_` and didn't take `gthx` back (fixed below).
  6. ✅ Retired `gthx2.26` (2026-10-27): its container is removed. Its images (`gthxnetbot:v2.26`, also used by the stopped `othx2.26`, and `pre-upgrade-backup/gthx2.26`) and its container settings are saved in `~/backups/gthx2.26/` on the Linux PC. Deleting `GthxTest` waits for phase 14's backups.
- [x] Error emails from Azure ([#53](https://github.com/gunnbr/GthxNetBot/pull/53), 2026-10-26): `enableEmail = true`, with the mail settings in the `email-*` Key Vault secrets (copied from `gthx2.26`). Warnings and errors are emailed, including the deliberate one on every startup.
- [x] Batched error emails and a quieter deploy ([#54](https://github.com/gunnbr/GthxNetBot/pull/54)): `Serilog.Sinks.Email` 4.x sent one email per line, where the 2.x sink batched them; the sink now batches for up to 10 seconds in the old line format. Also dropped the "Couldn't quit IRC cleanly" warning that appeared on deploys when the server closed the connection before `Quit()` finished.
- [x] Deleted the 2020 leftovers in `FirstTests` (2026-10-26): the container group and its network profile and vnet, the classic Application Insights resource and its smart detection rule and action group, the `GthxIrcLogs` workspace, the `okopipi` registry, and the two service principals the old workflow used (their secrets had expired in 2021).
- [x] Moved the `gthxdb` server (with `Gthx` and `GthxTest`) into `rg-gthx-prod` (2026-10-26); the bot kept reading and writing `Gthx` through the move. Then deleted the empty `FirstTests` and `GthxNetBot` groups and the unused `NetworkWatcherRG` (Azure recreates a network watcher on its own if a vnet is ever added).
- [x] Database at the cutover: kept `Gthx` as it is (decided 2026-10-27). Nothing has written to `GthxTest` since `gthx2.26` stopped on 2026-10-24, and a row-by-row comparison on 2026-10-26 found all of its data in `Gthx`; `Gthx` also holds the testing changes since then (soak-test factoid history, seen entries and higher reference counts). Delete `GthxTest` once phase 14 has a verified backup.

## 6. Automatic version numbers — ✅
- Versions come from Nerdbank.GitVersioning: `version.json` holds the major/minor (`3.0`, restarted for the .NET 10 / Azure rewrite) and the patch is the number of commits since that line last changed, e.g. `3.0.14+a1b2c3d4e5`, where the suffix is the commit. Every build gets it, including local ones; `Directory.Build.props` adds the package to the bot's projects but not to the submodules.
- The `status` command, the CTCP `VERSION` reply and the startup log line (so each startup email) show it. The deploy tags the image with both the version and the commit SHA.
- `.git` is part of the Docker build context so the build can count commits, and CI checks out the full history.
- No git tag per deploy: the version already identifies the commit, and every merge to `master` deploys, so tags would only add noise.
- `History.txt` has a 3.0 summary; later changes are described in the PRs.

## 7. GitHub build with status badges — ✅
- [x] `ci.yml` runs the tests on every PR, and `deploy.yml` runs them again before each deploy. Fixed in [#49](https://github.com/gunnbr/GthxNetBot/pull/49): the solution leaves `Gthx.Test` out of Release builds, so `dotnet test` on the solution passed without running a single test. CI now tests the project directly.
- [x] Status badges in `README.md`: CI (the latest PR test run) and the deploy workflow on `master`. A deploy that is rejected at the approval step counts as a failed run and turns the deploy badge red until the next approved deploy; documentation-only pushes no longer start one.

## 8. Automatic deploy to Azure from GitHub — ✅
- Done in phase 5 ([#48](https://github.com/gunnbr/GthxNetBot/pull/48)): every push to `master` tests, builds and pushes the image to Azure Container Registry, then deploys after the owner approves the `production` environment. Azure sign-in uses OpenID Connect.
- [x] Pushes that change only Markdown files (`**.md`) skip the workflow, so documentation updates don't need a rejected deploy ([#54](https://github.com/gunnbr/GthxNetBot/pull/54)). Run it by hand (`workflow_dispatch`) if one is ever needed.

## 9. Security — 🟡
Known issues:
- [x] `MailKit` 4.15.1 vulnerability [GHSA-9j88-vvj5-vhgr](https://github.com/advisories/GHSA-9j88-vvj5-vhgr): fixed in phase 1 (by `Serilog.Sinks.Email` 4.2.1).
- [x] **The bot didn't check the IRC server's TLS certificate** (fixed 2026-10-25). IrcDotNet accepts any certificate unless `ValidateSslCertificate` is handled, so anyone able to intercept the connection could pose as the server and collect the SASL password. The bot now rejects certificates with any policy error; `IrcInfo:TrustedCaCertificate` can add a private CA for a test server. Also fixed: after a failed TLS handshake the bot logged "Now connected" and hung instead of exiting.
- [x] Deleted the 18 GitHub Actions secrets from the 2020 workflow (`AZURE_CREDENTIALS`, `REGISTRY_*`, `IRC_*`, `EMAIL_*`, `CONNECTION_STRING` and so on) and the service principals behind them (2026-10-26).

Review on 2026-10-26: no known vulnerable or deprecated NuGet packages; EF Core queries only (no SQL built from strings); no secrets in the logs (sent IRC lines, including SASL's, aren't logged); a flood preventer on everything the bot sends; no public endpoint on the container app; the registry's admin user is off. The regexes that run on every channel message take at most about 16 ms on a worst-case 510-character line, so they can't stall the bot. Fixed in [#56](https://github.com/gunnbr/GthxNetBot/pull/56):
- [x] **Thingiverse look-alike hosts.** The dots in `www.thingiverse.com` weren't escaped, so a link to a host such as `wwwXthingiverse.com` (anyone could register one) made the bot fetch that site and repeat its title. Now only the real host matches.
- [x] **Page titles.** The bot read a whole page looking for its title, so a huge page (or one enormous line) could use up the container's 0.5 GiB. It now reads at most 2 MB, gives up after 20 s instead of 100 s, and turns control characters in a title (such as encoded line breaks, which IRC can't send) into spaces and cuts it off at 300 characters. Before, such a title made every later mention of that link fail with an error (and an error email).
- [x] The bot's nick is escaped in the "addressed to the bot" regex, so a nick with regex characters such as `[` or `|` can't break it.
- [x] **The container runs as the image's unprivileged `app` user** instead of root. The Azure deploy now passes the private test CA as a secret environment variable (PEM text) rather than a mounted file, so the bot can read it whichever user it runs as.
- [x] **GitHub Actions pinned to commit SHAs** (a tag can be moved to other code, and `azure/login` runs with the Azure credentials), and `.github/dependabot.yml` opens monthly grouped update PRs for NuGet packages and the actions.

Still to do (these change Azure or GitHub settings, so they're the owner's call):
- [ ] **Turn on Microsoft Entra-only authentication on `gthxdb`.** It still accepts SQL logins, including the admin login `gthxzeus`, whose password leaked into a session transcript on 2026-10-24. Entra-only turns off every SQL login, so the leaked password stops mattering and there's no password left to change. It also stops `gthx2.26` and the local `gthxtestbot`/rehearsal containers, which use the SQL login, so do it after the Libera cutover has settled (or change the password now and do this then). Command: `az sql server ad-only-auth enable -g rg-gthx-prod -n gthxdb`.
- [ ] **Split the GitHub deploy identity.** The `github-master` federated credential (used by the build job, which doesn't wait for approval) signs in as the same identity as the approved deploy, which is Contributor on `rg-gthx-prod` (now including `gthxdb`) and can grant itself Key Vault Secrets Officer. Give the build job its own identity with only `AcrPush`, so only an approved deploy can change Azure.
- [ ] **GitHub repository settings:** turn on secret scanning and push protection (free for public repositories); set the default workflow token to read-only and stop workflows from approving pull requests (both workflows already ask only for what they need).
- [x] **`master` is protected** (2026-10-26) by a "Protect master" ruleset: no force pushes or deletion, changes go through a pull request, and the CI `test` check must pass before merging. No review is required (GitHub doesn't let you approve your own PR). Repository admins can bypass it for an emergency push.
- [x] IrcDotNet fork: the bot never finished registering without a SASL password (no `CAP END`); fixed, see phase 10.
- Optional, not planned: SQL auditing to the Log Analytics workspace (counts against the 0.15 GB/day cap), and Microsoft Defender for SQL (about $15/month).

## 10. Reported bugs — 🟡
Open GitHub issues, triaged on 2026-10-26 (feature requests are in phase 11):
- [x] Factoids don't get set in queries ([#24](https://github.com/gunnbr/GthxNetBot/issues/24)). A private message that starts with the bot's nick (`/msg gthx gthx: foo is bar`) kept the nick, so it set the factoid `gthx: foo` (and a question got no reply). The nick is now removed in private messages too. Confirmed on the test server before the fix, and fixed there after #59 deployed.
- [x] Thingiverse titles ([#39](https://github.com/gunnbr/GthxNetBot/issues/39), closed as won't do on 2026-10-26): thingiverse.com answers the bot's title fetch with 403 Forbidden (a Cloudflare bot check), from sakura as well as Azure. The only legitimate route is Thingiverse's official API, and we don't agree to its API key's license terms, so the bot no longer fetches Thingiverse titles. It counts Thingiverse links and replies `<user> linked to thing <number> on thingiverse => <count> IRC mentions`, or with a title saved before.
- [x] EF errors don't appear in the log ([#23](https://github.com/gunnbr/GthxNetBot/issues/23)). A failure at startup, such as a database login or migration error, wasn't caught, so it only reached the console as an untimestamped unhandled exception (exit code 139) and was never emailed. It's now logged at Fatal with the version and the full exception, emailed, and the bot exits with code 1. EF's migration messages (each migration applied, or "already up to date") are now logged at Information. Errors while running were already logged (`GthxBot.HandleIncomingMessage`). Verified with a wrong database password and a fresh database on the local SQL container, and the email with Mailpit.
- [x] `GthxSqlDataTest` logged to the Windows-only path `c:\tmp\GthxSqlTests.json`, which Serilog quietly dropped on Linux; it now logs to the system temp folder (`/tmp/GthxSqlTests.json` on Linux).
- [ ] IrcDotNet fork: `CompleteIrcClientTest` now targets `irc.libera.chat` ([#3](https://github.com/gunnbr/IrcDotNet/pull/3)) but fails in `LocalUserModeTest`, because Libera gives new users `+w` by default and the test expects it to be off. The library is fine; the test assumes a server's default user modes. The local InspIRCd gives new users no modes, so it would fail there too (it expects `+i`), and `+w` works there. Planned, not started (deferred on 2026-10-26): make the test toggle `w` from whatever the server set, and skip the integration test unless `ServerHostName` is set, because a plain `dotnet test` in the fork connects two clients to Libera. Then run it against the local server.
- [x] IrcDotNet fork: without a SASL password the bot never finished signing in. The non-SASL path sends `CAP LS` after `NICK`/`USER`, which makes the server hold registration until `CAP END`, but `CAP END` was only sent after an `ACK`/`NAK`, so registration hung until the server timed it out (60 s on InspIRCd). Now `CAP END` is sent right after the `CAP LS` reply when nothing requested a capability ([IrcDotNet#4](https://github.com/gunnbr/IrcDotNet/pull/4)). Registered in 1 second on the test server. The upstream IrcDotNet project is abandoned, so fixes stay in the fork.

Already fixed; closed with a comment on 2026-10-26:
- [x] Unit tests need live network access to YouTube and Thingiverse ([#37](https://github.com/gunnbr/GthxNetBot/issues/37)): fixed on 2026-05-24 by `MockGthxUtil` (commit `1098dca`); the integration tests now also register `MockWebReader` instead of the real `WebReader`, so nothing in the tests can reach the internet. (The tests still need Docker for SQL Server.)
- [x] Not detecting disconnection ([#25](https://github.com/gunnbr/GthxNetBot/issues/25)): the IrcDotNet fork's TCP keepalive catches a dead connection, and the bot then exits so it's restarted. Tested on the local test server by silently dropping all IRC packets with iptables: the bot logged `Connection timed out` 47 seconds later and exited.
- [x] Special characters in YouTube titles ([#22](https://github.com/gunnbr/GthxNetBot/issues/22)): titles now come from YouTube's oEmbed JSON; the video in the issue comes back as `CppCon 2018: Jason Turner “Surprises in Object Lifetime”`.
- [x] Unhandled exception when YouTube isn't available ([#7](https://github.com/gunnbr/GthxNetBot/issues/7)): `GetAndSaveTitle` now catches and logs the error.
- [x] More information in the starting message ([#3](https://github.com/gunnbr/GthxNetBot/issues/3)): it now includes the version and commit (phase 6).
- [x] Getting our nick back when the old instance quits ([#2](https://github.com/gunnbr/GthxNetBot/issues/2)): the bot takes the nick back as soon as it's free; every deploy since #51 shows it happening.

## 11. New features — 🟡
- [x] **Printables and MakerWorld links** (2026-10-26). Both sites block the bot's requests (Cloudflare), but their links normally end with the model's name, such as `printables.com/model/1863827-heavy-duty-keyboard-holder-no-supports` or `makerworld.com/en/models/2459711-articulated-dragon`. The bot takes the model number from the link and makes the title from the name ("Heavy duty keyboard holder no supports"), stores both, and counts mentions like it does for Thingiverse: `<user> linked to "<title>" on Printables => <count> IRC mentions`. A link without the name uses the title stored from an earlier link, or replies `linked to a model on Printables` without one. New tables `PrintablesRef` and `MakerWorldRef` ([GthxNetData#7](https://github.com/gunnbr/GthxNetData/pull/7)). Titles are now cut at 250 characters (plus "...") so they fit the database's 255-character title columns; before, a title over 255 characters would have failed to save.

Requests from GitHub issues:
- [ ] Limit the length of factoids ([#11](https://github.com/gunnbr/GthxNetBot/issues/11)), so long sentences with "is" aren't stored as factoids (around 30 characters looked right).
- [ ] Relax the format for piping factoids to a user ([#33](https://github.com/gunnbr/GthxNetBot/issues/33)): make the `?`/`!` and the spaces in `foo? | bar` optional.
- [x] Reminders ([#30](https://github.com/gunnbr/GthxNetBot/issues/30), asked for as `!remindme <in time> <message>`). Only the addressed form, like the other commands: `gthx: remind me in 12 hours to post this suggestion`, `gthx: list reminders` and `gthx: cancel reminder <number>`. Relative times only (seconds, minutes, hours, days, weeks, 30-day months, 365-day years: `2h`, `1h30m`, `1 day and 3 hours`), from 1 minute to 1 year, at most 10 waiting per nick, 400 characters each. A timer checks every 30 seconds. A due reminder is posted if its author has said something in the last 5 minutes and is still online (shares a channel with the bot): in the channel it was asked in, or by private message if they've left it. Otherwise it's delivered the next time they speak, like `tell`. Kept in the new `Reminder` table ([GthxNetData#8](https://github.com/gunnbr/GthxNetData/pull/8)), so they survive restarts and deploys.
- [ ] Email the admin when certain keywords are said ([#4](https://github.com/gunnbr/GthxNetBot/issues/4)).
- [ ] Handle more CTCP requests, perhaps by switching from `IrcClient` to IrcDotNet's `CtcpClient`, which handles them itself (from the old README's TODO list).

## 12. Gthx.Data repository — 🟡
`Gthx.Data` is a submodule from its own repository, [gunnbr/GthxNetData](https://github.com/gunnbr/GthxNetData), so its packages, workflows and settings are looked after there.
- [x] Dependabot and a CI build ([GthxNetData#4](https://github.com/gunnbr/GthxNetData/pull/4)): monthly grouped NuGet and Actions update PRs, and a build check on every PR.
- [x] This repo's Dependabot config moves the `Gthx.Data` submodule to GthxNetData's latest master once a month ([#56](https://github.com/gunnbr/GthxNetBot/pull/56)).
- [x] EF Core versions match: GthxNetBot (#58) and GthxNetData (its #6) both moved to 10.0.14, and the submodule now points at GthxNetData's master (`fcd24d6`, which also has its Dependabot Actions update, #5). GthxNetBot references `Microsoft.EntityFrameworkCore.Design` and Gthx.Data references `Microsoft.EntityFrameworkCore.SqlServer` and `.Tools`; when one repo updates EF Core, update the other to the same version.
- [x] `master` is protected by the same "Protect master" ruleset as this repo, with the CI `build` check required (2026-10-26).
- [ ] Repository settings: Dependabot security updates are on; turn on secret scanning and push protection.
- [ ] Adding a migration with the README's `dotnet ef migrations add` command fails: the tools look for `SqlServerMigrations.dll` in Gthx.Data's output folder, but that project builds into `Gthx.Data/bin`. It only worked when an old copy happened to be there. Also, most projects in both repos build into one shared `bin/` folder at the GthxNetBot root, so stale files from older builds get mixed in (an EF Core Design DLL from 10.0.13, and `Serilog.AspNetCore` from the test project, which breaks `dotnet run` of the bot). For `AddPrintablesAndMakerWorldRefs`: delete `bin/`, build `Gthx.Data.sln`, copy `Gthx.Data/bin/Debug/net10.0/SqlServerMigrations.dll` into `bin/Debug/net10.0/`, then run the README command with the EF Core 10 tools. Fix: give each project its own output folder, and make the migrations project the startup project for the tools.
- [ ] The README says the data component is shared with the gthx website; check whether the website still uses it before changing the schema.

## 13. Documentation — 🟡
- [x] **README.md rewritten** (2026-10-26). It now says what the bot does and lists its commands and link types, how production runs (Azure, deploys, version numbers, logs, reconnect waits), the repository layout, building and testing, running locally (Compose, console mode, the test bot, soak traffic, Aspire), a configuration reference and how to make changes. The old TODO list was out of date (#29 was fixed in 2.24, the lurkers module exists and actions update "seen"); the CTCP item moved to phase 11. The "Why C#?" section was removed later as no longer relevant.
- [x] Update the documentation ([#28](https://github.com/gunnbr/GthxNetBot/issues/28)): closed as complete on 2026-10-26; the README covers setting up the database and the ways to configure the bot.
- [ ] Gthx.Data's README: fix the migration instructions (see phase 12).

## 14. Backups — ⬜
The data is in the Azure SQL database `Gthx` (Basic tier). Azure keeps automatic backups, but nobody has checked what they cover or tried a restore, and there's no copy outside Azure.
- [ ] Find out what Azure keeps for `Gthx`: point-in-time restore retention (7 days by default on Basic), whether long-term retention is set, and the backup storage redundancy.
- [ ] Test a restore: a point-in-time restore to a throwaway database, compare its row counts with `Gthx`, then delete it.
- [ ] Back up to the Linux PC on a schedule: for example a nightly `systemd` timer that exports `Gthx` (a `.bacpac` with SqlPackage, which holds the schema and the data) into a local folder, keeps the last few weeks, and reports failures. Decide how it signs in (an Entra identity rather than the SQL admin login, so it keeps working after phase 9's Entra-only change) and how often to run it.
- [ ] Test restoring a local backup into the local SQL Server container, and write down the restore steps (both from Azure and from a local backup) in `infra/README.md`.
- [ ] Before retiring `gthx2.26`, keep a copy of its image and of `GthxTest` (or confirm that a local backup of `Gthx` covers it).

## 15. Old branches — ⬜
Look through the branches still on GitHub, keep anything useful, and delete the rest. As of 2026-10-27 (besides `master`):
- [ ] **No commits that aren't in `master`** (so they look safe to delete; check nothing else refers to them): `AddIdMigration`, `DependencyInjection`, `net5`, `docs/readme` (#62), `feature/modernize` (#35), `feature/sasl-support` (#40), `fixinitlogging` (#32), `issue16` (#21), `issue17` (#20), `issue18` (#19).
- [ ] **One commit not in `master`, from merged PRs**: `docs/roadmap-progress` (#52) and `feature/enable-email` (#53). Check that the commit's changes reached `master` some other way.
- [ ] **`TcpKeepAlive`** (2022-05-30): 9 commits not in `master` and no PR. Probably superseded by the TCP keepalive in the IrcDotNet fork ([#25](https://github.com/gunnbr/GthxNetBot/issues/25) is fixed), but look for anything else worth keeping.
- [ ] Turn on "Automatically delete head branches" in the repository settings, so merged PR branches don't pile up again. Do the same for `gunnbr/GthxNetData` and `gunnbr/IrcDotNet`.

## 16. Move othx to Azure — ⬜
`othx` is the same bot for the OpenSCAD channels (`#openscad`, `#othxtest`; real name "OpenSCAD Factoid Bot"). It still runs the 2022 release as the Docker container `othx2.26` on sakura, with its data in the MariaDB database `othxnet` on sakura. It has been stopped since 2026-10-24, when the Libera ban started. The new release only supports SQL Server (phase 1), so the data has to move before othx can run the new build.

Decisions to make first:
- [ ] **Until the move:** restart `othx2.26` as it is (it still works with MariaDB, now that the ban is lifted), or leave othx off Libera until it runs in Azure.
- [ ] **Where the data goes:** a new database (for example `Othx`) on the existing `gthxdb` server (Basic tier, about $5/month), or something else.
- [ ] **How to copy it:** create the schema with the bot's EF migrations, then copy the rows from MariaDB (a one-off script, or a tool such as SQL Server Migration Assistant for MySQL); check that the row counts match and that text survives (character sets).
- [ ] **How to run it:** a second Container App in the same environment, with its own settings, sharing the registry, Key Vault, logs and managed identity (or its own identity); and whether `infra/main.bicep` becomes one template per bot or one template for both.
- [ ] **Deploys:** whether each push to `master` deploys both bots (with one approval or two).
- [ ] **Secrets:** othx's NickServ password in Key Vault (the old container uses `IrcInfo:Password`; the new build uses it for SASL if `SaslPassword` isn't set), and whether othx sends its own error emails.
- [ ] **Cost:** the extra container and database, and whether the budgets change.

Then:
- [ ] Copy the data, deploy othx, and check it on Libera (SASL, both channels, existing factoids).
- [ ] Retire `othx2.26` (save its image and settings as for `gthx2.26`), back up `othxnet`, then turn off MariaDB on sakura.

---
_Last updated: 2026-10-27_
