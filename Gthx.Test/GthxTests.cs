using Gthx.Bot;
using Gthx.Bot.Interfaces;
using Gthx.Data;
using Gthx.Test.Mocks;
using GthxData;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Serilog;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Gthx.Test
{
    public class GthxTestsStartup
    {
        private readonly IConfiguration _config;

        public GthxTestsStartup(IConfiguration config)
        {
            _config = config;
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(configure => configure.AddConsole().AddSerilog()).AddTransient<GthxTests>();

            services.TryAddSingleton<MockIrcClient>();
            services.AddSingleton<IIrcClient>(sp => sp.GetRequiredService<MockIrcClient>());
            services.AddSingleton<IBotNick>(sp => sp.GetRequiredService<MockIrcClient>());
            services.AddSingleton<IGthxData, MockData>();
            services.AddSingleton<IWebReader, MockWebReader>();
            services.AddSingleton<IGthxUtil, GthxUtil>();
            services.AddSingleton<GthxDataContext>();
            services.AddGthxBot();
            services.AddSingleton(_config);
            services.TryAddSingleton<GthxMessageConduit>();
            services.TryAddSingleton<IGthxMessageConduit>(s => s.GetRequiredService<GthxMessageConduit>());
            services.TryAddSingleton<IGthxMessageConsumer>(s => s.GetRequiredService<GthxMessageConduit>());
            services.AddSingleton<GthxBot>();
        }
    }

    [TestFixture]
    public class GthxTests
    {
        private readonly TestServer _server;
        private readonly IHost _host;
        private readonly GthxBot _gthx;
        private readonly MockIrcClient _client;
        private readonly MockData _data;
        private readonly IConfigurationRoot _config;
        private readonly Microsoft.Extensions.Logging.ILogger _logger;

        public GthxTests()
        {
            _config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(_config)
                .WriteTo.Console()
                .CreateLogger();

            try
            {
                Log.Information("Serilog enabled for GthxSqlDataTests");
                _host = new HostBuilder()
                    .ConfigureAppConfiguration(builder => builder.AddConfiguration(_config))
                    .ConfigureWebHost(webHostBuilder => webHostBuilder.UseTestServer().UseStartup<GthxTestsStartup>())
                    .UseSerilog()
                    .Start();
                _server = _host.GetTestServer();
                _data = (MockData)_host.Services.GetService<IGthxData>();
                _client = (MockIrcClient)_host.Services.GetService<IIrcClient>();
                _gthx = _host.Services.GetRequiredService<GthxBot>();
                _logger = _host.Services.GetService<ILogger<GthxTests>>();
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

        [Test]
        public void TestGenericResponse()
        {
            // Test channel message
            var testChannel = "#reprap";
            var testUser = "SomeUser";
            _gthx.HandleReceivedMessage(testChannel, testUser, "Which printer is best?");

            var replies = _client.GetReplies();
            ClassicAssert.AreEqual(0, replies.Messages.Count);

            // Test DM
            testChannel = "_gthx";
            testUser = "SomeOtherUser";
            _gthx.HandleReceivedMessage(testChannel, testUser, "Hey, can you help me?");

            replies = _client.GetReplies();
            ClassicAssert.AreEqual(0, replies.Messages.Count);
        }

        [Test]
        public void TestFactoidSetting()
        {
            var testFactoid = "testFactoid";
            var testValue = "working";
            var testValue2 = "is working better";
            var testChannel = "#reprap";
            var testUser = "SomeUser";
            var testUser2 = "AnotherUser";

            _gthx.HandleReceivedMessage(testChannel, testUser, $"{testFactoid} is {testValue}");
            var replies = _client.GetReplies();
            ClassicAssert.AreEqual(0, replies.Messages.Count);

            _gthx.HandleReceivedMessage(testChannel, testUser, $"{_client.BotNick}: {testFactoid} is {testValue}");
            replies = _client.GetReplies();
            ClassicAssert.AreEqual(1, replies.Messages.Count);
            ClassicAssert.AreEqual(testChannel, replies.Channel);
            ClassicAssert.AreEqual($"{testUser}: Okay.", replies.Messages[0]);
            ClassicAssert.AreEqual(testFactoid, _data.FactoidItem);
            ClassicAssert.AreEqual(testValue, _data.FactoidValue);
            ClassicAssert.IsFalse(_data.FactoidIsAre);
            ClassicAssert.AreEqual(testUser, _data.FactoidUser);
            ClassicAssert.IsTrue(_data.FactoidReplaceExisting);

            _gthx.HandleReceivedMessage(testChannel, testUser2, $"{_client.BotNick}: {testFactoid} are also {testValue2}");

            replies = _client.GetReplies();
            ClassicAssert.AreEqual(1, replies.Messages.Count);
            ClassicAssert.AreEqual(testChannel, replies.Channel);
            ClassicAssert.AreEqual($"{testUser2}: Okay.", replies.Messages[0]);
            ClassicAssert.AreEqual(testFactoid, _data.FactoidItem);
            ClassicAssert.AreEqual(testValue2, _data.FactoidValue);
            ClassicAssert.IsTrue(_data.FactoidIsAre);
            ClassicAssert.AreEqual(testUser2, _data.FactoidUser);
            ClassicAssert.IsFalse(_data.FactoidReplaceExisting);
        }

        [Test]
        public void TestFactoidSettingInPM()
        {
            // Issue #24: "/msg gthx gthx: foo is bar" replied "Okay." but set the factoid "gthx: foo"
            var testFactoid = "pmFactoid";
            var testValue = "set in a query";
            var testUser = "SomeUser";

            _gthx.HandleReceivedMessage(testUser, testUser, $"{testFactoid} is {testValue}");
            var replies = _client.GetReplies();
            ClassicAssert.AreEqual(1, replies.Messages.Count);
            ClassicAssert.AreEqual(testUser, replies.Channel);
            ClassicAssert.AreEqual(testFactoid, _data.FactoidItem);
            ClassicAssert.AreEqual(testValue, _data.FactoidValue);

            _gthx.HandleReceivedMessage(testUser, testUser, $"{_client.BotNick}: {testFactoid} is {testValue}");
            replies = _client.GetReplies();
            ClassicAssert.AreEqual(1, replies.Messages.Count);
            ClassicAssert.AreEqual(testUser, replies.Channel);
            ClassicAssert.AreEqual($"{testUser}: Okay.", replies.Messages[0]);
            ClassicAssert.AreEqual(testFactoid, _data.FactoidItem);
            ClassicAssert.AreEqual(testValue, _data.FactoidValue);
        }

        [Test]
        public void TestLockedFactoidSetting()
        {
            var testFactoid = "locked factoid";
            var testValue = "something that can't be changed.";
            var testChannel = "#reprap";
            var testUser = "MaliciousUser";

            _gthx.HandleReceivedMessage(testChannel, testUser, $"{_client.BotNick} {testFactoid} is {testValue}");

            var replies = _client.GetReplies();
            ClassicAssert.AreEqual(1, replies.Messages.Count);
            ClassicAssert.AreEqual(testChannel, replies.Channel);
            ClassicAssert.AreEqual($"I'm sorry, {testUser}. I'm afraid I can't do that.", replies.Messages[0]);
        }

        [Test]
        public void TestFactoidGetting()
        {
            // Test "is"
            var testChannel = "#reprap";
            var testUser = "sandman";
            var testFactoid = "reprap";

            _gthx.HandleReceivedMessage(testChannel, testUser, $"{testFactoid}?");

            var replies = _client.GetReplies();
            ClassicAssert.AreEqual(1, replies.Messages.Count);
            ClassicAssert.AreEqual(testChannel, replies.Channel);
            ClassicAssert.AreEqual(testFactoid, _data.FactoidGotten);
            ClassicAssert.AreEqual($"{testFactoid} is the best way to learn about 3D printing", replies.Messages[0]);

            // Same test with an exclamation point!
            _data.ResetFactoid();
            _gthx.HandleReceivedMessage(testChannel, testUser, $"{testFactoid}!");

            replies = _client.GetReplies();
            ClassicAssert.AreEqual(1, replies.Messages.Count);
            ClassicAssert.AreEqual(testChannel, replies.Channel);
            ClassicAssert.AreEqual(testFactoid, _data.FactoidGotten);
            ClassicAssert.AreEqual($"{testFactoid} is the best way to learn about 3D printing", replies.Messages[0]);

            // Test "are"
            testFactoid = "pennies";
            _gthx.HandleReceivedMessage(testChannel, testUser, $"{testFactoid}?");

            replies = _client.GetReplies();
            ClassicAssert.AreEqual(1, replies.Messages.Count);
            ClassicAssert.AreEqual(testChannel, replies.Channel);
            ClassicAssert.AreEqual(testFactoid, _data.FactoidGotten);
            ClassicAssert.AreEqual($"{testFactoid} are small coins", replies.Messages[0]);

            // Test multiple values set:
            testFactoid = "cake";
            _gthx.HandleReceivedMessage(testChannel, testUser, $"{testFactoid}?");

            replies = _client.GetReplies();
            ClassicAssert.AreEqual(1, replies.Messages.Count);
            ClassicAssert.AreEqual(testChannel, replies.Channel);
            ClassicAssert.AreEqual(testFactoid, _data.FactoidGotten);
            ClassicAssert.AreEqual($"{testFactoid} is really yummy and also a lie", replies.Messages[0]);

            // Test emoji
            testFactoid = "emoji";
            _gthx.HandleReceivedMessage(testChannel, testUser, $"{testFactoid}?");

            replies = _client.GetReplies();
            ClassicAssert.AreEqual(1, replies.Messages.Count);
            ClassicAssert.AreEqual(testChannel, replies.Channel);
            ClassicAssert.AreEqual(testFactoid, _data.FactoidGotten);
            ClassicAssert.AreEqual($"{testFactoid} is handled well: 😍🍕🎉💪", replies.Messages[0]);

            // Test extended unicode points
            testFactoid = "other languages";
            _gthx.HandleReceivedMessage(testChannel, testUser, $"{testFactoid}?");

            replies = _client.GetReplies();
            ClassicAssert.AreEqual(1, replies.Messages.Count);
            ClassicAssert.AreEqual(testChannel, replies.Channel);
            ClassicAssert.AreEqual(testFactoid, _data.FactoidGotten);
            ClassicAssert.AreEqual($"{testFactoid} are このアプリケーションで十分にサポートされています", replies.Messages[0]);
        }

        [Test]
        public void TestAdvancedFactoidGetting()
        {
            // Test "<reply>" and "!who"
            var testChannel = "#reprap";
            var testUser = "eliteBoi";
            var testFactoid = "botsmack";

            _gthx.HandleReceivedMessage(testChannel, testUser, $"{testFactoid}!");

            var replies = _client.GetReplies();
            ClassicAssert.AreEqual(1, replies.Messages.Count);
            ClassicAssert.AreEqual(testChannel, replies.Channel);
            ClassicAssert.AreEqual($"{testUser}, stop that!", replies.Messages[0]);
            ClassicAssert.AreEqual(testFactoid, _data.FactoidGotten);

            // Test "!who" and "!channel"
            testFactoid = "lost";
            _gthx.HandleReceivedMessage(testChannel, testUser, $"{testFactoid}?");

            replies = _client.GetReplies();
            ClassicAssert.AreEqual(1, replies.Messages.Count);
            ClassicAssert.AreEqual(testChannel, replies.Channel);
            ClassicAssert.AreEqual(testFactoid, _data.FactoidGotten);
            ClassicAssert.AreEqual($"{testUser}, you're in {testChannel}", replies.Messages[0]);

            // Test "<action>" and "!who"
            testFactoid = "dance";
            _gthx.HandleReceivedMessage(testChannel, testUser, $"{testFactoid}!");

            replies = _client.GetReplies();
            ClassicAssert.AreEqual(0, replies.Messages.Count);
            ClassicAssert.AreEqual(1, replies.Actions.Count);
            ClassicAssert.AreEqual(testChannel, replies.Channel);
            ClassicAssert.AreEqual(testFactoid, _data.FactoidGotten);
            ClassicAssert.AreEqual($"dances a little jig around {testUser}.", replies.Actions[0]);
        }

        [Test]
        public void TestFactoidForgetting()
        {
            var testFactoid = "testFactoid";
            var testChannel = "#reprap";
            var testUser = "SomeUser";

            /* Don't forget unless directly addressed */
            _gthx.HandleReceivedMessage(testChannel, testUser, $"forget {testFactoid}");

            var replies = _client.GetReplies();
            ClassicAssert.AreEqual(0, replies.Messages.Count);
            ClassicAssert.AreEqual(null, _data.ForgottenFactoid);
            ClassicAssert.AreEqual(null, _data.ForgettingUser);

            _gthx.HandleReceivedMessage(testChannel, testUser, $"{_client.BotNick}: forget {testFactoid}");

            replies = _client.GetReplies();
            ClassicAssert.AreEqual(1, replies.Messages.Count);
            ClassicAssert.AreEqual(testChannel, replies.Channel);
            ClassicAssert.AreEqual($"{testUser}: I've forgotten about {testFactoid}", replies.Messages[0]);
            ClassicAssert.AreEqual(testFactoid, _data.ForgottenFactoid);
            ClassicAssert.AreEqual(testUser, _data.ForgettingUser);

            var testUser2 = "MaliciousUser";
            var lockedFactoid = "locked factoid";
            _gthx.HandleReceivedMessage(testChannel, testUser2, $"{_client.BotNick}: forget {lockedFactoid}");

            replies = _client.GetReplies();
            ClassicAssert.AreEqual(1, replies.Messages.Count);
            ClassicAssert.AreEqual(testChannel, replies.Channel);
            ClassicAssert.AreEqual($"{testUser2}: Okay, but {lockedFactoid} didn't exist anyway", replies.Messages[0]);
        }

        [Test]
        public void TestFactoidInfo()
        {
            var testFactoid = "makers";
            var testChannel = "#reprap";
            var testUser = "PlayerOne";

            _gthx.HandleReceivedMessage(testChannel, testUser, $"info {testFactoid}");

            var replies = _client.GetReplies();
            ClassicAssert.AreEqual(1, replies.Messages.Count);
            ClassicAssert.AreEqual(testChannel, replies.Channel);
            ClassicAssert.AreEqual(testFactoid, _data.InfoFactoid);
            ClassicAssert.AreEqual($"Sorry, I couldn't find an entry for {testFactoid}", replies.Messages[0]);

            testFactoid = "cake";

            _gthx.HandleReceivedMessage(testChannel, testUser, $"info {testFactoid}?");

            replies = _client.GetReplies();
            ClassicAssert.AreEqual(5, replies.Messages.Count);
            ClassicAssert.AreEqual(testChannel, replies.Channel);
            ClassicAssert.AreEqual(testFactoid, _data.InfoFactoid);

            ClassicAssert.AreEqual($"Factoid '{testFactoid}' has been referenced 176 times", replies.Messages[0]);
            ClassicAssert.AreEqual("At Wed, 10 Oct 2007 08:00:00 GMT, GLaDOS set to: delicious", replies.Messages[1]);
            ClassicAssert.AreEqual("At Wed, 10 Oct 2007 14:34:53 GMT, Chell deleted this item", replies.Messages[2]);
            ClassicAssert.AreEqual("At Wed, 10 Oct 2007 14:34:53 GMT, UnknownEntity set to: a lie!", replies.Messages[3]);
            ClassicAssert.AreEqual("At Wed, 10 Oct 2007 14:34:55 GMT, Unknown set to: delicious", replies.Messages[4]);

            testFactoid = "cake";

            _gthx.HandleReceivedMessage(testChannel, testUser, $"info {testFactoid}.");

            replies = _client.GetReplies();
            ClassicAssert.AreEqual(5, replies.Messages.Count);
            ClassicAssert.AreEqual(testChannel, replies.Channel);
            ClassicAssert.AreEqual(testFactoid, _data.InfoFactoid);

            ClassicAssert.AreEqual($"Factoid '{testFactoid}' has been referenced 176 times", replies.Messages[0]);
            ClassicAssert.AreEqual("At Wed, 10 Oct 2007 08:00:00 GMT, GLaDOS set to: delicious", replies.Messages[1]);
            ClassicAssert.AreEqual("At Wed, 10 Oct 2007 14:34:53 GMT, Chell deleted this item", replies.Messages[2]);
            ClassicAssert.AreEqual("At Wed, 10 Oct 2007 14:34:53 GMT, UnknownEntity set to: a lie!", replies.Messages[3]);
            ClassicAssert.AreEqual("At Wed, 10 Oct 2007 14:34:55 GMT, Unknown set to: delicious", replies.Messages[4]);

            testFactoid = "cake";

            _gthx.HandleReceivedMessage(testChannel, testUser, $"info {testFactoid}!");

            replies = _client.GetReplies();
            ClassicAssert.AreEqual(5, replies.Messages.Count);
            ClassicAssert.AreEqual(testChannel, replies.Channel);
            ClassicAssert.AreEqual(testFactoid, _data.InfoFactoid);

            ClassicAssert.AreEqual($"Factoid '{testFactoid}' has been referenced 176 times", replies.Messages[0]);
            ClassicAssert.AreEqual("At Wed, 10 Oct 2007 08:00:00 GMT, GLaDOS set to: delicious", replies.Messages[1]);
            ClassicAssert.AreEqual("At Wed, 10 Oct 2007 14:34:53 GMT, Chell deleted this item", replies.Messages[2]);
            ClassicAssert.AreEqual("At Wed, 10 Oct 2007 14:34:53 GMT, UnknownEntity set to: a lie!", replies.Messages[3]);
            ClassicAssert.AreEqual("At Wed, 10 Oct 2007 14:34:55 GMT, Unknown set to: delicious", replies.Messages[4]);
        }

        [Test]
        public void TestTellSetting()
        {
            var testChannel = "#reprap";
            var testUser = "AcidBurn";
            var testTellToUser = "CrashOverride";
            var testMessage = "Mess with the best, die like the rest.";

            // Not directly addressed, so gthx should ignore
            _gthx.HandleReceivedMessage(testChannel, testUser, $"tell {testTellToUser} {testMessage}");
            var replies = _client.GetReplies();
            ClassicAssert.AreEqual(0, replies.Messages.Count);

            // Telling a message to someone not in the server
            _gthx.HandleReceivedMessage(testChannel, testUser, $"{_client.BotNick}: tell {testTellToUser} {testMessage}");
            replies = _client.GetReplies();
            ClassicAssert.AreEqual(1, replies.Messages.Count);
            ClassicAssert.AreEqual(testChannel, replies.Channel);
            ClassicAssert.AreEqual($"{testUser}: I'll pass that on when {testTellToUser} is around, but I've never seen them before.", replies.Messages[0]);

            ClassicAssert.AreEqual(testUser, _data.TellFromUser);
            ClassicAssert.AreEqual(testTellToUser, _data.TellToUser);
            ClassicAssert.AreEqual(testMessage, _data.TellMessage);

            // Message to a user who has been seen before
            testTellToUser = "Razor";
            _gthx.HandleReceivedMessage(testChannel, testUser, $"{_client.BotNick}: tell {testTellToUser} {testMessage}");
            replies = _client.GetReplies();
            ClassicAssert.AreEqual(1, replies.Messages.Count);
            ClassicAssert.AreEqual(testChannel, replies.Channel);
            ClassicAssert.AreEqual($"{testUser}: I'll pass that on when {testTellToUser} is around.", replies.Messages[0]);

            ClassicAssert.AreEqual(testUser, _data.TellFromUser);
            ClassicAssert.AreEqual(testTellToUser, _data.TellToUser);
            ClassicAssert.AreEqual(testMessage, _data.TellMessage);

            // "Tell" at the start of a sentence is capitalized, and a message ending in "!" must not be taken
            // as a factoid query
            testMessage = "Thanks for the suggestions! That worked great!";
            _gthx.HandleReceivedMessage(testChannel, testUser, $"{_client.BotNick}: Tell {testTellToUser} {testMessage}");
            replies = _client.GetReplies();
            ClassicAssert.AreEqual(1, replies.Messages.Count);
            ClassicAssert.AreEqual($"{testUser}: I'll pass that on when {testTellToUser} is around.", replies.Messages[0]);
            ClassicAssert.AreEqual(testTellToUser, _data.TellToUser);
            ClassicAssert.AreEqual(testMessage, _data.TellMessage);
        }

        [Test]
        public void TestTellGetting()
        {
            var testChannel = "#reprap";
            var testUser = "CrashOverride";
            var testMessage = "Hey all! What's up?";
            _gthx.HandleReceivedMessage(testChannel, testUser, testMessage);

            ClassicAssert.AreEqual(testUser, _data.TellCheckUser);

            var replies = _client.GetReplies();
            ClassicAssert.AreEqual(1, replies.Messages.Count);
            ClassicAssert.AreEqual(testChannel, replies.Channel);
            var foundTell = false;
            foreach (var message in replies.Messages)
            {
                _logger.LogInformation("Testing message: {message}", message);
                if (message.StartsWith($"{testUser}: "))
                {
                    if (message.EndsWith($"ago AcidBurn tell {testUser} Mess with the best, die like the rest."))
                    {
                        _logger.LogInformation("Found the message we're looking for!");
                        foundTell = true;
                        break;
                    }
                }
            }

            ClassicAssert.IsTrue(foundTell, "Expected reply not received");

            testUser = "gunnbr";
            testMessage = "Finally got my printer tuned!";
            _gthx.HandleReceivedMessage(testChannel, testUser, testMessage);

            ClassicAssert.AreEqual(testUser, _data.TellCheckUser);

            replies = _client.GetReplies();
            ClassicAssert.IsTrue(replies.Messages.Count > 1, "Not enough replies returned.");
            ClassicAssert.AreEqual(testChannel, replies.Channel);
            var foundJimmy = false;
            var foundPaul = false;
            foreach (var message in replies.Messages)
            {
                _logger.LogInformation("Testing message: {message}", message);
                if (message.StartsWith($"{testUser}: "))
                {
                    if (message.EndsWith($"ago JimmyRockets tell {testUser} Can you fix a gthx bug?"))
                    {
                        _logger.LogInformation("Found Jimmy's message.");
                        foundJimmy = true;
                    }

                    if (message.EndsWith($"ago PaulBunyan tell {testUser} Do you need any help with emoji 🧑🏿😨🍦?"))
                    {
                        _logger.LogInformation("Found Paul's message.");
                        foundPaul = true;
                    }
                }
            }

            ClassicAssert.IsTrue(foundJimmy, "Didn't get Jimmy's tell");
            ClassicAssert.IsTrue(foundPaul, "Didn't get Paul's tell");
        }

        [Test]
        public void TestGoogleForSomeone()
        {
            var testChannel = "#reprap";
            var testUser = "CerealKiller";
            var testGoogleUser = "Joey";

            _gthx.HandleReceivedMessage(testChannel, testUser, $"google plastic for {testGoogleUser}");
            var replies = _client.GetReplies();
            ClassicAssert.AreEqual(0, replies.Messages.Count);

            _gthx.HandleReceivedMessage(testChannel, testUser, $"{_client.BotNick}: google plastic for {testGoogleUser}");
            replies = _client.GetReplies();
            ClassicAssert.AreEqual(1, replies.Messages.Count);
            ClassicAssert.AreEqual(testChannel, replies.Channel);
            ClassicAssert.AreEqual($"{testGoogleUser}: http://lmgtfy.com/?q=plastic", replies.Messages[0]);

            _gthx.HandleReceivedMessage(testChannel, testUser, $"{_client.BotNick}, google does 3=4? for {testGoogleUser}");
            replies = _client.GetReplies();
            ClassicAssert.AreEqual(1, replies.Messages.Count);
            ClassicAssert.AreEqual(testChannel, replies.Channel);
            ClassicAssert.AreEqual($"{testGoogleUser}: http://lmgtfy.com/?q=does+3%3d4%3f", replies.Messages[0]);

            // "Google" at the start of a sentence is capitalized
            _gthx.HandleReceivedMessage(testChannel, testUser, $"{_client.BotNick}: Google plastic For {testGoogleUser}");
            replies = _client.GetReplies();
            ClassicAssert.AreEqual(1, replies.Messages.Count);
            ClassicAssert.AreEqual($"{testGoogleUser}: http://lmgtfy.com/?q=plastic", replies.Messages[0]);
        }

        [Test]
        public async Task TestYoutubeReferences()
        {
            var testChannel = "#reprap";
            var testUser = "BobYourUncle";
            _gthx.HandleReceivedMessage(testChannel, testUser, $"Check out this cool spinner: https://www.youtube.com/watch?v=ykKIZQKaT5c");
            var replies = _client.GetReplies();
            ClassicAssert.AreEqual(1, replies.Messages.Count);
            ClassicAssert.AreEqual(testChannel, replies.Channel);
            ClassicAssert.AreEqual($"{testUser} linked to YouTube video \"Spinner\" => 42 IRC mentions", replies.Messages[0]);

            // Test non-ASCII characters
            testUser = "AndrewJohnson";
            _gthx.HandleReceivedMessage(testChannel, testUser, $"Calm down and listen to this: https://youtu.be/W3B2C0nNpFU");
            replies = _client.GetReplies();
            ClassicAssert.AreEqual(1, replies.Messages.Count);
            ClassicAssert.AreEqual(testChannel, replies.Channel);
            ClassicAssert.AreEqual($"{testUser} linked to YouTube video \"Best relaxing piano studio ghibli complete collection ピアノスタジオジブリコレクション\" => 83 IRC mentions", replies.Messages[0]);

            // Test fetching a new title that uses the <title> element
            testUser = "RandomNick";
            _gthx.HandleReceivedMessage(testChannel, testUser, $"here's another link https://youtu.be/title");
            await Task.Delay(500);
            replies = _client.GetReplies();
            ClassicAssert.AreEqual(1, replies.Messages.Count);
            ClassicAssert.AreEqual(testChannel, replies.Channel);
            ClassicAssert.AreEqual($"{testUser} linked to YouTube video \"Dummy Title\" => 1 IRC mentions", replies.Messages[0]);

            // Test fetching a new title that uses the <meta> element for the title
            testUser = "AnotherNick";
            _gthx.HandleReceivedMessage(testChannel, testUser, $"Or take a look at this one https://youtu.be/meta which I think is better");
            await Task.Delay(500);
            replies = _client.GetReplies();
            ClassicAssert.AreEqual(1, replies.Messages.Count);
            ClassicAssert.AreEqual(testChannel, replies.Channel);
            ClassicAssert.AreEqual($"{testUser} linked to YouTube video \"Meta Title\" => 1 IRC mentions", replies.Messages[0]);

            // Test fetching a new title that uses the <meta> element for the title
            testUser = "QAGuy";
            _gthx.HandleReceivedMessage(testChannel, testUser, $"Did they fix that bugs with encoded characters? https://youtu.be/encoded");
            await Task.Delay(500);
            replies = _client.GetReplies();
            ClassicAssert.AreEqual(1, replies.Messages.Count);
            ClassicAssert.AreEqual(testChannel, replies.Channel);
            ClassicAssert.AreEqual($"{testUser} linked to YouTube video \"Rum & \"Coke\"\" => 1 IRC mentions", replies.Messages[0]);

            // Test fetching a new title from YouTube's oEmbed endpoint
            testUser = "EmbedFan";
            _gthx.HandleReceivedMessage(testChannel, testUser, $"oEmbed knows this one: https://youtu.be/oembed");
            await Task.Delay(500);
            replies = _client.GetReplies();
            ClassicAssert.AreEqual(1, replies.Messages.Count);
            ClassicAssert.AreEqual(testChannel, replies.Channel);
            ClassicAssert.AreEqual($"{testUser} linked to YouTube video \"oEmbed Title & \"Quotes\"\" => 1 IRC mentions", replies.Messages[0]);

            // Test a title with encoded line breaks and a tab, which can't be sent on IRC
            testUser = "Sneaky";
            _gthx.HandleReceivedMessage(testChannel, testUser, $"Look: https://youtu.be/newlinetitle");
            await Task.Delay(500);
            replies = _client.GetReplies();
            ClassicAssert.AreEqual(1, replies.Messages.Count);
            ClassicAssert.AreEqual(testChannel, replies.Channel);
            ClassicAssert.AreEqual($"{testUser} linked to YouTube video \"First line PRIVMSG #reprap :second line\" => 1 IRC mentions", replies.Messages[0]);

            // Test a page too big to read all of: the title past the limit isn't found
            testUser = "BigPage";
            _gthx.HandleReceivedMessage(testChannel, testUser, $"Huge: https://youtu.be/hugepage");
            await Task.Delay(1000);
            replies = _client.GetReplies();
            ClassicAssert.AreEqual(1, replies.Messages.Count);
            ClassicAssert.AreEqual(testChannel, replies.Channel);
            ClassicAssert.AreEqual($"{testUser} linked to a YouTube video with an unknown title => 1 IRC mentions", replies.Messages[0]);

            // Test a page whose <title> is empty but whose <meta> element has the title
            testUser = "CloudUser";
            _gthx.HandleReceivedMessage(testChannel, testUser, $"From a server: https://youtu.be/emptytitle");
            await Task.Delay(500);
            replies = _client.GetReplies();
            ClassicAssert.AreEqual(1, replies.Messages.Count);
            ClassicAssert.AreEqual(testChannel, replies.Channel);
            ClassicAssert.AreEqual($"{testUser} linked to YouTube video \"Meta After Empty Title\" => 1 IRC mentions", replies.Messages[0]);

            // Test message when no title is found
            testUser = "AnotherNick";
            _gthx.HandleReceivedMessage(testChannel, testUser, $"Does thie one work for you? https://youtu.be/notitle");
            await Task.Delay(500);
            replies = _client.GetReplies();
            ClassicAssert.AreEqual(1, replies.Messages.Count);
            ClassicAssert.AreEqual(testChannel, replies.Channel);
            ClassicAssert.AreEqual($"{testUser} linked to a YouTube video with an unknown title => 1 IRC mentions", replies.Messages[0]);
        }

        [Test]
        public async Task TestThingiverseReferences()
        {
            var testChannel = "#reprap";
            var testUser = "BobYourUncle";
            _gthx.HandleReceivedMessage(testChannel, testUser, $"Check out this cool spinner: https://www.thingiverse.com/thing:2823006");
            var replies = _client.GetReplies();
            ClassicAssert.AreEqual(1, replies.Messages.Count);
            ClassicAssert.AreEqual(testChannel, replies.Channel);
            ClassicAssert.AreEqual($"{testUser} linked to \"Air Spinner\" on thingiverse => 42 IRC mentions", replies.Messages[0]);

            // Test non-ASCII characters
            testUser = "AndrewJohnson";
            _gthx.HandleReceivedMessage(testChannel, testUser, $"https://www.thingiverse.com/thing:1276095 this is the coolest fish!!");
            replies = _client.GetReplies();
            ClassicAssert.AreEqual(1, replies.Messages.Count);
            ClassicAssert.AreEqual(testChannel, replies.Channel);
            ClassicAssert.AreEqual($"{testUser} linked to \"Flexifish 🦈🌊\" on thingiverse => 23 IRC mentions", replies.Messages[0]);

            // Test fetching a new title that uses the <title> element
            testUser = "RandomNick";
            _gthx.HandleReceivedMessage(testChannel, testUser, $"Your daughter would really like this: https://www.thingiverse.com/thing:2810756");
            await Task.Delay(500);
            replies = _client.GetReplies();
            ClassicAssert.AreEqual(1, replies.Messages.Count);
            ClassicAssert.AreEqual(testChannel, replies.Channel);
            ClassicAssert.AreEqual($"{testUser} linked to \"Articulated Butterfly\" on thingiverse => 1 IRC mentions", replies.Messages[0]);

            // Test fetching a new title that uses the <meta> element for the title
            testUser = "AnotherNick";
            _gthx.HandleReceivedMessage(testChannel, testUser, $"Or this one: https://www.thingiverse.com/thing:2818955");
            await Task.Delay(500);
            replies = _client.GetReplies();
            ClassicAssert.AreEqual(1, replies.Messages.Count);
            ClassicAssert.AreEqual(testChannel, replies.Channel);
            ClassicAssert.AreEqual($"{testUser} linked to \"Articulated Slug\" on thingiverse => 1 IRC mentions", replies.Messages[0]);

            // Test message when no title is found
            testUser = "AnotherNick";
            _gthx.HandleReceivedMessage(testChannel, testUser, $"Does thie one work for you? https://www.thingiverse.com/thing:4052802");
            await Task.Delay(500);
            replies = _client.GetReplies();
            ClassicAssert.AreEqual(1, replies.Messages.Count);
            ClassicAssert.AreEqual(testChannel, replies.Channel);
            ClassicAssert.AreEqual($"{testUser} linked to thing 4052802 on thingiverse => 1 IRC mentions", replies.Messages[0]);

            // A look-alike host isn't Thingiverse, so the bot doesn't fetch it
            testUser = "Phisher";
            _gthx.HandleReceivedMessage(testChannel, testUser, $"Free stuff: https://wwwXthingiverse.com/thing:2810756");
            await Task.Delay(500);
            replies = _client.GetReplies();
            ClassicAssert.AreEqual(0, replies.Messages.Count);
        }

        [Test]
        public async Task TestThingiverseInTell()
        {
            var testChannel = "#reprap";
            var testUser = "CerealKiller";
            var tellToUser = "Nikon";
            _gthx.HandleReceivedMessage(testChannel, testUser, $"gthxbot: tell {tellToUser} Can I crash at your place and print this out? https://www.thingiverse.com/thing:2823006");
            Console.WriteLine("Finished processing received message");
            // Wait for async replies to get sent
            await Task.Delay(500);
            Console.WriteLine("Finished delay for async messages");
            var replies = _client.GetReplies();
            Console.WriteLine($"Got {replies.Messages.Count} messages");
            ClassicAssert.AreEqual(2, replies.Messages.Count, "Didn't get the expected number of replies");
            ClassicAssert.AreEqual(testChannel, replies.Channel);

            var gotTellResponse = false;
            var gotTitleResponse = false;
            foreach (var message in replies.Messages)
            {
                if (message == $"{testUser}: I'll pass that on when {tellToUser} is around, but I've never seen them before.")
                {
                    gotTellResponse = true;
                }
                if (message == $"{testUser} linked to \"Air Spinner\" on thingiverse => 42 IRC mentions")
                {
                    gotTitleResponse = true;
                }
            }

            ClassicAssert.IsTrue(gotTellResponse, "Failed to get expected response to the tell request");
            ClassicAssert.IsTrue(gotTitleResponse, "Failed to get expected response to the thingi title request");
        }

        [Test]
        public void TestSeenRequests()
        {
            var testChannel = "#reprap";
            var testUser = "PhantomPhreak";
            var testSeenUser = "gunnbr";

            // TODO: Use data as a transient service and request it in each test
            //       to avoid having tests affect each other which requires this.
            _data.ResetFactoid();

            _gthx.HandleReceivedMessage(testChannel, testUser, $"seen {testSeenUser}?");
            var replies = _client.GetReplies();
            ClassicAssert.AreEqual(2, replies.Messages.Count);
            ClassicAssert.AreEqual(_data.LastSeenUserQuery, testSeenUser);

            ClassicAssert.AreEqual("gunnbr was last seen in #gthxtest ", replies.Messages[0].Substring(0, 34));
            ClassicAssert.AreEqual(" ago saying 'gthx: status?'.", replies.Messages[0].Substring(replies.Messages[0].Length - 28, 28));
            ClassicAssert.AreEqual("gunnbr_ was last seen in #reprap ", replies.Messages[1].Substring(0, 33));
            ClassicAssert.AreEqual(" ago saying 'Yeah, I'm trying to fix that.'.", replies.Messages[1].Substring(replies.Messages[1].Length - 44, 44));

            // Also make sure that asking about seen stops processing so it doesn't
            // attempt to find a factoid named "seen gunnbr"
            ClassicAssert.AreEqual(null, _data.FactoidGotten);

            // Test without the question mark at the end
            testChannel = "#openscad";
            testUser = "AcidBurn";
            testSeenUser = "Razor";

            _gthx.HandleReceivedMessage(testChannel, testUser, $"seen {testSeenUser}");
            replies = _client.GetReplies();
            ClassicAssert.AreEqual(1, replies.Messages.Count);
            ClassicAssert.AreEqual(testSeenUser, _data.LastSeenUserQuery);

            ClassicAssert.AreEqual("Razor was last seen in #twitch ", replies.Messages[0].Substring(0, 31));
            ClassicAssert.AreEqual("Stream is starting NOW! Tune in!'.", replies.Messages[0].Substring(replies.Messages[0].Length - 34, 34));

            // Make sure that messages which are longer than a nick don't trigger a seen response
            _data.ClearLastSeen();
            _gthx.HandleReceivedMessage(testChannel, testUser, $"seen him a dozen times since then.");
            replies = _client.GetReplies();
            ClassicAssert.AreEqual(0, replies.Messages.Count);
            ClassicAssert.IsNull(_data.LastSeenUserQuery);

            // TODO: Test Timestamp == null in seen
        }

        [Test]
        public void TestSeenMaxReplies()
        {
            // Test that only 3 replies are returned to seen queries, no matter how many _data returns

            var testChannel = "#reprap";
            var testUser = "someone";
            var testSeenUser = "The";

            _gthx.HandleReceivedMessage(testChannel, testUser, $"seen {testSeenUser}?");
            var replies = _client.GetReplies();
            ClassicAssert.AreEqual(3, replies.Messages.Count);
            ClassicAssert.AreEqual(_data.LastSeenUserQuery, testSeenUser);

            ClassicAssert.AreEqual("TheHelper was last seen in #openscad ", replies.Messages[0].Substring(0, 37));
            ClassicAssert.AreEqual("ThePlague was last seen in #leets ", replies.Messages[1].Substring(0, 34));
            ClassicAssert.AreEqual("Themyscira was last seen in #superherohigh ", replies.Messages[2].Substring(0, 43));
        }

        [Test]
        public void TestSeenUpdates()
        {
            var testChannel = "#reprap";
            var testUser = "Joey";
            var testMessage = "Yo, yo, YO guys! I need a handle!!";

            _gthx.HandleReceivedMessage(testChannel, testUser, testMessage);
            ClassicAssert.AreEqual(testUser, _data.LastSeenUser);
            ClassicAssert.AreEqual(testChannel, _data.LastSeenChannel);
            ClassicAssert.AreEqual(testMessage, _data.LastSeenMessage);
            // Not useful to test LastSeenTimestamp here as that's set in MockData.
            // Make an integration test for it.
        }

        [Test]
        public void TestSeenUpdateOnAction()
        {
            var testChannel = "#reprap";
            var testUser = "PhantomPhreak";
            var testAction = "smacks Joey.";

            _gthx.HandleReceivedAction(testChannel, testUser, testAction);
            ClassicAssert.AreEqual(testUser, _data.LastSeenUser);
            ClassicAssert.AreEqual(testChannel, _data.LastSeenChannel);
            ClassicAssert.AreEqual($"* {testUser} {testAction}", _data.LastSeenMessage);
            // Not useful to test LastSeenTimestamp here as tgat's set in MockData.
            // Make an integration test for it.
        }

        [Test]
        public void TestSeenInPM()
        {
            // Verify that private messages don't update the seen info
            _data.ResetLastSeen();

            var testChannel = "_gthx";
            var testUser = "Joey";
            var testMessage = "Dude! I found the garbage file!";

            _gthx.HandleReceivedMessage(testChannel, testUser, testMessage);
            ClassicAssert.AreEqual(null, _data.LastSeenUser);
            ClassicAssert.AreEqual(null, _data.LastSeenChannel);
            ClassicAssert.AreEqual(null, _data.LastSeenMessage);
        }

        [Test]
        public void TestStatus()
        {
            var testChannel = "#reprap";
            var testUser = "admin";

            _data.ResetFactoid();
            _gthx.HandleReceivedMessage(testChannel, testUser, $"status?");
            var replies = _client.GetReplies();
            ClassicAssert.AreEqual(1, replies.Messages.Count);
            ClassicAssert.IsTrue(replies.Messages[0].Contains(": OK; Up for "), "Invalid status reply");

            // Also make sure that asking about status stops processing so it doesn't
            // attempt to find a factoid named "status"
            ClassicAssert.AreEqual(null, _data.FactoidGotten);
        }

        [Test]
        public async Task TestLurkers()
        {
            var testChannel = "#reprap";
            var testUser = "CuriousKitty";

            _gthx.HandleReceivedMessage(testChannel, testUser, $"lurkers?");
            await Task.Delay(5000);
            var replies = _client.GetReplies();
            ClassicAssert.AreEqual(1, replies.Messages.Count);
            ClassicAssert.AreEqual(testChannel, replies.Channel);
            ClassicAssert.AreEqual($"2 of the 5 users in {testChannel} right now have never said anything.", replies.Messages[0]);
        }
    }
}