using Gthx.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace Gthx.Core.Modules
{
    public class GoogleModule : IGthxModule
    {
        private readonly Regex _GoogleRegex = new Regex(@$"\s*google\s+(?'search'.*?)\s+for\s+(?'nick'{Util.NickMatch})");

        public List<IrcResponse> ProcessMessage(string channel, string user, string message)
        {
            var googleMatch = _GoogleRegex.Match(message);
            if (!googleMatch.Success)
            {
                return null;
            }

            var nick = googleMatch.Groups["nick"].Value;
            var search = googleMatch.Groups["search"].Value;
            search = HttpUtility.UrlEncode(search);
            Debug.WriteLine($"{channel}:{user} asked to google '{search}' for {nick}");
            return new List<IrcResponse>
            {
                new IrcResponse($"{nick}: http://lmgtfy.com/?q={search}")
            };
        }

        public Task<List<IrcResponse>> ProcessMessageAsync(string channel, string user, string message)
        {
            throw new NotImplementedException();
        }
    }
}
