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
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using NUnit.Framework;
using Serilog;
using Serilog.Formatting.Json;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace Gthx.Test
{
    public class SqlDataTestsStartup
    {
        private readonly IConfiguration _config;

        public SqlDataTestsStartup(IConfiguration config)
        {
            this._config = config;
        }

        public void Configure(IApplicationBuilder _app, IWebHostEnvironment _env)
        {
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(configure => configure.AddConsole().AddSerilog()).AddTransient<GthxBot>();

            services.AddDbContext<GthxDataContext>(options =>
            {
                var connString = _config.GetConnectionString("GthxDb");
                options.UseSqlServer(connString, x => x.MigrationsAssembly("SqlServerMigrations.Migrations"));
            });

            services.AddSingleton<IGthxData, GthxSqlData>();
            services.AddSingleton<IWebReader, MockWebReader>();
        }
    }

    [TestFixture]
    public class GthxSqlDataTest
    {
        private readonly TestServer _server;
        private readonly IHost _host;
        private readonly GthxDataContext _Db;
        private readonly GthxSqlData _Data;
        private readonly IConfiguration _config;

        public GthxSqlDataTest()
        {
            // Use the SQL Server container connection string
            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false);

            // Override the connection string for tests
            var containerConnString = SqlServerTestContainerSetUp.GetConnectionStringAsync().GetAwaiter().GetResult();
            var sqlBuilder = new SqlConnectionStringBuilder(containerConnString)
            {
                InitialCatalog = "GthxSqlDataTests"
            };

            configBuilder.AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string>("ConnectionStrings:GthxDb", sqlBuilder.ConnectionString)
            });
            _config = configBuilder.Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(_config)
                .WriteTo.File(new JsonFormatter(), Path.Combine(Path.GetTempPath(), "GthxSqlTests.json"), shared: true)
                .CreateLogger();

            try
            {
                Log.Information("Serilog enabled for GthxSqlDataTests");
                _host = new HostBuilder()
                    .ConfigureAppConfiguration(builder => builder.AddConfiguration(_config))
                    .ConfigureWebHost(webHostBuilder => webHostBuilder.UseTestServer().UseStartup<SqlDataTestsStartup>())
                    .UseSerilog()
                    .Start();
                _server = _host.GetTestServer();
                _Db = _host.Services.GetRequiredService<GthxDataContext>();
                _Data = (GthxSqlData)_host.Services.GetService<IGthxData>();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "TestHost terminated unexpectedly");
                throw;
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
            ClassicAssert.IsNull(refData, "Test factoid already in the Ref table");

            var factoidData = _Data.GetFactoid(testFactoid);
            // 2) That the factoid doesn't exist and that
            //    if none exists, the return value is null
            ClassicAssert.IsNull(factoidData, "Test factoid already exists");

            // Verify that referencing a factoid that doesn't exist doesn't
            // add a reference for it
            refData = _Db.Ref.FirstOrDefault(r => r.Item == testFactoid);
            ClassicAssert.IsNull(refData, "Asking about an unknown factoid added a reference");

            var success = _Data.AddFactoid(testUser, testFactoid, false, testValue, true);

            factoidData = _Data.GetFactoid(testFactoid);
            refData = _Db.Ref.FirstOrDefault(r => r.Item == testFactoid);
            ClassicAssert.NotNull(refData, "After asking about a factoid, it still doesn't exist in the Ref table");
            ClassicAssert.AreEqual(1, refData.Count, "Incorrect ref count after first ask");

            factoidData = _Data.GetFactoid(testFactoid);
            factoidData = _Data.GetFactoid(testFactoid);
            refData = _Db.Ref.FirstOrDefault(r => r.Item == testFactoid);
            ClassicAssert.AreEqual(3, refData.Count, "Incorrect ref count after third ask");
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
            ClassicAssert.IsNull(history);

            _Data.AddFactoid(testUser1, testFactoid, false, testValue1, true);
            _Data.AddFactoid(testUser2, testFactoid, false, testValue2, false);
            _Data.AddFactoid(testUser3, testFactoid, false, testValue3, true);
            _Data.GetFactoid(testFactoid);
            _Data.GetFactoid(testFactoid);
            _Data.GetFactoid(testFactoid);

            history = _Data.GetFactoidInfo(testFactoid);
            ClassicAssert.AreEqual(3, history.RefCount);

            ClassicAssert.AreEqual(testUser3, history.InfoList[0].User);
            ClassicAssert.AreEqual(testFactoid, history.InfoList[0].Item);
            ClassicAssert.AreEqual(testValue3, history.InfoList[0].Value);

            ClassicAssert.AreEqual(testUser3, history.InfoList[1].User);
            ClassicAssert.AreEqual(testFactoid, history.InfoList[1].Item);
            ClassicAssert.IsNull(history.InfoList[1].Value);

            ClassicAssert.AreEqual(testUser2, history.InfoList[2].User);
            ClassicAssert.AreEqual(testFactoid, history.InfoList[2].Item);
            ClassicAssert.AreEqual(testValue2, history.InfoList[2].Value);

            ClassicAssert.AreEqual(testUser1, history.InfoList[3].User);
            ClassicAssert.AreEqual(testFactoid, history.InfoList[3].Item);
            ClassicAssert.AreEqual(testValue1, history.InfoList[3].Value);
        }

        [Test]
        public void GthxData_TestTell()
        {
            // Verify that GetTell() also clears the returned tells

            var toUser = "tellUser";
            var fromUser = "fromUser";
            var message = "Be sure to test tells";

            var tells = System.Linq.Queryable.Where(_Db.Tell, t => t.Recipient == toUser);
            ClassicAssert.AreEqual(0, tells.Count(), "Tell exists at the start of the test");

            var tellData = _Data.GetTell(toUser);
            ClassicAssert.AreEqual(0, tellData.Count, "Tell Data exists at the start of the test");

            _Data.AddTell(fromUser, toUser, message);

            tells = System.Linq.Queryable.Where(_Db.Tell, t => t.Recipient == toUser);
            ClassicAssert.AreEqual(1, tells.Count(), "Tell not added to the DB");

            tellData = _Data.GetTell(toUser);
            ClassicAssert.AreEqual(1, tellData.Count, "Tell not returned when it should be");
            ClassicAssert.AreEqual(toUser, tellData[0].Recipient);
            ClassicAssert.AreEqual(fromUser, tellData[0].Author);
            ClassicAssert.AreEqual(message, tellData[0].Message);

            tells = System.Linq.Queryable.Where(_Db.Tell, t => t.Recipient == toUser);
            ClassicAssert.AreEqual(0, tells.Count(), "Tell still exists after being returned");

            tellData = _Data.GetTell(toUser);
            ClassicAssert.AreEqual(0, tellData.Count, "Tell Data still exists after being returned");
        }

        [Test]
        public void GthxData_TestEmailKeywords()
        {
            _Db.EmailKeyword.Add(new Gthx.Core.EmailKeyword() { Keyword = "sqlTestKeyword", User = "web:tester", Timestamp = DateTime.UtcNow });
            _Db.SaveChanges();

            CollectionAssert.Contains(_Data.GetEmailKeywords(), "sqlTestKeyword");

            _Db.EmailKeyword.Where(k => k.Keyword == "sqlTestKeyword").ExecuteDelete();
        }

        [Test]
        public void GthxData_TestReminders()
        {
            var author = "reminderUser";
            var channel = "#reprap";
            var now = DateTime.UtcNow;

            ClassicAssert.AreEqual(0, _Data.GetReminders(author).Count, "Reminders exist at the start of the test");

            var later = _Data.AddReminder(author, channel, "to do it later", now.AddHours(2));
            var soon = _Data.AddReminder(author, channel, "to do it soon", now.AddMinutes(-1));
            var other = _Data.AddReminder("otherReminderUser", channel, "to do their thing", now.AddMinutes(-2));
            ClassicAssert.AreNotEqual(0, later.Id);

            // Nicks are matched without regard to case
            var reminders = _Data.GetReminders(author.ToUpperInvariant());
            CollectionAssert.AreEqual(new[] { soon.Id, later.Id }, reminders.Select(r => r.Id).ToList(), "Not sorted by due time");
            ClassicAssert.AreEqual("to do it soon", reminders[0].Message);
            ClassicAssert.AreEqual(channel, reminders[0].Channel);

            var due = _Data.GetDueReminders(now).Select(r => r.Id).ToList();
            CollectionAssert.Contains(due, soon.Id);
            CollectionAssert.Contains(due, other.Id);
            CollectionAssert.DoesNotContain(due, later.Id);

            // Only the author can cancel a reminder
            ClassicAssert.IsFalse(_Data.CancelReminder(author, other.Id));
            ClassicAssert.IsTrue(_Data.CancelReminder(author, later.Id));
            ClassicAssert.IsFalse(_Data.CancelReminder(author, later.Id));

            var taken = _Data.TakeDueReminders(author, now);
            CollectionAssert.AreEqual(new[] { soon.Id }, taken.Select(r => r.Id).ToList());
            ClassicAssert.AreEqual(0, _Data.GetReminders(author).Count, "Reminders still exist after being taken");

            // The timer and the message handling use separate contexts: only one of them gets to deliver it.
            using (var scope = _host.Services.CreateScope())
            {
                var otherData = new GthxSqlData(scope.ServiceProvider.GetRequiredService<GthxDataContext>());
                ClassicAssert.AreEqual(1, otherData.GetDueReminders(now).Count(r => r.Id == other.Id));
                ClassicAssert.IsTrue(_Data.DeleteReminder(other.Id));
                ClassicAssert.IsFalse(otherData.DeleteReminder(other.Id));
            }
            ClassicAssert.IsFalse(_Data.DeleteReminder(other.Id));
        }

        [Test]
        public void GthxData_TestSeenRequests()
        {
            var testChannel = "#reprap";

            _Data.UpdateLastSeen(testChannel, "gunnbr", "message 1");
            _Data.UpdateLastSeen(testChannel, "gunnbr_", "message 2");
            _Data.UpdateLastSeen(testChannel, "gunner", "message 3");

            var lastSeen = _Data.GetLastSeen("gunn");
            ClassicAssert.AreEqual(3, lastSeen.Count);
            var names = lastSeen.Select(s => s.User).ToList();
            ClassicAssert.IsTrue(names.Contains("gunnbr"));
            ClassicAssert.IsTrue(names.Contains("gunnbr_"));
            ClassicAssert.IsTrue(names.Contains("gunner"));

            // Test also with an asterisk cause people use that.
            lastSeen = _Data.GetLastSeen("gunn*");
            ClassicAssert.AreEqual(3, lastSeen.Count);
            names = lastSeen.Select(s => s.User).ToList();
            ClassicAssert.IsTrue(names.Contains("gunnbr"));
            ClassicAssert.IsTrue(names.Contains("gunnbr_"));
            ClassicAssert.IsTrue(names.Contains("gunner"));
        }

        [Test]
        public void GthxData_TestYouTubeRefCount()
        {
            var testItem = "12345abcde";
            var testTitle = "My first YouTube video";

            var refData = _Data.AddYoutubeReference(testItem);
            ClassicAssert.AreEqual(testItem, refData.Item, "Wrong item returned");
            ClassicAssert.AreEqual(1, refData.Count, "Wrong initial count value");
            ClassicAssert.IsNull(refData.Title, "Test failure: Title already exists");

            // Mainly test that this doesn't throw an exception
            _Data.AddYoutubeTitle(testItem, testTitle);

            // Verify that we have the title now and that the refcount was updated
            refData = _Data.AddYoutubeReference(testItem);
            ClassicAssert.AreEqual(testItem, refData.Item, "Wrong item returned");
            ClassicAssert.AreEqual(2, refData.Count, "Count not updated");
            ClassicAssert.AreEqual(testTitle, refData.Title, "Title not available in second attempt");
        }

        [Test]
        public void GthxData_TestThingiRefCount()
        {
            var testItem = "12345";
            var testTitle = "Spiral Gears";

            var refData = _Data.AddThingiverseReference(testItem);
            ClassicAssert.AreEqual(testItem, refData.Item.ToString(), "Wrong item returned");
            ClassicAssert.AreEqual(1, refData.Count, "Wrong initial count value");
            ClassicAssert.IsNull(refData.Title, "Test failure: Title already exists");

            // Mainly test that this doesn't throw an exception
            _Data.AddThingiverseTitle(testItem, testTitle);

            // Verify that we have the title now and that the refcount was updated
            refData = _Data.AddThingiverseReference(testItem);
            ClassicAssert.AreEqual(testItem, refData.Item.ToString(), "Wrong item returned");
            ClassicAssert.AreEqual(2, refData.Count, "Count not updated");
            ClassicAssert.AreEqual(testTitle, refData.Title, "Title not available in second attempt");
        }

        [Test]
        public void GthxData_TestPrintablesRefCount()
        {
            var testItem = 1863827;
            var testTitle = "Heavy duty keyboard holder";

            var refData = _Data.AddPrintablesReference(testItem);
            ClassicAssert.AreEqual(testItem, refData.Item, "Wrong item returned");
            ClassicAssert.AreEqual(1, refData.Count, "Wrong initial count value");
            ClassicAssert.IsNull(refData.Title, "Test failure: Title already exists");

            _Data.AddPrintablesTitle(testItem, testTitle);

            refData = _Data.AddPrintablesReference(testItem);
            ClassicAssert.AreEqual(2, refData.Count, "Count not updated");
            ClassicAssert.AreEqual(testTitle, refData.Title, "Title not available in second attempt");

            // The longest title the bot sends fits the column
            var longTitle = GthxUtil.CleanTitle(new string('x', 400));
            _Data.AddPrintablesTitle(testItem, longTitle);
            ClassicAssert.AreEqual(longTitle, _Data.AddPrintablesReference(testItem).Title);
        }

        [Test]
        public void GthxData_TestMakerWorldRefCount()
        {
            var testItem = 2459711;
            var testTitle = "Articulated dragon";

            var refData = _Data.AddMakerWorldReference(testItem);
            ClassicAssert.AreEqual(testItem, refData.Item, "Wrong item returned");
            ClassicAssert.AreEqual(1, refData.Count, "Wrong initial count value");
            ClassicAssert.IsNull(refData.Title, "Test failure: Title already exists");

            _Data.AddMakerWorldTitle(testItem, testTitle);

            refData = _Data.AddMakerWorldReference(testItem);
            ClassicAssert.AreEqual(2, refData.Count, "Count not updated");
            ClassicAssert.AreEqual(testTitle, refData.Title, "Title not available in second attempt");
        }
    }
}
