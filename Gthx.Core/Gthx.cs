using Gthx.Core.Interfaces;
using Gthx.Core.Modules;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Gthx.Core
{
    public class Gthx
    {
        private readonly IIrcClient _IrcClient;
        private readonly IGthxData _Data;
        private readonly IWebReader _WebReader;

        private readonly List<IGthxModule> _Modules;
        private readonly List<IGthxModule> _AsyncModules;

        public Gthx(IIrcClient ircClient, IGthxData data, IWebReader webReader)
        {
            _IrcClient = ircClient;
            _Data = data;
            _WebReader = webReader;

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
            };

            _AsyncModules = new List<IGthxModule>
            {
                // new ThingiverseModule(data),
                new YoutubeModule(data, _WebReader),
            };
        }

        public async Task HandleReceivedMessage(string channel, string user, string message)
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

            foreach (var asyncModule in _AsyncModules)
            {
                var asyncResponses = await asyncModule.ProcessMessageAsync(channel, user, message);
                if (asyncResponses != null)
                {
                    foreach (var response in asyncResponses)
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
