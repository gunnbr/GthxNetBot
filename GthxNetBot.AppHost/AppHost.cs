var builder = DistributedApplication.CreateBuilder(args);

// SQL Server container with a persistent data volume so the database survives restarts.
var sql = builder.AddSqlServer("sql")
    .WithDataVolume();

// Database resource. The connection string is exposed to referencing projects as "GthxDb".
var gthxDb = sql.AddDatabase("GthxDb");

// The IRC bot. Waits for SQL Server to be healthy and receives the "GthxDb" connection string.
builder.AddProject<Projects.GthxNetBot>("gthxbot")
    .WithReference(gthxDb)
    .WaitFor(gthxDb);

builder.Build().Run();
