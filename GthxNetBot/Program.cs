using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Sinks.Email;
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
                    // Register your services here
                    services.AddTransient<IBotRunner, IrcBot>();
                    // Add other services as needed
                })
                .Build();

            var bot = host.Services.GetRequiredService<IBotRunner>();
            bot.Run();
        }
    }
}
