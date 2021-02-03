using System;
using System.Text;
using System.Collections.Generic;

namespace PickleTrick.LoginServer.Data
{
    // A world has many islands.
    // A user will connect to an island, which is in a world.
    class World
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<Island> Islands { get; set; } = new List<Island>();
    }
}
