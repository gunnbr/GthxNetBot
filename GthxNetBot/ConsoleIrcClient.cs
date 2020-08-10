using Gthx.Bot.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GthxNetBot
{
    public class ConsoleIrcClient : IIrcClient, IBotNick
    {
        public string BotNick => "consolebot";
        public event EventHandler BotNickChangedEvent;

        public bool SendAction(string channel, string message)
        {
            Console.WriteLine($"{channel}: * {BotNick} {message}");
            return true;
        }

        public Task<List<string>> GetUsersInChannelAsync(string channel)
        {
            return Task.Run(() =>
            {
                var users = new List<string>
                {
                    "gunnbr",
                    BotNick,
                    "LurkerBot"
                };
                return users;
            });
        }

        public bool SendMessage(string channel, string message)
        {
            Console.WriteLine($"{channel}: {BotNick}> {message}");
            return true;
        }
    }
}
