using Gthx.Test.Mocks;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Gthx.Test
{
    [TestFixture]
    public class GthxTests
    {
        [Test]
        public void TestGenericResponse()
        {
            var client = new MockIrcClient();
            var data = new MockData();
            var mockReader = new MockWebReader();
            var gthx = new Core.Gthx(client, data, mockReader);

            // Test channel message
            var testChannel = "#reprap";
            var testUser = "SomeUser";
            gthx.HandleReceivedMessage(testChannel, testUser, "Which printer is best?");

            var replies = client.GetReplies();
            Assert.AreEqual(0, replies.Messages.Count);

            // Test DM
            testChannel = "gthx";
            testUser = "SomeOtherUser";
            gthx.HandleReceivedMessage(testChannel, testUser, "Hey, can you help me?");

            replies = client.GetReplies();
            Assert.AreEqual(0, replies.Messages.Count);
        }

        [Test]
        public void TestFactoidSetting()
        {
            var client = new MockIrcClient();
            var data = new MockData();
            var mockReader = new MockWebReader();
            var gthx = new Core.Gthx(client, data, mockReader);

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
            var client = new MockIrcClient();
            var data = new MockData();
            var mockReader = new MockWebReader();
            var gthx = new Core.Gthx(client, data, mockReader);

            var testFactoid = "locked factoid";
            var testValue = "something that can't be changed.";
            var testChannel = "#reprap";
            var testUser = "MaliciousUser";

            gthx.HandleReceivedMessage(testChannel, testUser, $"{testFactoid} is {testValue}");

            var replies = client.GetReplies();
            Assert.AreEqual(1, replies.Messages.Count);
            Assert.AreEqual(testChannel, replies.Channel);
            Assert.AreEqual($"I'm sorry, {testUser}. I'm afraid I can't do that.", replies.Messages[0]);
            Assert.AreEqual(null, data.FactoidItem);
            Assert.AreEqual(null, data.FactoidValue);
            Assert.AreEqual(null, data.FactoidUser);
        }

        [Test]
        public void TestFactoidGetting()
        {
            var client = new MockIrcClient();
            var data = new MockData();
            var mockReader = new MockWebReader();
            var gthx = new Core.Gthx(client, data, mockReader);

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
            var client = new MockIrcClient();
            var data = new MockData();
            var mockReader = new MockWebReader();
            var gthx = new Core.Gthx(client, data, mockReader);

            // Test "<reply>" and "!who"
            var testChannel = "#reprap";
            var testUser = "eliteBoi";
            var testFactoid = "botsmack";

            gthx.HandleReceivedMessage(testChannel, testUser, $"{testFactoid}!");

            var replies = client.GetReplies();
            Assert.AreEqual(1, replies.Messages.Count);
            Assert.AreEqual(testChannel, replies.Channel);
            Assert.AreEqual(testFactoid, data.FactoidGotten);
            Assert.AreEqual($"{testUser}, stop that!", replies.Messages[0]);

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
        public void TestTellSetting()
        {
            var client = new MockIrcClient();
            var data = new MockData();
            var mockReader = new MockWebReader();
            var gthx = new Core.Gthx(client, data, mockReader);

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
            var client = new MockIrcClient();
            var data = new MockData();
            var mockReader = new MockWebReader();
            var gthx = new Core.Gthx(client, data, mockReader);

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
            var client = new MockIrcClient();
            var data = new MockData();
            var mockReader = new MockWebReader();
            var gthx = new Core.Gthx(client, data, mockReader);

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
            var client = new MockIrcClient();
            var data = new MockData();
            var mockReader = new MockWebReader();
            var gthx = new Core.Gthx(client, data, mockReader);

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
            var client = new MockIrcClient();
            var data = new MockData();
            var mockReader = new MockWebReader();
            var gthx = new Core.Gthx(client, data, mockReader);

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
            var client = new MockIrcClient();
            var data = new MockData();
            var mockReader = new MockWebReader();
            var gthx = new Core.Gthx(client, data, mockReader);

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
    }
}