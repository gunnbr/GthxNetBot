using Gthx.Core.Interfaces;
using System;

namespace Gthx.Core
{
    public class Gthx
    {
        public IIrcClient _IrcClient;
        private readonly IGthxData _Data;

        public Gthx(IIrcClient ircClient, IGthxData data)
        {
            _IrcClient = ircClient;
            _Data = data;
        }

        public void HandleReceivedMessage(string channel, string user, string message)
        {
            _IrcClient.SendMessage(channel, $"Hello, {user}");
        }
    }
}
