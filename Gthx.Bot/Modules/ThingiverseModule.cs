using Gthx.Bot.Interfaces;
using Gthx.Data;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace Gthx.Bot.Modules
{
    /// <summary>
    /// Counts links to Thingiverse things. The bot doesn't fetch titles from thingiverse.com: the site
    /// refuses the bot's requests, and its API's license terms aren't acceptable (#39). Titles saved
    /// before that are still used.
    /// </summary>
    public class ThingiverseModule : IGthxModule
    {
        private readonly Regex _thingiRegex = new Regex(@$"http(s)?:\/\/www\.thingiverse\.com\/thing:(?'id'\d+)");

        private readonly IGthxData _data;
        private readonly IIrcClient _client;
        private readonly ILogger<ThingiverseModule> _logger;

        public ThingiverseModule(IGthxData data, IIrcClient ircClient, ILogger<ThingiverseModule> logger)
        {
            _data = data;
            _client = ircClient;
            _logger = logger;
        }

        public bool ProcessAction(string channel, string user, string message)
        {
            // TODO: Process links posted in actions too!
            return false;
        }

        public bool ProcessMessage(string channel, string user, string message, bool wasDirectlyAddressed)
        {
            var thingiMatch = _thingiRegex.Match(message);
            if (!thingiMatch.Success)
            {
                return false;
            }

            var id = thingiMatch.Groups["id"].Value;
            _logger.LogInformation("Thingiverse thing {id} linked", id);
            var referenceData = _data.AddThingiverseReference(id);
            if (referenceData == null)
            {
                return false;
            }

            if (string.IsNullOrEmpty(referenceData.Title))
            {
                _client.SendMessage(channel, $"{user} linked to thing {id} on thingiverse => {referenceData.Count} IRC mentions");
            }
            else
            {
                _client.SendMessage(channel, $"{user} linked to \"{referenceData.Title}\" on thingiverse => {referenceData.Count} IRC mentions");
            }

            return false;
        }
    }
}
