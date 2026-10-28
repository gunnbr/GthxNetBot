# Moving othx to Azure

Status legend: ⬜ not started · 🟡 in progress · ✅ done

`othx` is the same bot as gthx, for the OpenSCAD channels on Libera. This plan moves it from sakura to
Azure, next to gthx, running the current release. It replaces `ROADMAP.md`, which tracked the
modernization of gthx (phases 1–15, all done; see its
[last version](https://github.com/gunnbr/GthxNetBot/blob/2b5c0d7/ROADMAP.md)). Work left over from that
is in issues: [#4](https://github.com/gunnbr/GthxNetBot/issues/4) (keyword alert emails, to be done with
the website) and [#82](https://github.com/gunnbr/GthxNetBot/issues/82) (the final cleanup of build warnings).

**Testing rule:** never run test bots or probe clients against Libera (we were banned there on 2026-10-24
after test runs). Only the production bots connect to Libera. Use the local InspIRCd test server on sakura:
`10.0.0.125:6667` plaintext, or `10.0.0.125:6697` TLS (certificate from the private CA in `testing/tls/`).

## Where othx is now

- **Container:** `othx2.26` on sakura, image `gthxnetbot:v2.26` (the 2022 release, 2.26), host networking,
  restart policy `unless-stopped`. Stopped since 2026-10-24, when the Libera ban started; the ban was lifted
  on 2026-10-26.
- **Settings** (environment variables of the container): nick `othx`, channels `#openscad,#othxtest`, real
  name "OpenSCAD Factoid Bot", server `irc.libera.chat`. The NickServ password is in `IrcInfo__Password`
  (the old release identifies with it; the new one uses it for SASL if `IrcInfo__SaslPassword` isn't set).
  It also has the error email settings (`EmailConfiguration__*`).
- **Data:** the MariaDB database `othxnet` on sakura (MariaDB 11.8, a system service), connection type
  `mariadb`. Its schema comes from the old MariaDB migrations in GthxNetData (`InitialCreate` and
  `AddIdToRefTables`, 2021; removed in GthxNetData `4482149` when MariaDB support was dropped), so it has
  the same tables as gthx had then: `Factoid`, `FactoidHistory`, `Ref`, `Seen`, `Tell`, `ThingiverseRef`,
  `YoutubeRef` and `__EFMigrationsHistory`. The tables added since (`PrintablesRef`, `MakerWorldRef`,
  `Reminder`) start empty.
- **The new release only supports SQL Server**, so the data has to move before othx can run it.

For comparison, gthx's 2.26 container already used Azure SQL (`GthxTest`), so this is the first move from
MariaDB.

## Decisions to make first

| Decision | Recommendation |
|---|---|
| **Until the move:** restart `othx2.26` on Libera as it is, or leave othx off until it runs in Azure. | **Decided (2026-10-28): leave it off.** Its data is frozen, so the copy can be taken any time. |
| **Where the data goes.** | **Decided (2026-10-28):** a new Basic database `Othx` on the existing `gthxdb` server (about $5 a month), Entra-only like `Gthx`, rather than a schema in `Gthx` (that would need db_ddladmin on gthx's tables, shared restores and code changes). |
| **How to copy it.** | **Done:** `tools/OthxCopy` (see step 2). It repairs the double-encoded text and drops factoid #110, as decided on 2026-10-28. |
| **How to run it.** | A second Container App, `ca-othx-prod`, in the same environment, sharing the registry, Key Vault and logs, with its own identity `id-othx-prod` so each bot can only reach its own database. |
| **Bicep layout.** | **Done:** shared resources in `infra/main.bicep`, a per-bot module `infra/bot.bicep` (identity, app, settings), and a `bots` list in `main.bicepparam`. |
| **Deploys.** | One approval deploys both bots from the same image. |
| **Secrets.** | othx's own Key Vault secrets (for example `othx-irc-sasl-password`); the user sets them. Error emails go to the same address as gthx's, with a different subject. |
| **Backups.** | The same as `Gthx`: monthly long-term retention for 12 months, and a second nightly `scripts/gthx-backup` run with its own settings file (`SQL_DATABASE=Othx`), and the `gthx-backup` user in `Othx`. |
| **Cost.** | About $5 a month for the database plus $6–20 for the second container app (no free allowance left), so `rg-gthx-prod` goes from about $15–25 to $26–50 a month against the $50 budget. Whether the bots get billed at the idle rate decides it ([#84](https://github.com/gunnbr/GthxNetBot/issues/84)); real figures from about 2026-11-10. Raising the budget to $60 is still open. |

## Steps

### 1. Look at the data — ✅
- [x] Row counts: Factoid 107, FactoidHistory 207, Ref 61, Seen 2,701, Tell 0, ThingiverseRef 236,
      YoutubeRef 350. Every table and text column is utf8mb4, and the column sizes match the SQL Server model.
      No duplicate factoid names (not even by case), and all dates are from 2014 to 2026.
- [x] Odd data: about 34 values from before 2022 are encoded twice (`ðŸ°` for `🍰`), from the 2021 import
      from the latin1 database (`othx`); the copy repairs them. One factoid name is a 54-character sentence
      (#110); the copy drops it and its history row. 603 old Seen rows have no channel; they're copied as
      they are.
- [x] What else uses MariaDB on sakura: **Gitea** (`gitea.service`, database `gitea`), so MariaDB stays on.
      The only container using it is `othx2.26`. The other databases (`gthx`, `gthxfinal`, `gthxmigrations`,
      `gthxtestlatin`, `gthxtestutf8`, `othx`, `testlatin`, `testutf8`, `updatetest`) are leftovers.

### 2. Copy the data, locally first — ✅
- [x] [`tools/OthxCopy`](tools/OthxCopy/README.md) creates the schema with the bot's migrations and copies
      all tables in one transaction, keeping the ids. Tried on a throwaway database in the local SQL Server.
- [x] Row counts match (106 factoids and 206 history rows after dropping #110); repaired text (emoji,
      `¯\_(ツ)_/¯`, accents) and the newest rows (Seen from 2026-10-24) checked. A second run refuses to copy
      into a database that has rows.
- [x] The current bot ran against the copy on the local InspIRCd as `othx` in `#othxtest`: factoids, info and
      history, seen, set, also, forget and tell all work, and new rows get ids after the copied ones.

### 3. Azure — 🟡
- [x] Split the Bicep: `infra/main.bicep` has the shared resources and a `bots` list; `infra/bot.bicep` has
      each bot's identity and container app. othx shares `irc-server` and `email-*` (subject `Othx Error`)
      and has its own `othx-irc-sasl-password`. A what-if against production shows no change for gthx; the
      only new resources are `id-othx-prod` and its two role assignments. `scripts/gthx-logs -b othx` shows
      othx's logs. The deploy workflow needed no change: one deploy updates both bots.
- [ ] Deploy with othx's `deployApp: false`, to create `id-othx-prod`.
- [ ] Create `Othx` on `gthxdb`; add `id-othx-prod` (db_datareader, db_datawriter, db_ddladmin) and
      `gthx-backup` (db_datareader, VIEW DEFINITION). The user runs the SQL as the Entra admin (see
      `infra/README.md`, "Adding a bot").
- [ ] The user sets `othx-irc-sasl-password`.
- [ ] Turn on long-term retention for `Othx`, and add its nightly backup.

### 4. Move — ⬜
- [ ] Stop `othx2.26` (if it was restarted), copy the data into `Othx`, and check the row counts.
- [ ] Set othx's `deployApp: true` and deploy; check it on Libera: SASL, both channels, existing factoids.

### 5. Retire the old setup — ⬜
- [ ] Save `othx2.26`'s image and `docker inspect` output (it has secrets; keep it private) to
      `~/backups/othx2.26/`, as was done for `gthx2.26`, then remove the container.
- [ ] Dump `othxnet` (`mysqldump`) to `~/backups/othx2.26/`, then drop it (and the leftover databases, if
      wanted). MariaDB stays on for Gitea.
- [ ] Update `README.md` and `infra/README.md` for two bots.

---
_Last updated: 2026-10-28_
