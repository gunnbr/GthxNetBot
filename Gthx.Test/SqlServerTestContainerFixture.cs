using System;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace Gthx.Test
{
    public class SqlServerTestContainerFixture : IAsyncDisposable
    {
        private readonly TestcontainersContainer _container;
        public string ConnectionString { get; private set; }

        public SqlServerTestContainerFixture()
        {
            _container = new TestcontainersBuilder<TestcontainersContainer>()
                .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
                .WithEnvironment("ACCEPT_EULA", "Y")
                .WithEnvironment("SA_PASSWORD", "Your_password123")
                .WithPortBinding(1433, true)
                .WithWaitStrategy(
                    Wait.ForUnixContainer()
                        .UntilPortIsAvailable(1433))
                .Build();
        }

        public async Task InitializeAsync()
        {
            await _container.StartAsync();
            var mappedPort = _container.GetMappedPublicPort(1433);
            ConnectionString = $"Server=127.0.0.1,{mappedPort};Database=GthxNetBotTest;User Id=sa;Password=Your_password123;TrustServerCertificate=True;";
        }

        public async ValueTask DisposeAsync()
        {
            await _container.DisposeAsync();
        }
    }
}
