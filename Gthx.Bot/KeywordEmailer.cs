using Gthx.Bot.Interfaces;
using Gthx.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Gthx.Bot
{
    /// <summary>
    /// Emails the admin when someone says one of the keywords set on the website's admin panel, anywhere in a
    /// message and ignoring case, so they hear about it while they're away from IRC.
    /// </summary>
    public class KeywordEmailer
    {
        /// <summary>
        /// How often to read the keywords again, so changes on the website take effect without a restart
        /// </summary>
        public static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(1);

        /// <summary>
        /// After emailing about a keyword in a channel, how long to wait before emailing about it there again, so
        /// a conversation about it doesn't fill the admin's inbox
        /// </summary>
        public static readonly TimeSpan QuietTime = TimeSpan.FromMinutes(15);

        private readonly IEmailSender _sender;
        private readonly IBotNick _botNick;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<KeywordEmailer> _logger;
        private readonly object _lock = new();

        private List<string> _keywords = new();
        private DateTime? _loadedAt;

        /// <summary>
        /// When each keyword was last emailed about, keyed by channel and keyword in lower case
        /// </summary>
        private readonly Dictionary<(string Channel, string Keyword), DateTime> _lastSent = new();

        public KeywordEmailer(IEmailSender sender, IBotNick botNick, IServiceScopeFactory scopeFactory, ILogger<KeywordEmailer> logger)
        {
            _sender = sender;
            _botNick = botNick;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        /// <summary>
        /// Emails the admin if the message has any keywords that haven't been emailed about in this channel lately.
        /// Sending happens in the background. Returns the keywords emailed about.
        /// </summary>
        /// <param name="channel">Channel the message was said in, or the user's nick for a private message</param>
        /// <param name="user">Nick of the user who said it</param>
        /// <param name="message">The whole message, as said</param>
        /// <param name="now">The current UTC time</param>
        public List<string> Check(string channel, string user, string message, DateTime now)
        {
            if (!_sender.IsEnabled)
            {
                return new List<string>();
            }

            List<string> found;
            lock (_lock)
            {
                if (_loadedAt == null || now - _loadedAt >= RefreshInterval)
                {
                    LoadKeywords(now);
                }

                found = _keywords
                    .Where(k => message.Contains(k, StringComparison.OrdinalIgnoreCase))
                    .Where(k => !_lastSent.TryGetValue(Key(channel, k), out var sent) || now - sent >= QuietTime)
                    .ToList();
                foreach (var keyword in found)
                {
                    _lastSent[Key(channel, keyword)] = now;
                }
            }

            if (found.Count > 0)
            {
                _logger.LogInformation("Emailing about {Keywords} from {User} in {Channel}", found, user, channel);
                _ = Send(Subject(channel, found), Body(channel, user, message, found, now));
            }

            return found;
        }

        private static (string, string) Key(string channel, string keyword) =>
            (channel.ToLowerInvariant(), keyword.ToLowerInvariant());

        private void LoadKeywords(DateTime now)
        {
            // Whether or not it works, wait a while before trying again, so a database problem doesn't mean a
            // query (and an error) for every message.
            _loadedAt = now;
            try
            {
                using var scope = _scopeFactory.CreateScope();
                _keywords = scope.ServiceProvider.GetRequiredService<IGthxData>().GetEmailKeywords()
                    .Where(k => !string.IsNullOrWhiteSpace(k))
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Couldn't read the email keywords; still using the last ones read");
            }
        }

        private static string Where(string channel) =>
            channel.StartsWith('#') ? $"in {channel}" : "in a private message";

        private string Subject(string channel, List<string> keywords) =>
            $"{_botNick.BotNick}: {string.Join(", ", keywords.Select(k => $"\"{k}\""))} {Where(channel)}";

        private static string Body(string channel, string user, string message, List<string> keywords, DateTime now) =>
            $"{user} said {Where(channel)} at {now:yyyy-MM-dd HH:mm} UTC:\n\n" +
            $"{message}\n\n" +
            $"Keywords: {string.Join(", ", keywords)}. Change them at https://gthx.net/admin.\n";

        private async Task Send(string subject, string body)
        {
            try
            {
                await _sender.SendAsync(subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Couldn't send the keyword email \"{Subject}\"", subject);
            }
        }
    }
}
