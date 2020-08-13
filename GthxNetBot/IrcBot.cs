using Gthx.Bot;
using Gthx.Bot.Interfaces;
using GthxData;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

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
        }

        // TODO: Refactor this to work like the unit tests now that I know more about
        //       how to make the DI work.
        public void Run()
        {
            Debug.WriteLine("Welcome to Gthx");
            _logger.LogInformation($"irc client is {_ircClient}");

            var context = _services.GetRequiredService<GthxDataContext>();
            context.Database.EnsureCreated();
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
                WaitAWhile();
#endif
            }
        }

        private async Task WaitAWhile()
        {
            await Task.Delay(5000);
        }
    }
}
