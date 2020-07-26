using System;
using System.Text;

namespace Gthx.Bot
{
    public class Util
    {
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
        public static string TimeBetweenString(DateTime? firstTime, DateTime? secondTime = null)
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
    }
}
