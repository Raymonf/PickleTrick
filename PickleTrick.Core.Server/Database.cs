using System;
using System.Data.Common;
using PickleTrick.Core.Server.Data;
using PickleTrick.Core.Server.DatabaseConnectors;
using PickleTrick.Core.Server.Interfaces;

namespace PickleTrick.Core.Server
{
    public class Database
    {
        private static IDbConnector connector = null;

        public static bool Setup(DatabaseType dbType, DatabaseConfig config)
        {
            if (connector != null)
            {
                return true;
            }

            connector = dbType switch
            {
                DatabaseType.SqlServer => new SqlServerConnector(config),
                DatabaseType.MySql => new MySqlConnector(config),
                _ => null // Um...
            };

            return connector.Setup();
        }

        /// <summary>
        /// Gets a database connection to use to query the database.
        /// </summary>
        /// <returns>A (probably) usable database connection</returns>
        public static DbConnection Get()
        {
            return connector.Get();
        }
    }
}
