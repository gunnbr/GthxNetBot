using System;

namespace Gthx.Bot.Interfaces
{
    public interface IBotNick
    {
        // TODO: Split this out into 2 interfaces? One for getting, one for setting?
        //       Or come up with an entirely different solution. This one seems more
        //       complicated than it should be!

        /// <summary>
        /// Nickname this bot has on the server
        /// </summary>
        public string BotNick { get; set; }

        /// <summary>
        /// Notification of when the nickname of the bot changes.
        /// </summary>
        public event EventHandler BotNickChangedEvent;
    }
}
