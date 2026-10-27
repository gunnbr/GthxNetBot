using System;
using System.Text.RegularExpressions;

namespace Gthx.Bot
{
    /// <summary>
    /// Parses the relative times used by reminders, such as "12 hours", "an hour", "1h30m" or "2 days and 3 hours".
    /// Units are minutes, hours, days, weeks, months (30 days) and years (365 days).
    /// </summary>
    public static class ReminderTime
    {
        private const string Minutes = @"minutes?|mins?|m";
        private const string Hours = @"hours?|hrs?|h";
        private const string Days = @"days?|d";
        private const string Weeks = @"weeks?|wks?|w";
        private const string Months = @"months?|mos?";
        private const string Years = @"years?|yrs?|y";

        // "a"/"an" only goes with whole words ("an hour"), so a message such as "a meeting" isn't read as a time.
        private const string Part = @$"(?:\d{{1,6}}\s*(?:{Months}|{Minutes}|{Hours}|{Days}|{Weeks}|{Years})|an?\s+(?:minute|hour|day|week|month|year))(?![a-z])";

        /// <summary>
        /// Regex pattern for a whole time: one or more parts, optionally separated by spaces, commas or "and"
        /// </summary>
        public const string Pattern = @$"{Part}(?:(?:\s*,\s*|\s+and\s+|\s*){Part})*";

        private static readonly Regex _wholeRegex = new($@"^\s*{Pattern}\s*$", RegexOptions.IgnoreCase);
        private static readonly Regex _partRegex = new(@"(?'count'\d+|\ban?(?=\s))\s*(?'unit'[a-z]+)", RegexOptions.IgnoreCase);

        /// <summary>
        /// Parses a relative time
        /// </summary>
        /// <param name="text">Text of the time, such as "2 hours and 30 minutes"</param>
        /// <param name="time">The length of time, if it could be parsed</param>
        /// <returns>True if the whole text was a time, false otherwise</returns>
        public static bool TryParse(string text, out TimeSpan time)
        {
            time = TimeSpan.Zero;
            if (!_wholeRegex.IsMatch(text))
            {
                return false;
            }

            foreach (Match part in _partRegex.Matches(text))
            {
                var countText = part.Groups["count"].Value;
                var count = char.IsDigit(countText[0]) ? int.Parse(countText) : 1;
                var unit = part.Groups["unit"].Value.ToLowerInvariant();
                time += unit[0] switch
                {
                    // A month is taken as 30 days and a year as 365.
                    'm' when unit.StartsWith("mo") => TimeSpan.FromDays(30.0 * count),
                    'm' => TimeSpan.FromMinutes(count),
                    'h' => TimeSpan.FromHours(count),
                    'd' => TimeSpan.FromDays(count),
                    'w' => TimeSpan.FromDays(7.0 * count),
                    _ => TimeSpan.FromDays(365.0 * count),
                };
            }

            return true;
        }
    }
}
