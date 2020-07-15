using Gthx.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Gthx.Core.Modules
{
    public class FactoidModule : IGthxModule
    {
        private readonly Regex _FactoidSet = new Regex(@"(?'factoid'.+?)\s(?'isAre'is|are)(?'hasAlso'\salso)?\s(?'value'.+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private readonly Regex _InvalidRegex = new Regex(@"(here|how|it|something|that|this|what|when|where|which|who|why|you)", RegexOptions.IgnoreCase);
        private readonly Regex _FactoidGet = new Regex(@$"(?'factoid'.+)[?!](?'hasPipe'\s*$|\s*\|\s*(?'pipeToUser'{Util.NickMatch})$)", RegexOptions.Compiled);
        private IGthxData _Data;

        public FactoidModule(IGthxData data)
        {
            _Data = data;
        }

        public List<IrcResponse> ProcessMessage(string channel, string user, string message)
        {
            var response = ProcessFactoidGet(channel, user, message);
            if (response != null)
            {
                return new List<IrcResponse> { response };
            }

            // TODO: Implement info handling

            // TODO: Implement forget handling

            response = ProcessFactoidSet(channel, user, message);
            if (response != null)
            {
                return new List<IrcResponse> { response };
            }

            return null;
        }

        /// <summary>
        /// Handles setting a factoid
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="user"></param>
        /// <param name="message"></param>
        /// <returns>An IrcResponse if a command was found to set a message, null otherwise</returns>
        private IrcResponse ProcessFactoidSet(string channel, string user, string message)
        {
            var factoidMatch = _FactoidSet.Match(message);
            if (!factoidMatch.Success)
            {
                return null;
            }

            var invalidMatch = _InvalidRegex.Match(factoidMatch.Groups[1].ToString());
            if (invalidMatch.Success)
            {
                return null;
            }

            var factoidName = factoidMatch.Groups["factoid"].Value;
            var isAre = factoidMatch.Groups["isAre"].Value.Equals("are", StringComparison.CurrentCultureIgnoreCase);
            var hasAlso = factoidMatch.Groups["hasAlso"].Success;
            var value = factoidMatch.Groups["value"].Value;
            var success = _Data.AddFactoid(user, factoidName, isAre, value, !hasAlso);
            if (success)
            {
                return new IrcResponse($"{user}: Okay.");
            }

            return new IrcResponse($"I'm sorry, {user}. I'm afraid I can't do that.");
        }

        /// <summary>
        /// Handles getting a factoid
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="user"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        private IrcResponse ProcessFactoidGet(string channel, string user, string message)
        {
            var factoidMatch = _FactoidGet.Match(message);
            if (!factoidMatch.Success)
            {
                return null;
            }

            var factoid = factoidMatch.Groups["factoid"].Value;
            Console.WriteLine($"Factoid query from {user}:{channel} for '{factoid}'");

            var factoidValueList = _Data.GetFactoid(factoid);
            if (factoidValueList == null)
            {
                return null;
            }

            var factoidValue = string.Join(" and also ", factoidValueList.Select(f => f.Value));
            var article = factoidValueList[0].IsAre ? "are" : "is";
            factoidValue = factoidValue.Replace("!who", user);
            factoidValue = factoidValue.Replace("!channel", channel);

            if (factoidValue.StartsWith("<reply>"))
            {
                return new IrcResponse(factoidValue.Remove(0,7));
            }

            if (factoidValue.StartsWith("<action>"))
            {
                return new IrcResponse(factoidValue.Remove(0,8), ResponseType.Action);
            }

            if (factoidMatch.Groups["pipeToUser"].Success)
            {
                var pipeToUser = factoidMatch.Groups["pipeToUser"];
                return new IrcResponse($"{pipeToUser}, {factoid} {article} {factoidValue}");
            }

            return new IrcResponse($"{factoid} {article} {factoidValue}");
        }
    }
}
