# GthxNetBot Roadmap

Status legend: ⬜ not started · 🟡 in progress · ✅ done

| # | Phase | Status |
|---|-------|--------|
| 1 | Finish updating to .NET 10 | 🟡 |
| 2 | Add SASL support | 🟡 |
| 3 | Deploy as a locally running Docker container | ⬜ |
| 4 | Switch to deploy to Azure | ⬜ |
| 5 | Build on GitHub with build/test status badges on the main page | ⬜ |
| 6 | Automatically deploy to Azure from GitHub | ⬜ |
| 7 | Fix known security problems | ⬜ |
| 8 | Fix reported bugs | ⬜ |
| 9 | Implement new features | ⬜ |

## 1. Finish updating to .NET 10 — 🟡

Done:
- All projects target `net10.0`; Dockerfile uses the 10.0 SDK/runtime images; solution is `.slnx`.
- Solution builds (0 errors). All 30 tests pass, including the SQL Server Testcontainers tests (verified with Podman on Windows using the `docker_engine` pipe).

Remaining:
- [ ] Bump outdated packages to their .NET 10-era versions: `Microsoft.Extensions.*` 7.0.0, EF Core 9.0.0, `Microsoft.AspNetCore.TestHost` 7.0.17, `Serilog.*` 7.0.0, `Microsoft.NET.Test.Sdk`, NUnit 3 / adapter, coverlet, `DotNet.Testcontainers` 1.6.0 (very old; the replacement is `Testcontainers.MsSql`).
- [ ] Remove the redundant `System.Threading.Tasks.Dataflow` reference (NU1510).
- [ ] Replace the obsolete `WebHostBuilder` / `UseSerilog(IWebHostBuilder)` in tests (ASPDEPR004 / CS0618).

## 2. Add SASL support — 🟡

Done (uncommitted on `feature/sasl-support`):
- `GthxIrcClient`: SASL using the nick plus `SaslPassword` (falls back to `Password`), TLS on by default (port 6697), CAP/SASL diagnostic logging, faster connect-failure handling.
- AppHost passes `sasl-password` as a secret parameter; the bot has a `UserSecretsId`.
- SASL PLAIN support committed to the fork and merged to its `master` ([gunnbr/IrcDotNet#2](https://github.com/gunnbr/IrcDotNet/pull/2), commit `40864f9`). Upstream IrcDotNet looks abandoned, so the fork is the source of truth.
- `IrcDotNet` is now a proper git submodule (`gunnbr/IrcDotNet`), and the bot uses it as a project reference. The local `nuget.config` package feed is gone, so builds no longer depend on files outside the repo.

Remaining:
- [ ] Verify the login against Libera.
- [ ] Merge the `feature/sasl-support` PR into `master`.
- [ ] Fork: `CompleteIrcClientTest` now connects to `irc.libera.chat` instead of the defunct freenode ([gunnbr/IrcDotNet#3](https://github.com/gunnbr/IrcDotNet/pull/3)). It still fails in `LocalUserModeTest`, because Libera gives new users `+w` by default and the test expects it to be off.

## 3. Local Docker container — ⬜
- `podman build -t gthxnetbot .` builds the image successfully on Windows; the submodules are restored inside the container with no external package feed.
- Still to do: run the container locally with config/secrets and a database (and decide how it reaches the DB). The bot normally runs on a Linux PC with Docker.
- Clone with `git clone --recurse-submodules` (or run `git submodule update --init`) so that `Gthx.Data` and `IrcDotNet` are present before building.

## 4–6. Azure + GitHub CI/CD — ⬜
- `.github/workflows/main.yml` already builds and deploys to Azure Container Instances on push to `master`, but it runs no tests, uses outdated action versions (`azure/login@v1`, `actions/checkout@master`), and doesn't pass `IrcInfo__SaslPassword` / `UseSsl` / `Port`.
- Phase 5 plan: a separate build/test workflow for PRs and pushes, with a status badge in `README.md`.

## 7. Security — ⬜
Known issues:
- [ ] `MailKit` 4.15.1 (transitive, via `Serilog.Sinks.Email`) — moderate vulnerability [GHSA-9j88-vvj5-vhgr](https://github.com/advisories/GHSA-9j88-vvj5-vhgr).

## 8. Reported bugs — ⬜
- To do: collect from GitHub issues.

## 9. New features — ⬜
- To do: gather the list.

---
_Last updated: 2026-10-24_
