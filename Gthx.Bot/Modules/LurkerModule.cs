using Gthx.Bot.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gthx.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Gthx.Bot.Modules
{
    public class LurkerModule : IGthxModule
    {
        private readonly IIrcClient _client;
        private readonly IBotNick _botNick;
        private readonly ILogger<LurkerModule> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public LurkerModule(IIrcClient client, IBotNick botNick, ILogger<LurkerModule> logger, IServiceScopeFactory scopeFactory)
        {
            _client = client;
            _botNick = botNick;
            _logger = logger;
            _scopeFactory = scopeFactory;
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

            _logger.LogInformation("Lurkers request by {user} in {channel}", user, channel);
            _ = FindAndReportLurkersAsync(channel);
            return true;
        }

        private async Task FindAndReportLurkersAsync(string channel)
        {
            try
            {
                // This runs after the message's scope (and its DB context) has been disposed,
                // so it needs a scope of its own.
                using IServiceScope scope = _scopeFactory.CreateScope();
                var data = scope.ServiceProvider.GetRequiredService<IGthxData>();

                // TODO: Is there a better way to do or report performance data with Serilog?
                var start = DateTime.Now;

                var allUsers = await _client.GetUsersInChannelAsync(channel);
                _logger.LogInformation("Time to get all users: {TimeInSeconds}", DateTime.Now - start);

                var dbStart = DateTime.Now;
                // TODO: Check the performance of this. May want to add a different call to GthxData
                //       that takes a list of users so this can be optimized to search the DB more efficiently
                var lurkerCount = allUsers.Count(user => user != _botNick.BotNick &&
                                                         data.GetLastSeen(user) == null);

                _logger.LogInformation("Time to find lurkers: {TimeInSeconds}", DateTime.Now - dbStart);
                // Note: It doesn't matter that this doesn't limit the search to a single channel.
                //       If this instance of the bot shares a DB with other channels, it's because they want all
                //       the same info, so it's just okay that this counts people as saying something even
                //       if it's in a different channel.
                _client.SendMessage(channel,
                    $"{lurkerCount} of the {allUsers.Count} users in {channel} right now have never said anything.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LurkerModule.FindAndReportLurkersAsync failed!");
            }
        }
    }
}
