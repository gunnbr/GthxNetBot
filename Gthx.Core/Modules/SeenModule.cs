using Gthx.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Gthx.Core.Modules
{
    public class SeenModule : IGthxModule
    {
        public List<IrcResponse> ProcessMessage(string channel, string user, string message)
        {
            // TODO: Implement seen module
            throw new NotImplementedException();
        }
    }
}
