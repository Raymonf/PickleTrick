using System;
using System.Collections.Generic;
using System.Text;

namespace PickleTrick.LoginServer.Data
{
    class Island
    {
        public World Parent { get; set; }

        public int Id { get; set; }
        public int WorldId { get; set; }
        public string Name { get; set; }
        public string Ip { get; set; }
        public int Port { get; set; }

        public bool Active { get; set; }
        public bool Visible { get; set; }

        public int CurrentUsers { get; set; }
        public int MaxUsers { get; set; }

    }
}
