using Gthx.Bot.Interfaces;
using Gthx.Data;
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
        private readonly Regex _titleRegex = new Regex(@"<title>(?'title'.*) - .*<\/title>", RegexOptions.IgnoreCase | RegexOptions.Multiline);
        private readonly Regex _metaRegex = new Regex("<meta name=\"title\" content=\"(?'title'.*)\"", RegexOptions.IgnoreCase | RegexOptions.Multiline);

        private readonly IGthxData _Data;
        private readonly IIrcClient _IrcClient;
        private readonly IWebReader _WebReader;
        private readonly ILogger<ThingiverseModule> _Logger;

        public ThingiverseModule(IGthxData data, IIrcClient ircClient, IWebReader webReader, ILogger<ThingiverseModule> logger)
        {
            _Data = data;
            _IrcClient = ircClient;
            _WebReader = webReader;
            _Logger = logger;
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
            _Logger.LogInformation("Checking for Thingiverse title for '{id}'", id);
            var referenceData = _Data.AddThingiverseReference(id);
            if (referenceData.Title != null)
            {
                _Logger.LogInformation("Already have a title for Thingiverse {Item}: {Title}", referenceData.Item, referenceData.Title);
                _IrcClient.SendMessage(channel, $"{user} linked to \"{referenceData.Title}\" on thingiverse => {referenceData.Count} IRC mentions");
                return false;
            }

            GetAndSaveTitle(url, id, channel, user, referenceData?.Count ?? 1);
            return false;
        }

        // TODO: Move this to the Util class now that a DI solution is found that
        //       works with unit tests so the WebReader can be mocked
        private async Task<string> GetTitle(string url, string id)
        {
            _Logger.LogInformation("Getting thingiverse title for '{id}'", id);

            var webStream = await _WebReader.GetStreamFromUrlAsync(url);

            _Logger.LogInformation("Finished waiting for the web stream...");

            using (var reader = new StreamReader(webStream))
            {
                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();

                    var titleMatch = _titleRegex.Match(line);
                    if (titleMatch.Success)
                    {
                        _Logger.LogInformation("Found title: {Title}", titleMatch.Groups["title"].Value);
                        return titleMatch.Groups["title"].Value;
                    }

                    var metaMatch = _metaRegex.Match(line);
                    if (metaMatch.Success)
                    {
                        _Logger.LogInformation("Found meta title: {Title}", metaMatch.Groups["title"].Value);
                        return metaMatch.Groups["title"].Value;
                    }
                }
            }

            return null;
        }

        public async void GetAndSaveTitle(string url, string id, string channel, string user, int referenceCount)
        {
            var title = await GetTitle(url, id);
            _Logger.LogInformation("Got the title for ID {id} as '{title}'", id, title);
            _Data.AddThingiverseTitle(id, title);

            if (string.IsNullOrEmpty(title))
            {
                _IrcClient.SendMessage(channel, $"{user} linked to thing {id} on thingiverse => {referenceCount} IRC mentions");
            }
            else
            {
                _IrcClient.SendMessage(channel, $"{user} linked to \"{title}\" on thingiverse => {referenceCount} IRC mentions");
            }
        }
    }
}
