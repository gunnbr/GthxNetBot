using Gthx.Bot.Interfaces;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Gthx.Bot
{
    public class GthxBot
    {
        public static readonly string Version = "2.01 2020-09-06";

        private readonly List<IGthxModule> _Modules;
        private readonly IBotNick _botNick;
        private readonly ILogger<GthxBot> _logger;
        private readonly IGthxMessageConsumer _messageReader;

        private Regex _matchNick;
        
        public GthxBot(IEnumerable<IGthxModule> modules, IBotNick botNick, ILogger<GthxBot> logger, IGthxMessageConsumer messageReader) 
        {
            _botNick = botNick;
            _logger = logger;
            _messageReader = messageReader;
            
            _logger.LogDebug("GthxBot instantiated");

            _Modules = modules.ToList();

            _botNick.BotNickChangedEvent += BotNick_NickChangedEvent;
            _matchNick = new Regex($@"{_botNick.BotNick}(:|;|,|-|\s)+(?'message'.+)");

            _messageReader.MessageProducedHandler += HandleIncomingMessage;
        }

        /// <summary>
        /// Handle messages coming in from the IRC server
        /// </summary>
        /// <param name="sender">unused</param>
        /// <param name="e">Information about the incoming message</param>
        private void HandleIncomingMessage(object? sender, GthxMessageProducedEventArgs e)
        {
            _logger.LogDebug("Gthx: Incoming {Type}: {Message}", e.Type, e.Message);
            if (e.Type == GthxMessageType.Message)
            {
                HandleReceivedMessage(e.Channel, e.FromUser, e.Message);
            }
            else
            {
                HandleReceivedAction(e.Channel, e.FromUser, e.Message);
            }
        }

        private void BotNick_NickChangedEvent(object? sender, System.EventArgs e)
        {
            _logger.LogInformation("Bot nick changed to {nick}", _botNick.BotNick);
            _matchNick = new Regex($@"{_botNick.BotNick}(:|;|,|-|\s)+(?'message'.+)");
        }

        public void HandleReceivedMessage(string channel, string user, string message)
        {
            var wasDirectlyAddressed = false;

            if (channel == _botNick.BotNick)
            {
                wasDirectlyAddressed = true;
            }
            else
            {
                var nickMatch = _matchNick.Match(message);
                if (nickMatch.Success)
                {
                    wasDirectlyAddressed = true;
                    message = nickMatch.Groups["message"].Value;
                }
            }

            foreach (var module in _Modules)
            {
                var isDone = module.ProcessMessage(channel, user, message, wasDirectlyAddressed);
                if (isDone)
                {
                    break;
                }
            }
        }

        public void HandleReceivedAction(string channel, string user, string action)
        {
            foreach (var module in _Modules)
            {
                var isDone = module.ProcessAction(channel, user, action);
                if (isDone)
                {
                    break;
                }
            }
        }
    }
}
