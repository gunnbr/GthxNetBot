using Gthx.Bot.Interfaces;
using Gthx.Data;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace Gthx.Bot.Modules
{
    /// <summary>
    /// Counts links to MakerWorld models. makerworld.com doesn't let the bot read its pages, so the title
    /// comes from the words at the end of the link (makerworld.com/en/models/2459711-articulated-dragon),
    /// or from an earlier link to the same model that had them.
    /// </summary>
    public class MakerWorldModule : IGthxModule
    {
        // An optional language (makerworld.com/en/models/...), the model number, then the optional name
        private readonly Regex _makerWorldRegex = new(@"https?:\/\/(www\.)?makerworld\.com\/([a-z]{2}(-[a-z]{2})?\/)?models\/(?'id'\d+)(-(?'slug'[\w-]+))?", RegexOptions.IgnoreCase);

        private readonly IGthxData _data;
        private readonly IIrcClient _client;
        private readonly ILogger<MakerWorldModule> _logger;

        public MakerWorldModule(IGthxData data, IIrcClient ircClient, ILogger<MakerWorldModule> logger)
        {
            _data = data;
            _client = ircClient;
            _logger = logger;
        }

        public bool ProcessAction(string channel, string user, string message)
        {
            return false;
        }

        public bool ProcessMessage(string channel, string user, string message, bool wasDirectlyAddressed)
        {
            var match = _makerWorldRegex.Match(message);
            if (!match.Success || !int.TryParse(match.Groups["id"].Value, out var id))
            {
                return false;
            }

            var linkTitle = GthxUtil.TitleFromSlug(match.Groups["slug"].Value);
            _logger.LogInformation("MakerWorld model {id} linked with title '{title}'", id, linkTitle);

            var referenceData = _data.AddMakerWorldReference(id);
            if (linkTitle.Length > 0 && linkTitle != referenceData.Title)
            {
                _data.AddMakerWorldTitle(id, linkTitle);
            }

            var title = linkTitle.Length > 0 ? linkTitle : referenceData.Title;
            if (string.IsNullOrEmpty(title))
            {
                _client.SendMessage(channel, $"{user} linked to a model on MakerWorld => {referenceData.Count} IRC mentions");
            }
            else
            {
                _client.SendMessage(channel, $"{user} linked to \"{title}\" on MakerWorld => {referenceData.Count} IRC mentions");
            }

            return false;
        }
    }
}
