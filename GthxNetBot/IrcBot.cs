using Gthx.Bot;
using Gthx.Bot.Interfaces;
using GthxData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading;

namespace GthxNetBot
{
    class IrcBot : IBotRunner
    {
        private readonly IIrcClient _ircClient;
        private readonly ILogger<IrcBot> _logger;
        private readonly IServiceProvider _services;

        public IrcBot(IIrcClient ircClient, ILogger<IrcBot> logger, IServiceProvider services)
        {
            _logger = logger;
            _ircClient = ircClient;
            _services = services;
        }

        // TODO: Refactor this to work like the unit tests now that I know more about
        //       how to make the DI work.
        public void Run()
        {
            Console.WriteLine("Welcome to Gthx");
            
            // Just to get some output from Azure
            Trace.TraceError("Gthx running");

            _logger.LogInformation($"irc client is {_ircClient}");

            var context = _services.GetRequiredService<GthxDataContext>();
            RelationalDatabaseFacadeExtensions.Migrate(context.Database);
            var gthx = _services.GetRequiredService<GthxBot>();

            var done = false;
            while (!done)
            {
#if DEBUG
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
#else
                Thread.Sleep(5000);
#endif
            }
        }
    }
}