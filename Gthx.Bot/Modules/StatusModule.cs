using Gthx.Bot.Interfaces;
using Gthx.Data;
using System;

namespace Gthx.Bot.Modules
{
    public class StatusModule : IGthxModule
    {
        private readonly IGthxData _Data;
        private readonly IIrcClient _IrcClient;
        private readonly DateTime _StartTime = DateTime.UtcNow;

        public StatusModule(IGthxData data, IIrcClient ircClient)
        {
            this._Data = data;
            this._IrcClient = ircClient;
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

            var moodValue = _Data.GetMood();
            var moodString = MoodToString(moodValue);
            _IrcClient.SendMessage(channel, $"{GthxBot.Version}: OK; Up for {Util.TimeBetweenString(_StartTime)}; mood: {moodString}");
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
