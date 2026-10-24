using System;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

namespace Gthx.Test
{
    public class SqlServerTestContainerFixture : IAsyncDisposable
    {
        private const string SaPassword = "Your_password123";
        private readonly MsSqlContainer _container;
        public string ConnectionString { get; private set; }

        public SqlServerTestContainerFixture()
        {
            _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
                .WithPassword(SaPassword)
                .Build();
        }

        public async Task InitializeAsync()
        {
            await _container.StartAsync();
            var mappedPort = _container.GetMappedPublicPort(MsSqlBuilder.MsSqlPort);
            ConnectionString = $"Server=127.0.0.1,{mappedPort};Database=GthxNetBotTest;User Id=sa;Password={SaPassword};TrustServerCertificate=True;";

            // Waiting only for the TCP port is not sufficient: SQL Server accepts connections
            // before the sa login and database engine are ready. Poll with a real login so tests
            // do not start against a not-yet-ready server and fail intermittently.
            var probeConnectionString = $"Server=127.0.0.1,{mappedPort};Database=master;User Id=sa;Password={SaPassword};TrustServerCertificate=True;";
            await WaitForSqlServerReadyAsync(probeConnectionString);
        }

        private static async Task WaitForSqlServerReadyAsync(string connectionString, int maxAttempts = 30)
        {
            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    await using var connection = new SqlConnection(connectionString);
                    await connection.OpenAsync();
                    await using var command = connection.CreateCommand();
                    command.CommandText = "SELECT 1";
                    await command.ExecuteScalarAsync();
                    return;
                }
                catch (SqlException)
                {
                    if (attempt == maxAttempts)
                    {
                        throw;
                    }

                    await Task.Delay(TimeSpan.FromSeconds(1));
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            await _container.DisposeAsync();
        }
    }
}
