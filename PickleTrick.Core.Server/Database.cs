using MySql.Data.MySqlClient;
using Serilog;
using System;

namespace PickleTrick.Core.Server
{
    public class Database
    {
        // private static MySqlConnection connection;
        private static string connectionString = null;

        public static bool Setup(DatabaseConfig config)
        {
            connectionString = string.Format(
                "Server={0};database={1};UID={2};password={3}",
                config.Host,
                config.Database,
                config.Username,
                config.Password
            );

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
        /// This looks bad, but it'll help us with pooling later.
        /// We can wrap this in a using (...) statement to get a connection.
        /// </summary>
        /// <returns>A (probably) usable MySQL connection</returns>
        public static MySqlConnection Get()
        {
            return new MySqlConnection(connectionString);
        }
    }
}
