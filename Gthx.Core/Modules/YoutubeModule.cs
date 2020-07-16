using Gthx.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Gthx.Core.Modules
{
    public class YoutubeModule : IGthxModule
    {
        private readonly Regex _youtubeRegex = new Regex(@$"http(s)?:\/\/(?'url'www\.youtube\.com\/watch\?v=|youtu\.be\/)(?'id'[\w\-]*)(\S*)");

        private readonly IGthxData _Data;

        public YoutubeModule(IGthxData data)
        {
            this._Data = data;
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

            var url = youtubeMatch.Groups["url"].Value;
            var id = youtubeMatch.Groups["id"].Value;
            Console.WriteLine($"Checking for Youtube title for '{id}'");
            var referenceData = _Data.AddYoutubeReference(id);
            if (referenceData?.Title != null)
            {
                Debug.WriteLine($"Already have a title for youtube item {referenceData.Id}:{referenceData.Title}");
                return new List<IrcResponse>
                {
                    new IrcResponse($"{user} linked to YouTube video \"{referenceData.Title}\" => {referenceData.ReferenceCount} IRC mentions")
                };
            }

            // TODO: Implement getting of title from youtube!
            var title = await GetTitle(url, id);
            _Data.AddYoutubeTitle(id, title);
            return new List<IrcResponse>
            {
                new IrcResponse($"{user} linked to YouTube video \"{title}\" => 1 IRC mentions")
            };
        }

        private async Task<string> GetTitle(string url, string id)
        {
            await Task.Delay(3000);
            return "Dummy Title";
        } 
    }
}
