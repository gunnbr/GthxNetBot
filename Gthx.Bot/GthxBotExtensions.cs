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
                ServiceDescriptor.Scoped<IGthxModule, StatusModule>(),
                ServiceDescriptor.Scoped<IGthxModule, SeenModule>(),
                ServiceDescriptor.Scoped<IGthxModule, GoogleModule>(),
                ServiceDescriptor.Scoped<IGthxModule, LurkerModule>(),

                ServiceDescriptor.Scoped<IGthxModule, ThingiverseModule>(),
                ServiceDescriptor.Scoped<IGthxModule, YoutubeModule>(),

                ServiceDescriptor.Scoped<IGthxModule, TellModule>(),

                // Factoid module should be last to prevent excess queries for factoids
                // when that wasn't the user's intention.
                ServiceDescriptor.Scoped<IGthxModule, FactoidModule>(),
            });

            return services;
        }
    }
}
