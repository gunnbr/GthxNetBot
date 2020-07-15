using Gthx.Test.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;

namespace Gthx.Test
{
    [TestClass]
    public class GthxTests
    {
        [TestMethod]
        public void TestGenericResponse()
        {
            var client = new MockIrcClient();
            var data = new MockData();
            var gthx = new Core.Gthx(client, data);

            // Test channel message
            var testChannel = "#reprap";
            var testUser = "SomeUser";
            gthx.HandleReceivedMessage(testChannel, testUser, "Which printer is best?");

            var replies = client.GetReplies();
            Assert.AreEqual(testChannel, replies.Channel);
            Assert.AreEqual(1, replies.Messages.Count);
            Assert.AreEqual($"Hello, {testUser}", replies.Messages[0]);

            // Test DM
            testChannel = "gthx";
            testUser = "SomeOtherUser";
            gthx.HandleReceivedMessage(testChannel, testUser, "Hey, can you help me?");

            replies = client.GetReplies();
            Assert.AreEqual(testChannel, replies.Channel);
            Assert.AreEqual(testChannel, replies.Channel);
            Assert.AreEqual($"Hello, {testUser}", replies.Messages[0]);
        }

        [TestMethod]
        public void TestFactoidSetting()
        {
            var client = new MockIrcClient();
            var data = new MockData();
            var gthx = new Core.Gthx(client, data);

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

        [TestMethod]
        public void TestFactoidGetting()
        {
            var client = new MockIrcClient();
            var data = new MockData();
            var gthx = new Core.Gthx(client, data);

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

        [TestMethod]
        public void TestAdvancedFactoidGetting()
        {
            var client = new MockIrcClient();
            var data = new MockData();
            var gthx = new Core.Gthx(client, data);

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

        [TestMethod]
        public void TestTellSetting()
        {
            var client = new MockIrcClient();
            var data = new MockData();
            var gthx = new Core.Gthx(client, data);

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

        [TestMethod]
        public void TestTellGetting()
        {
            var client = new MockIrcClient();
            var data = new MockData();
            var gthx = new Core.Gthx(client, data);

            var testChannel = "#reprap";
            var testUser = "CrashOverride";
            var testMessage = "Hey all! What's up?";
            gthx.HandleReceivedMessage(testChannel, testUser, testMessage);

            Assert.AreEqual(testUser, data.TellCheckUser);

            var replies = client.GetReplies();
            Assert.AreEqual(2, replies.Messages.Count);
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
            Assert.IsTrue(replies.Messages.Count > 2, "Not enough replies returned.");
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

        [TestMethod]
        public void TestGoogleForSomeone()
        {
            var client = new MockIrcClient();
            var data = new MockData();
            var gthx = new Core.Gthx(client, data);

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

        [TestMethod]
        public void TestYoutubeReferences()
        {
            var client = new MockIrcClient();
            var data = new MockData();
            var gthx = new Core.Gthx(client, data);

            var testChannel = "#reprap";
            var testUser = "BobYourUncle";
            gthx.HandleReceivedMessage(testChannel, testUser, $"Check out this cool spinner: https://www.youtube.com/watch?v=ykKIZQKaT5c");
            var replies = client.GetReplies();
            Assert.AreEqual(1, replies.Messages.Count);
            Assert.AreEqual(testChannel, replies.Channel);
            Assert.AreEqual($"{testUser} linked to YouTube video \"Spinner\" => 42 IRC mentions", replies.Messages[0]);

            // Test non-Western characters
            testUser = "AndrewJohnson";
            gthx.HandleReceivedMessage(testChannel, testUser, $"Calm down and listen to this: https://youtu.be/W3B2C0nNpFU");
            replies = client.GetReplies();
            Assert.AreEqual(1, replies.Messages.Count);
            Assert.AreEqual(testChannel, replies.Channel);
            Assert.AreEqual($"{testUser} linked to YouTube video \"Best relaxing piano studio ghibli complete collection ピアノスタジオジブリコレクション\" => 83 IRC mentions", replies.Messages[0]);

        }
    }
}