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
                new TellModule(data),
                new FactoidModule(data)
            };
        }

        public void HandleReceivedMessage(string channel, string user, string message)
        {
            // TODO: Update last seen time

            // TODO: Check for waiting 'tell' messagae

            foreach (var module in _Modules)
            {
                var responses = module.ProcessMessage(channel, user, message);
                if (responses != null)
                {
                    foreach (var response in responses)
                    {
                        if (response.Type == ResponseType.Normal)
                        {
                            _IrcClient.SendMessage(channel, response.Message);
                        }

                        if (response.Type == ResponseType.Action)
                        {
                            _IrcClient.SendAction(channel, response.Message);
                        }

                        if (response.IsFinalResponse)
                        {
                            return;
                        }
                    }
                }
            }

            // TODO: Take this out!!
            _IrcClient.SendMessage(channel, $"Hello, {user}");
        }
    }
}
