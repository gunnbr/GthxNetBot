// Copies othx's data from MariaDB (othxnet, the 2.26 release's database) into a SQL Server database
// created with the bot's EF migrations. See README.md for how to run it.

using System.Data;
using System.Text;
using GthxData;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

const int MaxFactoidLength = 30; // FactoidModule.MaxFactoidLength

// Text written before this was imported from the old latin1 database in 2021 and may be encoded twice.
var mojibakeCutoff = new DateTime(2022, 1, 1);

// Tables in copy order.
string[] tables = ["Factoid", "FactoidHistory", "Ref", "Seen", "Tell", "ThingiverseRef", "YoutubeRef"];

var dryRun = args.Contains("--dry-run");
var myCnf = Environment.GetEnvironmentVariable("MYCNF") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".my.cnf");
var sourceDb = Environment.GetEnvironmentVariable("OTHX_SOURCE_DB") ?? "othxnet";
var target = Environment.GetEnvironmentVariable("OTHX_TARGET");
var token = Environment.GetEnvironmentVariable("SQLTOKEN");
if (!dryRun && string.IsNullOrEmpty(target))
{
    Console.Error.WriteLine("Set OTHX_TARGET to the SQL Server connection string (or pass --dry-run).");
    return 1;
}

// Read everything (a few thousand rows) into memory.
var data = new Dictionary<string, DataTable>();
await using (var source = new MySqlConnection(SourceConnectionString(myCnf, sourceDb)))
{
    await source.OpenAsync();
    foreach (var table in tables)
    {
        using var adapter = new MySqlDataAdapter($"SELECT * FROM `{table}`", source);
        var dt = new DataTable(table);
        adapter.Fill(dt);
        data[table] = dt;
        Console.WriteLine($"Read {dt.Rows.Count,5} rows from {table}");
    }
}

// Drop factoids whose names are over the length limit (sentences saved by mistake), with their history.
var longItems = data["Factoid"].Rows.Cast<DataRow>()
    .Select(r => r["Item"] as string)
    .Where(i => i != null && i.Length > MaxFactoidLength)
    .ToHashSet(StringComparer.OrdinalIgnoreCase);
foreach (var table in new[] { "Factoid", "FactoidHistory" })
{
    foreach (var row in data[table].Rows.Cast<DataRow>().Where(r => longItems.Contains(r["Item"] as string ?? "")).ToList())
    {
        Console.WriteLine($"Dropping {table} {row["Id"]}: '{row["Item"]}'");
        data[table].Rows.Remove(row);
    }
}

// Repair text that was encoded twice.
var repaired = 0;
foreach (var dt in data.Values)
{
    var textColumns = dt.Columns.Cast<DataColumn>().Where(c => c.DataType == typeof(string)).ToList();
    foreach (DataRow row in dt.Rows)
    {
        if (row["Timestamp"] is DateTime ts && ts >= mojibakeCutoff)
        {
            continue;
        }

        foreach (var column in textColumns)
        {
            if (row[column] is string text && TryRepair(text, out var fixedText))
            {
                Console.WriteLine($"Repairing {dt.TableName} {Key(row)} {column.ColumnName}: '{Shorten(text)}' -> '{Shorten(fixedText)}'");
                row[column] = fixedText;
                repaired++;
            }
        }
    }
}
Console.WriteLine($"Repaired {repaired} values");

if (dryRun)
{
    Console.WriteLine("Dry run: nothing written.");
    return 0;
}

// Create the schema with the bot's migrations, the same way the bot does at startup.
await using var connection = new SqlConnection(target);
if (!string.IsNullOrEmpty(token))
{
    connection.AccessToken = token;
}
await connection.OpenAsync();
var options = new DbContextOptionsBuilder<GthxDataContext>()
    .UseSqlServer(connection, o => o.MigrationsAssembly("SqlServerMigrations"))
    .Options;
await using (var context = new GthxDataContext(options))
{
    await context.Database.MigrateAsync();
}
Console.WriteLine($"Migrated {connection.Database} on {connection.DataSource}");

foreach (var table in tables)
{
    var existing = await CountAsync(connection, null, table);
    if (existing != 0)
    {
        Console.Error.WriteLine($"{table} already has {existing} rows; refusing to copy into a database that isn't empty.");
        return 1;
    }
}

// Copy all tables in one transaction, keeping the ids so history and references stay as they were.
await using (var transaction = connection.BeginTransaction())
{
    foreach (var table in tables)
    {
        var dt = data[table];
        using var bulk = new SqlBulkCopy(connection, SqlBulkCopyOptions.KeepIdentity | SqlBulkCopyOptions.CheckConstraints, transaction)
        {
            DestinationTableName = $"[dbo].[{table}]",
        };
        foreach (DataColumn column in dt.Columns)
        {
            bulk.ColumnMappings.Add(column.ColumnName, column.ColumnName);
        }
        await bulk.WriteToServerAsync(dt);
    }

    foreach (var table in tables)
    {
        var copied = await CountAsync(connection, transaction, table);
        var expected = data[table].Rows.Count;
        Console.WriteLine($"{table,-15} {copied,5} rows{(copied == expected ? "" : $" (expected {expected})")}");
        if (copied != expected)
        {
            Console.Error.WriteLine("Row counts don't match; rolling back.");
            return 1;
        }
    }
    await transaction.CommitAsync();
}

// Show where each identity column will continue from.
foreach (var table in tables.Where(t => t != "Ref"))
{
    await using var command = new SqlCommand($"SELECT IDENT_CURRENT('dbo.{table}'), (SELECT MAX(Id) FROM [dbo].[{table}])", connection);
    await using var reader = await command.ExecuteReaderAsync();
    await reader.ReadAsync();
    Console.WriteLine($"{table,-15} next id after {reader.GetValue(0)} (max id {reader.GetValue(1)})");
}
Console.WriteLine("Done.");
return 0;

static async Task<int> CountAsync(SqlConnection connection, SqlTransaction? transaction, string table)
{
    await using var command = new SqlCommand($"SELECT COUNT(*) FROM [dbo].[{table}]", connection, transaction);
    return (int)(await command.ExecuteScalarAsync())!;
}

// Builds the MariaDB connection string from the [client] section of a .my.cnf file, so the password never
// has to be on the command line or in the environment.
static string SourceConnectionString(string path, string database)
{
    var settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    var inClient = false;
    foreach (var raw in File.ReadAllLines(path))
    {
        var line = raw.Trim();
        if (line.StartsWith('['))
        {
            inClient = line.Equals("[client]", StringComparison.OrdinalIgnoreCase);
        }
        else if (inClient && line.Contains('=') && !line.StartsWith('#'))
        {
            var parts = line.Split('=', 2);
            settings[parts[0].Trim()] = parts[1].Trim().Trim('\'', '"');
        }
    }

    return new MySqlConnectionStringBuilder
    {
        Server = settings.GetValueOrDefault("host", "localhost"),
        Port = uint.Parse(settings.GetValueOrDefault("port", "3306")),
        UserID = settings["user"],
        Password = settings["password"],
        Database = database,
        CharacterSet = "utf8mb4",
    }.ConnectionString;
}

// Undoes UTF-8 text that was stored as if it were latin1 and then converted to UTF-8 again ("ðŸ°" -> "🍰").
// MariaDB's latin1 is Windows-1252, with its five unused bytes mapped to the matching control characters.
// Only changes text whose characters all map back to single bytes that form valid UTF-8 with something
// other than ASCII in it, so text that's already right is left alone.
static bool TryRepair(string text, out string repaired)
{
    repaired = text;
    var bytes = new byte[text.Length];
    for (var i = 0; i < text.Length; i++)
    {
        if (!Cp1252.TryGetByte(text[i], out bytes[i]))
        {
            return false;
        }
    }
    if (bytes.All(b => b < 0x80))
    {
        return false;
    }

    try
    {
        repaired = new UTF8Encoding(false, throwOnInvalidBytes: true).GetString(bytes);
        return true;
    }
    catch (DecoderFallbackException)
    {
        return false;
    }
}

static string Key(DataRow row) => row.Table.Columns.Contains("Id") ? row["Id"].ToString()! : $"'{row["Item"]}'";

static string Shorten(string text) => text.Length <= 70 ? text : text[..67] + "...";

static class Cp1252
{
    // The characters Windows-1252 puts at 0x80-0x9F. The unused bytes (0x81, 0x8D, 0x8F, 0x90, 0x9D) map to
    // themselves, as they do in MariaDB.
    private const string High = "€\u0081‚ƒ„…†‡ˆ‰Š‹Œ\u008DŽ\u008F\u0090‘’“”•–—˜™š›œ\u009DžŸ";

    public static bool TryGetByte(char c, out byte b)
    {
        var index = High.IndexOf(c);
        if (index >= 0)
        {
            b = (byte)(0x80 + index);
            return true;
        }
        if (c < 0x80 || (c >= 0xA0 && c <= 0xFF))
        {
            b = (byte)c;
            return true;
        }
        b = 0;
        return false;
    }
}
