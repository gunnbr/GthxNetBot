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

        public TellModule(IGthxData data)
        {
            this._Data = data;
        }

        public List<IrcResponse> ProcessMessage(string channel, string user, string message)
        {
            var replies = new List<IrcResponse>();

            var waitingMessages = _Data.GetTell(user);
            foreach (var waitingMessage in waitingMessages)
            {
                Debug.WriteLine($"Found tell for '{user}' from '{waitingMessage.FromUser}");
                var timeSince = Util.TimeBetweenString(waitingMessage.TimeSet);
                var reply = new IrcResponse($"{user}: {timeSince} ago {waitingMessage.FromUser} tell {waitingMessage.ToUser} {waitingMessage.Message}", 
                    ResponseType.Normal, false);
                replies.Add(reply);
            }

            var tellMatch = _TellRegex.Match(message);
            if (!tellMatch.Success)
            {
                return replies;
            }

            var nick = tellMatch.Groups["nick"].Value;
            var tellMessage = tellMatch.Groups["message"].Value;

            var success = _Data.AddTell(user, nick, tellMessage);
            if (success)
            {
                replies.Add(new IrcResponse($"{user}: Okay."));
            }
            else
            {
                replies.Add(new IrcResponse($"I'm sorry, {user}. I'm afraid I can't do that."));
            }

            return replies;
        }
    }
}
