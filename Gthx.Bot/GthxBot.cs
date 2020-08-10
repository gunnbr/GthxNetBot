using Gthx.Bot.Interfaces;
using Gthx.Bot.Modules;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Gthx.Bot
{
    public class GthxBot
    {
        public static readonly string Version = "0.8 2020-07-24";

        private readonly List<IGthxModule> _Modules;
        private readonly IBotNick _botNick;
        private readonly ILogger<GthxBot> _logger;
        private Regex _matchNick;
        
        public GthxBot(IEnumerable<IGthxModule> modules, IBotNick botNick, ILogger<GthxBot> logger)
        {
            _Modules = modules.ToList();
            _botNick = botNick;
            _logger = logger;

            _botNick.BotNickChangedEvent += BotNick_NickChangedEvent;
            _matchNick = new Regex($@"{_botNick.BotNick}(:|;|,|-|\s)+(?'message'.+)");
        }

        private void BotNick_NickChangedEvent(object sender, System.EventArgs e)
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
