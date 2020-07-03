using Gthx.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

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
            throw new NotImplementedException();
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

        public List<string> GetFactoid(string factoid)
        {
            FactoidGotten = factoid;

            switch (factoid)
            {
                case "reprap":
                    return new List<string>
                    {
                        "the best way to learn about 3D printing"
                    };

                case "cake":
                    return new List<string>
                    {
                        "really yummy",
                        "a lie!"
                    };
                case "emoji":
                    return new List<string>
                    {
                        "handled well: 😍🍕🎉💪"
                    };
                case "other languages":
                    return new List<string>
                    {
                        "このアプリケーションで十分にサポートされています"
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

        public string GetTell(string user)
        {
            throw new NotImplementedException();
        }

        public void UpdateLastSeen(string user, string channel, string message)
        {
            throw new NotImplementedException();
        }
    }
}
