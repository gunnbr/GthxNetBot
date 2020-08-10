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
        private readonly IGthxData _Data;
        private readonly IIrcClient _IrcClient;
        private readonly ILogger<SeenModule> _Logger;
        private readonly Regex _SeenRegex = new Regex(@$"\s*seen\s+(?'nick'{Util.NickMatch})[\s\?]*");

        public SeenModule(IGthxData data, IIrcClient ircClient, ILogger<SeenModule> logger)
        {
            _Data = data;
            _IrcClient = ircClient;
            _Logger = logger;
        }

        public bool ProcessMessage(string channel, string user, string message, bool wasDirectlyAddressed)
        {
            // Update the seen database, but only if it's not a private message
            if (channel.StartsWith("#")) 
            {
                _Data.UpdateLastSeen(channel, user, message);
            }

            var seenMatch = _SeenRegex.Match(message);
            if (!seenMatch.Success)
            {
                return false;
            }

            var nick = seenMatch.Groups["nick"].Value;
            _Logger.LogInformation("{user} asked about '{nick}'", user, nick);
            var seenList = _Data.GetLastSeen(nick);
            if (seenList == null)
            {
                _IrcClient.SendMessage(channel, $"Sorry, I haven't seen {nick}.");
                return true;
            }

            foreach (var info in seenList.Take(3))
            {
                var timeSince = Util.TimeBetweenString(info.Timestamp);
                _IrcClient.SendMessage(channel, $"{info.User} was last seen in {info.Channel} {timeSince} ago saying '{info.Message}'.");
            }

            return true;
        }

        public bool ProcessAction(string channel, string user, string message)
        {
            // Update the seen database, but only if it's not a private message
            if (channel.StartsWith("#"))
            {
                _Data.UpdateLastSeen(channel, user, $"* {user} {message}");
            }

            return false;
        }
    }
}
