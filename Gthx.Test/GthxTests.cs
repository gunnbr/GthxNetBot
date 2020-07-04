using Gthx.Test.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
            Assert.AreEqual(testChannel, client.SentToChannel);
            Assert.AreEqual($"Hello, {testUser}", client.SentMessage);

            // Test DM
            testChannel = "gthx";
            testUser = "SomeOtherUser";
            gthx.HandleReceivedMessage(testChannel, testUser, "Hey, can you help me?");
            Assert.AreEqual(testChannel, client.SentToChannel);
            Assert.AreEqual($"Hello, {testUser}", client.SentMessage);
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
            Assert.AreEqual(testChannel, client.SentToChannel);
            Assert.AreEqual($"{testUser}: Okay.", client.SentMessage);
            Assert.AreEqual(testFactoid, data.FactoidItem);
            Assert.AreEqual(testValue, data.FactoidValue);
            Assert.IsFalse(data.FactoidIsAre);
            Assert.AreEqual(testUser, data.FactoidUser);
            Assert.IsTrue(data.FactoidReplaceExisting);

            gthx.HandleReceivedMessage(testChannel, testUser2, $"{testFactoid} are also {testValue2}");
            Assert.AreEqual(testChannel, client.SentToChannel);
            Assert.AreEqual($"{testUser2}: Okay.", client.SentMessage);
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
            Assert.AreEqual(testChannel, client.SentToChannel);
            Assert.AreEqual(testFactoid, data.FactoidGotten);
            Assert.AreEqual($"{testFactoid} is the best way to learn about 3D printing", client.SentMessage);

            // Same test with an exclamation point!
            gthx.HandleReceivedMessage(testChannel, testUser, $"{testFactoid}!");
            Assert.AreEqual(testChannel, client.SentToChannel);
            Assert.AreEqual(testFactoid, data.FactoidGotten);
            Assert.AreEqual($"{testFactoid} is the best way to learn about 3D printing", client.SentMessage);

            // Test "are"
            testFactoid = "pennies";
            gthx.HandleReceivedMessage(testChannel, testUser, $"{testFactoid}?");
            Assert.AreEqual(testChannel, client.SentToChannel);
            Assert.AreEqual(testFactoid, data.FactoidGotten);
            Assert.AreEqual($"{testFactoid} are small coins", client.SentMessage);

            // Test multiple values set:
            testFactoid = "cake";
            gthx.HandleReceivedMessage(testChannel, testUser, $"{testFactoid}?");
            Assert.AreEqual(testChannel, client.SentToChannel);
            Assert.AreEqual(testFactoid, data.FactoidGotten);
            Assert.AreEqual($"{testFactoid} is really yummy and also a lie", client.SentMessage);

            // Test emoji
            testFactoid = "emoji";
            gthx.HandleReceivedMessage(testChannel, testUser, $"{testFactoid}?");
            Assert.AreEqual(testChannel, client.SentToChannel);
            Assert.AreEqual(testFactoid, data.FactoidGotten);
            Assert.AreEqual($"{testFactoid} is handled well: 😍🍕🎉💪", client.SentMessage);

            // Test extended unicode points
            testFactoid = "other languages";
            gthx.HandleReceivedMessage(testChannel, testUser, $"{testFactoid}?");
            Assert.AreEqual(testChannel, client.SentToChannel);
            Assert.AreEqual(testFactoid, data.FactoidGotten);
            Assert.AreEqual($"{testFactoid} are このアプリケーションで十分にサポートされています", client.SentMessage);
        }
    }
}
