using Gthx.Bot.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gthx.Data;
using Microsoft.Extensions.Logging;

namespace Gthx.Bot.Modules
{
    public class LurkerModule : IGthxModule
    {
        private readonly IIrcClient _client;
        private readonly IGthxData _data;
        private readonly IBotNick _botNick;
        private readonly ILogger<LurkerModule> _logger;

        public LurkerModule(IIrcClient client, IGthxData data, IBotNick botNick, ILogger<LurkerModule> logger)
        {
            _client = client;
            _data = data;
            _botNick = botNick;
            _logger = logger;
        }

        public bool ProcessAction(string channel, string user, string message)
        {
            return false;
        }

        public bool ProcessMessage(string channel, string user, string message, bool wasDirectlyAddressed)
        {
            if (message != "lurkers?")
            {
                return false;
            }

            FindAndReportLurkersAsync(channel);
            return true;
        }

        private async void FindAndReportLurkersAsync(string channel)
        {
            var allUsers = await _client.GetUsersInChannelAsync(channel);
            // TODO: Check the performance of this. May want to add a different call to GthxData
            //       that takes a list of users so this can be optimized to search the DB more efficiently
            var lurkers = allUsers.Where(user => user!= _botNick.BotNick && 
                                                 _data.GetLastSeen(user) == null);
            // TODO: It would be nice if this applied only to a single channel, but maybe it doesn't matter.
            //       If this instance of the bot shares a DB with other channels, it's because they want all
            //       the same info, so maybe it's just okay that this counts people as saying something even
            //       if it's in a different channel
            _client.SendMessage(channel,
                $"{lurkers.Count()} of the {allUsers.Count} users in {channel} right now have never said anything.");
        }
    }
}
