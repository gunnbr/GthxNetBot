using Gthx.Bot.Interfaces;
using Gthx.Data;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Gthx.Bot.Modules
{
    public class SeenModule : IGthxModule
    {
        private readonly IGthxData _data;
        private readonly IIrcClient _client;
        private readonly IGthxUtil _util;
        private readonly ILogger<SeenModule> _logger;
        private readonly Regex _seenRegex = new Regex(@$"^\s*seen\s+(?'nick'{GthxUtil.NickMatch})[\s\?]*");

        public SeenModule(IGthxData data, IIrcClient ircClient, IGthxUtil util, ILogger<SeenModule> logger)
        {
            _data = data;
            _client = ircClient;
            _util = util;
            _logger = logger;
        }

        public bool ProcessMessage(string channel, string user, string message, bool wasDirectlyAddressed)
        {
            // Update the seen database, but only if it's not a private message
            if (channel.StartsWith("#")) 
            {
                _data.UpdateLastSeen(channel, user, message);
            }

            var seenMatch = _seenRegex.Match(message);
            if (!seenMatch.Success)
            {
                return false;
            }

            var nick = seenMatch.Groups["nick"].Value;
            _logger.LogInformation("{user} asked about '{nick}'", user, nick);
            var seenList = _data.GetLastSeen(nick);
            if (seenList == null)
            {
                _client.SendMessage(channel, $"Sorry, I haven't seen {nick}.");
                return true;
            }

            foreach (var info in seenList.Take(3))
            {
                var timeSince = _util.TimeBetweenString(info.Timestamp);
                _client.SendMessage(channel, $"{info.User} was last seen in {info.Channel} {timeSince} ago saying '{info.Message}'.");
            }

            return true;
        }

        public bool ProcessAction(string channel, string user, string message)
        {
            // Update the seen database, but only if it's not a private message
            if (channel.StartsWith("#"))
            {
                _data.UpdateLastSeen(channel, user, $"* {user} {message}");
            }

            return false;
        }
    }
}
