using Gthx.Bot.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gthx.Test.Mocks
{
    public class Replies
    {
        public string Channel;
        public List<string> Actions;
        public List<string> Messages;
    }

    public class MockIrcClient : IIrcClient, IBotNick
    {
        private string SentToChannel { get; set; }

        private List<string> SentActions { get; set; } = new List<string>();

        private List<string> SentMessages { get; set; } = new List<string>();

        public string BotNick { get; set; } = "gthxbot";

        public DateTime StartupTime => DateTime.UtcNow;

        public event EventHandler BotNickChangedEvent;

        public bool SendAction(string channel, string action)
        {
            SentToChannel = channel;
            SentActions.Add(action);
            return true;
        }

        public bool SendMessage(string channel, string message)
        {
            SentToChannel = channel;
            SentMessages.Add(message);
            // TODO: Implement some kind of event trigger when a message is sent so
            //       that unit tests can use longer timeouts but still complete as soon
            //       as a message is sent..
            return true;
        }

        public Task<List<string>> GetUsersInChannelAsync(string channel)
        {
            return Task.Run(() =>
            {
                var users = new List<string>
                {
                    "gunnbr",
                    "Razor",
                    "LameLurker",
                    BotNick,
                    "SomeOtherBot"
                };
                return users;
            });
        }

        #region Methods for testing
        /// <summary>
        /// Returns only the replies sent since the last time this method was called.
        /// </summary>
        /// <returns></returns>
        public Replies GetReplies()
        {
            var replies = new Replies
            {
                Channel = SentToChannel,
                Actions = SentActions,
                Messages = SentMessages
            };

            SentToChannel = null;
            SentActions = new List<string>();
            SentMessages = new List<string>();

            return replies;
        }
        #endregion
    }
}
