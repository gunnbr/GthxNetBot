using System;
using System.Collections.Generic;
using System.Text;
using Gthx.Bot.Interfaces;

namespace Gthx.Bot
{
    public class NickManager : IBotNick
    {
        // TODO: Default this to config setting
        private string _botNick = "gnetbot";

        public string BotNick
        {
            get => _botNick;
            set
            {
                if (value == _botNick)
                {
                    return;
                }

                _botNick = value;
                BotNickChangedEvent?.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler? BotNickChangedEvent;
    }
}
