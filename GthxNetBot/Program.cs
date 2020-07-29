using Gthx.Bot;
using Gthx.Data;
using System;

namespace GthxNetBot
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Welcome to Gthx");

            var client = new ConsoleIrcClient();
            var context = new GthxData.GthxDataContext();
            context.Database.EnsureCreated();
            var data = new GthxSqlData(context);
            var mockReader = new WebReader();
            var gthx = new Gthx.Bot.Gthx(client, data, mockReader);

            var done = false;
            while (!done)
            {
                Console.Write("gunnbr> ");
                try
                {
                    var input = Console.ReadLine();
                    if (input == null)
                    {
                        done = true;
                        continue;
                    }
                    if (input.StartsWith("/me "))
                    {
                        gthx.HandleReceivedAction("#reprap", "gunnbr", input[4..]);
                    }
                    else
                    {
                        gthx.HandleReceivedMessage("#reprap", "gunnbr", input);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Caught exception: {ex}; {ex.Message}");
                    done = true;
                }
            }
        }
    }
}
