var builder = DistributedApplication.CreateBuilder(args);

// SQL Server container with a persistent data volume so the database survives restarts.
var sql = builder.AddSqlServer("sql")
    .WithDataVolume();

// Database resource. The connection string is exposed to referencing projects as "GthxDb".
var gthxDb = sql.AddDatabase("GthxDb");

// SASL password for the IRC nickname, supplied as a secret parameter. Store it with:
//   dotnet user-secrets set "Parameters:sasl-password" "<password>"
// (run from the GthxNetBot.AppHost project directory). It is injected into the bot as the
// "IrcInfo:SaslPassword" configuration value via an environment variable.
var saslPassword = builder.AddParameter("sasl-password", secret: true);

// The IRC bot. Waits for SQL Server to be healthy and receives the "GthxDb" connection string.
builder.AddProject<Projects.GthxNetBot>("gthxbot")
    .WithReference(gthxDb)
    .WithEnvironment("IrcInfo__SaslPassword", saslPassword)
    .WaitFor(gthxDb);

builder.Build().Run();
