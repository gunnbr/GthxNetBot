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

        public void ProcessAction(string channel, string user, string message)
        {
        }

        public void ProcessMessage(string channel, string user, string message)
        {
            // TODO: Only handle this if the message was directly addressed to us.
            var googleMatch = _GoogleRegex.Match(message);
            if (!googleMatch.Success)
            {
                return;
            }

            var nick = googleMatch.Groups["nick"].Value;
            var search = googleMatch.Groups["search"].Value;
            search = HttpUtility.UrlEncode(search);
            Debug.WriteLine($"{channel}:{user} asked to google '{search}' for {nick}");
            _IrcClient.SendMessage(channel,$"{nick}: http://lmgtfy.com/?q={search}");
        }
    }
}
