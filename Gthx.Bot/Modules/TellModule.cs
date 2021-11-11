using Gthx.Bot.Interfaces;
using Gthx.Data;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace Gthx.Bot.Modules
{
    public class TellModule : IGthxModule
    {
        private readonly Regex _tellRegex = new Regex(@$"^\s*tell\s+(?'nick'{GthxUtil.NickMatch})\s*(?'message'.+)");
        private readonly IGthxData _data;
        private readonly IIrcClient _client;
        private readonly IGthxUtil _util;
        private readonly ILogger<TellModule> _logger;

        public TellModule(IGthxData data, IIrcClient ircClient, IGthxUtil util, ILogger<TellModule> logger)
        {
            this._data = data;
            this._client = ircClient;
            _util = util;
            _logger = logger;
        }

        public bool ProcessAction(string channel, string user, string message)
        {
            return false;
        }

        public bool ProcessMessage(string channel, string user, string message, bool wasDirectlyAddressed)
        {
            var waitingMessages = _data.GetTell(user);
            foreach (var waitingMessage in waitingMessages)
            {
                _logger.LogInformation($"Found tell for '{user}' from '{waitingMessage.Author}'");
                var timeSince = _util.TimeBetweenString(waitingMessage.Timestamp);
                var reply = $"{user}: {timeSince} ago {waitingMessage.Author} tell {waitingMessage.Recipient} {waitingMessage.Message}";
                _client.SendMessage(channel, reply);
            }

            if (!wasDirectlyAddressed)
            {
                return false;
            }

            var tellMatch = _tellRegex.Match(message);
            if (!tellMatch.Success)
            {
                return false;
            }

            var nick = tellMatch.Groups["nick"].Value;
            var tellMessage = tellMatch.Groups["message"].Value;
            
            // TODO: Add a method to EXACTLY match the specified user.
            // This wildcard match works good enough for now and better than no check.
            var lastSeen = _data.GetLastSeen(nick);

            var success = _data.AddTell(user, nick, tellMessage);
            if (success)
            {
                if (lastSeen == null)
                {
                    _client.SendMessage(channel, $"{user}: I'll pass that on when {nick} is around, but I've never seen them before.");
                    // Logged as an error to send email so I can go clean this out of the DB if necessary
                    _logger.LogError($"Message to unseen user '{nick}' from '{user}");
                }
                else
                {
                    _client.SendMessage(channel, $"{user}: I'll pass that on when {nick} is around.");
                }
            }
            else
            {
                _client.SendMessage(channel, $"I'm sorry, {user}. I'm afraid I can't do that.");
            }

            return true;
        }
    }
}
