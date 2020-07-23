using System;
using System.Collections.Generic;
using System.Text;

namespace Gthx.Core.Interfaces
{
    public class ReferenceData
    {
        public string Id;
        public string Title;
        public int ReferenceCount;
    }

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

    public class SeenData
    {
        public string Channel;
        public string User;
        public string Message;
        public DateTime LastSeenTime;
    }

    public interface IGthxData
    {
        public List<SeenData> GetLastSeen(string user);
        public void UpdateLastSeen(string channel, string user, string message);
        public bool AddFactoid(string user, string factoid, bool isAre, string value, bool replaceExisting);
        public bool IsFactoidLocked(string factoid);
        public bool ForgetFactoid(string user, string factoid);
        public List<Factoid> GetFactoid(string factoid);
        public FactoidInfoReply GetFactoidInfo(string factoid);
        public bool AddTell(string fromUser, string toUser, string message);
        public List<Tell> GetTell(string forUser);
        public ReferenceData AddThingiverseReference(string item);
        public bool AddThingiverseTitle(string item, string title);
        public ReferenceData AddYoutubeReference(string item);
        public void AddYoutubeTitle(string item, string title);
        public int GetMood();
    }
}
