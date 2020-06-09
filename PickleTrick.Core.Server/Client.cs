using System;
using System.Net.Sockets;
using System.Collections.Generic;
using PickleTrick.Core.Crypto;

namespace PickleTrick.Core.Server
{
    /// <summary>
    /// A client. This will represent one "user" of the current server instance.
    /// </summary>
    public class Client
    {
        public CryptoClient Crypto { get; set; } = new CryptoClient();

        /// <summary>
        /// A flag for the first packet sent.
        /// </summary>
        public bool HasValidPacket { get; set; } = false;

        public object State { get; set; }

        public Socket Socket { get; set; }

        private byte[] _currentBuffer =  new byte[65535];
        public byte[] CurrentBuffer { get { return _currentBuffer; } }

        /// <summary>
        /// Trickster sometimes merges multiple packets into one.
        /// Trickster will also sometimes split its packets into multiple.
        /// This is used to account for the second case as a buffer to merge in when a full packet is received.
        /// 
        /// If this is null, that means we don't have a buffer. Otherwise, we should pretend that this buffer
        /// comes before the next packet (aka the packet we'd be currently processing inside TrickPacketHandler).
        /// </summary>
        private List<byte> _storedPacketBuffer = new List<byte>(32767);
        public List<byte> StoredPacketBuffer { get { return _storedPacketBuffer; } }

        public void DestroyBuffers()
        {
            _currentBuffer = null;
            _storedPacketBuffer = null;
        }
    }
}
