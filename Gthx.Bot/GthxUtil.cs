using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Gthx.Bot.Interfaces;
using Microsoft.Extensions.Logging;

namespace Gthx.Bot
{
    public class GthxUtil : IGthxUtil
    {
        private readonly IWebReader _webReader;
        private readonly ILogger<GthxUtil> _logger;
        private readonly Regex _titleRegex = new(@"<title>(?'title'[^<]*) - [^<]*<\/title>", RegexOptions.IgnoreCase | RegexOptions.Multiline);
        private readonly Regex _metaRegex = new("<meta name=\"title\" content=\"(?'title'[^\"]*)\"", RegexOptions.IgnoreCase | RegexOptions.Multiline);
        private readonly Regex _youtubeUrlRegex = new(@"^https?:\/\/(www\.youtube\.com|youtu\.be)\/", RegexOptions.IgnoreCase);
        private static readonly Regex _whitespaceRegex = new(@"\s+");

        /// <summary>
        /// Most of a web page that's read looking for its title. The title is normally near the top,
        /// and the limit keeps a huge page (or one enormous line) from using up the bot's memory.
        /// </summary>
        private const int MaxPageBytes = 2 * 1024 * 1024;

        /// <summary>
        /// Longest title the bot repeats; anything longer is cut off.
        /// </summary>
        private const int MaxTitleLength = 300;

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
            if (_youtubeUrlRegex.IsMatch(url))
            {
                var oEmbedTitle = await GetYoutubeOEmbedTitle(url);
                if (!string.IsNullOrWhiteSpace(oEmbedTitle))
                {
                    return oEmbedTitle;
                }
            }

            _logger.LogInformation("Getting title from {URL}", url);

            var webStream = new LimitedReadStream(await _webReader.GetStreamFromUrlAsync(url), MaxPageBytes);

            _logger.LogInformation("Finished waiting for the web stream...");

            string? encodedTitle = null;

            using var reader = new StreamReader(webStream);
            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {

                // YouTube sometimes serves a page whose <title> is just " - YouTube" (the script fills it in later),
                // so an empty title doesn't count; keep looking for the meta title.
                var titleMatch = _titleRegex.Match(line);
                if (titleMatch.Success && !string.IsNullOrWhiteSpace(titleMatch.Groups["title"].Value))
                {
                    _logger.LogInformation("Found title: {Title}", titleMatch.Groups["title"].Value);
                    encodedTitle = titleMatch.Groups["title"].Value;
                    break;
                }

                var metaMatch = _metaRegex.Match(line);
                if (metaMatch.Success && !string.IsNullOrWhiteSpace(metaMatch.Groups["title"].Value))
                {
                    _logger.LogInformation("Found meta title: {Title}", metaMatch.Groups["title"].Value);
                    encodedTitle = metaMatch.Groups["title"].Value;
                    break;
                }
            }

            if (encodedTitle == null)
            {
                return string.Empty;
            }

            return CleanTitle(HttpUtility.HtmlDecode(encodedTitle));
        }

        /// <summary>
        /// Makes a title from a web page safe to repeat on IRC: control characters (including line breaks,
        /// which IRC can't send) and runs of whitespace become single spaces, and long titles are cut off.
        /// </summary>
        public static string CleanTitle(string title)
        {
            var noControls = new StringBuilder(title.Length);
            foreach (var c in title)
            {
                noControls.Append(char.IsControl(c) ? ' ' : c);
            }

            var cleaned = _whitespaceRegex.Replace(noControls.ToString(), " ").Trim();
            if (cleaned.Length > MaxTitleLength)
            {
                cleaned = cleaned[..MaxTitleLength].TrimEnd() + "...";
            }

            return cleaned;
        }

        /// <summary>
        /// Gets a YouTube video's title from YouTube's oEmbed endpoint, which doesn't depend on the
        /// watch page's HTML (YouTube serves a different page to some clients, such as cloud hosts).
        /// </summary>
        /// <returns>The title, or null if oEmbed has none (e.g. a private or deleted video)</returns>
        private async Task<string?> GetYoutubeOEmbedTitle(string url)
        {
            var oEmbedUrl = $"https://www.youtube.com/oembed?format=json&url={Uri.EscapeDataString(url)}";
            _logger.LogInformation("Getting YouTube title from {URL}", oEmbedUrl);
            try
            {
                await using var stream = await _webReader.GetStreamFromUrlAsync(oEmbedUrl);
                using var json = await JsonDocument.ParseAsync(stream);
                if (json.RootElement.ValueKind == JsonValueKind.Object &&
                    json.RootElement.TryGetProperty("title", out var title) &&
                    title.ValueKind == JsonValueKind.String)
                {
                    _logger.LogInformation("Found oEmbed title: {Title}", title.GetString());
                    return CleanTitle(title.GetString() ?? string.Empty);
                }
            }
            catch (Exception ex) when (ex is HttpRequestException or JsonException)
            {
                _logger.LogWarning("No oEmbed title for {URL}: {Message}", url, ex.Message);
            }

            return null;
        }

    }
}
