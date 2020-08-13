using System;
using System.Security.Policy;
using Gthx.Bot;
using Gthx.Bot.Interfaces;
using GthxData;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GthxNetBot
{
    public class ConsoleTestBot : IBotRunner
    {
        private readonly IIrcClient _ircClient;
        private readonly ILogger<ConsoleTestBot> _logger;
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _services;

        public ConsoleTestBot(IIrcClient ircClient, ILogger<ConsoleTestBot> logger, IConfiguration configuration, IServiceProvider services)
        {
            _ircClient = ircClient;
            _logger = logger;
            _configuration = configuration;
            _services = services;
            _logger.LogInformation("ConsoleTestBot constructor: Logging enabled");
        }

        // TODO: Refactor this to work like the unit tests now that I know more about
        //       how to make the DI work.
        public void Run()
        {
            Console.WriteLine("Welcome to Gthx");
            _logger.LogInformation($"irc client is {_ircClient}");

            var context = _services.GetRequiredService<GthxDataContext>();
            context.Database.EnsureCreated();
            var gthx = _services.GetRequiredService<GthxBot>();

            var done = false;
            while (!done)
            {
                Console.Write("gunnbr> ");
                try
                {
                    var input = Console.ReadLine();
                    if (input == null || input == "quit")
                    {
                        done = true;
                        continue;
                    }
                    else if (input.StartsWith("/me "))
                    {
                        gthx.HandleReceivedAction("#reprap", "gunnbr", input[4..]);
                    }
                    else
                    {
                        gthx.HandleReceivedMessage("#reprap", "gunnbr", input);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception in gthx program");
                    done = true;
                }
            }
        }
    }
}