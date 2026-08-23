using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;

namespace Gthx.Test
{
    [SetUpFixture]
    public class SqlServerTestContainerSetUp
    {
        private static readonly SemaphoreSlim _syncLock = new(1, 1);
        public static SqlServerTestContainerFixture SqlServerFixture { get; private set; }

        public static async Task<string> GetConnectionStringAsync()
        {
            if (SqlServerFixture != null)
            {
                return SqlServerFixture.ConnectionString;
            }

            await _syncLock.WaitAsync();
            try
            {
                if (SqlServerFixture == null)
                {
                    // Only publish the fixture after initialization succeeds. Otherwise a failed
                    // container start / readiness probe would leave a non-null but unready fixture
                    // that later callers reuse instead of retrying.
                    var fixture = new SqlServerTestContainerFixture();
                    await fixture.InitializeAsync();
                    SqlServerFixture = fixture;
                }

                return SqlServerFixture.ConnectionString;
            }
            finally
            {
                _syncLock.Release();
            }
        }

        // NOTE: The container is intentionally NOT started in a [OneTimeSetUp]. Doing so would
        // force every test in the Gthx.Test namespace (including mock-only unit tests) to require
        // Docker. Instead, the container starts lazily the first time a SQL integration test calls
        // GetConnectionStringAsync, and GlobalTeardown disposes it only if it was actually created.
        [OneTimeTearDown]
        public async Task GlobalTeardown()
        {
            await _syncLock.WaitAsync();
            try
            {
                if (SqlServerFixture != null)
                {
                    await SqlServerFixture.DisposeAsync();
                    SqlServerFixture = null;
                }
            }
            finally
            {
                _syncLock.Release();
            }
        }
    }
}
