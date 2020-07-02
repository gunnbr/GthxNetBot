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
        public void TestSimpleFactoid()
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

            var retrievedFactoid = data.GetFactoid(testFactoid);
            Assert.AreEqual(0, retrievedFactoid.Count);

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
    }
}
