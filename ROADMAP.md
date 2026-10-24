# GthxNetBot Roadmap

Status legend: ⬜ not started · 🟡 in progress · ✅ done

| # | Phase | Status |
|---|-------|--------|
| 1 | Finish updating to .NET 10 | ✅ |
| 2 | Add SASL support | ✅ |
| 3 | Deploy as a locally running Docker container on the Linux PC | 🟡 |
| 4 | Logging that's accessible once deployed to Azure | ⬜ |
| 5 | Switch to deploy to Azure | ⬜ |
| 6 | Automatic version numbers suited to CI/CD | ⬜ |
| 7 | Build on GitHub with build/test status badges on the main page | ⬜ |
| 8 | Automatically deploy to Azure from GitHub | ⬜ |
| 9 | Fix known security problems | ⬜ |
| 10 | Fix reported bugs | ⬜ |
| 11 | Implement new features | ⬜ |

**Where development happens:** phases 1–2 on the Windows dev machine; phase 3 onward on the Linux PC (which has Docker).

**Next up:** finish phase 3: decide which database the containerized bot uses in production (see below), then cut over.

**Testing rule:** never run test bots or probe clients against Libera (we were banned there on 2026-10-24 after phase 3 test runs). Use the local InspIRCd test server on sakura: `10.0.0.125:6667`, plaintext.

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

## 3. Local Docker container on the Linux PC — 🟡
- Clone with `git clone --recurse-submodules` (or run `git submodule update --init`) so that `Gthx.Data` and `IrcDotNet` are present before building.

Done on the Linux PC (2026-10-24):
- Solution builds and all 30 tests pass on Linux with Docker (Testcontainers SQL Server included).
- **Fixed: the phase 1 image couldn't start** ("Microsoft.AspNetCore.App not found"). `Serilog.AspNetCore` pulled the ASP.NET Core shared framework into a plain generic-host app; replaced it with `Serilog.Extensions.Hosting` + `Serilog.Sinks.Console` and referenced `Microsoft.Extensions.Hosting` directly, so the `dotnet/runtime` image works.
- Missing required `IrcInfo` settings now fail at startup, naming the keys, instead of leaving a running container that never connects.
- Added `.dockerignore` (host `bin/`/`obj/` and the secret-holding `gthx.env`/`.env` were being sent into the build context) and git-ignored `gthx.env`/`.env`.
- Added `compose.yaml`: bot + SQL Server 2022 container (persistent `gthx-sql-data` volume, health-checked, on `127.0.0.1:14333`).
- Verified in the container: EF migrations create the database; over IRC on the local InspIRCd server the bot joins, stores and returns factoids, and answers `status`. Console mode (`--console`) works against the same database.

To do:
- [ ] Decide the production database for the container. Today `gthx2.26` (still the 2022 .NET 6 image) uses Azure SQL (`gthxdb.database.windows.net`, catalog `GthxTest`), and `othx2.26` uses MariaDB on sakura, which the new build no longer supports (port othx's data to SQL Server first).
- [ ] SASL login could not be re-checked from the container: it was skipped against the local server (no NickServ there), and Libera is off-limits for testing. Check it during the production cutover.
- [ ] Cut over: replace the `gthx2.26` container with the new image (back up the old image first, as was done with `pre-upgrade-backup/*`).

## 4. Logging accessible after Azure deploy — ⬜
- Today the bot logs through Serilog to console/file/Seq, plus email for warnings and above. None of that is easy to reach from Azure.
- To do: choose a destination (e.g. Azure Monitor / Application Insights via OpenTelemetry, or a hosted Seq) and wire it up before deploying.

## 5. Switch to deploy to Azure — ⬜
- `.github/workflows/main.yml` already builds and deploys to Azure Container Instances on push to `master`, but it runs no tests, uses outdated action versions (`azure/login@v1`, `actions/checkout@master`), and doesn't pass `IrcInfo__SaslPassword` / `UseSsl` / `Port`. Its runs on 2026-08-23 and after merging #40 (2026-10-24) both failed at the Azure login step, so nothing was deployed.

## 6. Automatic version numbers — ⬜
- Today the version is set by hand: `GthxBot.Version = "2.26 2022-12-28"` in `Gthx.Bot/GthxBot.cs` (shown by the status command), `<Version>1.0.1</Version>` in `Gthx.Data.csproj`, and release notes in `History.txt`.
- To do: derive versions from git (e.g. tags plus commit height, via a tool such as MinVer or Nerdbank.GitVersioning), stamp them into the assemblies and the Docker image tag, and have the status command read the assembly's informational version.

## 7. GitHub build with status badges — ⬜
- Plan: a separate build/test workflow for PRs and pushes, with a status badge in `README.md`.

## 8. Automatic deploy to Azure from GitHub — ⬜
- Rework the existing deploy workflow once phases 5–7 are in place.

## 9. Security — ⬜
Known issues:
- [x] `MailKit` 4.15.1 vulnerability [GHSA-9j88-vvj5-vhgr](https://github.com/advisories/GHSA-9j88-vvj5-vhgr): fixed in phase 1 (by `Serilog.Sinks.Email` 4.2.1).
- To do: review for other issues (secrets handling, dependency audit in CI).

## 10. Reported bugs — ⬜
- To do: collect from GitHub issues.
- [ ] `GthxSqlDataTest` logs to the Windows-only path `c:\tmp\GthxSqlTests.json`; make it portable for Linux/CI.
- [ ] IrcDotNet fork: `CompleteIrcClientTest` now targets `irc.libera.chat` ([#3](https://github.com/gunnbr/IrcDotNet/pull/3)) but fails in `LocalUserModeTest`, because Libera gives new users `+w` by default and the test expects it to be off.

## 11. New features — ⬜
- To do: gather the list.

---
_Last updated: 2026-10-24_
