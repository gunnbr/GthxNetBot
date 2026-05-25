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
                    SqlServerFixture = new SqlServerTestContainerFixture();
                    await SqlServerFixture.InitializeAsync();
                }

                return SqlServerFixture.ConnectionString;
            }
            finally
            {
                _syncLock.Release();
            }
        }

        [OneTimeSetUp]
        public async Task GlobalSetup()
        {
            await GetConnectionStringAsync();
        }

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
