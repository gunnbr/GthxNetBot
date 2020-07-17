using Gthx.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
        private readonly IWebReader _WebReader;

        public ThingiverseModule(IGthxData data, IWebReader webReader)
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
            var youtubeMatch = _thingiRegex.Match(message);
            if (!youtubeMatch.Success)
            {
                return null;
            }

            var reply = new List<IrcResponse>();

            var url = youtubeMatch.Groups[0].Value;
            var id = youtubeMatch.Groups["id"].Value;
            Console.WriteLine($"Checking for Thingiverse title for '{id}'");
            var referenceData = _Data.AddThingiverseReference(id);
            if (referenceData.Title != null)
            {
                Console.WriteLine($"Already have a title for Thingiverse item {referenceData.Id}:{referenceData.Title}");
                reply.Add(new IrcResponse($"{user} linked to \"{referenceData.Title}\" on thingiverse => {referenceData.ReferenceCount} IRC mentions"));
                return reply;
            }

            var title = await GetTitle(url, id);
            _Data.AddYoutubeTitle(id, title);

            if (string.IsNullOrEmpty(title))
            {
                reply.Add(new IrcResponse($"{user} linked to thing {id} on thingiverse => {referenceData.ReferenceCount} IRC mentions"));
            }
            else
            {
                reply.Add(new IrcResponse($"{user} linked to \"{title}\" on thingiverse => {referenceData.ReferenceCount} IRC mentions"));
            }

            return reply;
        }

        // TODO: Move this to the Util class once a DI solution is found
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
