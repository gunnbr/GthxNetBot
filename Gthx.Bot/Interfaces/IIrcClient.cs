using System;
using System.Collections.Generic;
using System.Text;

namespace Gthx.Bot.Interfaces
{
    public interface IIrcClient
    {
        /// <summary>
        /// Nickname this bot has on the server
        /// </summary>
        public string BotNick { get; }

        public bool SendMessage(string channel, string message);
        public bool SendAction(string channel, string message);
    }
}
