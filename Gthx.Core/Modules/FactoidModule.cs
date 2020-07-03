using Gthx.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Gthx.Core.Modules
{
    public class FactoidModule : IGthxModule
    {
        private Regex _FactoidRegex = new Regex(@"(?'factoid'.+?)\s(?'isAre'is|are)(?'hasAlso'\salso)?\s(?'value'.+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private Regex _InvalidRegex = new Regex(@"(here|how|it|something|that|this|what|when|where|which|who|why|you)", RegexOptions.IgnoreCase);
        private IGthxData _Data;

        public FactoidModule(IGthxData data)
        {
            _Data = data;
        }


        public string ProcessMessage(string channel, string user, string message)
        {
            var factoidMatch = _FactoidRegex.Match(message);
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
                return $"{user}: Okay.";
            }

            return $"I'm sorry, {user}. I'm afraid I can't do that.";
        }
    }
}
