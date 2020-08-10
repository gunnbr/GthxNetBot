using Gthx.Bot;
using Gthx.Bot.Interfaces;
using Gthx.Data;
using Gthx.Test.Mocks;
using GthxData;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Serilog;
using Serilog.Formatting.Json;
using System;
using System.IO;
using System.Linq;

namespace Gthx.Test
{
    public class SqlDataTestsStartup
    {
        private readonly IConfiguration _config;

        public SqlDataTestsStartup(IConfiguration config)
        {
            this._config = config;
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(configure => configure.AddConsole().AddSerilog()).AddTransient<GthxBot>();

            services.AddDbContext<GthxDataContext>(options =>
            {
                options.UseSqlServer(_config.GetConnectionString("GthxDb"));
            });

            services.AddSingleton<IGthxData, GthxSqlData>();
            services.AddSingleton<IWebReader, MockWebReader>();
        }
    }

    [TestFixture]
    public class GthxSqlDataTest
    {
        private readonly TestServer _server;
        private readonly GthxDataContext _Db;
        private readonly GthxSqlData _Data;
        private readonly IConfiguration _config;

        public GthxSqlDataTest()
        {
            _config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(_config)
                .WriteTo.File(new JsonFormatter(), @"c:\tmp\GthxSqlTests.json", shared: true)
                .CreateLogger();

            try
            {
                Log.Information("Serilog enabled for GthxSqlDataTests");
                _server = new TestServer(new WebHostBuilder().UseConfiguration(_config).UseStartup<SqlDataTestsStartup>().UseSerilog());
                _Db = _server.Host.Services.GetRequiredService<GthxDataContext>();
                _Data = (GthxSqlData)_server.Host.Services.GetService<IGthxData>();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "TestHost terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        [OneTimeSetUp]
        public void TestInitialise()
        {
            try
            {
                _Db.Database.EnsureCreated();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "TestInitialize failed to create the DB: {Message}", ex.Message);
            }
        }

        [OneTimeTearDown]
        public void TestTearDown()
        {
            try
            {
                _Db.Database.EnsureDeleted();
            }
            catch (Exception ex)
            {
                Log.Fatal("Failed to delete the DB: {Message}", ex.Message);
            }
        }

        [Test]
        public void GthxData_TestRefCount()
        {
            var testFactoid = "newFactoid";
            var testUser = "user1";
            var testValue = "a new factoid";

            // Verify initial conditions:
            // 1) No Ref for the factoid exists
            var refData = _Db.Ref.FirstOrDefault(r => r.Item == testFactoid);
            Assert.IsNull(refData, "Test factoid already in the Ref table");

            var factoidData = _Data.GetFactoid(testFactoid);
            // 2) That the factoid doesn't exist and that
            //    if none exists, the return value is null
            Assert.IsNull(factoidData, "Test factoid already exists");

            // Verify that referencing a factoid that doesn't exist doesn't
            // add a reference for it
            refData = _Db.Ref.FirstOrDefault(r => r.Item == testFactoid);
            Assert.IsNull(refData, "Asking about an unknown factoid added a reference");

            var success = _Data.AddFactoid(testUser, testFactoid, false, testValue, true);

            factoidData = _Data.GetFactoid(testFactoid);
            refData = _Db.Ref.FirstOrDefault(r => r.Item == testFactoid);
            Assert.NotNull(refData, "After asking about a factoid, it still doesn't exist in the Ref table");
            Assert.AreEqual(1, refData.Count, "Incorrect ref count after first ask");

            factoidData = _Data.GetFactoid(testFactoid);
            factoidData = _Data.GetFactoid(testFactoid);
            refData = _Db.Ref.FirstOrDefault(r => r.Item == testFactoid);
            Assert.AreEqual(3, refData.Count, "Incorrect ref count after third ask");
        }

        [Test]
        public void GthxData_TestFactoidHistory()
        {
            // Verify the factoid history and refcount get properly updated
            var testFactoid = "history";
            var testUser1 = "historyUser";
            var testUser2 = "historyPerson";
            var testUser3 = "historyTroll";
            var testValue1 = "a good thing to test";
            var testValue2 = "about the past";
            var testValue3 = "boring";

            var history = _Data.GetFactoidInfo(testFactoid);
            Assert.IsNull(history);

            _Data.AddFactoid(testUser1, testFactoid, false, testValue1, true);
            _Data.AddFactoid(testUser2, testFactoid, false, testValue2, false);
            _Data.AddFactoid(testUser3, testFactoid, false, testValue3, true);
            _Data.GetFactoid(testFactoid);
            _Data.GetFactoid(testFactoid);
            _Data.GetFactoid(testFactoid);

            history = _Data.GetFactoidInfo(testFactoid);
            Assert.AreEqual(3, history.RefCount);

            Assert.AreEqual(testUser3, history.InfoList[0].User);
            Assert.AreEqual(testFactoid, history.InfoList[0].Item);
            Assert.AreEqual(testValue3, history.InfoList[0].Value);

            Assert.AreEqual(testUser3, history.InfoList[1].User);
            Assert.AreEqual(testFactoid, history.InfoList[1].Item);
            Assert.IsNull(history.InfoList[1].Value);

            Assert.AreEqual(testUser2, history.InfoList[2].User);
            Assert.AreEqual(testFactoid, history.InfoList[2].Item);
            Assert.AreEqual(testValue2, history.InfoList[2].Value);

            Assert.AreEqual(testUser1, history.InfoList[3].User);
            Assert.AreEqual(testFactoid, history.InfoList[3].Item);
            Assert.AreEqual(testValue1, history.InfoList[3].Value);
        }

        [Test]
        public void GthxData_TestTell()
        {
            // Verify that GetTell() also clears the returned tells

            var toUser = "tellUser";
            var fromUser = "fromUser";
            var message = "Be sure to test tells";

            var tells = _Db.Tell.Where(t => t.Recipient == toUser);
            Assert.AreEqual(0, tells.Count(), "Tell exists at the start of the test");

            var tellData = _Data.GetTell(toUser);
            Assert.AreEqual(0, tellData.Count(), "Tell Data exists at the start of the test");

            _Data.AddTell(fromUser, toUser, message);

            tells = _Db.Tell.Where(t => t.Recipient == toUser);
            Assert.AreEqual(1, tells.Count(), "Tell not added to the DB");

            tellData = _Data.GetTell(toUser);
            Assert.AreEqual(1, tellData.Count(), "Tell not returned when it should be");
            Assert.AreEqual(toUser, tellData[0].Recipient);
            Assert.AreEqual(fromUser, tellData[0].Author);
            Assert.AreEqual(message, tellData[0].Message);

            tells = _Db.Tell.Where(t => t.Recipient == toUser);
            Assert.AreEqual(0, tells.Count(), "Tell still exists after being returned");

            tellData = _Data.GetTell(toUser);
            Assert.AreEqual(0, tellData.Count(), "Tell Data still exists after being returned");
        }

        [Test]
        public void GthxData_TestSeenRequests()
        {
            var testChannel = "#reprap";

            _Data.UpdateLastSeen(testChannel, "gunnbr", "message 1");
            _Data.UpdateLastSeen(testChannel, "gunnbr_", "message 2");
            _Data.UpdateLastSeen(testChannel, "gunner", "message 3");

            var lastSeen = _Data.GetLastSeen("gunn");
            Assert.AreEqual(3, lastSeen.Count);
            var names = lastSeen.Select(s => s.User).ToList();
            Assert.IsTrue(names.Contains("gunnbr"));
            Assert.IsTrue(names.Contains("gunnbr_"));
            Assert.IsTrue(names.Contains("gunner"));

            // Test also with an asterisk cause people use that.
            lastSeen = _Data.GetLastSeen("gunn*");
            Assert.AreEqual(3, lastSeen.Count);
            names = lastSeen.Select(s => s.User).ToList();
            Assert.IsTrue(names.Contains("gunnbr"));
            Assert.IsTrue(names.Contains("gunnbr_"));
            Assert.IsTrue(names.Contains("gunner"));
        }
    }
}
