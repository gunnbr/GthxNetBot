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
| **Until the move:** restart `othx2.26` on Libera as it is, or leave othx off until it runs in Azure. | Restart it now if the channel misses it: it works as before. Copy the data after stopping it again, so nothing is lost. |
| **Where the data goes.** | A new Basic database `Othx` on the existing `gthxdb` server (about $5 a month), Entra-only like `Gthx`. |
| **How to copy it.** | Create the schema with the bot's EF migrations, then copy the rows with a one-off console app (MySqlConnector to read, `SqlBulkCopy` with kept identity values to write). Try it on a local SQL Server database first. |
| **How to run it.** | A second Container App, `ca-othx-prod`, in the same environment, sharing the registry, Key Vault and logs, with its own identity `id-othx-prod` so each bot can only reach its own database. |
| **Bicep layout.** | Split `infra/main.bicep` into the shared resources and a per-bot module (app, identity, settings), with one parameter set per bot. |
| **Deploys.** | One approval deploys both bots from the same image. |
| **Secrets.** | othx's own Key Vault secrets (for example `othx-irc-sasl-password`); the user sets them. Error emails go to the same address as gthx's, with a different subject. |
| **Backups.** | The same as `Gthx`: monthly long-term retention for 12 months, and a second nightly `scripts/gthx-backup` run with its own settings file (`SQL_DATABASE=Othx`), and the `gthx-backup` user in `Othx`. |
| **Cost.** | About $5 a month for the database plus the container's share; check the budgets still fit. |

## Steps

### 1. Look at the data — ⬜
- [ ] Row counts per table in `othxnet`, the character set and collation of each table and column (latin1 or
      utf8mb4), the longest values, and anything odd (factoid names over the 30-character limit added in
      2026, duplicates that differ only in case, dates out of SQL Server's range).
- [ ] Check what else uses MariaDB on sakura, before planning to turn it off.

### 2. Copy the data, locally first — ⬜
- [ ] Write the copy app; run it into a throwaway database in the local SQL Server container (port 14333),
      migrated with the current EF migrations.
- [ ] Compare row counts, and spot-check text with non-ASCII characters and the newest rows.
- [ ] Run the local bot against the copy on the local InspIRCd (as `othx`, in `#othxtest`): factoids, tells,
      seen, history.

### 3. Azure — ⬜
- [ ] Create `Othx` on `gthxdb`; add `id-othx-prod` (db_datareader, db_datawriter, db_ddladmin) and
      `gthx-backup` (db_datareader, VIEW DEFINITION). The user runs the SQL as the Entra admin.
- [ ] Split the Bicep (shared plus per-bot), add othx's settings and secrets, and update the deploy workflow.
      Check that a deploy leaves gthx unchanged.
- [ ] Turn on long-term retention for `Othx`, and add its nightly backup.

### 4. Move — ⬜
- [ ] Stop `othx2.26` (if it was restarted), copy the data into `Othx`, and check the row counts.
- [ ] Deploy othx; check it on Libera: SASL, both channels, existing factoids.

### 5. Retire the old setup — ⬜
- [ ] Save `othx2.26`'s image and `docker inspect` output (it has secrets; keep it private) to
      `~/backups/othx2.26/`, as was done for `gthx2.26`, then remove the container.
- [ ] Dump `othxnet` (`mysqldump`) to `~/backups/othx2.26/`, then turn off MariaDB on sakura if nothing else
      uses it.
- [ ] Update `README.md` and `infra/README.md` for two bots.

---
_Last updated: 2026-10-28_
