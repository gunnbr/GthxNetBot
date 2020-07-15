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
            // In this order because that's how it was in the
            // original source. Could probably change it without problems.
            // TODO: Rearrange these in a better priority order and retest once
            //       all unit tests are done.
                new TellModule(data),
                // new StatusModule(data),
                // new LurkerModule(data),
                // new SeenModule(data),
                new GoogleModule(),
                new FactoidModule(data),
                // new ThingiverseModule(data),
                new YoutubeModule(data),
            };
        }

        public void HandleReceivedMessage(string channel, string user, string message)
        {
            // TODO: Update last seen time

            // TODO: Handle some messages directly addressed to gthx differently than
            //       the same message not addressed to gthx.

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

#if DEBUG
            // TODO: Take this out!!
            _IrcClient.SendMessage(channel, $"Hello, {user}");
#endif
        }
    }
}
