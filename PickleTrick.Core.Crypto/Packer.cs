using System;
using PickleTrick.Core.Common;

namespace PickleTrick.Core.Crypto
{
    public class Packer
    {
        // We're going to keep a static Random object here so we can generate RandKeys as we wish.
        private static readonly Random _random = new Random();

        private static void PackHeader(CryptoClient client, Span<byte> packet)
        {
            var key = client.ServerKey;

            var randKey = _random.Next(0, 255);
            packet[6] = (byte)randKey; // Set RandKey field
            packet[7] = 0x07; // Set Packing field to 0x07 (change key)
            packet[8] = CryptoCommon.MakeChecksum(packet, key); // Update the checkflag

            // Encrypt packing
            packet[7] = KeyTable.Table[(ushort)((randKey << 8) + packet[7])];

            // Encrypt length
            var length = (ushort)((KeyTable.Table[(ushort)(((packet[6] ^ packet[7]) << 8) + packet[1])] << 8)
                + KeyTable.Table[(ushort)(((packet[7] ^ key) << 8) + packet[0])]);
            ByteUtil.CopyTo(packet, 0, length);

            // Encrypt opcode
            var opcode = (ushort)((KeyTable.Table[(ushort)(((packet[1] ^ key) << 8) + packet[3])] << 8)
                + KeyTable.Table[(ushort)(((packet[6] ^ packet[0]) << 8) + packet[2])]);
            ByteUtil.CopyTo(packet, 2, opcode);

            // Encrypt sequence
            var sequence = (ushort)((KeyTable.Table[(ushort)(((packet[3] ^ packet[7]) << 8) + packet[5])] << 8)
                + KeyTable.Table[(ushort)(((packet[2] ^ key) << 8) + packet[4])]);
            ByteUtil.CopyTo(packet, 4, sequence);
        }

        private static void PackStream(CryptoClient client, ushort opcode, Span<byte> packet)
        {
            // Get the packet data only, from offset 9 to (len - 2)
            var data = packet[9..^2];

            // The magic key is calculated with opcode * RandKey
            var magicKey = (byte)((opcode * packet[6]) % 256);

            // This will store the expected tail value for the client to check later.
            var tail = 0;

            var offset = 0; // This will be the amount of bytes packed.
            for (; offset < data.Length; offset++)
            {
                tail += data[offset];
                data[offset] = KeyTable.Table[(ushort)(((magicKey ^ offset) << 8) + data[offset])];
            }

            // Again, we know that Packing is 7.
            // 7 & 1 is 1
            // Update the key.
            CryptoCommon.UpdateKey(client, tail);

            // Now we can pack the tail value.
            var seq = offset;
            var tailBytes = BitConverter.GetBytes((ushort)tail);
            for (var i = 0; i < 2; i++, seq++)
            {
                // We can probably do packet[^2] or something...
                packet[i + offset + 9] = KeyTable.Table[(ushort)(((magicKey ^ seq) << 8) + tailBytes[i])];
            }
        }

        public static byte[] Pack(CryptoClient client, ushort opcode, byte[] data)
        {
            // Length: 9 bytes (header  ) + data + 2 bytes (tail checksum)
            var result = /*stackalloc*/ new byte[9 + data.Length + 2];
            Span<byte> span = result;

            ByteUtil.CopyTo(span, 0, (ushort)span.Length);
            ByteUtil.CopyTo(span, 2, opcode);
            ByteUtil.CopyTo(span, 4, client.ServerSequence);
            Buffer.BlockCopy(data, 0, result, 9, data.Length);

            PackHeader(client, span);
            PackStream(client, opcode, span);

            return result;
        }
    }
}
