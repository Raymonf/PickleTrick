using System;
using System.Data.Common;

namespace PickleTrick.Core.Server.Interfaces
{
    interface IDbConnector
    {
        public bool Setup();

        public DbConnection Get();
    }
}
