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

                // Reference and title checkers must come first because they aren't the final word
                // and we want additional modules to run after them.
                new ThingiverseModule(data, _IrcClient, _WebReader),
                new YoutubeModule(data, _IrcClient, _WebReader),

                new TellModule(data, _IrcClient),
                // new StatusModule(data),
                // new LurkerModule(data),
                // new SeenModule(data),
                new GoogleModule(_IrcClient),
                new FactoidModule(data, _IrcClient),
            };
        }

        public void HandleReceivedMessage(string channel, string user, string message)
        {
            // TODO: Update last seen time

            // TODO: Handle some messages directly addressed to gthx differently than
            //       the same message not addressed to gthx.

            foreach (var module in _Modules)
            {
                module.ProcessMessage(channel, user, message);
            }

#if DEBUG
                // TODO: Take this out!!
                //_IrcClient.SendMessage(channel, $"Hello, {user}");
#endif
        }
    }
}
