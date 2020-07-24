using Gthx.Core.Interfaces;
using Gthx.Core.Modules;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Gthx.Core
{
    public class Gthx
    {
        public readonly static string Version = "0.8 2020-07-24";

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
                new StatusModule(data, _IrcClient),
                new FactoidModule(data, _IrcClient),
                new TellModule(data, _IrcClient),
                new SeenModule(data, _IrcClient),
                // new LurkerModule(data, _IrcClient),
                new GoogleModule(_IrcClient),

                // Reference and title checkers come last because their
                // responses should come after any responses from the above
                // modules, if any.
                new ThingiverseModule(data, _IrcClient, _WebReader),
                new YoutubeModule(data, _IrcClient, _WebReader),
            };
        }

        public void HandleReceivedMessage(string channel, string user, string message)
        {
            // TODO: Handle some messages directly addressed to gthx differently than
            //       the same message not addressed to gthx.

            foreach (var module in _Modules)
            {
                module.ProcessMessage(channel, user, message);
            }
        }
        public void HandleReceivedAction(string channel, string user, string action)
        {
        }
    }
}
