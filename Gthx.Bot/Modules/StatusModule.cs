using Gthx.Bot.Interfaces;
using Gthx.Data;
using System;

namespace Gthx.Bot.Modules
{
    public class StatusModule : IGthxModule
    {
        private readonly IGthxData _data;
        private readonly IIrcClient _client;
        private readonly IGthxUtil _util;
        private readonly DateTime _startTime = DateTime.UtcNow;

        public StatusModule(IGthxData data, IIrcClient ircClient, IGthxUtil util)
        {
            _data = data;
            _client = ircClient;
            _util = util;
        }

        public bool ProcessAction(string channel, string user, string message)
        {
            return false;
        }

        public bool ProcessMessage(string channel, string user, string message, bool wasDirectlyAddressed)
        {
            if (message != "status?")
            {
                return false;
            }

            var moodValue = _data.GetMood();
            var moodString = MoodToString(moodValue);
            _client.SendMessage(channel, $"{GthxBot.Version}: OK; Up for {_util.TimeBetweenString(_startTime)}; mood: {moodString}");
            return true;
        }

        private string MoodToString(int mood)
        {
            if (mood < -100)
                return "suicidal!";
            if (mood < -50)
                return "really depressed.";
            if (mood < -10)
                return "depressed.";
            if (mood < 0)
                return "kinda bummed.";
            if (mood == 0)
                return "meh, okay I guess.";
            if (mood < 10)
                return "alright.";
            if (mood < 50)
                return "pretty good.";
            return "great, Great, GREAT!!";
        }
    }
}
