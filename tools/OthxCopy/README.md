# OthxCopy

One-off tool that copies othx's data from the MariaDB database `othxnet` (used by the 2.26 release) into a
SQL Server database, for the move in [OTHX-MIGRATION.md](../../OTHX-MIGRATION.md). It:

1. Reads every table from `othxnet`, with the credentials from the `[client]` section of `~/.my.cnf`
   (`MYCNF` to use another file, `OTHX_SOURCE_DB` for another database).
2. Drops factoids whose names are over the 30-character limit, with their history (sentences saved by mistake
   before the limit existed).
3. Repairs text from before 2022 that was encoded twice when the data was imported from the old latin1
   database in 2021 (`ðŸ°` becomes `🍰`). Each change is printed.
4. Creates the schema with the bot's EF migrations, refuses to go on if any table already has rows, then
   copies all tables in one transaction with their original ids and checks the row counts.

The target database must exist. Try it without writing anything first:

```sh
dotnet run --project tools/OthxCopy -- --dry-run
```

Local SQL Server (the database must be created first):

```sh
OTHX_TARGET="Server=127.0.0.1,14333;Database=OthxCopyTest;User Id=sa;Password=...;TrustServerCertificate=True" \
  dotnet run --project tools/OthxCopy
```

Azure SQL (Entra-only), run by the Entra admin:

```sh
SQLTOKEN=$(az account get-access-token --resource https://database.windows.net/ --query accessToken -o tsv) \
OTHX_TARGET="Server=tcp:gthxdb.database.windows.net,1433;Initial Catalog=Othx;Encrypt=True" \
  dotnet run --project tools/OthxCopy
```
