using System;

namespace PickleTrick.Core.Crypto
{
    public class CryptoClient
    {
        /// <summary>
        /// First packet validity packet.
        /// This is probably used to determine if a connection is legitimate early on.
        /// Check Unpacker.CheckHeader for more information.
        /// </summary>
        public bool IsFirstPacket { get; set; } = true;

        /// <summary>
        /// Current encryption key.
        /// </summary>
        public byte Key { get; set; } = 0x01; // 0x01 is the default key.

        /// <summary>
        /// Current server encryption key. This is a constant.
        /// </summary>
        public byte ServerKey { get { return 0x01; } }

        /// <summary>
        /// The current packet ID (sort of).
        /// </summary>
        public ushort Sequence { get; set; } = 0x00;

        /// <summary>
        /// The current server packet ID.
        /// </summary>
        public ushort ServerSequence { get; set; } = 0x00;
    }
}
