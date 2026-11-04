using Gthx.Bot;
using NUnit.Framework;
using System.Collections.Generic;

namespace Gthx.Test
{
    [TestFixture]
    public class BotModeTests
    {
        [Test]
        public void TestBotTokenGivesModeLetter()
        {
            var isupport = new Dictionary<string, string> { ["CHANTYPES"] = "#", ["BOT"] = "B", ["NETWORK"] = "Libera.Chat" };
            ClassicAssert.AreEqual('B', BotMode.GetModeLetter(isupport));
        }

        [Test]
        public void TestBotTokenIgnoresCase()
        {
            var isupport = new Dictionary<string, string> { ["bot"] = "B" };
            ClassicAssert.AreEqual('B', BotMode.GetModeLetter(isupport));
        }

        [Test]
        public void TestNoBotToken()
        {
            var isupport = new Dictionary<string, string> { ["CHANTYPES"] = "#" };
            ClassicAssert.IsNull(BotMode.GetModeLetter(isupport));
        }

        [TestCase("")]
        [TestCase(null)]
        public void TestBotTokenWithoutValue(string value)
        {
            var isupport = new Dictionary<string, string> { ["BOT"] = value };
            ClassicAssert.IsNull(BotMode.GetModeLetter(isupport));
        }

        [Test]
        public void TestNoISupportYet()
        {
            ClassicAssert.IsNull(BotMode.GetModeLetter(null));
        }
    }
}
