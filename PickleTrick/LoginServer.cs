using System;
using System.Text;
using System.Collections.Generic;
using PickleTrick.Core.Server;
using PickleTrick.Core.Server.Packets;

namespace PickleTrick.LoginServer
{
    class LoginServer : ServerApp
    {
        public LoginServer(int port) : base(port) {}

        public override void PrivateInit()
        {
            OnPacket += LoginServer_OnPacket;
        }

        private void LoginServer_OnPacket(Client client, Span<byte> packetData)
        {
            var packet = new InPacket(packetData);
            var length = packet.ReadUInt16();
            var opcode = packet.ReadUInt16();
            packet.Seek(9); // Skip the header.
            Console.WriteLine("Length: {0:X2}", length);
            Console.WriteLine("Opcode: {0:X2}", opcode);
            if (opcode == 0x2CED)
            {
                var data = new OutPacket(0x2CEF, client);
                data.WriteBytes(new byte[] { 0x63, 0xEA, 0x00, 0x00 });
                data.Send();
            }
        }
    }
}
