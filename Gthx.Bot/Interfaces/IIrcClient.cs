using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Gthx.Bot.Interfaces
{
    public interface IIrcClient
    {
        /// <summary>
        /// Send a message on the given channel
        /// </summary>
        /// <param name="channel">Channel (or user) to send message to</param>
        /// <param name="message">Message to send</param>
        /// <returns>True if the message was sent successfully, false otherwise</returns>
        public bool SendMessage(string channel, string message);

        /// <summary>
        /// Send an action on the given channel
        /// </summary>
        /// <param name="channel">Channel (or user) to send action to</param>
        /// <param name="action">Action to send</param>
        /// <returns>True if the action was sent successfully, false otherwise</returns>
        public bool SendAction(string channel, string action);

        /// <summary>
        /// Returns a list of the users currently in a given channel
        /// </summary>
        /// <param name="channel">Channel on which to get the list of users</param>
        /// <returns>A list of users in the given channel</returns>
        public Task<List<string>> GetUsersInChannelAsync(string channel);
    }
}
