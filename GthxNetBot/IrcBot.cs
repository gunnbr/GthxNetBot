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
        private readonly ILogger<IrcBot> _logger;
        private readonly IServiceProvider _services;
        private readonly SemaphoreSlim _exitSemaphore;

        public IrcBot(ILogger<IrcBot> logger, IServiceProvider services)
        {
            _logger = logger;
            _services = services;
            _exitSemaphore = new SemaphoreSlim(0);
        }

        // TODO: Refactor this to work like the unit tests now that I know more about
        //       how to make the DI work.
        public void Run()
        {
            Console.WriteLine("Welcome to Gthx");
            
            // Just to get some output from Azure
            Trace.TraceError("Gthx running");

            var context = _services.GetRequiredService<GthxDataContext>();
            _logger.LogInformation("Running migrations on the database");
            RelationalDatabaseFacadeExtensions.Migrate(context.Database);
            _logger.LogInformation("Database migration complete.");

            var gthx = _services.GetRequiredService<GthxBot>();

            _exitSemaphore.Wait();
        }

        /// <summary>
        /// Exit and allow the bot to close
        /// </summary>
        public void Exit()
        {
            _exitSemaphore.Release();
        }
    }
}