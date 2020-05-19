using MySql.Data.MySqlClient;
using Serilog;
using System;

namespace PickleTrick.Core.Server
{
    public class Database
    {
        private static MySqlConnection connection;
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

            connection = new MySqlConnection(connectionString);

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

        public static MySqlConnection Get()
        {
            return connection;
        }
    }
}
