using System;
using System.Collections.Generic;
using System.Text;

namespace Gthx.Bot
{
    public enum GthxMessageType
    {
        Message,
        Action
    };

    public class GthxMessageProducedEventArgs
    {
        public string Channel { get; }
        public string FromUser { get; }
        public string Message { get; }
        public GthxMessageType Type { get; }

        public GthxMessageProducedEventArgs(string channel, string fromUser, string message, GthxMessageType type = GthxMessageType.Message)
        {
            Channel = channel;
            FromUser = fromUser;
            Message = message;
            Type = type;
        }
    }
}
