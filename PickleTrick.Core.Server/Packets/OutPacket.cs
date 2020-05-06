using PickleTrick.Core.Common;
using PickleTrick.Core.Crypto;
using PickleTrick.Core.Server.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace PickleTrick.Core.Server.Packets
{
    public class OutPacket
    {
        private List<byte> packet;
        private readonly ushort opcode;
        private Client client;

        public OutPacket(ushort opcode, IUser user)
        {
            packet = new List<byte>(512); // initial capacity of 512
            this.opcode = opcode;
            client = user.Client;
        }

        public OutPacket(ushort opcode, Client client)
        {
            packet = new List<byte>(512); // initial capacity of 512
            this.opcode = opcode;
            this.client = client;
        }

        public bool Send()
        {
            byte[] data = Packer.Pack(client.Crypto, opcode, packet.ToArray());
            try
            {
                client.Socket.Send(data);
                client.Crypto.ServerSequence++;
            }
            catch (Exception ex)
            {
                if (System.Diagnostics.Debugger.IsAttached)
                    throw ex;
                return false;
            }
            return true;
        }

        public void WriteBytePadding(int n, byte b = 0x00)
        {
            var bytes = new byte[n];

            if (b != 0x00)
                for (int i = 0; i < n; i++)
                    bytes[i] = b;

            packet.AddRange(bytes);
        }

        public void WriteByte(byte b)
        {
            packet.Add(b);
        }

        public void WriteBytes(byte[] b)
        {
            packet.AddRange(b);
        }

        public void WriteInt16(int i)
        {
            if (!BitConverter.IsLittleEndian)
            {
                var b1 = (i >> 0) & 0xff;
                var b2 = (i >> 8) & 0xff;

                i = b1 << 8 | b2 << 0;
            }

            packet.AddRange(BitConverter.GetBytes(i));
        }

        public void WriteInt32(int i)
        {
            if (!BitConverter.IsLittleEndian)
            {
                var b1 = (i >> 0) & 0xff;
                var b2 = (i >> 8) & 0xff;
                var b3 = (i >> 16) & 0xff;
                var b4 = (i >> 24) & 0xff;

                i = b1 << 24 | b2 << 16 | b3 << 8 | b4 << 0;
            }

            packet.AddRange(BitConverter.GetBytes(i));
        }

        public void WriteInt64(long i)
        {
            if (!BitConverter.IsLittleEndian)
            {
                var b1 = (i >> 0) & 0xff;
                var b2 = (i >> 8) & 0xff;
                var b3 = (i >> 16) & 0xff;
                var b4 = (i >> 24) & 0xff;
                var b5 = (i >> 32) & 0xff;
                var b6 = (i >> 40) & 0xff;
                var b7 = (i >> 48) & 0xff;
                var b8 = (i >> 56) & 0xff;

                i = b1 << 56 | b2 << 48 | b3 << 40 | b4 << 32 | b5 << 24 | b6 << 15 | b7 << 8 | b8 << 0;
            }

            packet.AddRange(BitConverter.GetBytes(i));
        }

        public void WriteUInt16(uint u)
        {
            if (!BitConverter.IsLittleEndian)
            {
                var b1 = (u >> 0) & 0xff;
                var b2 = (u >> 8) & 0xff;

                u = b1 << 8 | b2 << 0;
            }

            packet.AddRange(BitConverter.GetBytes(u));
        }

        public void WriteUInt32(uint u)
        {
            if (!BitConverter.IsLittleEndian)
            {
                var b1 = (u >> 0) & 0xff;
                var b2 = (u >> 8) & 0xff;
                var b3 = (u >> 16) & 0xff;
                var b4 = (u >> 24) & 0xff;

                u =  b1 << 24 | b2 << 16 | b3 << 8 | b4 << 0;
            }

            packet.AddRange(BitConverter.GetBytes(u));
        }

        public void WriteUInt64(ulong u)
        {
            if (!BitConverter.IsLittleEndian)
            {
                var b1 = (u >> 0) & 0xff;
                var b2 = (u >> 8) & 0xff;
                var b3 = (u >> 16) & 0xff;
                var b4 = (u >> 24) & 0xff;
                var b5 = (u >> 32) & 0xff;
                var b6 = (u >> 40) & 0xff;
                var b7 = (u >> 48) & 0xff;
                var b8 = (u >> 56) & 0xff;

                u = b1 << 56 | b2 << 48 | b3 << 40 | b4 << 32 | b5 << 24 | b6 << 15 | b7 << 8 | b8 << 0;
            }

            packet.AddRange(BitConverter.GetBytes(u));
        }

        /// <summary>
        /// Writes a string to the packet "buffer"
        /// </summary>
        /// <param name="str">The string to write</param>
        public void WriteString(string str)
        {
            packet.AddRange(Constants.Encoding.GetBytes(str));
            return;
        }

        /// <summary>
        /// Writes a string to the packet buffer with a fixed length
        /// If the string is smaller than the length, 0x00s will be written
        /// </summary>
        /// <param name="str">The string to write</param>
        /// <param name="len">The length of the string</param>
        public void WriteString(string str, int len)
        {
            var b = Constants.Encoding.GetBytes(str);

            if (b.Length > len)
            {
                WriteBytes(b);
            }
            else if (len - 1 >= 0)
            {
                WriteBytes(b[..(len - 1)]);
            }

            if (b.Length < len)
            {
                WriteBytePadding(len - b.Length);
            }
        }

        public void WriteHexString(string hs)
        {
            WriteBytes(HexUtil.ToBytes(hs));
        }
    }
}
