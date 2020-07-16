using Gthx.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Gthx.Core.Modules
{
    public class LurkerModule : IGthxModule
    {
        public List<IrcResponse> ProcessMessage(string channel, string user, string message)
        {
            // TODO: Implement lurker module
            throw new NotImplementedException();
        }

        public Task<List<IrcResponse>> ProcessMessageAsync(string channel, string user, string message)
        {
            throw new NotImplementedException();
        }
    }
}
