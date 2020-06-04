using System;
using Serilog;
using System.Data.Common;
using MySql.Data.MySqlClient;
using PickleTrick.Core.Server.Interfaces;

namespace PickleTrick.Core.Server.DatabaseConnectors
{
    class MySqlConnector : IDbConnector
    {
        private DatabaseConfig config = null;
        private string connectionString = null;

        public MySqlConnector(DatabaseConfig config)
        {
            this.config = config;
        }

        public bool Setup()
        {
            connectionString = string.Format(
                "Server={0};database={1};UID={2};password={3}",
                config.Host,
                config.Database,
                config.Username,
                config.Password
            );

            Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

            var connection = new MySqlConnection(connectionString);
            try
            {
                connection.Open();
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unable to connect to the database. Attempted connection string: {0}", connectionString);
                return false;
            }
        }

        /// <summary>
        /// Gets a database connection to use to query the database.
        /// </summary>
        /// <returns>A (probably) usable database connection</returns>
        public DbConnection Get()
        {
            // This looks bad, but it'll help us with pooling later.
            // We can wrap this in a using (...) statement to get a connection.
            return new MySqlConnection(connectionString);
        }
    }
}
