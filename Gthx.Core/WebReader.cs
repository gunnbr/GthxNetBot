﻿using Gthx.Core.Interfaces;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Gthx.Core
{
    public class WebReader : IWebReader
    {
        private static readonly HttpClient _HttpClient;

        static WebReader()
        {
            _HttpClient = new HttpClient();
        }

        public Task<Stream> GetStreamFromUrlAsync(string url)
        {
            return _HttpClient.GetStreamAsync(url);
        }
    }
}
