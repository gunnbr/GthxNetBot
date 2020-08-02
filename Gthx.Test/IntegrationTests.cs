using Gthx.Bot;
using Gthx.Bot.Interfaces;
using Gthx.Data;
using Gthx.Test.Mocks;
using GthxData;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System.Threading.Tasks;

namespace Gthx.Test
{
    [TestFixture]
    public class IntegrationTests
    {
        protected readonly TestServer server;
        protected readonly GthxDataContext _dataContext;
        protected readonly Gthx.Bot.Gthx _gthx;
        private readonly IIrcClient _client;
        private readonly IGthxData _data;

        public IntegrationTests(IIrcClient client, IGthxData data)
        {
            this.server = new TestServer(new WebHostBuilder().UseStartup<Startup>());
            this._dataContext = this.server.Host.Services.GetRequiredService<GthxDataContext>();
            this._gthx = server.Host.Services.GetRequiredService<Gthx.Bot.Gthx>();
            this._client = client;
            this._data = data;
        }

        [OneTimeSetUp]
        public void TestInitialise()
        {
            this._dataContext.Database.EnsureCreated();
        }

        [OneTimeTearDown]
        public void TestTearDown()
        {
            this._dataContext.Database.EnsureDeleted();
        }

        // TODO: Add integration test for "seen user*" to verify the asterisk is handled correctly

        [Test]
        public async Task TestLiveYoutubeReferences()
        {
            var client = (MockIrcClient)_client;
            var data = (MockData)_data;
            var gthx = _gthx;

            var testChannel = "#reprap";
            var testUser = "BobYourUncle";
            gthx.HandleReceivedMessage(testChannel, testUser, $"OMG! Check this out! https://www.youtube.com/watch?v=I7nVrT00ST4");
            await Task.Delay(5000);
            var replies = client.GetReplies();
            Assert.AreEqual(1, replies.Messages.Count);
            Assert.AreEqual(testChannel, replies.Channel);
            Assert.AreEqual("I7nVrT00ST4", data.AddedYoutubeId);
            Assert.AreEqual("Pro Riders Laughing", data.AddedYoutubeTitle);
            Assert.AreEqual($"{testUser} linked to YouTube video \"Pro Riders Laughing\" => 1 IRC mentions", replies.Messages[0]);

            // Test non-Western characters
            testUser = "AndrewJohnson";
            gthx.HandleReceivedMessage(testChannel, testUser, $"Calm down and listen to this: https://www.youtube.com/watch?v=xtAHgrNs7r4");
            await Task.Delay(5000);
            replies = client.GetReplies();
            Assert.AreEqual(1, replies.Messages.Count);
            Assert.AreEqual(testChannel, replies.Channel);
            Assert.AreEqual("xtAHgrNs7r4", data.AddedYoutubeId);
            Assert.AreEqual("Studio Ghibli - Music Collection (Piano and Violin Duo) 株式会社スタジオジブリ- Relaxing music song", data.AddedYoutubeTitle);
            Assert.AreEqual($"{testUser} linked to YouTube video \"Studio Ghibli - Music Collection (Piano and Violin Duo) 株式会社スタジオジブリ- Relaxing music song\" => 1 IRC mentions", replies.Messages[0]);
        }

        [Test]
        public async Task TestLiveThingiverseReferences()
        {
            var client = (MockIrcClient)_client;
            var data = (MockData)_data;
            var gthx = _gthx;

            // Test fetching a new title that uses the <title> element
            var testChannel = "#reprap";
            var testUser = "RandomNick";
            gthx.HandleReceivedMessage(testChannel, testUser, $"Your daughter would really like this: https://www.thingiverse.com/thing:2810756");
            await Task.Delay(5000);
            var replies = client.GetReplies();
            Assert.AreEqual(1, replies.Messages.Count);
            Assert.AreEqual(testChannel, replies.Channel);
            Assert.AreEqual("2810756", data.AddedThingiverseId);
            Assert.AreEqual("Articulated Butterfly by 8ran", data.AddedThingiverseTitle);
            Assert.AreEqual($"{testUser} linked to \"Articulated Butterfly by 8ran\" on thingiverse => 1 IRC mentions", replies.Messages[0]);
        }

        [Test]
        public void TestFactoidSetAndGet()
        {
#if false
            var client = new MockIrcClient();
            var context = new GthxData.GthxDataContext();
            context.Database.EnsureCreated();
            var data = new GthxSqlData(context);
            var mockReader = new MockWebReader();
            var gthx = new Bot.Gthx(client, data, mockReader);

            
            var testFactoid = "testFactoid";
            var testValue = "working";
            var testChannel = "#reprap";
            var testUser = "SomeUser";

            gthx.HandleReceivedMessage(testChannel, testUser, $"{testFactoid} is {testValue}");
            var replies = client.GetReplies();
            Assert.AreEqual(1, replies.Messages.Count);
            Assert.AreEqual(testChannel, replies.Channel);
            Assert.AreEqual($"{testUser}: Okay.", replies.Messages[0]);

            gthx.HandleReceivedMessage(testChannel, testUser, $"{testFactoid}?");
            replies = client.GetReplies();
            Assert.AreEqual(1, replies.Messages.Count);
            Assert.AreEqual(testChannel, replies.Channel);
            Assert.AreEqual($"{testFactoid} is {testValue}", replies.Messages[0]);
#endif
        }
    }
}
