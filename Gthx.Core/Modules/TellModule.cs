using Gthx.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Gthx.Core.Modules
{
    public class TellModule : IGthxModule
    {
        public IrcResponse ProcessMessage(string channel, string user, string message)
        {
            // TODO: Implemente tell module
            throw new NotImplementedException();
        }
    }
}
