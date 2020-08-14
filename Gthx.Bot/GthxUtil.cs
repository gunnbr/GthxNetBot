using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Gthx.Bot.Interfaces;
using Microsoft.Extensions.Logging;

namespace Gthx.Bot
{
    public class GthxUtil : IGthxUtil
    {
        private readonly IWebReader _webReader;
        private readonly ILogger<GthxUtil> _logger;
        private readonly Regex _titleRegex = new Regex(@"<title>(?'title'.*) - .*<\/title>", RegexOptions.IgnoreCase | RegexOptions.Multiline);
        private readonly Regex _metaRegex = new Regex("<meta name=\"title\" content=\"(?'title'.*)\"", RegexOptions.IgnoreCase | RegexOptions.Multiline);

        public GthxUtil(IWebReader webReader, ILogger<GthxUtil> logger)
        {
            _webReader = webReader;
            _logger = logger;
        }

        /// <summary>
        /// Regex string to match IRC nicknames
        /// </summary>
        public static readonly string NickMatch = @"[a-zA-Z\*_\\\[\]\{\}^`|\*][a-zA-Z0-9\*_\\\[\]\{\}^`|-]*";

        /// <summary>
        /// User readable "gthx standard" time between two times
        /// </summary>
        /// <param name="firstTime">UTC DateTime of the first time to compare against <paramref name="secondTime"/></param>
        /// <param name="secondTime">UTC DateTime of the first time to compare against <paramref name="firstTime"/></param>
        /// <returns>A string in user readable format between the times passed in</returns>
        public string TimeBetweenString(DateTime? firstTime, DateTime? secondTime = null)
        {
            if (firstTime == null)
            {
                return "<Unknown>";
            }

            var replyString = new StringBuilder();
            var since = (secondTime ?? DateTime.UtcNow) - firstTime.Value;

            var years = since.Days / 365;
            var days = since.Days % 365;

            if (years > 0)
            {
                replyString.Append($"{years} year");
                if (years > 1)
                {
                    replyString.Append("s");
                }

                if ((days > 0) ||
                    (since.Hours > 0) ||
                    (since.Minutes > 0) ||
                    (since.Seconds > 0))
                {
                    replyString.Append(", ");
                }
            }

            if (days > 0)
            {
                replyString.Append($"{days} day");
                if (days > 1)
                {
                    replyString.Append("s");
                }
                if ((since.Hours > 0) ||
                    (since.Minutes > 0) ||
                    (since.Seconds > 0))
                {
                    replyString.Append(", ");
                }
            }

            if (since.Hours > 0)
            {
                replyString.Append($"{since.Hours} hour");
                if (since.Hours > 1)
                {
                    replyString.Append("s");
                }
                if ((since.Minutes > 0) || (since.Seconds > 0))
                {
                    replyString.Append(", ");
                }
            }

            if (since.Minutes > 0)
            {
                replyString.Append($"{since.Minutes} minute");
                if (since.Minutes > 1)
                {
                    replyString.Append("s");
                }
                if (since.Seconds > 0)
                {
                    replyString.Append(", ");
                }
            }

            if (since.Seconds > 0 || replyString.Length == 0)
            {
                replyString.Append($"{since.Seconds} second");
                if (since.Seconds != 1)
                {
                    replyString.Append("s");
                }
            }

            return replyString.ToString();
        }

        /// <summary>
        /// Gets the title from a given URL from either the title element
        /// or the 'meta title' element.
        /// </summary>
        /// <param name="url">URL to load</param>
        /// <returns></returns>
        public async Task<string> GetTitle(string url)
        {
            _logger.LogInformation("Getting title from {URL}", url);

            var webStream = await _webReader.GetStreamFromUrlAsync(url);

            _logger.LogInformation("Finished waiting for the web stream...");

            using var reader = new StreamReader(webStream);
            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (line == null)
                {
                    break;
                }

                var titleMatch = _titleRegex.Match(line);
                if (titleMatch.Success)
                {
                    _logger.LogInformation("Found title: {Title}", titleMatch.Groups["title"].Value);
                    return titleMatch.Groups["title"].Value;
                }

                var metaMatch = _metaRegex.Match(line);
                if (metaMatch.Success)
                {
                    _logger.LogInformation("Found meta title: {Title}", metaMatch.Groups["title"].Value);
                    return metaMatch.Groups["title"].Value;
                }
            }

            return string.Empty;
        }

    }
}
