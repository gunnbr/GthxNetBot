using Gthx.Bot;
using Gthx.Bot.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Sinks.Email;
using System;
using System.Collections.Generic;

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
        static void Main(string[] args)
        {
            var useConsoleTestBot = Array.Exists(args, arg =>
                string.Equals(arg, "--console", StringComparison.OrdinalIgnoreCase));

            var host = Host.CreateDefaultBuilder(args)
                .UseSerilog((context, services, configuration) =>
                {
                    var emailOptions = new EmailOptions();
                    context.Configuration.GetSection(EmailOptions.EmailConfiguration).Bind(emailOptions);

                    configuration.ReadFrom.Configuration(context.Configuration);

                    if (!string.IsNullOrWhiteSpace(emailOptions.EmailSubject) &&
                        !string.IsNullOrWhiteSpace(emailOptions.FromName) &&
                        !string.IsNullOrWhiteSpace(emailOptions.MailServer) &&
                        !string.IsNullOrWhiteSpace(emailOptions.Password) &&
                        !string.IsNullOrWhiteSpace(emailOptions.ToEmail) &&
                        !string.IsNullOrWhiteSpace(emailOptions.UserName) &&
                        emailOptions.Port != null)
                    {
                        configuration.WriteTo.Email(
                            new EmailSinkOptions
                            {
                                From = emailOptions.FromName,
                                To = new List<string> { emailOptions.ToEmail }
                            }
                        );
                    }
                })
                .ConfigureServices((context, services) =>
                {
                    // Register DbContext with scoped lifetime
                    services.AddDbContext<GthxData.GthxDataContext>(options =>
                    {
                        var connectionString =
                            context.Configuration.GetConnectionString("GthxDb") ??
                            throw new InvalidOperationException("Missing connection string. Configure ConnectionStrings:GthxDb.");

                        var dbType = (context.Configuration.GetConnectionString("GthxDb_Type") ?? "sqlserver")
                            .Trim()
                            .ToLowerInvariant();

                        if (dbType is "mariadb" or "mysql")
                        {
                            options.UseMySql(
                                connectionString,
                                new MariaDbServerVersion(new Version(10, 3, 29)),
                                mySqlOptions => mySqlOptions.MigrationsAssembly("MariaDbMigrations"));
                        }
                        else if (dbType == "sqlserver")
                        {
                            options.UseSqlServer(
                                connectionString,
                                sqlOptions => sqlOptions.MigrationsAssembly("SqlServerMigrations"));
                        }
                        else
                        {
                            throw new InvalidOperationException("Invalid ConnectionStrings:GthxDb_Type. Supported values are 'sqlserver', 'mariadb', or 'mysql'.");
                        }
                    });

                    // Register IGthxData as scoped
                    services.AddScoped<Gthx.Data.IGthxData, Gthx.Data.GthxSqlData>();

                    // Register core bot services
                    services.AddSingleton<IBotNick, NickManager>();
                    services.AddSingleton<GthxMessageConduit>();
                    services.AddSingleton<IGthxMessageConduit>(provider => provider.GetRequiredService<GthxMessageConduit>());
                    services.AddSingleton<IGthxMessageConsumer>(provider => provider.GetRequiredService<GthxMessageConduit>());
                    services.AddSingleton<IWebReader, WebReader>();
                    services.AddSingleton<IGthxUtil, GthxUtil>();
                    services.AddSingleton<GthxBot>();
                    services.AddGthxBot();

                    // Register IRC client implementations
                    services.AddSingleton<ConsoleIrcClient>();
                    services.AddSingleton<GthxIrcClient>();
                    services.AddSingleton<IIrcClient>(provider =>
                        useConsoleTestBot
                            ? provider.GetRequiredService<ConsoleIrcClient>()
                            : provider.GetRequiredService<GthxIrcClient>());

                    // Register bot runners
                    services.AddSingleton<IrcBot>();
                    services.AddSingleton<ConsoleTestBot>();
                    services.AddSingleton<IBotRunner>(provider =>
                        useConsoleTestBot
                            ? provider.GetRequiredService<ConsoleTestBot>()
                            : provider.GetRequiredService<IrcBot>());
                })
                .Build();

            // Apply any pending EF Core migrations so a freshly provisioned database
            // (for example, the SQL Server container started by the Aspire AppHost) is schema-ready.
            using (var scope = host.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<GthxData.GthxDataContext>();
                dbContext.Database.Migrate();
            }

            try
            {
                var bot = host.Services.GetRequiredService<IBotRunner>();
                bot.Run();
            }
            finally
            {
                host.Dispose();
                Log.CloseAndFlush();
            }
        }
    }
}
