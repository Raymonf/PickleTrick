using System;
using System.Collections.Generic;
using System.Text;

namespace PickleTrickShared.Network
{
    /// <summary>
    /// A client. This will represent one "user" of the current server instance.
    /// </summary>
    public class Client
    {
        // ...
        // I'm just not sure how to handle this right now.
        // Will we ever need this?
        public TricksterPacketHandler PacketHandler { get; } = new TricksterPacketHandler();
        
        /// <summary>
        /// First packet validity packet.
        /// This is probably used to determine if a connection is legitimate early on.
        /// Check Unpacker.CheckHeader for more information.
        /// </summary>
        public bool IsFirstPacket { get; set; } = true;
        
        /// <summary>
        /// Current encryption key.
        /// </summary>
        public byte Key { get; set; } = 0x01;

        /// <summary>
        /// Stored encryption key, in case we have a split packet.
        /// </summary>
        public byte LastKey { get; set; } = 0x01;

        /// <summary>
        /// The current packet ID (sort of).
        /// </summary>
        public ushort Sequence { get; set; } = 0x00;

        /// <summary>
        /// Trickster sometimes merges multiple packets into one.
        /// Trickster will also sometimes split its packets into multiple.
        /// This is used to account for the second case as a buffer to merge in when a full packet is received.
        /// 
        /// If this is null, that means we don't have a buffer. Otherwise, we should pretend that this buffer
        /// comes before the next packet (aka the packet we'd be currently processing inside TrickPacketHandler).
        /// </summary>
        public byte[] StoredPacketBuffer { get; set; } = null;
    }
}
