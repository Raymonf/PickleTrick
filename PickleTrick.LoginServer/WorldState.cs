using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using PickleTrick.Core.Server;
using PickleTrick.LoginServer.Data;
using Serilog;

namespace PickleTrick.LoginServer
{
    class WorldState
    {
        private static List<World> _worlds = new List<World>();

        private static async Task Refresh()
        {
            // Ugly, but will work for now.

            List<World> worlds;
            await using (var db = Database.Get())
            {
                worlds = (await db.QueryAsync<World>("SELECT * FROM worlds;")).ToList();

                var islands = (await db.QueryAsync<Island>("SELECT * FROM islands;")).ToList();
                foreach (var island in islands)
                {
                    var world = worlds.FirstOrDefault(w => w.Id == island.WorldId);
                    if (world == null)
                        continue;
                    island.Parent = world;
                    world.Islands.Add(island);
                }
            }

            lock (_worlds)
            {
                _worlds = worlds;
            }
        }

        private static async Task RefreshWorker()
        {
            while (true)
            {
                try
                {
                    await Refresh();
                    Log.Debug("Refreshed worlds.");
                    await Task.Delay(TimeSpan.FromMinutes(1));
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Unable to refresh worlds. Retrying in 10 seconds.");
                    await Task.Delay(TimeSpan.FromSeconds(10));
                }
            }
        }

        public static void StartRefreshWorker()
        {
            Task.Run(RefreshWorker);
        }

        public static IReadOnlyList<World> GetWorlds()
        {
            return _worlds;
        }
    }
}
