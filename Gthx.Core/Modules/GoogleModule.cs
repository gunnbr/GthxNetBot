﻿using Gthx.Core.Interfaces;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Web;

namespace Gthx.Core.Modules
{
    public class GoogleModule : IGthxModule
    {
        private IIrcClient _IrcClient;

        private readonly Regex _GoogleRegex = new Regex(@$"\s*google\s+(?'search'.*?)\s+for\s+(?'nick'{Util.NickMatch})");

        public GoogleModule(IIrcClient ircClient)
        {
            _IrcClient = ircClient;
        }

        public void ProcessMessage(string channel, string user, string message)
        {
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
