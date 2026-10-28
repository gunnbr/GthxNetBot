using Gthx.Bot;
using NUnit.Framework;
using System;

namespace Gthx.Test
{
    [TestFixture]
    public class CtcpTests
    {
        [TestCase("\u0001VERSION\u0001", "VERSION", "")]
        [TestCase("\u0001version\u0001", "VERSION", "")]
        [TestCase("\u0001PING 1761630000 123\u0001", "PING", "1761630000 123")]
        [TestCase("\u0001ACTION waves at everyone\u0001", "ACTION", "waves at everyone")]
        [TestCase("\u0001ACTION waves without the closing delimiter", "ACTION", "waves without the closing delimiter")]
        [TestCase("\u0001ACTION\u0001", "ACTION", "")]
        public void TestParse(string text, string command, string parameters)
        {
            ClassicAssert.IsTrue(Ctcp.TryParse(text, out var parsedCommand, out var parsedParameters));
            ClassicAssert.AreEqual(command, parsedCommand);
            ClassicAssert.AreEqual(parameters, parsedParameters);
        }

        [TestCase("hello there")]
        [TestCase("")]
        [TestCase("\u0001")]
        [TestCase("\u0001\u0001")]
        [TestCase("VERSION\u0001")]
        public void TestNotCtcp(string text)
        {
            ClassicAssert.IsFalse(Ctcp.TryParse(text, out _, out _));
        }

        [Test]
        public void TestReplies()
        {
            var now = new DateTime(2026, 10, 28, 6, 30, 15, DateTimeKind.Utc);

            ClassicAssert.AreEqual($"VERSION Gthx Bot Version {GthxBot.Version}", Ctcp.GetReply("VERSION", "", now));
            ClassicAssert.AreEqual("PING 1761630000 123", Ctcp.GetReply("PING", "1761630000 123", now));
            ClassicAssert.AreEqual("PING", Ctcp.GetReply("PING", "", now));
            ClassicAssert.AreEqual("PING ab", Ctcp.GetReply("PING", "a\u0001b", now));
            ClassicAssert.AreEqual("TIME Wed, 28 Oct 2026 06:30:15 GMT", Ctcp.GetReply("TIME", "", now));
            ClassicAssert.AreEqual("SOURCE https://github.com/gunnbr/GthxNetBot", Ctcp.GetReply("SOURCE", "", now));
            ClassicAssert.AreEqual("CLIENTINFO ACTION CLIENTINFO PING SOURCE TIME VERSION", Ctcp.GetReply("CLIENTINFO", "", now));

            // ACTION is passed on to the bot, and unknown requests get no reply
            ClassicAssert.IsNull(Ctcp.GetReply("ACTION", "waves", now));
            ClassicAssert.IsNull(Ctcp.GetReply("FINGER", "", now));
            ClassicAssert.IsNull(Ctcp.GetReply("DCC", "SEND file 0 0 0", now));
        }
    }
}
