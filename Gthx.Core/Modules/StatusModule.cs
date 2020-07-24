using Gthx.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Gthx.Core.Modules
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

        public void ProcessMessage(string channel, string user, string message)
        {
            if (message != "status?")
            {
                return;
            }

            var moodValue = _Data.GetMood();
            var moodString = MoodToString(moodValue);
            _IrcClient.SendMessage(channel, $"{Gthx.Version}: OK; Up for {Util.TimeBetweenString(_StartTime)}; mood: {moodString}");
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
