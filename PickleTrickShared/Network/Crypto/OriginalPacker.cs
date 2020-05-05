using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PickleTrickShared.Network.Crypto
{
    public class OriginalPacker
    {
        ushort originalOpcode = 0;
        byte[] originalData = null;


        void PackHeader(Client sessionInfo, byte[] packet, byte randKey)
        {
            var key = sessionInfo.Key;

            // Update the randKey
            packet[6] = randKey;
            // Update the checkflag
            packet[8] = CryptoCommon.MakeChecksum(packet, sessionInfo.Key);

            // Encrypt Packing
            packet[7] = KeyTable.Table[(ushort)((randKey << 8) + packet[7])];

            // Encrypt length
            var length = (ushort)((KeyTable.Table[(ushort)(((packet[6] ^ packet[7]) << 8) + packet[1])] << 8)
                + KeyTable.Table[(ushort)(((packet[7] ^ key) << 8) + packet[0])]);

            // Encrypt opcode
            var opcode = (ushort)((KeyTable.Table[(ushort)(((packet[1] ^ key) << 8) + packet[3])] << 8)
                + KeyTable.Table[(ushort)(((packet[6] ^ packet[0]) << 8) + packet[2])]);

            // Encrypt sequence
            var sequence = (ushort)((KeyTable.Table[(ushort)(((packet[3] ^ packet[7]) << 8) + packet[5])] << 8)
                + KeyTable.Table[(ushort)(((packet[2] ^ key) << 8) + packet[4])]);

            // Replace non-encrypted fields
            ByteUtil.CopyTo(packet, 0, length);
            ByteUtil.CopyTo(packet, 2, opcode);
            ByteUtil.CopyTo(packet, 4, sequence);
        }

        bool PackStream(Client sessionInfo, ushort cmd, byte[] packet, int len)
        {
            byte[] data = packet;

            var randKey = 0x1F; // (byte)new Random().Next(1, 32767);

            // Update originalData randKey
            originalData[6] = (byte)(randKey % 256);

            if (len > 9)
            {
                // Make the header

                PackHeader(sessionInfo, data, (byte)(randKey % 256));

                // ???????????????????????????????????????
                data = packet.Skip(9).ToArray();
                len -= 9;

                var origLength = BitConverter.ToUInt16(originalData, 0);
                origLength -= 11;
                ByteUtil.CopyTo(originalData, 0, origLength);
            }

            int ecx = cmd;
            int eax = originalData[6];
            eax *= ecx;
            // -> cdq ?
            byte magicKey = (byte)(eax % 256);

            bool v17 = (originalData[7] & 4) == 0;
            eax = BitConverter.ToUInt16(originalData, 0);

            var tailVal = 0;
            var bytesPacked = 0;

            if (v17)
            {
                throw new Exception("why is v17 true???");
            }
            else
            {
                var i = 0;
                while (i < eax)
                {
                    tailVal += data[i];
                    data[i] = KeyTable.Table[(ushort)(data[i] + ((magicKey ^ i) << 8))];
                    i++;
                }
                bytesPacked = i;
            }

            if (bytesPacked != eax || bytesPacked >= len)
            {
                // error?
                return true;
            }

            // From this point on, do NOT update data. Instead, update packet.
            for (int i = 0; i < data.Length; i++)
            {
                packet[i + 9] = data[i];
            }

            // ????????????
            // what is this packedTimes thing
            //if (packedTimes == 0)
            //{
            if ((packet[7] & 1) >= 0)
            {
                CryptoCommon.UpdateKey(sessionInfo, tailVal);
            }

            if ((packet[7] & 4) >= 0)
            {
                // divRes:
                // edx = 0xF1 [actual]
                // eax = 0x570 

                // pack the tail value
                // make the sequence to be the amount of bytes packed
                var i = 0;
                var tailBytes = BitConverter.GetBytes((ushort)tailVal);
                var seq = bytesPacked;
                do
                {
                    var val = KeyTable.Table[(ushort)(tailBytes[i] + ((magicKey ^ seq) << 8))];
                    packet[i + bytesPacked + 9] = val;
                    i++;
                    seq++;
                }
                while (i < 2);
            }
            //}

            // do we need any of the other code?

            return true;
        }

        public byte[] Pack(Client sessionInfo, ushort opcode, byte[] data)
        {
            // the header is 9 bytes

            var cons = new List<byte>();
            cons.AddRange(BitConverter.GetBytes((ushort)(data.Length + 2 + 9))); // 0-1
            cons.AddRange(BitConverter.GetBytes(opcode)); // 2-3
            cons.AddRange(BitConverter.GetBytes(sessionInfo.Sequence)); // 4-5
            // 6, 7, 8: randkey, packing, checkflag
            cons.AddRange(new byte[3]);
            cons.AddRange(data);
            cons.AddRange(new byte[2]); // space for the tail flag on the client? not sure

            cons[7] = 0x07; // Packing = 7

            originalOpcode = opcode;
            originalData = cons.ToArray();

            var newPacket = cons.ToArray();

            PackStream(sessionInfo, opcode, newPacket, cons.Count);

            return newPacket;
        }
    }
}