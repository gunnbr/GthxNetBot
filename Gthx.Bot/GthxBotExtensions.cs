using Gthx.Bot.Interfaces;
using Gthx.Bot.Modules;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Gthx.Bot
{
    public static class GthxBotExtensions
    {
        /// <summary>
        /// Registers all modules necessary for GthxBot to function properly
        /// </summary>
        public static IServiceCollection AddGthxBot(this IServiceCollection services)
        {
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

            return services;
        }
    }
}
