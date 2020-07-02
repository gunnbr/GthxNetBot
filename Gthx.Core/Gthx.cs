using Gthx.Core.Interfaces;
using Gthx.Core.Modules;
using System;
using System.Collections.Generic;

namespace Gthx.Core
{
    public class Gthx
    {
        private readonly IIrcClient _IrcClient;
        private readonly IGthxData _Data;
        private readonly List<IGthxModule> _Modules;

        public Gthx(IIrcClient ircClient, IGthxData data)
        {
            _IrcClient = ircClient;
            _Data = data;
            _Modules = new List<IGthxModule>
            {
                new FactoidModule(data)
            };
        }

        public void HandleReceivedMessage(string channel, string user, string message)
        {
            foreach (var module in _Modules)
            {
                var response = module.ProcessMessage(channel, user, message);
                if (response != null)
                {
                    _IrcClient.SendMessage(channel, response);
                    return;
                }
            }

            _IrcClient.SendMessage(channel, $"Hello, {user}");
        }
    }
}
