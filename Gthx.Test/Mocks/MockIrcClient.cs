using Gthx.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Gthx.Test.Mocks
{
    public class MockIrcClient : IIrcClient
    {
        public string SentToChannel { get; set; }

        public string SentAction { get; set; }

        public string SentMessage { get; set; }

        public bool SendAction(string channel, string action)
        {
            SentToChannel = channel;
            SentAction = action;
            return true;
        }

        public bool SendMessage(string channel, string message)
        {
            SentToChannel = channel;
            SentMessage = message;
            return true;
        }
    }
}
