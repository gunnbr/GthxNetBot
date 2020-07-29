using Gthx.Bot.Interfaces;
using System;

namespace GthxNetBot
{
    public class ConsoleIrcClient : IIrcClient
    {
        public bool SendAction(string channel, string message)
        {
            Console.WriteLine($"{channel}: * gthx {message}");
            return true;
        }

        public bool SendMessage(string channel, string message)
        {
            Console.WriteLine($"{channel}: {message}");
            return true;
        }
    }
}
