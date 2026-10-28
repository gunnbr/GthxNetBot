using System;
using System.Globalization;

namespace Gthx.Bot
{
    /// <summary>
    /// Parses CTCP requests (a message wrapped in \x01) and builds the replies the bot sends back as a NOTICE.
    /// See https://modern.ircdocs.horse/ctcp
    /// </summary>
    public static class Ctcp
    {
        public const char Delimiter = '\u0001';

        public const string SourceUrl = "https://github.com/gunnbr/GthxNetBot";

        /// <summary>
        /// Requests the bot answers, as listed in its CLIENTINFO reply
        /// </summary>
        public static readonly string[] SupportedCommands = { "ACTION", "CLIENTINFO", "PING", "SOURCE", "TIME", "VERSION" };

        /// <summary>
        /// Splits a CTCP message into its command (upper case) and parameters.
        /// </summary>
        /// <param name="text">Text of the message</param>
        /// <param name="command">The command, such as "VERSION"</param>
        /// <param name="parameters">Everything after the command, or an empty string</param>
        /// <returns>True if the text is a CTCP message</returns>
        public static bool TryParse(string text, out string command, out string parameters)
        {
            command = "";
            parameters = "";
            if (string.IsNullOrEmpty(text) || text[0] != Delimiter)
            {
                return false;
            }

            // The closing delimiter is optional.
            var body = text[1..];
            if (body.EndsWith(Delimiter))
            {
                body = body[..^1];
            }

            var space = body.IndexOf(' ');
            command = (space < 0 ? body : body[..space]).ToUpperInvariant();
            parameters = space < 0 ? "" : body[(space + 1)..];
            return command.Length > 0;
        }

        /// <summary>
        /// Builds the reply to a CTCP request, without the delimiters.
        /// </summary>
        /// <param name="command">The request's command, from TryParse</param>
        /// <param name="parameters">The request's parameters, from TryParse</param>
        /// <param name="nowUtc">The current time, for TIME</param>
        /// <returns>The reply, or null if the bot doesn't answer this request (ACTION, or an unknown command)</returns>
        public static string? GetReply(string command, string parameters, DateTime nowUtc)
        {
            switch (command)
            {
                case "CLIENTINFO":
                    return $"CLIENTINFO {string.Join(' ', SupportedCommands)}";
                case "PING":
                    // The parameters are echoed back, so don't let them end the reply early.
                    var token = parameters.Replace(Delimiter.ToString(), "");
                    return token.Length == 0 ? "PING" : $"PING {token}";
                case "SOURCE":
                    return $"SOURCE {SourceUrl}";
                case "TIME":
                    return $"TIME {nowUtc.ToString("R", CultureInfo.InvariantCulture)}";
                case "VERSION":
                    return $"VERSION Gthx Bot Version {GthxBot.Version}";
                default:
                    return null;
            }
        }
    }
}
