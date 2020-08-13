﻿using Gthx.Bot;
using Gthx.Bot.Interfaces;
using Gthx.Data;
using GthxData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GthxNetBot
{
    class Program
    {
        private static ServiceProvider? _serviceProvider;
        private static IConfiguration? _configuration;

        static void Main(string[] args)
        {
            // From https://docs.microsoft.com/en-us/azure/app-service/troubleshoot-diagnostic-logs, 
            // this should display in the log.
            System.Diagnostics.Trace.TraceError("GthxNetBot.Main is running!");

            _configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(_configuration)
                .CreateLogger();

            _serviceProvider = RegisterServices();

            Log.Information("gthx running with: {args}", args);

            var scope = _serviceProvider.CreateScope();
            var myBot = scope.ServiceProvider.GetRequiredService<IBotRunner>();
            myBot.Run();
            DisposeServices();
        }
        
        public static readonly ILoggerFactory ConsoleLoggerFactory
             = LoggerFactory.Create(builder =>
             {
                 builder.AddFilter((category, level) =>
                   category == DbLoggerCategory.Database.Command.Name
                   && level == LogLevel.Information)
               .AddConsole();
             });

        private static ServiceProvider RegisterServices()
        {
            var services = new ServiceCollection();
            // Note: .AddConsole() here also logs SQL statements into the console, even if the
            //       LoggerFactory above isn't used.
            // TODO: Add something to filter out those and only display warning or above in the console.
            services.AddLogging(configure => configure.AddSerilog()).AddTransient<ConsoleTestBot>();
            services.TryAddSingleton<IGthxUtil, GthxUtil>();
            services.TryAddSingleton<IGthxData, GthxSqlData>();
            services.TryAddSingleton<IWebReader, WebReader>();
            services.TryAddSingleton<IBotNick, NickManager>();
            services.TryAddSingleton(_configuration);
            services.TryAddSingleton<GthxMessageConduit>();
            services.TryAddSingleton<IGthxMessageConduit>(s => s.GetRequiredService<GthxMessageConduit>());
            services.TryAddSingleton<IGthxMessageConsumer>(s => s.GetRequiredService<GthxMessageConduit>());
            services.AddDbContext<GthxDataContext>(options =>
            {
                options.UseSqlServer(_configuration.GetConnectionString("GthxDb"));//.UseLoggerFactory(ConsoleLoggerFactory);
            }, ServiceLifetime.Singleton);
            services.AddGthxBot();
            services.TryAddSingleton<GthxBot>();

#if false
            // Use console test bot
            services.TryAddSingleton<IIrcClient, ConsoleIrcClient>();
            services.TryAddSingleton<IBotRunner, ConsoleTestBot>();
#else
            services.TryAddSingleton<IIrcClient, GthxIrcClient>();
            services.AddSingleton<IBotRunner, IrcBot>();
#endif

            return services.BuildServiceProvider(true);
        }

        private static void DisposeServices()
        {
            if (_serviceProvider == null)
            {
                return;
            }

            if (_serviceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
