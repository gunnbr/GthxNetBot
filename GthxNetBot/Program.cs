using Gthx.Bot;
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
        private static ServiceProvider _serviceProvider;
        private static IConfiguration _configuration;

        static void Main(string[] args)
        {
            Console.WriteLine("In Main...");
            _configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            Console.WriteLine("Done reading configuration");

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(_configuration)
                .CreateLogger();

            Console.WriteLine("Logger configured.");

            RegisterServices();

            Console.WriteLine("Services registered");

            Log.Information("Serilog enabled with args: {args}", args);
            Log.Warning("Emoji text: 🍕🍛👧🧑🏼🎎");

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

        private static void RegisterServices()
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

            _serviceProvider = services.BuildServiceProvider(true);
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
