using System;

namespace PickleTrick.Core.Crypto
{
    public class Header
    {
        public ushort Len { get; set; }
        public ushort Cmd { get; set; }
        public ushort Seq { get; set; }
        public byte RandKey { get; set; }
        public byte Packing { get; set; }
        public byte CheckFlag { get; set; }
    }

    public class CryptoCommon
    {
        public static byte MakeChecksum(byte[] packet, byte key)
        {
            var x = KeyTable.Table[(packet[0] << 8) + packet[2]];
            var bl = KeyTable.Table[(key << 8) + x];

            var y = KeyTable.Table[(packet[3] << 8) + packet[1]];
            var al = KeyTable.Table[(packet[6] << 8) + y];

            return KeyTable.Table[(bl << 8) + al];
        }

        public static byte MakeChecksum(Span<byte> packet, byte key)
        {
            var x = KeyTable.Table[(packet[0] << 8) + packet[2]];
            var bl = KeyTable.Table[(key << 8) + x];

            var y = KeyTable.Table[(packet[3] << 8) + packet[1]];
            var al = KeyTable.Table[(packet[6] << 8) + y];

            return KeyTable.Table[(bl << 8) + al];
        }

        public static void UpdateKey(CryptoClient client, int tail)
        {
            byte key = client.Key;
            client.Key = (byte)(KeyTable.Table[((tail & 0xff) << 8) + key] + key);
        }
    }
}
