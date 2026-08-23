using Gthx.Bot;
using Gthx.Bot.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace GthxNetBot
{
    public class ConsoleTestBot : IBotRunner
    {
        private readonly IIrcClient _ircClient;
        private readonly ILogger<ConsoleTestBot> _logger;
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _services;
        private bool _ExitRequested = false;

        public ConsoleTestBot(IIrcClient ircClient, ILogger<ConsoleTestBot> logger, IConfiguration configuration, IServiceProvider services)
        {
            _ircClient = ircClient;
            _logger = logger;
            _configuration = configuration;
            _services = services;
            _logger.LogInformation("ConsoleTestBot constructor: Logging enabled");
        }

        public void Exit()
        {
            _logger.LogInformation("Exiting as requested");
            _ExitRequested = true;
        }

        // TODO: Refactor this to work like the unit tests now that I know more about
        //       how to make the DI work.
        public void Run()
        {
            Console.WriteLine("Welcome to Gthx");
            _logger.LogInformation($"irc client is {_ircClient}");

            // Database migrations are applied once at startup in Program.cs (within a
            // dedicated scope), so we must not resolve the scoped GthxDataContext from the
            // root provider here.
            var gthx = _services.GetRequiredService<GthxBot>();

            while (!_ExitRequested)
            {
                Console.Write("gunnbr> ");
                try
                {
                    var input = Console.ReadLine();
                    if (input == null || input == "quit")
                    {
                        _ExitRequested = true;
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
                    _ExitRequested = true;
                }
            }
        }
    }
}