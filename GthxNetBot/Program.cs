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
using System.Net;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Serilog.Sinks.Email;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace GthxNetBot
{
    public class EmailOptions
    {
        public const string EmailConfiguration = "EmailConfiguration";

        public string? FromName { get; set; }
        public string? ToEmail { get; set; }
        public string? EmailSubject { get; set; }
        public string? UserName { get; set; }
        public string? Password { get; set; }
        public string? MailServer { get; set; }
        public int? Port { get; set; }
    }

    class Program
    {
        private static ServiceProvider? _serviceProvider;
        private static IConfiguration _configuration;

        static void Main(string[] args)
        { 
            // From https://docs.microsoft.com/en-us/azure/app-service/troubleshoot-diagnostic-logs, 
            // this should display in the log.
            System.Diagnostics.Trace.TraceError("GthxNetBot.Main is running!");

            _configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            var loggerConfig = new LoggerConfiguration()
                .ReadFrom.Configuration(_configuration);

            Serilog.Core.Logger logger;

            var emailOptions = new EmailOptions();
            _configuration.GetSection(EmailOptions.EmailConfiguration).Bind(emailOptions);
            if (string.IsNullOrWhiteSpace(emailOptions.EmailSubject) ||
                string.IsNullOrWhiteSpace(emailOptions.FromName) ||
                string.IsNullOrWhiteSpace(emailOptions.MailServer) ||
                string.IsNullOrWhiteSpace(emailOptions.Password) ||
                string.IsNullOrWhiteSpace(emailOptions.ToEmail) ||
                string.IsNullOrWhiteSpace(emailOptions.UserName) ||
                emailOptions.Port == null)
            {
                logger = loggerConfig.CreateLogger();
                logger.Warning("Email logging not configured");
            }
            else
            {
                loggerConfig = loggerConfig.WriteTo.Email(new EmailConnectionInfo
                    {
                        FromEmail = emailOptions.FromName,
                        ToEmail = emailOptions.ToEmail,
                        EmailSubject = emailOptions.EmailSubject,
                        MailServer = emailOptions.MailServer,
                        Port = emailOptions.Port.Value,
                        EnableSsl = true,
                        NetworkCredentials = new NetworkCredential
                        {
                            UserName = emailOptions.UserName,
                            Password = emailOptions.Password
                        },
                    },
                    "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level}] {Message}{NewLine}{Exception}",
                    batchPostingLimit: 20,
                    restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Warning);
                 logger = loggerConfig.CreateLogger();
            }

            Log.Logger = logger;
            try
            {
                _serviceProvider = RegisterServices();

                Log.Information("gthx running with: {args}", args);

                var scope = _serviceProvider.CreateScope();
                var myBot = scope.ServiceProvider.GetRequiredService<IBotRunner>();
                myBot.Run();
                DisposeServices();
            }
            finally
            {
                Log.Error("GthxNetBot exiting.");
                logger.Dispose();
            }
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

            var useMariaDb = false;
            var dbType = _configuration.GetConnectionString("GthxDb_Type");
            if (dbType == "mariadb")
            {
                Log.Information("Using MariaDB mode");
                useMariaDb = true;
            }
            else
            {
                Log.Information("Using SQL Server mode");
            }
            services.AddDbContext<GthxDataContext>(options => _ = useMariaDb switch
            {
                true => options.UseMySql(_configuration.GetConnectionString("GthxDb"), new MariaDbServerVersion(new Version(10, 3, 29)), x => x.MigrationsAssembly("MariaDbMigrations")),
                false => options.UseSqlServer(_configuration.GetConnectionString("GthxDb"), x => x.MigrationsAssembly("SqlServerMigrations")), //.UseLoggerFactory(ConsoleLoggerFactory);,,
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

        // EF Core uses this method at design time to access the DbContext
        public static IHostBuilder CreateHostBuilder(string[] args)
            => Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(
                    webBuilder => webBuilder.UseStartup<Startup>());
    }

    public class Startup
    {
        private IConfiguration _configuration;

        public Startup()
        {
            _configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();
        }

        public void ConfigureServices(IServiceCollection services)
            => services.AddDbContext<GthxDataContext>(options =>
                options.UseMySql(_configuration.GetConnectionString("GthxDb"),
                                 new MariaDbServerVersion(new Version(10, 3, 29)), x => x.MigrationsAssembly("MariaDbMigrations.Migrations")));

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
        }
    }
}
