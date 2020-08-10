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
    public class ConsoleTestBot
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

    class Program
    {
        private static ServiceProvider _serviceProvider;
        private static IConfiguration _configuration;

        static void Main(string[] args)
        {
            _configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(_configuration)
                .CreateLogger();

            RegisterServices();

            Log.Information("Serilog enabled: {args}", args);
            Log.Warning("Emoji text: 🍕🍛👧🧑🏼🎎");

            IServiceScope scope = _serviceProvider.CreateScope();
            scope.ServiceProvider.GetRequiredService<ConsoleTestBot>().Run();
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
            services.TryAddSingleton<ConsoleIrcClient>();
            services.AddSingleton<IIrcClient>(sp => sp.GetRequiredService<ConsoleIrcClient>());
            services.AddSingleton<IBotNick>(sp => sp.GetRequiredService<ConsoleIrcClient>());
            services.AddSingleton<IGthxData, GthxSqlData>();
            services.AddSingleton<IWebReader, WebReader>();
            services.AddSingleton(_configuration);
            services.AddDbContext<GthxDataContext>(options =>
            {
                options.UseSqlServer(_configuration.GetConnectionString("GthxDb"));//.UseLoggerFactory(ConsoleLoggerFactory);
            }, ServiceLifetime.Singleton);
            services.AddSingleton<ConsoleTestBot>();
            services.AddGthxBot();
            services.AddSingleton<GthxBot>();
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
