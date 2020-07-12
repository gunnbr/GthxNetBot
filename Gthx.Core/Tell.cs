using System;
using System.Collections.Generic;
using System.Text;

namespace Gthx.Core
{
    public class Tell
    {
        public string FromUser { get; set; }
        public string ToUser { get; set; }
        public string Message { get; set; }
        public DateTime TimeSet { get; set; }
    
        public Tell(string fromUser, string toUser, string message, DateTime timeSet)
        {
            FromUser = fromUser;
            ToUser = toUser;
            Message = message;
            TimeSet = timeSet;
        }
    }
}
