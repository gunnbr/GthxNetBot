using Gthx.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Gthx.Core.Modules
{
    public class ThingiverseModule : IGthxModule
    {
        private readonly Regex _thingiRegex = new Regex(@$"http(s)?:\/\/www.thingiverse.com\/thing:(?'id'\d+)");
        private readonly Regex _titleRegex = new Regex(@"<title>(?'title'.*) - .*<\/title>", RegexOptions.IgnoreCase | RegexOptions.Multiline);
        private readonly Regex _metaRegex = new Regex("<meta name=\"title\" content=\"(?'title'.*)\"", RegexOptions.IgnoreCase | RegexOptions.Multiline);

        private readonly IGthxData _Data;
        private readonly IIrcClient _IrcClient;
        private readonly IWebReader _WebReader;

        public ThingiverseModule(IGthxData data, IIrcClient ircClient, IWebReader webReader)
        {
            this._Data = data;
            this._IrcClient = ircClient;
            this._WebReader = webReader;
        }

        public void ProcessMessage(string channel, string user, string message)
        {
            var youtubeMatch = _thingiRegex.Match(message);
            if (!youtubeMatch.Success)
            {
                return;
            }

            var url = youtubeMatch.Groups[0].Value;
            var id = youtubeMatch.Groups["id"].Value;
            Console.WriteLine($"Checking for Thingiverse title for '{id}'");
            var referenceData = _Data.AddThingiverseReference(id);
            if (referenceData.Title != null)
            {
                Console.WriteLine($"Already have a title for Thingiverse item {referenceData.Id}:{referenceData.Title}");
                _IrcClient.SendMessage(channel, $"{user} linked to \"{referenceData.Title}\" on thingiverse => {referenceData.ReferenceCount} IRC mentions");
                return;
            }

            GetAndSaveTitle(url, id, channel, user, referenceData?.ReferenceCount ?? 1);
        }

        // TODO: Move this to the Util class once a DI solution is found that
        //       works with unit tests so the WebReader can be mocked
        private async Task<string> GetTitle(string url, string id)
        {
            Console.WriteLine($"Getting thingiverse title for '{id}'");

            var webStream = await _WebReader.GetStreamFromUrlAsync(url);

            Console.WriteLine("Finished waiting for the web stream...");

            using (var reader = new StreamReader(webStream))
            {
                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();

                    var titleMatch = _titleRegex.Match(line);
                    if (titleMatch.Success)
                    {
                        Console.WriteLine($"Found title: {titleMatch.Groups["title"].Value}");
                        return titleMatch.Groups["title"].Value;
                    }

                    var metaMatch = _metaRegex.Match(line);
                    if (metaMatch.Success)
                    {
                        Console.WriteLine($"Found title: {metaMatch.Groups["title"].Value}");
                        return metaMatch.Groups["title"].Value;
                    }
                }
            }

            return null;
        }

        public async void GetAndSaveTitle(string url, string id, string channel, string user, int referenceCount)
        {
            var title = await GetTitle(url, id);
            Console.WriteLine($"Got the title for ID {id} as '{title}'");
            _Data.AddYoutubeTitle(id, title);

            if (string.IsNullOrEmpty(title))
            {
                _IrcClient.SendMessage(channel, $"{user} linked to thing {id} on thingiverse => {referenceCount} IRC mentions");
            }
            else
            {
                Console.WriteLine($"Sending reply about the title");
                _IrcClient.SendMessage(channel, $"{user} linked to \"{title}\" on thingiverse => {referenceCount} IRC mentions");
                Console.WriteLine("Done sending reply about the title");
            }
        }
    }
}
