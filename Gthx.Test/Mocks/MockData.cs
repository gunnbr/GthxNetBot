using Gthx.Core;
using Gthx.Data;
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

        // TODO: Don't we need these fields for Thingiverse too?
        public string AddedYoutubeId { get; private set; }
        public string AddedYoutubeTitle { get; private set; }

        public string ForgettingUser { get; private set; }
        public string ForgottenFactoid { get; private set; }

        public string InfoFactoid { get; private set; }

        public string LastSeenChannel { get; private set; }
        public string LastSeenUser { get; private set; }
        public string LastSeenMessage { get; private set; }
        public DateTime LastSeenTimestamp { get; private set; }
        public string LastSeenUserQuery { get; private set; }
        public string AddedThingiverseId { get; private set; }
        public string AddedThingiverseTitle { get; private set; }

        public bool AddFactoid(string user, string factoid, bool isAre, string value, bool replaceExisting)
        {
            FactoidUser = user;
            FactoidItem = factoid;
            FactoidIsAre = isAre;
            FactoidValue = value;
            FactoidReplaceExisting = replaceExisting;
            return true;
        }

        public bool IsFactoidLocked(string factoid)
        {
            return factoid == "locked factoid";
        }

        public bool AddTell(string fromUser, string toUser, string message)
        {
            TellFromUser = fromUser;
            TellToUser = toUser;
            TellMessage = message;
            return true;
        }

        public ThingiverseRef AddThingiverseReference(string item)
        {
            var success = int.TryParse(item, out var intItem);
            if (!success)
            {
                return null;
            }

            switch (item)
            {
                case "2823006":
                    return new ThingiverseRef
                    {
                        Item = intItem,
                        Count = 42,
                        Title = "Air Spinner"
                    };

                case "1276095":
                    return new ThingiverseRef
                    {
                        Item = intItem,
                        Count = 23,
                        Title = "Flexifish 🦈🌊"
                    };
            }

            return new ThingiverseRef
            {
                Item = intItem,
                Count = 1
            };
        }

        public void AddThingiverseTitle(string item, string title)
        {
            AddedThingiverseId = item;
            AddedThingiverseTitle = title;
        }

        public YoutubeRef AddYoutubeReference(string item)
        {
            switch (item)
            {
                case "ykKIZQKaT5c":
                    return new YoutubeRef
                    {
                        Item = item,
                        Count = 42,
                        Title = "Spinner"
                    };

                case "W3B2C0nNpFU":
                    return new YoutubeRef
                    {
                        Item = item,
                        Count = 83,
                        Title = "Best relaxing piano studio ghibli complete collection ピアノスタジオジブリコレクション"
                    };
            }

            return new YoutubeRef
            {
                Item = item,
                Count = 1
            };
        }

        public void AddYoutubeTitle(string item, string title)
        {
            AddedYoutubeId = item;
            AddedYoutubeTitle = title;
        }

        public bool ForgetFactoid(string user, string factoid)
        {
            if (factoid == "locked factoid")
            {
                return false;
            }

            ForgettingUser = user;
            ForgottenFactoid = factoid;
            return true;
        }

        private Factoid CreateFactoid(string factoid, string value, bool isAre = false)
        {
            return new Factoid
            {
                Item = factoid,
                Value = value,
                IsAre = isAre,
                User = "MockData",
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

        public FactoidInfoReply GetFactoidInfo(string factoid)
        {
            InfoFactoid = factoid;

            switch (factoid)
            {
                case "cake":
                    return new FactoidInfoReply()
                    {
                        RefCount = 176,
                        InfoList = new List<FactoidHistory>
                        {
                            new FactoidHistory() { User = "GLaDOS", Value = "delicious", Timestamp = new DateTime(2007, 10, 10, 8, 0, 0)},
                            new FactoidHistory() { User = "Chell", Value = null, Timestamp = new DateTime(2007, 10, 10, 14, 34, 53)},
                            new FactoidHistory() { User = "UnknownEntity", Value = "a lie!", Timestamp = new DateTime(2007, 10, 10, 14, 34, 53)},
                            new FactoidHistory() { User = null, Value = "delicious", Timestamp = new DateTime(2007, 10, 10, 14, 34, 55)},
                        }
                    };
            }

            return null;
        }

        public List<Seen> GetLastSeen(string user)
        {
            LastSeenUserQuery = user;

            switch (user)
            {
                case "gunnbr":
                    return new List<Seen>()
                    {
                        new Seen() { Channel = "#gthxtest", User = "gunnbr", Message = "gthx: status?", Timestamp = new DateTime(2020, 7, 23, 8, 23, 43)},
                        new Seen() { Channel = "#reprap", User = "gunnbr_", Message = "Yeah, I'm trying to fix that.", Timestamp = new DateTime(2020, 2, 3, 13, 44, 1)}
                    };
                case "Razor":
                    return new List<Seen>()
                    {
                        new Seen() { Channel = "#twitch", User = "Razor", Message = "Stream is starting NOW! Tune in!", Timestamp = new DateTime(2020, 7, 24, 6, 52, 11)},
                    };
                case "The":
                    return new List<Seen>()
                    {
                        new Seen() { Channel = "#openscad", User = "TheHelper", Message = "Just get rid of that let statement.", Timestamp = new DateTime(2020, 7, 14, 22, 3, 15)},
                        new Seen() { Channel = "#leets", User = "ThePlague", Message = "Which one of you losers thinks you can beat me?", Timestamp = new DateTime(1995, 9, 15, 22, 3, 15)},
                        new Seen() { Channel = "#superherohigh", User = "Themyscira", Message = "Hey everyone, come visit!", Timestamp = new DateTime(2020, 7, 20, 5, 6, 7)},
                        new Seen() { Channel = "#TheMatrix", User = "TheOne", Message = "Whoah", Timestamp = new DateTime(1999, 3, 31, 14, 0, 0)},
                        new Seen() { Channel = "#reprap", User = "TheOwner", Message = "Don't make me kick you.", Timestamp = new DateTime(2020, 7, 17, 13, 31, 32)},
                    };
            }

            return null;
        }

        public int GetMood()
        {
            return 4;
        }

        public List<Tell> GetTell(string forUser)
        {
            TellCheckUser = forUser;
            switch (forUser)
            {
                case "CrashOverride":
                    return new List<Tell> 
                    {
                        new Tell() { Author = "AcidBurn", Recipient = "CrashOverride", Message = "Mess with the best, die like the rest.", Timestamp = new DateTime(1995, 11, 4, 23, 49, 13)}
                    };

                case "gunnbr":
                    return new List<Tell>
                    {
                        new Tell() { Author = "JimmyRockets", Recipient = "gunnbr", Message = "Can you fix a gthx bug?", Timestamp = new DateTime(2015, 10, 8, 12, 30, 32) },
                        new Tell() { Author = "PaulBunyan", Recipient = "gunnbr", Message = "Do you need any help with emoji 🧑🏿😨🍦?", Timestamp = new DateTime(2015, 10, 9, 3, 34, 43) },
                    };
            }

            return new List<Tell>();
        }

        public void UpdateLastSeen(string channel, string user, string message)
        {
            LastSeenChannel = channel;
            LastSeenUser = user;
            LastSeenMessage = message;
            LastSeenTimestamp = DateTime.UtcNow;
        }
    }
}
