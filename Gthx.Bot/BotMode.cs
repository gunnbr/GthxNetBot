using System;
using System.Collections.Generic;

namespace Gthx.Bot
{
    /// <summary>
    /// Finds the user mode a server uses to mark clients as bots.
    /// See https://ircv3.net/specs/extensions/bot-mode
    /// </summary>
    public static class BotMode
    {
        /// <summary>
        /// Gets the bot mode letter from the server's RPL_ISUPPORT values (the "BOT" token, e.g. BOT=B on Libera).
        /// </summary>
        /// <param name="isupport">The RPL_ISUPPORT names and values the server sent</param>
        /// <returns>The mode letter, or null if the server doesn't support bot mode</returns>
        public static char? GetModeLetter(IEnumerable<KeyValuePair<string, string?>>? isupport)
        {
            if (isupport == null)
            {
                return null;
            }

            foreach (var (name, value) in isupport)
            {
                if (string.Equals(name, "BOT", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(value))
                {
                    return value[0];
                }
            }

            return null;
        }
    }
}
