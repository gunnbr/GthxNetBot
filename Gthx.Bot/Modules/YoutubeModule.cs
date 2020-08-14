using Gthx.Bot.Interfaces;
using Gthx.Data;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Gthx.Bot.Modules
{
    public class YoutubeModule : IGthxModule
    {
        private readonly Regex _youtubeRegex = new Regex(@$"http(s)?:\/\/(?'url'www\.youtube\.com\/watch\?v=|youtu\.be\/)(?'id'[\w\-]*)(\S*)");

        private readonly IGthxData _data;
        private readonly IIrcClient _client;
        private readonly IGthxUtil _util;
        private readonly ILogger<YoutubeModule> _logger;

        public YoutubeModule(IGthxData data, IIrcClient ircClient, IGthxUtil util, ILogger<YoutubeModule> logger)
        {
            _data = data;
            _client = ircClient;
            _util = util;
            _logger = logger;
        }

        public bool ProcessAction(string channel, string user, string message)
        {
            // TODO: Process links posted in actions too?
            return false;
        }

        public bool ProcessMessage(string channel, string user, string message, bool wasDirectlyAddressed)
        {
            var youtubeMatch = _youtubeRegex.Match(message);
            if (!youtubeMatch.Success)
            {
                return false;
            }

            var url = youtubeMatch.Groups[0].Value;
            var id = youtubeMatch.Groups["id"].Value;
            _logger.LogInformation("Checking for Youtube title for '{id}'", id);
            var referenceData = _data.AddYoutubeReference(id);
            if (referenceData.Title != null)
            {
                Debug.WriteLine($"Already have a title for youtube item {referenceData.Item}:{referenceData.Title}");
                _client.SendMessage(channel, $"{user} linked to YouTube video \"{referenceData.Title}\" => {referenceData.Count} IRC mentions");
                return false;
            }

            GetAndSaveTitle(url, id, channel, user, referenceData?.Count ?? 1);
            return false;
        }

        public async void GetAndSaveTitle(string url, string id, string channel, string user, int referenceCount)
        {
            var title = await _util.GetTitle(url);
            _logger.LogInformation("Got the title for ID {id} as '{title}'", id, title);
            _data.AddYoutubeTitle(id, title);
            if (string.IsNullOrEmpty(title))
            {
                _client.SendMessage(channel, $"{user} linked to a YouTube video with an unknown title => {referenceCount} IRC mentions");
            }
            else
            {
                _client.SendMessage(channel, $"{user} linked to YouTube video \"{title}\" => 1 IRC mentions");
            }
        }
    }
}
