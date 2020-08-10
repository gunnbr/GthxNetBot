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
        /// <param name="wasDirectlyAddressed">True if the bot was directly addressed for this message</param>
        /// <returns>True if the message was handled in a final way such that no more modules should be called</returns>
        public bool ProcessMessage(string channel, string user, string message, bool wasDirectlyAddressed);

        /// <summary>
        /// Evaluate a received action and act on it if necessary
        /// </summary>
        /// <param name="channel">Channel the message was received on</param>
        /// <param name="user">Nick of the user who sent the message</param>
        /// <param name="message">Text of the action message that was sent</param>
        /// <returns>True if the message was handled in a final way such that no more modules should be called</returns>
        public bool ProcessAction(string channel, string user, string message);
    }
}
