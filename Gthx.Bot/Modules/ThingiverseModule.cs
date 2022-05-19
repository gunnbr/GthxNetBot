using Gthx.Bot.Interfaces;
using Gthx.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Gthx.Bot.Modules
{
    public class ThingiverseModule : IGthxModule
    {
        private readonly Regex _thingiRegex = new Regex(@$"http(s)?:\/\/www.thingiverse.com\/thing:(?'id'\d+)");

        private readonly IGthxData _data;
        private readonly IIrcClient _client;
        private readonly IGthxUtil _util;
        private readonly ILogger<ThingiverseModule> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public ThingiverseModule(IGthxData data, IIrcClient ircClient, IGthxUtil util, ILogger<ThingiverseModule> logger, IServiceScopeFactory scopeFactory)
        {
            _data = data;
            _client = ircClient;
            _util = util;
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public bool ProcessAction(string channel, string user, string message)
        {
            // TODO: Process links posted in actions too!
            return false;
        }

        public bool ProcessMessage(string channel, string user, string message, bool wasDirectlyAddressed)
        {
            var youtubeMatch = _thingiRegex.Match(message);
            if (!youtubeMatch.Success)
            {
                return false;
            }

            var url = youtubeMatch.Groups[0].Value;
            var id = youtubeMatch.Groups["id"].Value;
            _logger.LogInformation("Checking for Thingiverse title for '{id}'", id);
            var referenceData = _data.AddThingiverseReference(id);
            if (!string.IsNullOrEmpty(referenceData.Title))
            {
                _logger.LogInformation("Already have a title for Thingiverse {Item}: {Title}", referenceData.Item, referenceData.Title);
                _client.SendMessage(channel, $"{user} linked to \"{referenceData.Title}\" on thingiverse => {referenceData.Count} IRC mentions");
                return false;
            }

            Task.Run(() => GetAndSaveTitle(url, id, channel, user, referenceData?.Count ?? 1));
            return false;
        }


        public async Task GetAndSaveTitle(string url, string id, string channel, string user, int referenceCount)
        {
            try
            {
                using IServiceScope messageScope = _scopeFactory.CreateScope();

                var data = messageScope.ServiceProvider.GetRequiredService<IGthxData>();

                var title = await _util.GetTitle(url);
                _logger.LogInformation("Got the title for ID {id} as '{title}'", id, title);
                data.AddThingiverseTitle(id, title);

                if (string.IsNullOrEmpty(title))
                {
                    _client.SendMessage(channel, $"{user} linked to thing {id} on thingiverse => {referenceCount} IRC mentions");
                }
                else
                {
                    _client.SendMessage(channel, $"{user} linked to \"{title}\" on thingiverse => {referenceCount} IRC mentions");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ThingiverseModule.GetAndSaveTitle failed!");
            }
        }
    }
}
