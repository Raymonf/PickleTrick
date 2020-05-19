using PickleTrick.Core.Server;
using PickleTrick.Core.Server.Attributes;
using PickleTrick.Core.Server.Interfaces;
using PickleTrick.Core.Server.Packets;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;

namespace PickleTrick.LoginServer.Packets
{
    [HandlesPacket(InOpcode.LoginRequest)]
    class LoginRequest : IPacketHandler
    {
        public void Handle(Client client, Span<byte> packet)
        {
            // Notice packet
            // Usually would contain a URL and version ID.
            // The client will open that URL if it's:
            // 1) Enabled (a flag)
            // 2) A new version ID (?)
            new OutPacket(OutOpcode.NoticeInfo, client)
                .WriteBytePadding(0x415)
                .Send();

            // Send server select
            // Temporary. We'll populate this with data from the configuration soon.
            var servers = new OutPacket(OutOpcode.ServerListInfo, client)
                .WriteUInt32(100038499) // User ID
                .WriteUInt64(17069199591863541) // User authentication token for LoginServer
                .WriteHexString("01 01 01 00 01 00 44 6F 6E 20 43 61 76 61 6C 69 65 72 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 53 65 72 76 65 72 20 31 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 B8 0B 03 00 B7 0D")
                .Send();

            Log.Verbose("Sent notice and server list packet to {0}.", client.Socket.RemoteEndPoint);
        }
    }
}
