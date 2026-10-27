using Gthx.Bot.Interfaces;
using Gthx.Data;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace Gthx.Bot.Modules
{
    /// <summary>
    /// Counts links to Printables models. printables.com doesn't let the bot read its pages, so the title
    /// comes from the words at the end of the link (printables.com/model/1863827-heavy-duty-keyboard-holder),
    /// or from an earlier link to the same model that had them.
    /// </summary>
    public class PrintablesModule : IGthxModule
    {
        // An optional language (printables.com/de/model/...), the model number, then the optional name
        private readonly Regex _printablesRegex = new(@"https?:\/\/(www\.)?printables\.com\/([a-z]{2}(-[a-z]{2})?\/)?model\/(?'id'\d+)(-(?'slug'[\w-]+))?", RegexOptions.IgnoreCase);

        private readonly IGthxData _data;
        private readonly IIrcClient _client;
        private readonly ILogger<PrintablesModule> _logger;

        public PrintablesModule(IGthxData data, IIrcClient ircClient, ILogger<PrintablesModule> logger)
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
            var match = _printablesRegex.Match(message);
            if (!match.Success || !int.TryParse(match.Groups["id"].Value, out var id))
            {
                return false;
            }

            var linkTitle = GthxUtil.TitleFromSlug(match.Groups["slug"].Value);
            _logger.LogInformation("Printables model {id} linked with title '{title}'", id, linkTitle);

            var referenceData = _data.AddPrintablesReference(id);
            if (linkTitle.Length > 0 && linkTitle != referenceData.Title)
            {
                _data.AddPrintablesTitle(id, linkTitle);
            }

            var title = linkTitle.Length > 0 ? linkTitle : referenceData.Title;
            if (string.IsNullOrEmpty(title))
            {
                _client.SendMessage(channel, $"{user} linked to a model on Printables => {referenceData.Count} IRC mentions");
            }
            else
            {
                _client.SendMessage(channel, $"{user} linked to \"{title}\" on Printables => {referenceData.Count} IRC mentions");
            }

            return false;
        }
    }
}
