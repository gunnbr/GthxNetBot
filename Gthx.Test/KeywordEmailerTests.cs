using Gthx.Bot;
using Gthx.Bot.Interfaces;
using Gthx.Data;
using Gthx.Test.Mocks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;
using System.Linq;

namespace Gthx.Test
{
    [TestFixture]
    public class KeywordEmailerTests
    {
        private static readonly DateTime Start = new(2026, 10, 29, 21, 0, 0, DateTimeKind.Utc);

        private ServiceProvider _services;
        private MockData _data;
        private MockEmailSender _email;
        private KeywordEmailer _emailer;

        // A new bot for each test, so each starts without keywords read or emails sent.
        [SetUp]
        public void SetUp()
        {
            _services = Services(new MockEmailSender());
            _data = (MockData)_services.GetRequiredService<IGthxData>();
            _email = (MockEmailSender)_services.GetRequiredService<IEmailSender>();
            _emailer = _services.GetRequiredService<KeywordEmailer>();
        }

        [TearDown]
        public void TearDown()
        {
            _services.Dispose();
        }

        private static ServiceProvider Services(IEmailSender emailSender)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<MockIrcClient>();
            services.AddSingleton<IIrcClient>(sp => sp.GetRequiredService<MockIrcClient>());
            services.AddSingleton<IBotNick>(sp => sp.GetRequiredService<MockIrcClient>());
            services.AddSingleton<IGthxData, MockData>();
            services.AddSingleton<IWebReader, MockWebReader>();
            services.AddSingleton<IGthxUtil, GthxUtil>();
            services.AddSingleton(emailSender);
            services.AddGthxBot();
            services.AddSingleton<GthxMessageConduit>();
            services.AddSingleton<IGthxMessageConsumer>(sp => sp.GetRequiredService<GthxMessageConduit>());
            services.AddSingleton<GthxBot>();
            return services.BuildServiceProvider();
        }

        [Test]
        public void Message_MatchesAnywhere_IgnoringCase()
        {
            _data.EmailKeywords.AddRange(new[] { "printer", "Fire", "unused" });
            var gthx = _services.GetRequiredService<GthxBot>();

            gthx.HandleReceivedMessage("#reprap", "joe", "my 3D PRINTERS are on fire");

            var email = _email.Sent.Single();
            ClassicAssert.AreEqual("gthxbot: \"printer\", \"Fire\" in #reprap", email.Subject);
            StringAssert.Contains("joe said in #reprap at ", email.Body);
            StringAssert.Contains("my 3D PRINTERS are on fire", email.Body);
            StringAssert.Contains("Keywords: printer, Fire.", email.Body);
        }

        [Test]
        public void Message_KeywordCanBeTheBotsNick_AndOtherModulesStillRun()
        {
            _data.EmailKeywords.Add("gthxbot");
            var gthx = _services.GetRequiredService<GthxBot>();

            gthx.HandleReceivedMessage("#reprap", "joe", "gthxbot: printer?");

            ClassicAssert.AreEqual(1, _email.Sent.Count);
            ClassicAssert.AreEqual("printer", _data.FactoidGotten);
        }

        [Test]
        public void Action_And_PrivateMessage()
        {
            _data.EmailKeywords.Add("printer");
            var gthx = _services.GetRequiredService<GthxBot>();

            gthx.HandleReceivedAction("#reprap", "joe", "fixes his printer");
            gthx.HandleReceivedMessage("joe", "joe", "is my printer broken?");

            ClassicAssert.AreEqual(2, _email.Sent.Count);
            StringAssert.Contains("* joe fixes his printer", _email.Sent[0].Body);
            ClassicAssert.AreEqual("gthxbot: \"printer\" in a private message", _email.Sent[1].Subject);
        }

        [Test]
        public void NoMatch_SendsNothing()
        {
            _data.EmailKeywords.Add("printer");

            CollectionAssert.IsEmpty(_emailer.Check("#reprap", "joe", "my extruder is clogged", Start));
            CollectionAssert.IsEmpty(_email.Sent);
        }

        [Test]
        public void WaitsBeforeEmailingAboutAKeywordInAChannelAgain()
        {
            _data.EmailKeywords.AddRange(new[] { "printer", "fire" });

            CollectionAssert.AreEqual(new[] { "printer" }, _emailer.Check("#reprap", "joe", "printer", Start));
            // Only the keyword that hasn't been emailed about lately.
            CollectionAssert.AreEqual(new[] { "fire" }, _emailer.Check("#reprap", "joe", "Printer on fire", Start.AddMinutes(1)));
            CollectionAssert.IsEmpty(_emailer.Check("#reprap", "amy", "printer", Start.AddMinutes(14)));
            CollectionAssert.AreEqual(new[] { "printer" }, _emailer.Check("#other", "joe", "printer", Start.AddMinutes(14)));
            CollectionAssert.AreEqual(new[] { "printer" }, _emailer.Check("#RepRap", "joe", "printer", Start + KeywordEmailer.QuietTime));

            ClassicAssert.AreEqual(4, _email.Sent.Count);
        }

        [Test]
        public void ReadsTheKeywordsAgainEveryMinute()
        {
            _data.EmailKeywords.Add("printer");
            _emailer.Check("#reprap", "joe", "hello", Start);

            _data.EmailKeywords.Add("extruder");
            CollectionAssert.IsEmpty(_emailer.Check("#reprap", "joe", "extruder", Start.AddSeconds(59)));
            CollectionAssert.AreEqual(new[] { "extruder" }, _emailer.Check("#reprap", "joe", "extruder", Start + KeywordEmailer.RefreshInterval));

            ClassicAssert.AreEqual(2, _data.EmailKeywordReads);
        }

        [Test]
        public void KeepsTheLastKeywords_WhenTheDatabaseFails()
        {
            _data.EmailKeywords.Add("printer");
            _emailer.Check("#reprap", "joe", "hello", Start);

            _data.FailEmailKeywordReads = true;
            CollectionAssert.AreEqual(new[] { "printer" }, _emailer.Check("#reprap", "joe", "printer", Start.AddMinutes(1)));
            // And doesn't try again for every message.
            _emailer.Check("#reprap", "joe", "hello", Start.AddMinutes(1.5));
            ClassicAssert.AreEqual(2, _data.EmailKeywordReads);
        }

        [Test]
        public void SendingFailure_DoesNotStopTheBot()
        {
            _data.EmailKeywords.Add("printer");
            _email.Fail = true;
            var gthx = _services.GetRequiredService<GthxBot>();

            Assert.DoesNotThrow(() => gthx.HandleReceivedMessage("#reprap", "joe", "gthxbot: printer?"));
            ClassicAssert.AreEqual("printer", _data.FactoidGotten);
        }

        [Test]
        public void WithoutEmail_DoesNotReadKeywords()
        {
            using var services = Services(new NoEmailSender());
            var data = (MockData)services.GetRequiredService<IGthxData>();
            data.EmailKeywords.Add("printer");

            CollectionAssert.IsEmpty(services.GetRequiredService<KeywordEmailer>().Check("#reprap", "joe", "printer", Start));
            ClassicAssert.AreEqual(0, data.EmailKeywordReads);
        }
    }
}
