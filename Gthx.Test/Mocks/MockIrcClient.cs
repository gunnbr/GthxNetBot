using Gthx.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Gthx.Test.Mocks
{
    public class MockIrcClient : IIrcClient
    {
        public string SentToChannel { get; set; }
        public string SentMessage { get; set; }

        public bool SendAction(string channel, string message)
        {
            throw new NotImplementedException();
        }

        public bool SendMessage(string channel, string message)
        {
            SentToChannel = channel;
            SentMessage = message;
            return true;
        }
    }
}
