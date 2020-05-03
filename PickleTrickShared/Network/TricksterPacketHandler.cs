using System;
using System.Collections.Generic;
using System.Text;

namespace PickleTrickShared.Network
{
    /// <summary>
    /// This class will handle raw requests from the ServerApp
    /// and eventually pass individual packets back to ServerApp's packet receive callback.
    /// 
    /// Trickster sometimes merges multiple packets into one.
    /// Trickster will also sometimes split its packets into multiple.
    /// </summary>
    public class TricksterPacketHandler
    {
        /*public static bool IsPacketSplit(Client client, byte[] packet)
        {
            if (client.StoredPacketBuffer.Length > 65535)
            {
                // Invalid packet. Let's just say it's not split.
                return false;
            }

            if (packet.Length < 11)
            {
                // 9 (header) + 2 (tail checksum) bytes
                return true;
            }

            if (...)
            {
                
            }

            return false;
        }*/
    }
}
