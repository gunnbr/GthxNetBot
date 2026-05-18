using NUnit.Framework;
using System.Threading.Tasks;

namespace Gthx.Test
{
    [SetUpFixture]
    public class SqlServerTestContainerSetUp
    {
        public static SqlServerTestContainerFixture SqlServerFixture { get; private set; }

        [OneTimeSetUp]
        public async Task GlobalSetup()
        {
            SqlServerFixture = new SqlServerTestContainerFixture();
            await SqlServerFixture.InitializeAsync();
        }

        [OneTimeTearDown]
        public async Task GlobalTeardown()
        {
            if (SqlServerFixture != null)
            {
                await SqlServerFixture.DisposeAsync();
            }
        }
    }
}
