using System;
using System.Collections.Generic;
using System.Text;

namespace Gthx.Core.Interfaces
{
    interface IGthxModule
    {
        public string ProcessMessage(string channel, string user, string message);
    }
}
