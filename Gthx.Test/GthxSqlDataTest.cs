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

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {

        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(configure => configure.AddConsole().AddSerilog()).AddTransient<GthxBot>();

            services.AddDbContextPool<GthxDataContext>(options =>
            {
                options.UseSqlServer(_config.GetConnectionString("GthxTestDb"));
            });

            //services.AddDbContext<GthxDataContext>(options => options.UseSqlServer(_config.GetConnectionString("GthxTestDb")),
            //    ServiceLifetime.Singleton);

            services.AddScoped<GthxDataContext>();
            services.AddSingleton<IIrcClient, MockIrcClient>();
            services.AddSingleton<IGthxData, GthxSqlData>();
            services.AddSingleton<IWebReader, MockWebReader>();
            services.AddSingleton<GthxDataContext>();
            GthxBot.RegisterServices(services as ServiceCollection);
            //var sc = services as ServiceCollection;
            //sc.AddLogging(configure => configure.AddConsole()).AddTransient<GthxTests>();
            services.AddSingleton(_config);
            services.AddSingleton<GthxBot>();
            //services.AddScoped<IGthxData>(provider => (IGthxData)provider.GetService<GthxDataContext>());
        }
    }

    [TestFixture]
    public class GthxSqlDataTest
    {
        private TestServer _server;
        private GthxDataContext _Db;
        private GthxSqlData _Data;
        private IIrcClient _client;
        private GthxBot _gthx;
        private readonly IConfiguration _config;

        public GthxSqlDataTest()
        {
            _config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            // TODO:
            // * Use this logger to get the correct DB loaded from appsettings.json
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(_config)
                .WriteTo.File(new JsonFormatter(), @"c:\tmp\GthxSqlTests.json", shared: true)
                .CreateLogger();

            try
            {
                Log.Information("------------------------------------------Serilog enabled for GthxSqlDataTests");

                _server = new TestServer(new WebHostBuilder().UseStartup<SqlDataTestsStartup>().UseSerilog());
                _Db = _server.Host.Services.GetRequiredService<GthxDataContext>();
                _Data = (GthxSqlData)_server.Host.Services.GetService<IGthxData>();
                _client = _server.Host.Services.GetService<IIrcClient>();
                _gthx = _server.Host.Services.GetRequiredService<GthxBot>();
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
            _Db.Database.EnsureCreated();
        }

        [OneTimeTearDown]
        public void TestTearDown()
        {
            _Db.Database.EnsureDeleted();
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
    }
}
