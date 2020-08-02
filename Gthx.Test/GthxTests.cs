using Gthx.Bot.Interfaces;
using Gthx.Data;
using Gthx.Test.Mocks;
using GthxData;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Gthx.Test
{
    [TestFixture]
    public class GthxTests
    {
        protected readonly TestServer server;
        protected readonly Gthx.Bot.Gthx _gthx;
        private readonly IIrcClient _client;
        private readonly IGthxData _data;

        public GthxTests()
        {
            this.server = new TestServer(new WebHostBuilder().UseStartup<Startup>());
            _data = server.Host.Services.GetService<IGthxData>();
            this._client = server.Host.Services.GetService<IIrcClient>();
            Console.WriteLine($"Constructor using client {_client.Key}");
            this._gthx = server.Host.Services.GetRequiredService<Gthx.Bot.Gthx>();
        }

        [Test]
        public void TestGenericResponse()
        {
            var client = (MockIrcClient)_client;
            var data = (MockData)_data;

            // Test channel message
            var testChannel = "#reprap";
            var testUser = "SomeUser";
            _gthx.HandleReceivedMessage(testChannel, testUser, "Which printer is best?");

            var replies = client.GetReplies();
            Assert.AreEqual(0, replies.Messages.Count);

            // Test DM
            testChannel = "gthx";
            testUser = "SomeOtherUser";
            _gthx.HandleReceivedMessage(testChannel, testUser, "Hey, can you help me?");

            replies = client.GetReplies();
            Assert.AreEqual(0, replies.Messages.Count);
        }

        [Test]
        public void TestFactoidSetting()
        {
            var client = (MockIrcClient)_client;
            var data = (MockData)_data;
            var gthx = _gthx;

            var testFactoid = "testFactoid";
            var testValue = "working";
            var testValue2 = "is working better";
            var testChannel = "#reprap";
            var testUser = "SomeUser";
            var testUser2 = "AnotherUser";

            gthx.HandleReceivedMessage(testChannel, testUser, $"{testFactoid} is {testValue}");

            var replies = client.GetReplies();
            Assert.AreEqual(1, replies.Messages.Count);
            Assert.AreEqual(testChannel, replies.Channel);
            Assert.AreEqual($"{testUser}: Okay.", replies.Messages[0]);
            Assert.AreEqual(testFactoid, data.FactoidItem);
            Assert.AreEqual(testValue, data.FactoidValue);
            Assert.IsFalse(data.FactoidIsAre);
            Assert.AreEqual(testUser, data.FactoidUser);
            Assert.IsTrue(data.FactoidReplaceExisting);

            gthx.HandleReceivedMessage(testChannel, testUser2, $"{testFactoid} are also {testValue2}");

            replies = client.GetReplies();
            Assert.AreEqual(1, replies.Messages.Count);
            Assert.AreEqual(testChannel, replies.Channel);
            Assert.AreEqual($"{testUser2}: Okay.", replies.Messages[0]);
            Assert.AreEqual(testFactoid, data.FactoidItem);
            Assert.AreEqual(testValue2, data.FactoidValue);
            Assert.IsTrue(data.FactoidIsAre);
            Assert.AreEqual(testUser2, data.FactoidUser);
            Assert.IsFalse(data.FactoidReplaceExisting);
        }

        [Test]
        public void TestLockedFactoidSetting()
        {
            var client = (MockIrcClient)_client;
            var data = (MockData)_data;
            var gthx = _gthx;

            var testFactoid = "locked factoid";
            var testValue = "something that can't be changed.";
            var testChannel = "#reprap";
            var testUser = "MaliciousUser";

            gthx.HandleReceivedMessage(testChannel, testUser, $"{testFactoid} is {testValue}");

            var replies = client.GetReplies();
            Assert.AreEqual(1, replies.Messages.Count);
            Assert.AreEqual(testChannel, replies.Channel);
            Assert.AreEqual($"I'm sorry, {testUser}. I'm afraid I can't do that.", replies.Messages[0]);
        }

        [Test]
        public void TestFactoidGetting()
        {
            var client = (MockIrcClient)_client;
            var data = (MockData)_data;
            var gthx = _gthx;

            // Test "is"
            var testChannel = "#reprap";
            var testUser = "sandman";
            var testFactoid = "reprap";

            gthx.HandleReceivedMessage(testChannel, testUser, $"{testFactoid}?");

            var replies = client.GetReplies();
            Assert.AreEqual(1, replies.Messages.Count);
            Assert.AreEqual(testChannel, replies.Channel);
            Assert.AreEqual(testFactoid, data.FactoidGotten);
            Assert.AreEqual($"{testFactoid} is the best way to learn about 3D printing", replies.Messages[0]);

            // Same test with an exclamation point!
            gthx.HandleReceivedMessage(testChannel, testUser, $"{testFactoid}!");

            replies = client.GetReplies();
            Assert.AreEqual(1, replies.Messages.Count);
            Assert.AreEqual(testChannel, replies.Channel);
            Assert.AreEqual(testFactoid, data.FactoidGotten);
            Assert.AreEqual($"{testFactoid} is the best way to learn about 3D printing", replies.Messages[0]);

            // Test "are"
            testFactoid = "pennies";
            gthx.HandleReceivedMessage(testChannel, testUser, $"{testFactoid}?");

            replies = client.GetReplies();
            Assert.AreEqual(1, replies.Messages.Count);
            Assert.AreEqual(testChannel, replies.Channel);
            Assert.AreEqual(testFactoid, data.FactoidGotten);
            Assert.AreEqual($"{testFactoid} are small coins", replies.Messages[0]);

            // Test multiple values set:
            testFactoid = "cake";
            gthx.HandleReceivedMessage(testChannel, testUser, $"{testFactoid}?");

            replies = client.GetReplies();
            Assert.AreEqual(1, replies.Messages.Count);
            Assert.AreEqual(testChannel, replies.Channel);
            Assert.AreEqual(testFactoid, data.FactoidGotten);
            Assert.AreEqual($"{testFactoid} is really yummy and also a lie", replies.Messages[0]);

            // Test emoji
            testFactoid = "emoji";
            gthx.HandleReceivedMessage(testChannel, testUser, $"{testFactoid}?");

            replies = client.GetReplies();
            Assert.AreEqual(1, replies.Messages.Count);
            Assert.AreEqual(testChannel, replies.Channel);
            Assert.AreEqual(testFactoid, data.FactoidGotten);
            Assert.AreEqual($"{testFactoid} is handled well: 😍🍕🎉💪", replies.Messages[0]);

            // Test extended unicode points
            testFactoid = "other languages";
            gthx.HandleReceivedMessage(testChannel, testUser, $"{testFactoid}?");

            replies = client.GetReplies();
            Assert.AreEqual(1, replies.Messages.Count);
            Assert.AreEqual(testChannel, replies.Channel);
            Assert.AreEqual(testFactoid, data.FactoidGotten);
            Assert.AreEqual($"{testFactoid} are このアプリケーションで十分にサポートされています", replies.Messages[0]);
        }

        [Test]
        public void TestAdvancedFactoidGetting()
        {
            var client = (MockIrcClient)_client;
            var data = (MockData)_data;
            var gthx = _gthx;

            // Test "<reply>" and "!who"
            var testChannel = "#reprap";
            var testUser = "eliteBoi";
            var testFactoid = "botsmack";

            gthx.HandleReceivedMessage(testChannel, testUser, $"{testFactoid}!");

            var replies = client.GetReplies();
            Assert.AreEqual(1, replies.Messages.Count);
            Assert.AreEqual(testChannel, replies.Channel);
            Assert.AreEqual($"{testUser}, stop that!", replies.Messages[0]);
            Assert.AreEqual(testFactoid, data.FactoidGotten);

            // Test "!who" and "!channel"
            testFactoid = "lost";
            gthx.HandleReceivedMessage(testChannel, testUser, $"{testFactoid}?");

            replies = client.GetReplies();
            Assert.AreEqual(1, replies.Messages.Count);
            Assert.AreEqual(testChannel, replies.Channel);
            Assert.AreEqual(testFactoid, data.FactoidGotten);
            Assert.AreEqual($"{testUser}, you're in {testChannel}", replies.Messages[0]);

            // Test "<action>" and "!who"
            testFactoid = "dance";
            gthx.HandleReceivedMessage(testChannel, testUser, $"{testFactoid}!");

            replies = client.GetReplies();
            Assert.AreEqual(0, replies.Messages.Count);
            Assert.AreEqual(1, replies.Actions.Count);
            Assert.AreEqual(testChannel, replies.Channel);
            Assert.AreEqual(testFactoid, data.FactoidGotten);
            Assert.AreEqual($"dances a little jig around {testUser}.", replies.Actions[0]);
        }

        [Test]
        public void TestFactoidForgetting()
        {
            var client = (MockIrcClient)_client;
            var data = (MockData)_data;
            var gthx = _gthx;

            var testFactoid = "testFactoid";
            var testChannel = "#reprap";
            var testUser = "SomeUser";

            gthx.HandleReceivedMessage(testChannel, testUser, $"forget {testFactoid}");

            var replies = client.GetReplies();
            Assert.AreEqual(1, replies.Messages.Count);
            Assert.AreEqual(testChannel, replies.Channel);
            Assert.AreEqual($"{testUser}: I've forgotten about {testFactoid}", replies.Messages[0]);
            Assert.AreEqual(testFactoid, data.ForgottenFactoid);
            Assert.AreEqual(testUser, data.ForgettingUser);

            var testUser2 = "MaliciousUser";
            var lockedFactoid = "locked factoid";
            gthx.HandleReceivedMessage(testChannel, testUser2, $"forget {lockedFactoid}");

            replies = client.GetReplies();
            Assert.AreEqual(1, replies.Messages.Count);
            Assert.AreEqual(testChannel, replies.Channel);
            Assert.AreEqual($"{testUser2}: Okay, but {lockedFactoid} didn't exist anyway", replies.Messages[0]);
        }

        [Test]
        public void TestFactoidInfo()
        {
            var client = (MockIrcClient)_client;
            var data = (MockData)_data;
            var gthx = _gthx;

            var testFactoid = "makers";
            var testChannel = "#reprap";
            var testUser = "PlayerOne";

            gthx.HandleReceivedMessage(testChannel, testUser, $"info {testFactoid}");

            var replies = client.GetReplies();
            Assert.AreEqual(1, replies.Messages.Count);
            Assert.AreEqual(testChannel, replies.Channel);
            Assert.AreEqual(testFactoid, data.InfoFactoid);
            Assert.AreEqual($"Sorry, I couldn't find an entry for {testFactoid}", replies.Messages[0]);

            testFactoid = "cake";

            gthx.HandleReceivedMessage(testChannel, testUser, $"info {testFactoid}");

            replies = client.GetReplies();
            Assert.AreEqual(5, replies.Messages.Count);
            Assert.AreEqual(testChannel, replies.Channel);
            Assert.AreEqual(testFactoid, data.InfoFactoid);

            Assert.AreEqual($"Factoid '{testFactoid}' has been referenced 176 times", replies.Messages[0]);
            Assert.AreEqual("At Wed, 10 Oct 2007 08:00:00 GMT, GLaDOS set to: delicious", replies.Messages[1]);
            Assert.AreEqual("At Wed, 10 Oct 2007 14:34:53 GMT, Chell deleted this item", replies.Messages[2]);
            Assert.AreEqual("At Wed, 10 Oct 2007 14:34:53 GMT, UnknownEntity set to: a lie!", replies.Messages[3]);
            Assert.AreEqual("At Wed, 10 Oct 2007 14:34:55 GMT, Unknown set to: delicious", replies.Messages[4]);
        }

        [Test]
        public void TestTellSetting()
        {
            var client = (MockIrcClient)_client;
            var data = (MockData)_data;
            var gthx = _gthx;

            var testChannel = "#reprap";
            var testUser = "AcidBurn";
            var testTellToUser = "CrashOverride";
            var testMessage = "Mess with the best, die like the rest.";

            gthx.HandleReceivedMessage(testChannel, testUser, $"tell {testTellToUser} {testMessage}");

            var replies = client.GetReplies();
            Assert.AreEqual(1, replies.Messages.Count);
            Assert.AreEqual(testChannel, replies.Channel);
            Assert.AreEqual($"{testUser}: Okay.", replies.Messages[0]);

            Assert.AreEqual(testUser, data.TellFromUser);
            Assert.AreEqual(testTellToUser, data.TellToUser);
            Assert.AreEqual(testMessage, data.TellMessage);
        }

        [Test]
        public void TestTellGetting()
        {
            var client = (MockIrcClient)_client;
            var data = (MockData)_data;
            var gthx = _gthx;

            var testChannel = "#reprap";
            var testUser = "CrashOverride";
            var testMessage = "Hey all! What's up?";
            gthx.HandleReceivedMessage(testChannel, testUser, testMessage);

            Assert.AreEqual(testUser, data.TellCheckUser);

            var replies = client.GetReplies();
            Assert.AreEqual(1, replies.Messages.Count);
            Assert.AreEqual(testChannel, replies.Channel);
            var foundTell = false;
            foreach (var message in replies.Messages)
            {
                Debug.WriteLine("Testing message: {message}");
                if (message.StartsWith($"{testUser}: "))
                {
                    if (message.EndsWith($"ago AcidBurn tell {testUser} Mess with the best, die like the rest."))
                    {
                        Debug.WriteLine("Found the message we're looking for!");
                        foundTell = true;
                        break;
                    }
                }
            }

            Assert.IsTrue(foundTell, "Expected reply not received");

            testUser = "gunnbr";
            testMessage = "Finally got my printer tuned!";
            gthx.HandleReceivedMessage(testChannel, testUser, testMessage);

            Assert.AreEqual(testUser, data.TellCheckUser);

            replies = client.GetReplies();
            Assert.IsTrue(replies.Messages.Count > 1, "Not enough replies returned.");
            Assert.AreEqual(testChannel, replies.Channel);
            var foundJimmy = false;
            var foundPaul = false;
            foreach (var message in replies.Messages)
            {
                Debug.WriteLine("Testing message: {message}");
                if (message.StartsWith($"{testUser}: "))
                {
                    if (message.EndsWith($"ago JimmyRockets tell {testUser} Can you fix a gthx bug?"))
                    {
                        Debug.WriteLine("Found Jimmy's message.");
                        foundJimmy = true;
                    }

                    if (message.EndsWith($"ago PaulBunyan tell {testUser} Do you need any help with emoji 🧑🏿😨🍦?"))
                    {
                        Debug.WriteLine("Found Paul's message.");
                        foundPaul = true;
                    }
                }
            }

            Assert.IsTrue(foundJimmy, "Didn't get Jimmy's tell");
            Assert.IsTrue(foundPaul, "Didn't get Paul's tell");
        }

        [Test]
        public void TestGoogleForSomeone()
        {
            var client = (MockIrcClient)_client;
            var data = (MockData)_data;
            var gthx = _gthx;

            var testChannel = "#reprap";
            var testUser = "CerealKiller";
            var testGoogleUser = "Joey";

            gthx.HandleReceivedMessage(testChannel, testUser, $"google plastic for {testGoogleUser}");
            var replies = client.GetReplies();
            Assert.AreEqual(1, replies.Messages.Count);
            Assert.AreEqual(testChannel, replies.Channel);
            Assert.AreEqual($"{testGoogleUser}: http://lmgtfy.com/?q=plastic", replies.Messages[0]);

            gthx.HandleReceivedMessage(testChannel, testUser, $"google does 3=4? for {testGoogleUser}");
            replies = client.GetReplies();
            Assert.AreEqual(1, replies.Messages.Count);
            Assert.AreEqual(testChannel, replies.Channel);
            Assert.AreEqual($"{testGoogleUser}: http://lmgtfy.com/?q=does+3%3d4%3f", replies.Messages[0]);
        }

        [Test]
        public async Task TestYoutubeReferences()
        {
            var client = (MockIrcClient)_client;
            var data = (MockData)_data;
            var gthx = _gthx;

            var testChannel = "#reprap";
            var testUser = "BobYourUncle";
            gthx.HandleReceivedMessage(testChannel, testUser, $"Check out this cool spinner: https://www.youtube.com/watch?v=ykKIZQKaT5c");
            var replies = client.GetReplies();
            Assert.AreEqual(1, replies.Messages.Count);
            Assert.AreEqual(testChannel, replies.Channel);
            Assert.AreEqual($"{testUser} linked to YouTube video \"Spinner\" => 42 IRC mentions", replies.Messages[0]);

            // Test non-ASCII characters
            testUser = "AndrewJohnson";
            gthx.HandleReceivedMessage(testChannel, testUser, $"Calm down and listen to this: https://youtu.be/W3B2C0nNpFU");
            replies = client.GetReplies();
            Assert.AreEqual(1, replies.Messages.Count);
            Assert.AreEqual(testChannel, replies.Channel);
            Assert.AreEqual($"{testUser} linked to YouTube video \"Best relaxing piano studio ghibli complete collection ピアノスタジオジブリコレクション\" => 83 IRC mentions", replies.Messages[0]);

            // Test fetching a new title that uses the <title> element
            testUser = "RandomNick";
            gthx.HandleReceivedMessage(testChannel, testUser, $"here's another link https://youtu.be/title");
            await Task.Delay(500);
            replies = client.GetReplies();
            Assert.AreEqual(1, replies.Messages.Count);
            Assert.AreEqual(testChannel, replies.Channel);
            Assert.AreEqual($"{testUser} linked to YouTube video \"Dummy Title\" => 1 IRC mentions", replies.Messages[0]);

            // Test fetching a new title that uses the <meta> element for the title
            testUser = "AnotherNick";
            gthx.HandleReceivedMessage(testChannel, testUser, $"Or take a look at this one https://youtu.be/meta which I think is better");
            await Task.Delay(500);
            replies = client.GetReplies();
            Assert.AreEqual(1, replies.Messages.Count);
            Assert.AreEqual(testChannel, replies.Channel);
            Assert.AreEqual($"{testUser} linked to YouTube video \"Meta Title\" => 1 IRC mentions", replies.Messages[0]);

            // Test message when no title is found
            testUser = "AnotherNick";
            gthx.HandleReceivedMessage(testChannel, testUser, $"Does thie one work for you? https://youtu.be/notitle");
            await Task.Delay(500);
            replies = client.GetReplies();
            Assert.AreEqual(1, replies.Messages.Count);
            Assert.AreEqual(testChannel, replies.Channel);
            Assert.AreEqual($"{testUser} linked to a YouTube video with an unknown title => 1 IRC mentions", replies.Messages[0]);
        }

        [Test]
        public async Task TestThingiverseReferences()
        {
            var client = (MockIrcClient)_client;
            var data = (MockData)_data;
            var gthx = _gthx;

            var testChannel = "#reprap";
            var testUser = "BobYourUncle";
            gthx.HandleReceivedMessage(testChannel, testUser, $"Check out this cool spinner: https://www.thingiverse.com/thing:2823006");
            var replies = client.GetReplies();
            Assert.AreEqual(1, replies.Messages.Count);
            Assert.AreEqual(testChannel, replies.Channel);
            Assert.AreEqual($"{testUser} linked to \"Air Spinner\" on thingiverse => 42 IRC mentions", replies.Messages[0]);

            // Test non-ASCII characters
            testUser = "AndrewJohnson";
            gthx.HandleReceivedMessage(testChannel, testUser, $"https://www.thingiverse.com/thing:1276095 this is the coolest fish!!");
            replies = client.GetReplies();
            Assert.AreEqual(1, replies.Messages.Count);
            Assert.AreEqual(testChannel, replies.Channel);
            Assert.AreEqual($"{testUser} linked to \"Flexifish 🦈🌊\" on thingiverse => 23 IRC mentions", replies.Messages[0]);

            // Test fetching a new title that uses the <title> element
            testUser = "RandomNick";
            gthx.HandleReceivedMessage(testChannel, testUser, $"Your daughter would really like this: https://www.thingiverse.com/thing:2810756");
            await Task.Delay(500);
            replies = client.GetReplies();
            Assert.AreEqual(1, replies.Messages.Count);
            Assert.AreEqual(testChannel, replies.Channel);
            Assert.AreEqual($"{testUser} linked to \"Articulated Butterfly\" on thingiverse => 1 IRC mentions", replies.Messages[0]);

            // Test fetching a new title that uses the <meta> element for the title
            testUser = "AnotherNick";
            gthx.HandleReceivedMessage(testChannel, testUser, $"Or this one: https://www.thingiverse.com/thing:2818955");
            await Task.Delay(500);
            replies = client.GetReplies();
            Assert.AreEqual(1, replies.Messages.Count);
            Assert.AreEqual(testChannel, replies.Channel);
            Assert.AreEqual($"{testUser} linked to \"Articulated Slug\" on thingiverse => 1 IRC mentions", replies.Messages[0]);

            // Test message when no title is found
            testUser = "AnotherNick";
            gthx.HandleReceivedMessage(testChannel, testUser, $"Does thie one work for you? https://www.thingiverse.com/thing:4052802");
            await Task.Delay(500);
            replies = client.GetReplies();
            Assert.AreEqual(1, replies.Messages.Count);
            Assert.AreEqual(testChannel, replies.Channel);
            Assert.AreEqual($"{testUser} linked to thing 4052802 on thingiverse => 1 IRC mentions", replies.Messages[0]);
        }

        [Test]
        public async Task TestThingiverseInTell()
        {
            var client = (MockIrcClient)_client;
            var data = (MockData)_data;
            var gthx = _gthx;

            var testChannel = "#reprap";
            var testUser = "CerealKiller";
            var tellToUser = "Nikon";
            gthx.HandleReceivedMessage(testChannel, testUser, $"tell {tellToUser} Can I crash at your place and print this out? https://www.thingiverse.com/thing:2823006");
            Console.WriteLine("Finished processing received message");
            // Wait for async replies to get sent
            await Task.Delay(500);
            Console.WriteLine("Finished delay for async messages");
            var replies = client.GetReplies();
            Console.WriteLine($"Got {replies.Messages.Count} messages");
            Assert.AreEqual(2, replies.Messages.Count, "Didn't get the expected number of replies");
            Assert.AreEqual(testChannel, replies.Channel);

            var gotTellResponse = false;
            var gotTitleResponse = false;
            foreach (var message in replies.Messages)
            {
                if (message == $"{testUser}: Okay.")
                {
                    gotTellResponse = true;
                }
                if (message == $"{testUser} linked to \"Air Spinner\" on thingiverse => 42 IRC mentions")
                {
                    gotTitleResponse = true;
                }
            }

            Assert.IsTrue(gotTellResponse, "Failed to get expected response to the tell request");
            Assert.IsTrue(gotTitleResponse, "Failed to get expected response to the thingi title request");
        }

        [Test]
        public void TestSeenRequests()
        {
            var client = (MockIrcClient)_client;
            var data = (MockData)_data;
            var gthx = _gthx;

            var testChannel = "#reprap";
            var testUser = "PhantomPhreak";
            var testSeenUser = "gunnbr";

            gthx.HandleReceivedMessage(testChannel, testUser, $"seen {testSeenUser}?");
            var replies = client.GetReplies();
            Assert.AreEqual(2, replies.Messages.Count);
            Assert.AreEqual(data.LastSeenUserQuery, testSeenUser);

            Assert.AreEqual("gunnbr was last seen in #gthxtest ", replies.Messages[0].Substring(0, 34));
            Assert.AreEqual(" ago saying 'gthx: status?'.", replies.Messages[0].Substring(replies.Messages[0].Length - 28, 28));
            Assert.AreEqual("gunnbr_ was last seen in #reprap ", replies.Messages[1].Substring(0, 33));
            Assert.AreEqual(" ago saying 'Yeah, I'm trying to fix that.'.", replies.Messages[1].Substring(replies.Messages[1].Length - 44, 44));

            // Test without the question mark at the end
            testChannel = "#openscad";
            testUser = "AcidBurn";
            testSeenUser = "Razor";

            gthx.HandleReceivedMessage(testChannel, testUser, $"seen {testSeenUser}");
            replies = client.GetReplies();
            Assert.AreEqual(1, replies.Messages.Count);
            Assert.AreEqual(data.LastSeenUserQuery, testSeenUser);

            Assert.AreEqual("Razor was last seen in #twitch ", replies.Messages[0].Substring(0, 31));
            Assert.AreEqual("Stream is starting NOW! Tune in!'.", replies.Messages[0].Substring(replies.Messages[0].Length - 34, 34));

            // TODO: Test Timestamp == null in seen
        }

        [Test]
        public void TestSeenMaxReplies()
        {
            // Test that only 3 replies are returned to seen queries, no matter how many data returns

            var client = (MockIrcClient)_client;
            var data = (MockData)_data;
            var gthx = _gthx;

            var testChannel = "#reprap";
            var testUser = "someone";
            var testSeenUser = "The";

            gthx.HandleReceivedMessage(testChannel, testUser, $"seen {testSeenUser}?");
            var replies = client.GetReplies();
            Assert.AreEqual(3, replies.Messages.Count);
            Assert.AreEqual(data.LastSeenUserQuery, testSeenUser);

            Assert.AreEqual("TheHelper was last seen in #openscad ", replies.Messages[0].Substring(0, 37));
            Assert.AreEqual("ThePlague was last seen in #leets ", replies.Messages[1].Substring(0, 34));
            Assert.AreEqual("Themyscira was last seen in #superherohigh ", replies.Messages[2].Substring(0, 43));
        }

        [Test]
        public void TestSeenUpdates()
        {
            var client = (MockIrcClient)_client;
            var data = (MockData)_data;
            var gthx = _gthx;

            var testChannel = "#reprap";
            var testUser = "Joey";
            var testMessage = "Yo, yo, YO guys! I need a handle!!";

            gthx.HandleReceivedMessage(testChannel, testUser, testMessage);
            Assert.AreEqual(testUser, data.LastSeenUser);
            Assert.AreEqual(testChannel, data.LastSeenChannel);
            Assert.AreEqual(testMessage, data.LastSeenMessage);
            // Not useful to test LastSeenTimestamp here as that's set in MockData.
            // Make an integration test for it.
        }

        [Test]
        public void TestSeenUpdateOnAction()
        {
            var client = (MockIrcClient)_client;
            var data = (MockData)_data;
            var gthx = _gthx;

            var testChannel = "#reprap";
            var testUser = "PhantomPhreak";
            var testAction = "smacks Joey.";

            gthx.HandleReceivedAction(testChannel, testUser, testAction);
            Assert.AreEqual(testUser, data.LastSeenUser);
            Assert.AreEqual(testChannel, data.LastSeenChannel);
            Assert.AreEqual($"* {testUser} {testAction}", data.LastSeenMessage);
            // Not useful to test LastSeenTimestamp here as tgat's set in MockData.
            // Make an integration test for it.
        }

        [Test]
        public void TestSeenInPM()
        {
            // Verify that private messages don't update the seen info
            var client = (MockIrcClient)_client;
            var data = (MockData)_data;
            var gthx = _gthx;

            var testChannel = "gthx";
            var testUser = "Joey";
            var testMessage = "Dude! I found the garbage file!";

            gthx.HandleReceivedMessage(testChannel, testUser, testMessage);
            Assert.AreEqual(null, data.LastSeenUser);
            Assert.AreEqual(null, data.LastSeenChannel);
            Assert.AreEqual(null, data.LastSeenMessage);
        }

        [Test]
        public void TestStatus()
        {
            var client = (MockIrcClient)_client;
            var data = (MockData)_data;
            var gthx = _gthx;

            var testChannel = "#reprap";
            var testUser = "admin";

            gthx.HandleReceivedMessage(testChannel, testUser, $"status?");
            var replies = client.GetReplies();
            Assert.AreEqual(1, replies.Messages.Count);
            Assert.IsTrue(replies.Messages[0].Contains(": OK; Up for "), "Invalid status reply");
        }
    }
}