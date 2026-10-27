using System;
using System.Collections.Generic;
using System.Text;

namespace GthxNetBot
{
    public enum IrcProtocolErrorEnum
    {
        NoSuchNick = 401,
        MotdMissing = 422,
        NickInUse = 433,
        YoureBanned = 465,
    }
}
