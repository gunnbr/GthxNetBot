using Gthx.Core.Interfaces;
using System;

namespace Gthx.Core
{
    public class Gthx
    {
        public IIrcClient _IrcClient;

        public Gthx(IIrcClient ircClient)
        {
            _IrcClient = ircClient;
        }

        public void HandleReceivedMessage(string channel, string user, string message)
        {
            _IrcClient.SendMessage(channel, $"Hello, {user}");
        }
    }
}
