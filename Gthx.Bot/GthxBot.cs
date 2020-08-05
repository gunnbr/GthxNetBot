using Gthx.Bot.Interfaces;
using Gthx.Bot.Modules;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Gthx.Bot
{
    public class GthxBot
    {
        public readonly static string Version = "0.8 2020-07-24";

        private readonly List<IGthxModule> _Modules;

        public GthxBot(IEnumerable<IGthxModule> modules)
        {
            _Modules = modules.ToList();
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
