using Gthx.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Gthx.Core.Modules
{
    public class TellModule : IGthxModule
    {
        private readonly Regex _TellRegex = new Regex(@$"\s*tell\s+(?'nick'{Util.NickMatch})\s*(?'message'.+)");
        private readonly IGthxData _Data;
        private readonly IIrcClient _IrcClient;

        public TellModule(IGthxData data, IIrcClient ircClient)
        {
            this._Data = data;
            this._IrcClient = ircClient;
        }

        public void ProcessMessage(string channel, string user, string message)
        {
            var waitingMessages = _Data.GetTell(user);
            foreach (var waitingMessage in waitingMessages)
            {
                Debug.WriteLine($"Found tell for '{user}' from '{waitingMessage.FromUser}");
                var timeSince = Util.TimeBetweenString(waitingMessage.TimeSet);
                var reply = $"{user}: {timeSince} ago {waitingMessage.FromUser} tell {waitingMessage.ToUser} {waitingMessage.Message}";
                _IrcClient.SendMessage(channel, reply);
            }

            var tellMatch = _TellRegex.Match(message);
            if (!tellMatch.Success)
            {
                return;
            }

            var nick = tellMatch.Groups["nick"].Value;
            var tellMessage = tellMatch.Groups["message"].Value;

            var success = _Data.AddTell(user, nick, tellMessage);
            if (success)
            {
                _IrcClient.SendMessage(channel, $"{user}: Okay.");
            }
            else
            {
                _IrcClient.SendMessage(channel, $"I'm sorry, {user}. I'm afraid I can't do that.");
            }
        }
    }
}
