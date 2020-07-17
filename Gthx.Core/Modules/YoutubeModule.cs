using Gthx.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Gthx.Core.Modules
{
    public class YoutubeModule : IGthxModule
    {
        private readonly Regex _youtubeRegex = new Regex(@$"http(s)?:\/\/(?'url'www\.youtube\.com\/watch\?v=|youtu\.be\/)(?'id'[\w\-]*)(\S*)");
        private readonly Regex _titleRegex = new Regex(@"<title>(?'title'.*) - .*<\/title>", RegexOptions.IgnoreCase | RegexOptions.Multiline);
        private readonly Regex _metaRegex = new Regex("<meta name=\"title\" content=\"(?'title'.*)\"", RegexOptions.IgnoreCase | RegexOptions.Multiline);

        private readonly IGthxData _Data;
        private readonly IWebReader _WebReader;

        public YoutubeModule(IGthxData data, IWebReader webReader)
        {
            this._Data = data;
            this._WebReader = webReader;
        }

        public List<IrcResponse> ProcessMessage(string channel, string user, string message)
        {
            throw new NotImplementedException();
        }

        public async Task<List<IrcResponse>> ProcessMessageAsync(string channel, string user, string message)
        {
            var youtubeMatch = _youtubeRegex.Match(message);
            if (!youtubeMatch.Success)
            {
                return null;
            }

            var reply = new List<IrcResponse>();

            var url = youtubeMatch.Groups[0].Value;
            var id = youtubeMatch.Groups["id"].Value;
            Console.WriteLine($"Checking for Youtube title for '{id}'");
            var referenceData = _Data.AddYoutubeReference(id);
            if (referenceData.Title != null)
            {
                Debug.WriteLine($"Already have a title for youtube item {referenceData.Id}:{referenceData.Title}");
                reply.Add(new IrcResponse($"{user} linked to YouTube video \"{referenceData.Title}\" => {referenceData.ReferenceCount} IRC mentions"));
                return reply;
            }

            var title = await GetTitle(url, id);
            _Data.AddYoutubeTitle(id, title);
            if (string.IsNullOrEmpty(title))
            {
                reply.Add(new IrcResponse($"{user} linked to a YouTube video with an unknown title => {referenceData.ReferenceCount} IRC mentions"));
            }
            else
            {
                reply.Add(new IrcResponse($"{user} linked to YouTube video \"{title}\" => 1 IRC mentions"));
            }

            return reply;
        }

        private async Task<string> GetTitle(string url, string id)
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
    }
}
