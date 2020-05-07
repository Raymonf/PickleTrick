using System;
using System.IO;
using Serilog;
using Tomlyn;
using Tomlyn.Model;
using PickleTrick.Core.Server;
using PickleTrick.Core.Server.Packets;

namespace PickleTrick.LoginServer
{
    class LoginServer : ServerApp
    {
        public LoginServer() : base() { }
        // public LoginServer(int port) : base(port) { }

        public override string GetServerName() => "PickleTrick - Login Server";

        public override void PrivateInit()
        {
            OnPacket += LoginServer_OnPacket;
        }

        public override void PrivateConfigure()
        {
            var login = Toml.Parse(File.ReadAllText("login.toml")).ToModel();
            var table = (TomlTable) login["loginserver"];
            port = (int)(long) table["port"]; // Tomlyn reads ints as longs, so cast to long and then int.
        }

        // TODO: We should move this elsewhere, use reflection, or something else.
        private void LoginServer_OnPacket(Client client, Span<byte> packetData)
        {
            var packet = new InPacket(packetData);
            var length = packet.ReadUInt16();
            var opcode = (InOpcode) packet.ReadUInt16();
            packet.Seek(9); // Skip the header.

            Log.Verbose(
                "Received packet from {0}: opcode 0x{1:X2}, length 0x{2:X2} ({3})",
                client.Socket.RemoteEndPoint,
                opcode,
                length,
                length
            );

            if (opcode == InOpcode.LoginRequest)
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
}
