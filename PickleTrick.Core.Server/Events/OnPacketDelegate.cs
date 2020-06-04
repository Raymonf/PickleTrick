using System;

namespace PickleTrick.Core.Server.Events
{
    public delegate void OnPacketDelegate(Client client, byte[] packet);
}
