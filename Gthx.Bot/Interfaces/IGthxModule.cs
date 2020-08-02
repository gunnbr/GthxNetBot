using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Gthx.Bot.Interfaces
{
    public interface IGthxModule
    {
        /// <summary>
        /// Evaluate a received message and act of it if necessary.
        /// </summary>
        /// <param name="channel">Channel the message was received on</param>
        /// <param name="user">Nick of the user who sent the message</param>
        /// <param name="message">Text of the message that was sent</param>
        public void ProcessMessage(string channel, string user, string message);

        /// <summary>
        /// Evaluate a received action and act on it if necessary
        /// </summary>
        /// <param name="channel">Channel the message was received on</param>
        /// <param name="user">Nick of the user who sent the message</param>
        /// <param name="message">Text of the action message that was sent</param>
        public void ProcessAction(string channel, string user, string message);
    }
}
