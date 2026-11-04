using Gthx.Bot.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Gthx.Test.Mocks
{
    public class MockEmailSender : IEmailSender
    {
        public List<(string Subject, string Body)> Sent { get; } = new();
        public bool Fail { get; set; }

        public bool IsEnabled => true;

        public Task SendAsync(string subject, string body)
        {
            if (Fail)
            {
                throw new InvalidOperationException("The mail server is down");
            }

            Sent.Add((subject, body));
            return Task.CompletedTask;
        }
    }
}
