using Gthx.Bot.Interfaces;
using Gthx.Data;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Gthx.Bot.Modules
{
    public class ReminderModule : IGthxModule
    {
        /// <summary>
        /// Furthest ahead a reminder can be set
        /// </summary>
        public static readonly TimeSpan MaxTime = TimeSpan.FromDays(365);

        /// <summary>
        /// Most reminders one nick can have waiting
        /// </summary>
        public const int MaxPending = 10;

        /// <summary>
        /// Longest reminder message, the size of the database column
        /// </summary>
        public const int MaxMessageLength = 400;

        /// <summary>
        /// Longest part of a message shown when listing reminders
        /// </summary>
        private const int MaxListedMessageLength = 100;

        private readonly Regex _remindRegex = new(@"^\s*remind\s+me\b", RegexOptions.IgnoreCase);
        // People often put punctuation after the time: "remind me in 7 days, it's Tuesday again!"
        private readonly Regex _remindInRegex = new(@$"^\s*remind\s+me\s+in\s+(?'time'{ReminderTime.Pattern})(?:\s*[,:;-]\s*|\s+)(?'message'\S.*?)\s*$", RegexOptions.IgnoreCase);
        private readonly Regex _listRegex = new(@"^\s*list\s+reminders\s*\??\s*$", RegexOptions.IgnoreCase);
        private readonly Regex _cancelRegex = new(@"^\s*cancel\s+reminder\s+#?(?'id'\d{1,9})\s*$", RegexOptions.IgnoreCase);
        private readonly IGthxData _data;
        private readonly IIrcClient _client;
        private readonly IGthxUtil _util;
        private readonly ReminderScheduler _scheduler;
        private readonly ILogger<ReminderModule> _logger;

        public ReminderModule(IGthxData data, IIrcClient ircClient, IGthxUtil util, ReminderScheduler scheduler, ILogger<ReminderModule> logger)
        {
            _data = data;
            _client = ircClient;
            _util = util;
            _scheduler = scheduler;
            _logger = logger;
        }

        public bool ProcessAction(string channel, string user, string message)
        {
            return false;
        }

        public bool ProcessMessage(string channel, string user, string message, bool wasDirectlyAddressed)
        {
            var now = DateTime.UtcNow;
            _scheduler.UserSpoke(user, now);

            foreach (var reminder in _data.TakeDueReminders(user, now))
            {
                _logger.LogInformation("Delivering held reminder {Id} to {User} in {Channel}", reminder.Id, user, channel);
                _client.SendMessage(channel, ReminderScheduler.DeliveryMessage(reminder, _util, now));
            }

            if (!wasDirectlyAddressed)
            {
                return false;
            }

            if (_listRegex.IsMatch(message))
            {
                ListReminders(channel, user, now);
                return true;
            }

            var cancelMatch = _cancelRegex.Match(message);
            if (cancelMatch.Success)
            {
                var id = int.Parse(cancelMatch.Groups["id"].Value);
                var reply = _data.CancelReminder(user, id) ? $"Reminder {id} cancelled." : $"I don't have a reminder {id} for you.";
                _client.SendMessage(channel, $"{user}: {reply}");
                return true;
            }

            if (!_remindRegex.IsMatch(message))
            {
                return false;
            }

            var remindMatch = _remindInRegex.Match(message);
            if (!remindMatch.Success || !ReminderTime.TryParse(remindMatch.Groups["time"].Value, out var time))
            {
                _logger.LogInformation("{User} in {Channel}: couldn't read the reminder '{Message}'", user, channel, message);
                _client.SendMessage(channel, $"{user}: Say \"remind me in <time> <message>\", such as \"remind me in 2 hours to check the print\".");
                return true;
            }

            AddReminder(channel, user, remindMatch.Groups["message"].Value, time, now);
            return true;
        }

        private void AddReminder(string channel, string user, string message, TimeSpan time, DateTime now)
        {
            if (time < TimeSpan.FromMinutes(1))
            {
                _client.SendMessage(channel, $"{user}: I can only remind you a minute or more from now.");
                return;
            }

            if (time > MaxTime)
            {
                _client.SendMessage(channel, $"{user}: I can only remind you up to a year ahead.");
                return;
            }

            if (message.Length > MaxMessageLength)
            {
                _client.SendMessage(channel, $"{user}: Sorry, that's too long for me to remember ({message.Length} characters; the most is {MaxMessageLength}).");
                return;
            }

            if (_data.GetReminders(user).Count >= MaxPending)
            {
                _client.SendMessage(channel, $"{user}: You already have {MaxPending} reminders waiting. Cancel one first with \"cancel reminder <number>\".");
                return;
            }

            var reminder = _data.AddReminder(user, channel, message, now + time);
            _logger.LogInformation("{User} in {Channel} set reminder {Id} for {Due}", user, channel, reminder.Id, reminder.DueTimestamp);
            _client.SendMessage(channel, $"{user}: Okay, I'll remind you in {_util.TimeBetweenString(now, now + time)} (reminder {reminder.Id}).");
        }

        private void ListReminders(string channel, string user, DateTime now)
        {
            var reminders = _data.GetReminders(user);
            if (!reminders.Any())
            {
                _client.SendMessage(channel, $"{user}: You have no reminders.");
                return;
            }

            foreach (var reminder in reminders)
            {
                var message = reminder.Message.Length > MaxListedMessageLength
                    ? reminder.Message.Substring(0, MaxListedMessageLength) + "..."
                    : reminder.Message;

                // Rounded up to the minute, since the seconds are just noise here.
                var left = reminder.DueTimestamp - now;
                var when = left <= TimeSpan.Zero
                    ? "now"
                    : $"in {_util.TimeBetweenString(now, now + TimeSpan.FromMinutes(Math.Ceiling(left.TotalMinutes)))}";
                _client.SendMessage(channel, $"{user}: {reminder.Id}: {when}: {message}");
            }
        }
    }
}
