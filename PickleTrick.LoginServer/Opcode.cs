using System;
using System.Collections.Generic;
using System.Text;

namespace PickleTrick.LoginServer
{
    public enum InOpcode : ushort
    {
        LoginRequest = 0x2CED,
        SelectServer = 0x2CF1,
    }

    public enum OutOpcode : ushort
    {
        ServerListInfo = 0x2CEE,
        LoginError = 0x2CEF,
        ChannelServerInfo = 0x2CF2,
        NoticeInfo = 0x2D51,
    }
}
