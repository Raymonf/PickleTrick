using System;
using System.Collections.Generic;
using System.Text;

namespace PickleTrick.Core.Common
{
    public class ByteUtil
    {
        public static string ByteToHex(int i)
        {
            return BitConverter.ToString(BitConverter.GetBytes(i)).Replace("-", " ");
        }

        public static string ByteToHex(ushort us)
        {
            return BitConverter.ToString(BitConverter.GetBytes(us)).Replace("-", " ");
        }

        public static string ByteToHex(byte b)
        {
            return BitConverter.ToString(new byte[] { b }).Replace("-", " ");
        }

        public static string ByteToHex(byte[] b)
        {
            return BitConverter.ToString(b).Replace("-", " ");
        }

        public static void CopyTo(Span<byte> b, int i, ushort us)
        {
            b[i] = (byte)us;
            b[i + 1] = (byte)(us >> 8);
        }

        public static void CopyTo(byte[] b, int i, ushort us)
        {
            b[i] = (byte)us;
            b[i + 1] = (byte)(us >> 8);
        }

        public static void CopyTo(byte[] b, int i, int us)
        {
            b[i] = (byte)us;
            b[i + 1] = (byte)(us >> 8);
            b[i + 2] = (byte)(us >> 0x10);
            b[i + 3] = (byte)(us >> 0x18);
        }

        public static void CopyTo(byte[] b, int i, uint us)
        {
            b[i] = (byte)us;
            b[i + 1] = (byte)(us >> 8);
            b[i + 2] = (byte)(us >> 0x10);
            b[i + 3] = (byte)(us >> 0x18);
        }
    }
}
