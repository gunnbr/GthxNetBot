# GthxNetBot Roadmap

Status legend: ⬜ not started · 🟡 in progress · ✅ done

Phases 1–11 are done: .NET 10, SASL, the move to Azure Container Apps (production `gthx` on Libera since
2026-10-27), logging, version numbers, CI/CD with approved deploys, security fixes, the reported bugs and
the new features. Their details were removed on 2026-10-28 to keep this file short; see the
[last version that had them](https://github.com/gunnbr/GthxNetBot/blob/d1af1d7/ROADMAP.md).

| # | Phase | Status |
|---|-------|--------|
| 12 | Look after the Gthx.Data repository (`gunnbr/GthxNetData`) | ✅ |
| 13 | Bring the documentation up to date | ✅ |
| 14 | Verify the database backups, and back up to the Linux PC on a schedule | ⬜ |
| 15 | Clean up the old branches in this repo | ⬜ |
| 16 | Move othx to Azure | ⬜ |

**Testing rule:** never run test bots or probe clients against Libera (we were banned there on 2026-10-24 after test runs; the ban was lifted on 2026-10-26). Only the production bot connects to Libera. Use the local InspIRCd test server on sakura: `10.0.0.125:6667` plaintext, or `10.0.0.125:6697` TLS (certificate from the private CA in `testing/tls/`).

## Deferred and leftover work
- [ ] **Email the admin when certain keywords are said** ([#4](https://github.com/gunnbr/GthxNetBot/issues/4)), deferred on 2026-10-28. The admin needs a way to set the keywords, which is planned as an admin page on the website ([GthxWebsite](https://github.com/gunnbr/GthxWebsite)), so this will be done during work on the website.
- [ ] **Final cleanup of build warnings**, left for the very end: CS8981 (the migration class named `init` is all lower case) and CS0618 (the obsolete `IrcRawMessageEventArgs.Message` in `GthxNetBot/GthxIrcClient.cs`).

## 12. Gthx.Data repository — ✅
`Gthx.Data` is a submodule from its own repository, [gunnbr/GthxNetData](https://github.com/gunnbr/GthxNetData), so its packages, workflows and settings are looked after there.
- [x] Dependabot and a CI build ([GthxNetData#4](https://github.com/gunnbr/GthxNetData/pull/4)): monthly grouped NuGet and Actions update PRs, and a build check on every PR.
- [x] This repo's Dependabot config moves the `Gthx.Data` submodule to GthxNetData's latest master once a month ([#56](https://github.com/gunnbr/GthxNetBot/pull/56)).
- [x] EF Core versions match: GthxNetBot (#58) and GthxNetData (its #6) both moved to 10.0.14, and the submodule now points at GthxNetData's master (`fcd24d6`, which also has its Dependabot Actions update, #5). GthxNetBot references `Microsoft.EntityFrameworkCore.Design`, Gthx.Data references `Microsoft.EntityFrameworkCore.SqlServer` and SqlServerMigrations references `.Tools`; when one repo updates EF Core, update the other to the same version.
- [x] `master` is protected by the same "Protect master" ruleset as this repo, with the CI `build` check required (2026-10-26).
- [x] Repository settings: Dependabot security updates, secret scanning and push protection are on, the default workflow token is read-only, and workflows can't approve pull requests (2026-10-28).
- [x] Each project builds into its own `bin/` folder, and adding a migration works ([GthxNetData#10](https://github.com/gunnbr/GthxNetData/pull/10), this repo's #77). Before, the EF tools looked for `SqlServerMigrations.dll` where it wasn't built, and the shared `bin/` folder at the GthxNetBot root collected stale files (`Serilog.AspNetCore` from the test project broke `dotnet run` of the bot). `SqlServerMigrations` is now the startup project for the tools and holds the `Microsoft.EntityFrameworkCore.Tools` reference; Gthx.Data's README has the new command.
- [x] The website uses it: [GthxWebsite](https://github.com/gunnbr/GthxWebsite) has GthxNetData as a submodule (`lib/GthxNetData`) and reads the same `Gthx` database through `IGthxWebData`, with a read-only identity. A schema change has to keep the website's queries working (GthxNetBot's `GthxWebDataTest` covers them).

## 13. Documentation — ✅
- [x] **README.md rewritten** (2026-10-26). It now says what the bot does and lists its commands and link types, how production runs (Azure, deploys, version numbers, logs, reconnect waits), the repository layout, building and testing, running locally (Compose, console mode, the test bot, soak traffic, Aspire), a configuration reference and how to make changes. The old TODO list was out of date (#29 was fixed in 2.24, the lurkers module exists and actions update "seen"); its CTCP item has since been done. The "Why C#?" section was removed later as no longer relevant.
- [x] Update the documentation ([#28](https://github.com/gunnbr/GthxNetBot/issues/28)): closed as complete on 2026-10-26; the README covers setting up the database and the ways to configure the bot.
- [x] Gthx.Data's README: the migration instructions are fixed (see phase 12).

## 14. Backups — ⬜
The data is in the Azure SQL database `Gthx` (Basic tier). Azure keeps automatic backups, but nobody has checked what they cover or tried a restore, and there's no copy outside Azure.
- [ ] Find out what Azure keeps for `Gthx`: point-in-time restore retention (7 days by default on Basic), whether long-term retention is set, and the backup storage redundancy.
- [ ] Test a restore: a point-in-time restore to a throwaway database, compare its row counts with `Gthx`, then delete it.
- [ ] Back up to the Linux PC on a schedule: for example a nightly `systemd` timer that exports `Gthx` (a `.bacpac` with SqlPackage, which holds the schema and the data) into a local folder, keeps the last few weeks, and reports failures. Decide how it signs in (an Entra identity: SQL logins are off since 2026-10-28) and how often to run it.
- [ ] Test restoring a local backup into the local SQL Server container, and write down the restore steps (both from Azure and from a local backup) in `infra/README.md`.
- [ ] `gthx2.26` is retired; keep a copy of `GthxTest` (old 2022 data on `gthxdb`), or confirm that a local backup of `Gthx` covers it, then delete it.

## 15. Old branches — ⬜
Look through the branches still on GitHub, keep anything useful, and delete the rest. As of 2026-10-27 (besides `master`):
- [ ] **No commits that aren't in `master`** (so they look safe to delete; check nothing else refers to them): `AddIdMigration`, `DependencyInjection`, `net5`, `docs/readme` (#62), `feature/modernize` (#35), `feature/sasl-support` (#40), `fixinitlogging` (#32), `issue16` (#21), `issue17` (#20), `issue18` (#19).
- [ ] **One commit not in `master`, from merged PRs**: `docs/roadmap-progress` (#52) and `feature/enable-email` (#53). Check that the commit's changes reached `master` some other way.
- [ ] **`TcpKeepAlive`** (2022-05-30): 9 commits not in `master` and no PR. Probably superseded by the TCP keepalive in the IrcDotNet fork ([#25](https://github.com/gunnbr/GthxNetBot/issues/25) is fixed), but look for anything else worth keeping.
- [ ] Turn on "Automatically delete head branches" in the repository settings, so merged PR branches don't pile up again. Do the same for `gunnbr/GthxNetData`, `gunnbr/IrcDotNet` and `gunnbr/GthxWebsite`.

## 16. Move othx to Azure — ⬜
`othx` is the same bot for the OpenSCAD channels (`#openscad`, `#othxtest`; real name "OpenSCAD Factoid Bot"). It still runs the 2022 release as the Docker container `othx2.26` on sakura, with its data in the MariaDB database `othxnet` on sakura. It has been stopped since 2026-10-24, when the Libera ban started. The new release only supports SQL Server, so the data has to move before othx can run the new build.

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
_Last updated: 2026-10-28_
