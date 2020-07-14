using Gthx.Core;
using Gthx.Core.Interfaces;
using System;
using System.Collections.Generic;

namespace Gthx.Test.Mocks
{
    public class MockData : IGthxData
    {
        public string FactoidUser { get; private set; }
        public string FactoidItem { get; private set; }
        public bool FactoidIsAre { get; private set; }
        public string FactoidValue { get; private set; }
        public bool FactoidReplaceExisting { get; private set; }
        public string FactoidGotten { get; private set; }

        public string TellFromUser { get; private set; }
        public string TellToUser { get; private set; }
        public string TellMessage { get; private set; }
        public string TellCheckUser { get; private set; }

        public bool AddFactoid(string user, string factoid, bool isAre, string value, bool replaceExisting)
        {
            FactoidUser = user;
            FactoidItem = factoid;
            FactoidIsAre = isAre;
            FactoidValue = value;
            FactoidReplaceExisting = replaceExisting;
            return true;
        }

        public bool AddTell(string fromUser, string toUser, string message)
        {
            TellFromUser = fromUser;
            TellToUser = toUser;
            TellMessage = message;
            return true;
        }

        public bool AddThingiverseReference(string item)
        {
            throw new NotImplementedException();
        }

        public bool AddThingiverseTitle(string item, string title)
        {
            throw new NotImplementedException();
        }

        public bool AddYoutubeReference(string item)
        {
            throw new NotImplementedException();
        }

        public bool AddYoutubeTitle(string item, string title)
        {
            throw new NotImplementedException();
        }

        public bool ForgetFactoid(string user, string factoid)
        {
            throw new NotImplementedException();
        }

        private Factoid CreateFactoid(string factoid, string value, bool isAre = false)
        {
            return new Factoid
            {
                Name = factoid,
                Value = value,
                IsAre = isAre,
                SetByUser = "MockData",
                Timestamp = DateTime.UtcNow
            };
        }

        public List<Factoid> GetFactoid(string factoid)
        {
            FactoidGotten = factoid;

            switch (factoid)
            {
                case "reprap":
                    return new List<Factoid>
                    {
                        CreateFactoid("reprap", "the best way to learn about 3D printing")
                    };
                case "cake":
                    return new List<Factoid>
                    {
                        CreateFactoid("cake", "really yummy"),
                        CreateFactoid("cake", "a lie")
                    };
                case "emoji":
                    return new List<Factoid>
                    {
                        CreateFactoid("emoji","handled well: 😍🍕🎉💪")
                    };
                case "other languages":
                    return new List<Factoid>
                    {
                        CreateFactoid("other languages","このアプリケーションで十分にサポートされています", true)
                    };
                case "pennies":
                    return new List<Factoid>
                    {
                        CreateFactoid("pennies", "small coins", true)
                    };
                case "botsmack":
                    return new List<Factoid>
                    {
                        CreateFactoid("botsmack","<reply>!who, stop that!")
                    };
                case "lost":
                    return new List<Factoid>
                    {
                        CreateFactoid("lost","<reply>!who, you're in !channel")
                    };
                case "dance":
                    return new List<Factoid>
                    {
                        CreateFactoid("dance","<action>dances a little jig around !who.")
                    };
            }

            return null;
        }

        public List<string> GetFactoidInfo(string factoid)
        {
            throw new NotImplementedException();
        }

        public void GetLastSeen(string user)
        {
            throw new NotImplementedException();
        }

        public int GetMood()
        {
            throw new NotImplementedException();
        }

        public List<Tell> GetTell(string forUser)
        {
            TellCheckUser = forUser;
            switch (forUser)
            {
                case "CrashOverride":
                    return new List<Tell> 
                    {
                        new Tell("AcidBurn", "CrashOverride", "Mess with the best, die like the rest.", new DateTime(1995, 11, 4, 23, 49, 13)) 
                    };

                case "gunnbr":
                    return new List<Tell>
                    {
                        new Tell("JimmyRockets", "gunnbr", "Can you fix a gthx bug?", new DateTime(2015, 10, 8, 12, 30, 32)),
                        new Tell("PaulBunyan", "gunnbr", "Do you need any help with emoji 🧑🏿😨🍦?", new DateTime(2015, 10, 9, 3, 34, 43)),
                    };
            }

            return new List<Tell>();
        }

        public void UpdateLastSeen(string user, string channel, string message)
        {
            throw new NotImplementedException();
        }
    }
}
