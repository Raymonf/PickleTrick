using PickleTrick.Core.Server;
using PickleTrick.Core.Server.Attributes;
using PickleTrick.Core.Server.Interfaces;
using PickleTrick.Core.Server.Packets;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using PickleTrick.LoginServer.Data;

namespace PickleTrick.LoginServer.Packets
{
    [HandlesPacket(InOpcode.LoginRequest)]
    class LoginRequest : IPacketHandler
    {
        static RNGCryptoServiceProvider _rngCryptoServiceProvider = new RNGCryptoServiceProvider();

        public long GenerateLoginToken()
        {
            var bytes = new byte[sizeof(ulong)];
            _rngCryptoServiceProvider.GetBytes(bytes);
            return Math.Abs(BitConverter.ToInt64(bytes, 0));
        }

        public async Task OnLoginOk(Client client, DbUser user)
        {
            // Notice packet. Might be unused in 2014 kTO?
            // Usually would contain a URL and version ID.
            // The client will open that URL if it's:
            // 1) Enabled (a flag)
            // 2) A new version ID (?)
            new OutPacket(OutOpcode.NoticeInfo, client)
                .WriteBytePadding(0x415)
                .Send();

            // Send server select
            var serverList = new OutPacket(OutOpcode.ServerListInfo, client)
                .WriteInt32(user.Id) // User ID
                .WriteInt64(user.AuthToken); // User authentication token for LoginServer

            var worlds = WorldState.GetWorlds();
            lock (worlds)
            {
                serverList.WriteByte((byte)worlds.Sum(x => x.Islands.Count)); // Number of islands.

                // We'll get the data from the worlds. Trickster wants all of the islands.
                foreach (var world in worlds)
                {
                    foreach (var island in world.Islands)
                    {
                        serverList.WriteBoolean(island.Active) // Is server online?
                            .WriteUInt16((ushort)world.Id) // World number
                            .WriteUInt16((ushort)island.Id) // Island code
                            .WriteString(world.Name, 32)
                            .WriteString(island.Name, 32)
                            .WriteUInt16((ushort)island.MaxUsers) // Max users
                            .WriteUInt16((ushort)island.CurrentUsers); // Current users
                    }
                }
            }

            serverList.Send();

            Log.Verbose("Sent notice and server list packet to {0}.", client.Socket.RemoteEndPoint);
        }

        private void OnLoginFail(Client client)
        {
            // 60003 is the incorrect password login code.
            // If we want, we can later expand this to include bans (opcode 0x2CF4).

            new OutPacket(OutOpcode.LoginError, client)
                .WriteInt32(60003)
                .Send();
        }

        public async Task Handle(Client client, byte[] packet)
        {
            var p = new InPacket(packet);
            var username = p.ReadString();
            p.Seek(19); // Skip past the 17 character + \0 username
            var password = p.ReadString();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                OnLoginFail(client);
                return;
            }


            DbUser user;
            await using (var db = Database.Get())
            {
                user = await db.QueryFirstAsync<DbUser>("SELECT * FROM users WHERE username = @username;", new { username });
                if (user == null || user.Password != password)
                {
                    OnLoginFail(client);
                    return;
                }
                
                // Update the auth token so the user can authenticate later
                try
                {
                    var token = GenerateLoginToken();
                    await db.ExecuteAsync("UPDATE users SET auth_token = @token WHERE id = @id;",
                        new {token, id = user.Id});
                    user.AuthToken = token;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Unable to set the user's auth token.");
                    OnLoginFail(client);
                    return;
                }
            }

            await OnLoginOk(client, user);
        }
    }
}
