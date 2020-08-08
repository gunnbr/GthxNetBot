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
using System.Threading.Tasks;

namespace Gthx.Test
{
    public class IntegrationTestsStartup
    {
        private readonly IConfiguration _config;

        public IntegrationTestsStartup(IConfiguration config)
        {
            _config = config;
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
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
            services.AddSingleton<IWebReader, WebReader>();
            services.AddSingleton<IIrcClient, MockIrcClient>();
            services.AddGthxBot();
            services.AddSingleton<GthxBot>();
            services.AddSingleton(_config);
        }
    }

    [TestFixture]
    public class IntegrationTests
    {
        private readonly TestServer _server;
        private readonly GthxDataContext _Db;
        private readonly GthxBot _gthx;
        private readonly MockIrcClient _client;
        private readonly GthxSqlData _data;
        private readonly IConfigurationRoot _config;

        public IntegrationTests()
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
                Log.Information("Serilog enabled for IntegrationTests");
                _server = new TestServer(new WebHostBuilder()
                    .UseConfiguration(_config)
                    .UseStartup<IntegrationTestsStartup>()
                    .UseSerilog());
                _Db = _server.Host.Services.GetRequiredService<GthxDataContext>();
                _data = _server.Host.Services.GetService<IGthxData>() as GthxSqlData;
                _client = _server.Host.Services.GetService<IIrcClient>() as MockIrcClient;
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

        // TODO: Add integration test for "seen user*" to verify the asterisk is handled correctly

        [Test]
        public async Task TestLiveYoutubeReferences()
        {
            var testChannel = "#reprap";
            var testUser = "BobYourUncle";
            _gthx.HandleReceivedMessage(testChannel, testUser, $"OMG! Check this out! https://www.youtube.com/watch?v=I7nVrT00ST4");
            await Task.Delay(5000);
            var replies = _client.GetReplies();
            Assert.AreEqual(1, replies.Messages.Count);
            Assert.AreEqual(testChannel, replies.Channel);
            Assert.AreEqual($"{testUser} linked to YouTube video \"Pro Riders Laughing\" => 1 IRC mentions", replies.Messages[0]);

            // Test non-Western characters
            testUser = "AndrewJohnson";
            _gthx.HandleReceivedMessage(testChannel, testUser, $"Calm down and listen to this: https://www.youtube.com/watch?v=xtAHgrNs7r4");
            await Task.Delay(5000);
            replies = _client.GetReplies();
            Assert.AreEqual(1, replies.Messages.Count);
            Assert.AreEqual(testChannel, replies.Channel);
            Assert.AreEqual($"{testUser} linked to YouTube video \"Studio Ghibli - Music Collection (Piano and Violin Duo) 株式会社スタジオジブリ- Relaxing music song\" => 1 IRC mentions", replies.Messages[0]);
        }

        [Test]
        public async Task TestLiveThingiverseReferences()
        {
            // Test fetching a new title that uses the <title> element
            var testChannel = "#reprap";
            var testUser = "RandomNick";
            _gthx.HandleReceivedMessage(testChannel, testUser, $"Your daughter would really like this: https://www.thingiverse.com/thing:2810756");
            await Task.Delay(5000);
            var replies = _client.GetReplies();
            Assert.AreEqual(1, replies.Messages.Count);
            Assert.AreEqual(testChannel, replies.Channel);
            Assert.AreEqual($"{testUser} linked to \"Articulated Butterfly by 8ran\" on thingiverse => 1 IRC mentions", replies.Messages[0]);
        }

        /// <summary>
        /// Verify that the command to get a factoid returns it after
        /// the factoid is set.
        /// </summary>
        [Test]
        public void TestFactoidSetAndGet()
        {
            var testFactoid = "testFactoid";
            var testValue = "working";
            var testChannel = "#reprap";
            var testUser = "SomeUser";

            _gthx.HandleReceivedMessage(testChannel, testUser, $"{testFactoid} is {testValue}");
            var replies = _client.GetReplies();
            Assert.AreEqual(1, replies.Messages.Count);
            Assert.AreEqual(testChannel, replies.Channel);
            Assert.AreEqual($"{testUser}: Okay.", replies.Messages[0]);

            _gthx.HandleReceivedMessage(testChannel, testUser, $"{testFactoid}?");
            replies = _client.GetReplies();
            Assert.AreEqual(1, replies.Messages.Count);
            Assert.AreEqual(testChannel, replies.Channel);
            Assert.AreEqual($"{testFactoid} is {testValue}", replies.Messages[0]);
        }
    }
}
