using Gthx.Bot.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Gthx.Bot
{
    public class GthxBot
    {
        public static readonly string Version = "2.24 2022-05-18";

        private readonly IBotNick _botNick;
        private readonly ILogger<GthxBot> _logger;
        private readonly IGthxMessageConsumer _messageReader;
        private readonly IServiceScopeFactory _scopeFactory;
        private Regex _matchNick;
        
        public GthxBot(IBotNick botNick, ILogger<GthxBot> logger, IGthxMessageConsumer messageReader, IServiceScopeFactory scopeFactory) 
        {
            _botNick = botNick;
            _logger = logger;
            _messageReader = messageReader;
            _scopeFactory = scopeFactory;

            _logger.LogDebug($"GthxBot instantiated");

            _botNick.BotNickChangedEvent += BotNick_NickChangedEvent;
            _matchNick = new Regex($@"^{_botNick.BotNick}(:|;|,|-|\s)+(?'message'.+)");

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
            if (e.Message.Any(c => c > 127))
            {
                var sb = new StringBuilder();
                sb.Append("Non-ASCII characters: ");
                int i = 0;
                foreach (var c in e.Message)
                {
                    if (c > 127)
                    {
                        string hex = ((int)c).ToString("X4");
                        sb.Append($"{i}: 0x{hex} ");
                    }
                    i++;
                }

                _logger.LogDebug(sb.ToString());
            }

            try
            {
                if (e.Type == GthxMessageType.Message)
                {
                    HandleReceivedMessage(e.Channel, e.FromUser, e.Message);
                }
                else
                {
                    HandleReceivedAction(e.Channel, e.FromUser, e.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failure while processing '{message}' from {user} in {channel}", e.Message, e.FromUser, e.Channel);
            }
        }

        private void BotNick_NickChangedEvent(object? sender, System.EventArgs e)
        {
            _logger.LogInformation("Bot nick changed to {nick}", _botNick.BotNick);
            _matchNick = new Regex($@"^{_botNick.BotNick}(:|;|,|-|\s)+(?'message'.+)");
        }

        public void HandleReceivedMessage(string channel, string user, string message)
        {
            var wasDirectlyAddressed = false;

            if (!channel.StartsWith('#') && channel == user)
            {
                // Not sent to a real channel and the channel and user are the same, so this must be a PM
                // TODO: See if there's a better/more reliable way to determine this.
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

            using IServiceScope messageScope = _scopeFactory.CreateScope();

            var modules = messageScope.ServiceProvider.GetRequiredService<IEnumerable<IGthxModule>>();

            foreach (var module in modules)
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
            using IServiceScope messageScope = _scopeFactory.CreateScope();

            var modules = messageScope.ServiceProvider.GetRequiredService<IEnumerable<IGthxModule>>();

            foreach (var module in modules)
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
