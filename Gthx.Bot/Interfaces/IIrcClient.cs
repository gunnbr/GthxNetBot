using System;
using System.Collections.Generic;
using System.Text;

namespace Gthx.Bot.Interfaces
{
    public interface IIrcClient
    {
        public bool SendMessage(string channel, string message);
        public bool SendAction(string channel, string message);
    }
}
