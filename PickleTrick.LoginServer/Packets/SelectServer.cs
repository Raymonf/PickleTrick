using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using PickleTrick.Core.Server;
using PickleTrick.Core.Server.Attributes;
using PickleTrick.Core.Server.Interfaces;
using PickleTrick.Core.Server.Packets;
using PickleTrick.LoginServer.Data;
using Serilog;

namespace PickleTrick.LoginServer.Packets
{
    [HandlesPacket(InOpcode.SelectServer)]
    class SelectServer : IPacketHandler
    {
        public async Task Handle(Client client, byte[] packet)
        {
            var p = new InPacket(packet);
            var worldId = p.ReadUInt16();
            var islandId = p.ReadUInt16();

            Island island;
            var worlds = WorldState.GetWorlds();
            lock (worlds)
            {
                var world = worlds.FirstOrDefault(w => w.Id == worldId);
                if (world == null)
                {
                    Log.Error("Invalid world selected. world: {0}  island: {1}", worldId, islandId);
                    return;
                }

                island = world.Islands.FirstOrDefault(i => i.Id == islandId);
                if (island == null)
                {
                    Log.Error("Invalid island selected. world: {0}  island: {1}", worldId, islandId);
                    return;
                }
            }

            new OutPacket(OutOpcode.ChannelServerInfo, client)
                .WriteString(island.Ip, 15) // max 15 len IP + \0
                .WriteUInt16((ushort)island.Port)
                .Send();

            Log.Verbose("User selected a server.");
        }
    }
}
