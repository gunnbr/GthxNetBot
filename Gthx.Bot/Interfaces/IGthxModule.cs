using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Gthx.Bot.Interfaces
{
    interface IGthxModule
    {
        /// <summary>
        /// Evaluate a received message and return a response if the module
        /// is able to handle the message.
        /// </summary>
        /// <param name="channel">Channel the message was received on</param>
        /// <param name="user">Nick of the user who sent the message</param>
        /// <param name="message">Text of the message that was sent</param>
        /// <returns>IrcResponse to reply if the message was handled or null otherwise</returns>
        public void ProcessMessage(string channel, string user, string message);
    }
}
