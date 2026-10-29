using Gthx.Bot.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System;
using System.Threading.Tasks;

namespace GthxNetBot
{
    /// <summary>
    /// Sends email with the same server and addresses as the error emails (<see cref="EmailOptions"/>)
    /// </summary>
    public class SmtpEmailSender : IEmailSender
    {
        private readonly string _from;
        private readonly string _to;
        private readonly string _server;
        private readonly int _port;
        private readonly string _userName;
        private readonly string _password;

        public SmtpEmailSender(EmailOptions options)
        {
            if (!options.IsComplete)
            {
                throw new ArgumentException("Email isn't fully set up.", nameof(options));
            }

            _from = options.FromName;
            _to = options.ToEmail;
            _server = options.MailServer;
            _port = options.Port.Value;
            _userName = options.UserName;
            _password = options.Password;
        }

        public bool IsEnabled => true;

        public async Task SendAsync(string subject, string body)
        {
            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(_from));
            message.To.Add(MailboxAddress.Parse(_to));
            message.Subject = subject;
            message.Body = new TextPart("plain") { Text = body };

            using var client = new SmtpClient();
            await client.ConnectAsync(_server, _port, SecureSocketOptions.Auto);
            await client.AuthenticateAsync(_userName, _password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}
