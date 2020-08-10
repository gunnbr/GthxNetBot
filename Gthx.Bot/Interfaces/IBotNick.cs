using System;

namespace Gthx.Bot.Interfaces
{
    public interface IBotNick
    {
        /// <summary>
        /// Nickname this bot has on the server
        /// </summary>
        public string BotNick { get; }

        /// <summary>
        /// Notification of when the nickname of the bot changes.
        /// </summary>
        public event EventHandler BotNickChangedEvent;
    }
}
