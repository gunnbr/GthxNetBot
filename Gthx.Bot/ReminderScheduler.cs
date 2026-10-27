using Gthx.Bot.Interfaces;
using Gthx.Core;
using Gthx.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.RegularExpressions;
using System.Timers;

namespace Gthx.Bot
{
    /// <summary>
    /// Delivers reminders when they come due, if the person who asked for them has spoken recently
    /// and is still online. Reminders for someone who isn't are left for <see cref="Modules.ReminderModule"/>
    /// to deliver the next time they speak.
    /// </summary>
    public class ReminderScheduler : IDisposable
    {
        /// <summary>
        /// How often to look for reminders that have come due
        /// </summary>
        public static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Someone who has spoken this recently is taken to be around to see a reminder
        /// </summary>
        public static readonly TimeSpan ActiveTime = TimeSpan.FromMinutes(5);

        private readonly IIrcClient _client;
        private readonly IGthxUtil _util;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ReminderScheduler> _logger;
        private readonly Timer _timer;
        private readonly object _checkLock = new();
        private static readonly Regex _readsOnRegex = new(@"^(to|about|that|of)\b", RegexOptions.IgnoreCase);

        /// <summary>
        /// When each nick last spoke anywhere (a channel or a private message), keyed by the nick in lower case
        /// </summary>
        private readonly ConcurrentDictionary<string, DateTime> _lastSpoke = new();

        public ReminderScheduler(IIrcClient client, IGthxUtil util, IServiceScopeFactory scopeFactory, ILogger<ReminderScheduler> logger)
        {
            _client = client;
            _util = util;
            _scopeFactory = scopeFactory;
            _logger = logger;

            _timer = new Timer(CheckInterval.TotalMilliseconds);
            _timer.AutoReset = false;
            _timer.Elapsed += HandleTimer;
        }

        /// <summary>
        /// Starts checking for due reminders
        /// </summary>
        public void Start()
        {
            _timer.Start();
        }

        /// <summary>
        /// Records that a user spoke, so a reminder that comes due soon after can be posted straight away
        /// </summary>
        public void UserSpoke(string user, DateTime when)
        {
            _lastSpoke[user.ToLowerInvariant()] = when;
        }

        /// <summary>
        /// Builds the message that delivers a reminder. "to ...", "about ...", "that ..." and "of ..." follow
        /// "remind you" directly; anything else (such as "hey it's Tuesday again!") comes after a colon.
        /// </summary>
        public static string DeliveryMessage(Reminder reminder, IGthxUtil util, DateTime now)
        {
            var separator = _readsOnRegex.IsMatch(reminder.Message) ? " " : ": ";
            return $"{reminder.Author}: {util.TimeBetweenString(reminder.Timestamp, now)} ago you asked me to remind you{separator}{reminder.Message}";
        }

        private void HandleTimer(object? sender, ElapsedEventArgs e)
        {
            try
            {
                CheckDueReminders(DateTime.UtcNow);
            }
            finally
            {
                _timer.Start();
            }
        }

        /// <summary>
        /// Posts each reminder that's due by <paramref name="now"/> if its author has spoken recently
        /// </summary>
        public void CheckDueReminders(DateTime now)
        {
            lock (_checkLock)
            {
                try
                {
                    foreach (var old in _lastSpoke.Where(s => now - s.Value > ActiveTime))
                    {
                        _lastSpoke.TryRemove(old);
                    }

                    using IServiceScope scope = _scopeFactory.CreateScope();
                    var data = scope.ServiceProvider.GetRequiredService<IGthxData>();

                    foreach (var reminder in data.GetDueReminders(now))
                    {
                        if (!_lastSpoke.ContainsKey(reminder.Author.ToLowerInvariant()) || !_client.IsUserOnline(reminder.Author))
                        {
                            continue;
                        }

                        // Only the one that deletes it delivers it, in case it was just delivered as they spoke.
                        if (!data.DeleteReminder(reminder.Id))
                        {
                            continue;
                        }

                        var target = Target(reminder);
                        _logger.LogInformation("Delivering reminder {Id} to {User} in {Channel}", reminder.Id, reminder.Author, target);
                        _client.SendMessage(target, DeliveryMessage(reminder, _util, now));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failure while delivering reminders");
                }
            }
        }

        /// <summary>
        /// Where to deliver a reminder: the channel it was asked for in if they're still there,
        /// otherwise a private message
        /// </summary>
        private string Target(Reminder reminder)
        {
            if (reminder.Channel.StartsWith('#'))
            {
                // This also checks that the bot is still in the channel.
                var users = _client.GetUsersInChannelAsync(reminder.Channel).Result;
                if (users.Any(u => string.Equals(u, reminder.Author, StringComparison.OrdinalIgnoreCase)))
                {
                    return reminder.Channel;
                }
            }

            return reminder.Author;
        }

        public void Dispose()
        {
            _timer.Dispose();
        }
    }
}
