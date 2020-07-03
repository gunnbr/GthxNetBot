using System;
using System.Collections.Generic;
using System.Text;

namespace Gthx.Core.Interfaces
{
    /// <summary>
    /// Response types: Normal messages or actions
    /// </summary>
    public enum ResponseType
    {
        Normal,
        Action
    }

    /// <summary>
    /// Response to an incoming message
    /// </summary>
    public class IrcResponse
    {
        public IrcResponse(string message, ResponseType type = ResponseType.Normal)
        {
            Message = message;
            Type = type;
        }

        public ResponseType Type { get; set; }
        public string Message { get; set; }
    }

    interface IGthxModule
    {
        /// <summary>
        /// Evaluate a received message and return a response if the module
        /// is able to handle the message.
        /// </summary>
        /// <param name="channel">Channel the message was received on</param>
        /// <param name="user">Nick of the user who sent the message</param>
        /// <param name="message">Text of the message that was sent</param>
        /// <returns>IrcResponse to reply if the message was handled or null otherwise</returns>
        public IrcResponse ProcessMessage(string channel, string user, string message);
    }
}
