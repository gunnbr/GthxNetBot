using System;
using System.Collections.Generic;
using System.Text;

namespace Gthx.Core.Interfaces
{
    public interface IIrcClient
    {
        public bool SendMessage(string channel, string message);
    }
}
