using Gthx.Bot.Interfaces;
using System;

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

        public bool SendMessage(string channel, string message)
        {
            Console.WriteLine($"{channel}: {BotNick}> {message}");
            return true;
        }
    }
}
