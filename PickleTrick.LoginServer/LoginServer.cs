using System;
using System.IO;
using Serilog;
using Tomlyn;
using Tomlyn.Model;
using PickleTrick.Core.Server;
using PickleTrick.Core.Server.Packets;
using PickleTrick.Core.Server.Interfaces;

namespace PickleTrick.LoginServer
{
    class LoginServer : ServerApp
    {
        public LoginServer() : base() { }

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

        // We don't need this, but this is useful for debugging.
        private void LoginServer_OnPacket(Client client, Span<byte> packetData)
        {
            var packet = new InPacket(packetData);
            var length = packet.ReadUInt16();
            var opcodeId = packet.ReadUInt16();
            var opcode = (InOpcode)opcodeId;
            // packet.Seek(9); // Skip the header.

            Log.Verbose(
                "Received packet from {0}: opcode {1} 0x{2:x}, length 0x{3:x} ({3})",
                client.Socket.RemoteEndPoint,
                opcode,
                opcodeId,
                length
            );
        }
    }
}
