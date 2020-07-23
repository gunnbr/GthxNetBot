using Gthx.Core.Interfaces;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace Gthx.Core.Modules
{
    public class FactoidModule : IGthxModule
    {
        private readonly Regex _FactoidSet = new Regex(@"(?'factoid'.+?)\s(?'isAre'is|are)(?'hasAlso'\salso)?\s(?'value'.+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private readonly Regex _InvalidRegex = new Regex(@"(here|how|it|something|that|this|what|when|where|which|who|why|you)", RegexOptions.IgnoreCase);
        private readonly Regex _FactoidGet = new Regex(@$"(?'factoid'.+)[?!](?'hasPipe'\s*$|\s*\|\s*(?'pipeToUser'{Util.NickMatch})$)", RegexOptions.Compiled);
        private IGthxData _Data;
        private readonly IIrcClient _IrcClient;

        public FactoidModule(IGthxData data, IIrcClient ircClient)
        {
            _Data = data;
            this._IrcClient = ircClient;
        }

        public void ProcessMessage(string channel, string user, string message)
        {
            var wasProcessed = ProcessFactoidGet(channel, user, message);
            if (wasProcessed)
            {
                return;
            }

            wasProcessed = ProcessFactoidInfo(channel, user, message);
            if (wasProcessed)
            {
                return;
            }

            wasProcessed = ProcessFactoidForget(channel, user, message);
            if (wasProcessed)
            {
                return;
            }

            ProcessFactoidSet(channel, user, message);
        }

        /// <summary>
        /// Handles forgetting a factoid
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="user"></param>
        /// <param name="message"></param>
        /// <returns>True if a command was found to set a message, false otherwise</returns>
        private bool ProcessFactoidForget(string channel, string user, string message)
        {
            if (!message.StartsWith("forget "))
            {
                return false;
            }

            var factoid = message.Remove(0, 7);
            Console.WriteLine($"forget request for '{factoid}'");

            var isForgotten = _Data.ForgetFactoid(user, factoid);
            if (isForgotten)
            {
                _IrcClient.SendMessage(channel, $"{user}: I've forgotten about {factoid}");
            }
            else
            {
                _IrcClient.SendMessage(channel, $"{user}: Okay, but {factoid} didn't exist anyway");
            }

            return true;
        }

        /// <summary>
        /// Handles factoid info
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="user"></param>
        /// <param name="message"></param>
        /// <returns>True if a command was found to get info for a factoid, false otherwise</returns>
        private bool ProcessFactoidInfo(string channel, string user, string message)
        {
            if (!message.StartsWith("info "))
            {
                return false;
            }

            var factoid = message.Remove(0, 5);
            Console.WriteLine($"info request for '{factoid}' in {channel}");

            var info = _Data.GetFactoidInfo(factoid);
            if (info == null)
            {
                Console.WriteLine($"No info for factoid '{factoid}'");
                _IrcClient.SendMessage(channel, $"Sorry, I couldn't find an entry for {factoid}");
                return true;
            }

            Console.WriteLine($"Factoid '{factoid}' has been referenced {info.RefCount} times");
            _IrcClient.SendMessage(channel, $"Factoid '{factoid}' has been referenced {info.RefCount} times");
            foreach (var data in info.InfoList)
            {
                if (string.IsNullOrWhiteSpace(data.User))
                {
                    data.User = "Unknown";
                }

                if (data.Value == null)
                {
                    Console.WriteLine($"At {data.Timestamp}, {data.User} deleted this item");
                    _IrcClient.SendMessage(channel, $"At {data.Timestamp:R}, {data.User} deleted this item");
                }
                else
                {
                    Console.WriteLine($"At {data.Timestamp}, {data.User} set to: {data.Value}");
                    _IrcClient.SendMessage(channel, $"At {data.Timestamp:R}, {data.User} set to: {data.Value}");
                }
            }

            return true;
        }

        /// <summary>
        /// Handles setting a factoid
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="user"></param>
        /// <param name="message"></param>
        /// <returns>True if a command was found to set a message, false otherwise</returns>
        private bool ProcessFactoidSet(string channel, string user, string message)
        {
            var factoidMatch = _FactoidSet.Match(message);
            if (!factoidMatch.Success)
            {
                return false;
            }

            var invalidMatch = _InvalidRegex.Match(factoidMatch.Groups[1].ToString());
            if (invalidMatch.Success)
            {
                return false;
            }

            var factoidName = factoidMatch.Groups["factoid"].Value;
            var isAre = factoidMatch.Groups["isAre"].Value.Equals("are", StringComparison.CurrentCultureIgnoreCase);
            var hasAlso = factoidMatch.Groups["hasAlso"].Success;
            var value = factoidMatch.Groups["value"].Value;
            var success = _Data.AddFactoid(user, factoidName, isAre, value, !hasAlso);
            if (success)
            {
                _IrcClient.SendMessage(channel, $"{user}: Okay.");
            }
            else
            {
                _IrcClient.SendMessage(channel, $"I'm sorry, {user}. I'm afraid I can't do that.");
            }

            return true;
        }

        /// <summary>
        /// Handles getting a factoid
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="user"></param>
        /// <param name="message"></param>
        /// <returns>True if a factoid get message was detected and handled, false otherwise</returns>
        private bool ProcessFactoidGet(string channel, string user, string message)
        {
            var factoidMatch = _FactoidGet.Match(message);
            if (!factoidMatch.Success)
            {
                return false;
            }

            var factoid = factoidMatch.Groups["factoid"].Value;
            Console.WriteLine($"Factoid query from {user}:{channel} for '{factoid}'");

            var factoidValueList = _Data.GetFactoid(factoid);
            if (factoidValueList == null)
            {
                return false;
            }

            var factoidValue = string.Join(" and also ", factoidValueList.Select(f => f.Value));
            var article = factoidValueList[0].IsAre ? "are" : "is";
            factoidValue = factoidValue.Replace("!who", user);
            factoidValue = factoidValue.Replace("!channel", channel);

            if (factoidValue.StartsWith("<reply>"))
            {
                _IrcClient.SendMessage(channel, factoidValue.Remove(0, 7));
                return true;
            }

            if (factoidValue.StartsWith("<action>"))
            {
                _IrcClient.SendAction(channel, factoidValue.Remove(0, 8));
                return true;
            }

            if (factoidMatch.Groups["pipeToUser"].Success)
            {
                var pipeToUser = factoidMatch.Groups["pipeToUser"];
                _IrcClient.SendMessage(channel, $"{pipeToUser}, {factoid} {article} {factoidValue}");
                return true;
            }

            _IrcClient.SendMessage(channel, $"{factoid} {article} {factoidValue}");
            return true;
        }
    }
}
