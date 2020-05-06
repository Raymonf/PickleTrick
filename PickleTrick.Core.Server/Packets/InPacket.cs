using PickleTrick.Core.Common;
using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Text;

namespace PickleTrick.Core.Server.Packets
{
    public class InPacket
    {
        private int offset = 0;
        private readonly byte[] packet = null;

        public InPacket(Span<byte> packet)
        {
            // TODO: Is there any way to kill this allocation?
            this.packet = packet.ToArray();
        }

        public InPacket(byte[] packet)
        {
            this.packet = packet;
        }

        public void Seek(long offset)
        {
            this.offset = (int)offset;
        }

        public int Read()
        {
            return packet[offset++] & 0xFF;
        }

        public byte ReadByte()
        {
            return (byte)Read();
        }

        public byte[] ReadBytes(int n)
        {
            byte[] b = new byte[n];

            for (int i = 0; i < n; i++)
                b[i] = ReadByte();
            offset += n;
            return b;
        }

        public ushort ReadUInt16()
        {
            var i = BitConverter.ToUInt16(packet, offset);
            offset += 2;
            return i;
        }

        public int ReadInt()
        {
            var i = BitConverter.ToInt32(packet, offset);
            offset += 4;
            return i;
        }

        public uint ReadUInt()
        {
            var i = BitConverter.ToUInt32(packet, offset);
            offset += 4;
            return i;
        }

        public long ReadLong()
        {
            var i = BitConverter.ToInt64(packet, offset);
            offset += 8;
            return i;
        }

        /// <summary>
        /// Read string until a null character is encountered.
        /// </summary>
        /// <returns>The read string</returns>
        public string ReadString()
        {
            // Determine the end of the string. Find the first 0x00.
            int len = 0;
            for (int i = offset; i < packet.Length; i++)
            {
                if (packet[i] != 0x00)
                    len++;
                else
                    break;
            }

            var b = ReadBytes(len + 1);
            return Constants.Encoding.GetString(b).TrimEnd('\0');
        }

        public string ReadString(int length)
        {
            var b = ReadBytes(length);
            return Constants.Encoding.GetString(b).TrimEnd('\0');
        }
    }
}
