using Gthx.Bot.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Gthx.Bot.Modules
{
    public class LurkerModule : IGthxModule
    {
        public bool ProcessAction(string channel, string user, string message)
        {
            return false;
        }

        public bool ProcessMessage(string channel, string user, string message, bool wasDirectlyAddressed)
        {
            // TODO: Implement lurker module
            throw new NotImplementedException();
        }
    }
}
