using Gthx.Core;
using System;
using System.Collections.Generic;

namespace Gthx.Bot.Interfaces
{
    public class FactoidInfo
    {
        public string User;
        public string Value;
        public DateTime Timestamp;
    }

    public class FactoidInfoReply
    {
        public int RefCount;
        public List<FactoidInfo> InfoList;
    }

    public interface IGthxData
    {
        public List<Seen> GetLastSeen(string user);
        public void UpdateLastSeen(string channel, string user, string message);
        public bool AddFactoid(string user, string factoid, bool isAre, string value, bool replaceExisting);
        public bool IsFactoidLocked(string factoid);
        public bool ForgetFactoid(string user, string factoid);
        public List<Factoid> GetFactoid(string factoid);
        public FactoidInfoReply GetFactoidInfo(string factoid);
        public bool AddTell(string fromUser, string toUser, string message);
        public List<Tell> GetTell(string forUser);
        public ThingiverseRefs AddThingiverseReference(string item);
        public void AddThingiverseTitle(string item, string title);
        public YoutubeRefs AddYoutubeReference(string item);
        public void AddYoutubeTitle(string item, string title);
        public int GetMood();
    }
}
