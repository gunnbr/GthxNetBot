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
            var gthx = new Core.Gthx(client);

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
    }
}
