using Gthx.Core.Interfaces;
using System;
using System.Collections.Generic;
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

        public IrcResponse ProcessMessage(string channel, string user, string message)
        {
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
                return new IrcResponse($"{user}: Okay.");
            }

            return new IrcResponse($"I'm sorry, {user}. I'm afraid I can't do that.");
        }
    }
}
