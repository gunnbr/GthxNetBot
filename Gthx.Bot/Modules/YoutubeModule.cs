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
        private readonly Regex _titleRegex = new Regex(@"<title>(?'title'.*) - .*<\/title>", RegexOptions.IgnoreCase | RegexOptions.Multiline);
        private readonly Regex _metaRegex = new Regex("<meta name=\"title\" content=\"(?'title'.*)\"", RegexOptions.IgnoreCase | RegexOptions.Multiline);

        private readonly IGthxData _Data;
        private readonly IIrcClient _IrcClient;
        private readonly IWebReader _WebReader;
        private readonly ILogger<YoutubeModule> _Logger;

        public YoutubeModule(IGthxData data, IIrcClient ircClient, IWebReader webReader, ILogger<YoutubeModule> logger)
        {
            _Data = data;
            _IrcClient = ircClient;
            _WebReader = webReader;
            _Logger = logger;
        }

        public void ProcessAction(string channel, string user, string message)
        {
        }

        public void ProcessMessage(string channel, string user, string message)
        {
            var youtubeMatch = _youtubeRegex.Match(message);
            if (!youtubeMatch.Success)
            {
                return;
            }

            var url = youtubeMatch.Groups[0].Value;
            var id = youtubeMatch.Groups["id"].Value;
            _Logger.LogInformation("Checking for Youtube title for '{id}'", id);
            var referenceData = _Data.AddYoutubeReference(id);
            if (referenceData.Title != null)
            {
                Debug.WriteLine($"Already have a title for youtube item {referenceData.Item}:{referenceData.Title}");
                _IrcClient.SendMessage(channel, $"{user} linked to YouTube video \"{referenceData.Title}\" => {referenceData.Count} IRC mentions");
                return;
            }

            GetAndSaveTitle(url, id, channel, user, referenceData?.Count ?? 1);
        }

        private async Task<string> GetTitle(string url)
        {
            var webStream = await _WebReader.GetStreamFromUrlAsync(url);

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
            var title = await GetTitle(url);
            _Data.AddYoutubeTitle(id, title);
            if (string.IsNullOrEmpty(title))
            {
                _IrcClient.SendMessage(channel, $"{user} linked to a YouTube video with an unknown title => {referenceCount} IRC mentions");
            }
            else
            {
                _IrcClient.SendMessage(channel, $"{user} linked to YouTube video \"{title}\" => 1 IRC mentions");
            }
        }
    }
}
