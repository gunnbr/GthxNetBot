using Gthx.Bot.Interfaces;
using Gthx.Bot.Modules;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace Gthx.Bot
{
    public class GthxBot
    {
        public readonly static string Version = "0.8 2020-07-24";

        private readonly List<IGthxModule> _Modules;

        public static void RegisterServices(ServiceCollection services)
        {
            // This seems a little strange to have this class register for all
            // the services it needs, but I think they have to be registered
            // this way to get DI to work correctly and the Microsoft DI container
            // won't automatically register all these for us.
            // But it seems wrong for something outside of Gthx to have to specify
            // what modules are needed, so I'll try this for now.
            services.TryAddEnumerable(new[]
            {
                ServiceDescriptor.Singleton<IGthxModule, StatusModule>(),
                ServiceDescriptor.Singleton<IGthxModule, FactoidModule>(),
                ServiceDescriptor.Singleton<IGthxModule, TellModule>(),
                ServiceDescriptor.Singleton<IGthxModule, SeenModule>(),
                //ServiceDescriptor.Singleton<IGthxModule, LurkerModule>(),
                ServiceDescriptor.Singleton<IGthxModule, GoogleModule>(),

                // Reference and title checkers come last because their
                // responses should come after any responses from the above
                // modules, if any.
                ServiceDescriptor.Singleton<IGthxModule, ThingiverseModule>(),
                ServiceDescriptor.Singleton<IGthxModule, YoutubeModule>(),
            });
        }

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
