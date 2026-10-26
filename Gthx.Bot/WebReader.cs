using Gthx.Bot.Interfaces;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Gthx.Bot
{
    public class WebReader : IWebReader
    {
        private static readonly HttpClient _HttpClient;

        static WebReader()
        {
            // Titles are fetched in the background, so give up on a slow site well before the default 100 seconds.
            _HttpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(20)
            };
        }

        public Task<Stream> GetStreamFromUrlAsync(string url)
        {
            return _HttpClient.GetStreamAsync(url);
        }
    }
}
