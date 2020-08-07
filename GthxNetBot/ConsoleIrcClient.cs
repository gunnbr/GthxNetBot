using Gthx.Bot.Interfaces;
using System;

namespace GthxNetBot
{
    public class ConsoleIrcClient : IIrcClient
    {
        public string BotNick => "consolebot";

        public bool SendAction(string channel, string message)
        {
            Console.WriteLine($"{channel}: * {BotNick} {message}");
            return true;
        }

        public bool SendMessage(string channel, string message)
        {
            Console.WriteLine($"{channel}: {BotNick}> {message}");
            return true;
        }
    }
}
