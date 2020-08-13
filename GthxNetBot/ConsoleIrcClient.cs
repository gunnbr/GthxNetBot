using Gthx.Bot.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GthxNetBot
{
    public class ConsoleIrcClient : IIrcClient
    {
        private readonly IBotNick _botNick;

        public ConsoleIrcClient(IBotNick botNick)
        {
            _botNick = botNick;
        }

        public bool SendAction(string channel, string message)
        {
            Console.WriteLine($"{channel}: * {_botNick.BotNick} {message}");
            return true;
        }

        public Task<List<string>> GetUsersInChannelAsync(string channel)
        {
            return Task.Run(() =>
            {
                var users = new List<string>
                {
                    "gunnbr",
                    _botNick.BotNick,
                    "LurkerBot"
                };
                return users;
            });
        }

        public bool SendMessage(string channel, string message)
        {
            Console.WriteLine($"{channel}: {_botNick.BotNick}> {message}");
            return true;
        }
    }
}
