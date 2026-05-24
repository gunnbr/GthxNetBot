using Gthx.Bot.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Gthx.Test.Mocks
{
    public class MockGthxUtil : IGthxUtil
    {
        private static readonly Dictionary<string, string> UrlTitles = new(StringComparer.OrdinalIgnoreCase)
        {
            ["https://www.youtube.com/watch?v=I7nVrT00ST4"] = "Pro Riders Laughing",
            ["https://www.youtube.com/watch?v=RE9gtTLZ5Ic"] = "BEST Japanese Fried Rice Recipe (焼き飯 - Yakimeshi)",
            ["https://www.youtube.com/watch?v=qFoNGyFrjl4"] = "2022 Weekly Beats Week 2: Forest Adventure",
            ["https://www.thingiverse.com/thing:2810756"] = "Articulated Butterfly by 8ran"
        };

        public string TimeBetweenString(DateTime? firstTime, DateTime? secondTime = null)
        {
            return string.Empty;
        }

        public Task<string> GetTitle(string url)
        {
            return Task.FromResult(UrlTitles.TryGetValue(url, out var title) ? title : string.Empty);
        }
    }
}
