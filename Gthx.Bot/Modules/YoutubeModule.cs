using Gthx.Bot.Interfaces;
using Gthx.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
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
        private readonly IServiceScopeFactory _scopeFactory;

        public YoutubeModule(IGthxData data, IIrcClient ircClient, IGthxUtil util, ILogger<YoutubeModule> logger, IServiceScopeFactory scopeFactory)
        {
            _data = data;
            _client = ircClient;
            _util = util;
            _logger = logger;
            _scopeFactory = scopeFactory;
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
            if (!string.IsNullOrEmpty(referenceData.Title))
            {
                _logger.LogInformation($"Already have a title for youtube item {referenceData.Item}:{referenceData.Title}");
                _client.SendMessage(channel, $"{user} linked to YouTube video \"{referenceData.Title}\" => {referenceData.Count} IRC mentions");
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
                data.AddYoutubeTitle(id, title);
                if (string.IsNullOrEmpty(title))
                {
                    _client.SendMessage(channel, $"{user} linked to a YouTube video with an unknown title => {referenceCount} IRC mentions");
                }
                else
                {
                    _client.SendMessage(channel, $"{user} linked to YouTube video \"{title}\" => {referenceCount} IRC mentions");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "YoutubeModule.GetAndSaveTitle failed!");
            }
        }
    }
}
