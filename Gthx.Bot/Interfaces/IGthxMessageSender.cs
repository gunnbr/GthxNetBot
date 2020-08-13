using System;
using System.Collections.Generic;
using System.Text;

namespace Gthx.Bot.Interfaces
{
    // TODO: Seriously refactor this. The names here make it very difficult
    //       to keep track of what's going on!

    /// <summary>
    /// Interface for transferring received messages from the IrcClient
    /// to gthx for processing
    /// </summary>
    public interface IGthxMessageConduit
    {
        public void ReceiveMessage(string channel, string fromUser, string message);
        public void ReceiveAction(string channel, string fromUser, string action);
    }
}
