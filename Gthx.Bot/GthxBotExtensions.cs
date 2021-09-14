using Gthx.Bot.Interfaces;
using Gthx.Bot.Modules;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
                // This group of commands shouldn't have any
                // YouTube or Thingiverse links embedded, so they're safe
                // to process first, before the link handling modules.
                ServiceDescriptor.Singleton<IGthxModule, StatusModule>(),
                ServiceDescriptor.Singleton<IGthxModule, SeenModule>(),
                ServiceDescriptor.Singleton<IGthxModule, GoogleModule>(),
                ServiceDescriptor.Singleton<IGthxModule, LurkerModule>(),

                ServiceDescriptor.Singleton<IGthxModule, ThingiverseModule>(),
                ServiceDescriptor.Singleton<IGthxModule, YoutubeModule>(),

                ServiceDescriptor.Singleton<IGthxModule, TellModule>(),

                // Factoid module should be last to prevent excess queries for factoids
                // when that wasn't the user's intention.
                ServiceDescriptor.Singleton<IGthxModule, FactoidModule>(),
            });

            return services;
        }
    }
}
