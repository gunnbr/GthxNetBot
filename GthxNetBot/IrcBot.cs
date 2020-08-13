using System;
using System.Collections.Generic;
using System.Text;
using Gthx.Bot;
using Gthx.Bot.Interfaces;
using GthxData;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GthxNetBot
{
    class IrcBot : IBotRunner
    {
        private readonly IIrcClient _ircClient;
        private readonly ILogger<IrcBot> _logger;
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _services;

        public IrcBot(IIrcClient ircClient, ILogger<IrcBot> logger, IConfiguration configuration, IServiceProvider services)
        {
            _logger = logger;
            _ircClient = ircClient;
            _configuration = configuration;
            _services = services;
            Console.WriteLine("IrcBot constructed");
            _logger.LogInformation("IrcBot: Logging enabled");
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
                Console.Write("command> ");
                try
                {
                    var input = Console.ReadLine();
                    if (input == null || input == "quit")
                    {
                        done = true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception in IrcBot program");
                    done = true;
                }
            }
        }
    }
}
