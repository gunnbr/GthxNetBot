using Gthx.Bot.Interfaces;
using Gthx.Bot.Modules;
using Gthx.Data;
using System.Collections.Generic;

namespace Gthx.Bot
{
    public class Gthx
    {
        public readonly static string Version = "0.8 2020-07-24";

        private readonly List<IGthxModule> _Modules;

        public Gthx(IIrcClient ircClient, IGthxData data, IWebReader webReader)
        {
            _Modules = new List<IGthxModule>
            {
                new StatusModule(data, ircClient),
                new FactoidModule(data, ircClient),
                new TellModule(data, ircClient),
                new SeenModule(data, ircClient),
                // new LurkerModule(data, _IrcClient),
                new GoogleModule(ircClient),

                // Reference and title checkers come last because their
                // responses should come after any responses from the above
                // modules, if any.
                new ThingiverseModule(data, ircClient, webReader),
                new YoutubeModule(data, ircClient, webReader),
            };
        }

        public void HandleReceivedMessage(string channel, string user, string message)
        {
            // TODO: Handle some messages directly addressed to gthx differently than
            //       the same message not addressed to gthx.

            // TODO: Add return value from modules so some can stop further processing.
            //       For instance: "seen gunnbr?" should not run a factoid check
            //       and "status?" also should not run a factoid check.
            foreach (var module in _Modules)
            {
                module.ProcessMessage(channel, user, message);
            }
        }

        public void HandleReceivedAction(string channel, string user, string action)
        {
            foreach (var module in _Modules)
            {
                module.ProcessAction(channel, user, action);
            }
        }
    }
}
