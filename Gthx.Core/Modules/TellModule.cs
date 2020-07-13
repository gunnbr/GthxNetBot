using Gthx.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace Gthx.Core.Modules
{
    public class TellModule : IGthxModule
    {
        private readonly Regex _TellRegex = new Regex(@"\s*tell\s+(?'nick'[a-zA-Z\*_\\\[\]\{\}^`|\*][a-zA-Z0-9\*_\\\[\]\{\}^`|-]*)\s*(?'message'.+)");
        private readonly IGthxData _Data;

        public TellModule(IGthxData data)
        {
            this._Data = data;
        }

        public List<IrcResponse> ProcessMessage(string channel, string user, string message)
        {
            var waitingMessages = _Data.GetTell(user);
            foreach (var waitingMessage in waitingMessages)
            {
                Debug.WriteLine($"Found tell for '{user}' from '{waitingMessage.FromUser}");
                // TODO: Implement timeSinceString()
                var timeSince = DateTime.UtcNow - waitingMessage.TimeSet;
                // TODO: Fix this cause it's not the proper logic
                return new List<IrcResponse>
                {
                    new IrcResponse($"{user}: {timeSince} ago {waitingMessage.FromUser} tell {waitingMessage.ToUser} {waitingMessage.Message}")
                };
            }

            var tellMatch = _TellRegex.Match(message);
            if (!tellMatch.Success)
            {
                return null;
            }

            var nick = tellMatch.Groups["nick"].Value;
            var tellMessage = tellMatch.Groups["message"].Value;

            var success = _Data.AddTell(user, nick, tellMessage);
            if (success)
            {
                return new List<IrcResponse>
                { 
                    new IrcResponse($"{user}: Okay.")
                };
            }

            return new List<IrcResponse>
            {
                new IrcResponse($"I'm sorry, {user}. I'm afraid I can't do that.")
            };
        }
    }
}
