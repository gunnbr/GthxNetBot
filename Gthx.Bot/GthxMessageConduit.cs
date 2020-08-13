using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks.Dataflow;
using Gthx.Bot.Interfaces;
using Microsoft.Extensions.Logging;

namespace Gthx.Bot
{
    public class GthxMessageConduit : IGthxMessageConduit, IGthxMessageConsumer
    {
        private readonly ILogger<GthxMessageConduit> _logger;
        private readonly ActionBlock<GthxMessageProducedEventArgs> _queue;

        public GthxMessageConduit(ILogger<GthxMessageConduit> logger)
        {
            _logger = logger;

            _queue = new ActionBlock<GthxMessageProducedEventArgs>(
                x => MessageProducedHandler?.Invoke(this, x));
        }

        /// <summary>
        /// Receive message from the server to send to GTHX for processing
        /// </summary>
        /// <param name="channel">Channel the message was received on</param>
        /// <param name="fromUser">Nickname of the user the message was from</param>
        /// <param name="message">Text of the message</param>
        public void ReceiveMessage(string channel, string fromUser, string message)
        {
            var newMessage = new GthxMessageProducedEventArgs(channel, fromUser, message);
            var success = _queue.Post(newMessage);
            if (!success)
            {
                _logger.LogError("Failed to add message to the queue!");
            }
        }

        public void ReceiveAction(string channel, string fromUser, string action)
        {
            var newMessage = new GthxMessageProducedEventArgs(channel, fromUser, action, GthxMessageType.Action);
            var success = _queue.Post(newMessage);
            if (!success)
            {
                _logger.LogError("Failed to add message to the queue!");
            }
        }

        public EventHandler<GthxMessageProducedEventArgs> MessageProducedHandler { get; set; }
    }
}
