using System;
using System.Collections.Generic;
using System.Text;

namespace PickleTrick.Core.Server
{
    public interface IServerConfig
    {
        // We'll use this interface to share a server config object.
    }

    public class DatabaseConfig
    {
        public string Host { get; set; }
        public string Database { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class ServerConfiguration
    {
        public DatabaseConfig Database { get; set; } = new DatabaseConfig();
        public IServerConfig Server { get; set; }
    }
}
