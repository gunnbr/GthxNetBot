using Gthx.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Gthx.Core.Modules
{
    public class SeenModule : IGthxModule
    {
        private readonly IGthxData _Data;
        private readonly IIrcClient _IrcClient;

        private readonly Regex _SeenRegex = new Regex(@$"\s*seen\s+(?'nick'{Util.NickMatch})[\s\?]*");

        public SeenModule(IGthxData data, IIrcClient ircClient)
        {
            this._Data = data;
            this._IrcClient = ircClient;
        }

        public void ProcessMessage(string channel, string user, string message)
        {
            // Update the seen database, but only if it's not a private message
            if (channel.StartsWith("#")) 
            {
                _Data.UpdateLastSeen(channel, user, message);
            }

            var seenMatch = _SeenRegex.Match(message);
            if (!seenMatch.Success)
            {
                return;
            }

            var nick = seenMatch.Groups["nick"].Value;
            Console.WriteLine($"{user} asked about '{nick}'");
            var seenList = _Data.GetLastSeen(nick);
            if (seenList == null)
            {
                _IrcClient.SendMessage(channel, $"Sorry, I haven't seen {nick}.");
                return;
            }

            foreach (var info in seenList.Take(3))
            {
                _IrcClient.SendMessage(channel, $"{info.User} was last seen in {info.Channel} {Util.TimeBetweenString(info.LastSeenTime)} ago saying '{info.Message}'.");
            }
        }

        // TODO: Add ProcessAction to do:
        // self.db.updateSeen(sender, channel, "* %s %s" % (sender, message))

    }
}
