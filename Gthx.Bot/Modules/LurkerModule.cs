using Gthx.Bot.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Gthx.Bot.Modules
{
    public class LurkerModule : IGthxModule
    {
        public void ProcessAction(string channel, string user, string message)
        {
        }

        public void ProcessMessage(string channel, string user, string message)
        {
            // TODO: Implement lurker module
            throw new NotImplementedException();
        }
    }
}
