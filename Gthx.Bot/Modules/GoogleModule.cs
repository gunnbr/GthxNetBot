using Gthx.Bot.Interfaces;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Web;

namespace Gthx.Bot.Modules
{
    public class GoogleModule : IGthxModule
    {
        private readonly IIrcClient _IrcClient;

        private readonly Regex _GoogleRegex = new Regex(@$"\s*google\s+(?'search'.*?)\s+for\s+(?'nick'{Util.NickMatch})");

        public GoogleModule(IIrcClient ircClient)
        {
            _IrcClient = ircClient;
        }

        public bool ProcessAction(string channel, string user, string message)
        {
            return false;
        }

        public bool ProcessMessage(string channel, string user, string message, bool wasDirectlyAddressed)
        {
            if (!wasDirectlyAddressed)
            {
                // Only handle Google requests if the message was directly addressed to us.
                return false;
            }

            var googleMatch = _GoogleRegex.Match(message);
            if (!googleMatch.Success)
            {
                return false;
            }

            var nick = googleMatch.Groups["nick"].Value;
            var search = googleMatch.Groups["search"].Value;
            search = HttpUtility.UrlEncode(search);
            Debug.WriteLine($"{channel}:{user} asked to google '{search}' for {nick}");
            _IrcClient.SendMessage(channel,$"{nick}: http://lmgtfy.com/?q={search}");
            return true;
        }
    }
}
