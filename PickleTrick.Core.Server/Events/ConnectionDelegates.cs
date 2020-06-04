using System;
using System.Collections.Generic;
using System.Text;

namespace PickleTrick.Core.Server.Events
{
    public delegate void OnConnectDelegate(Client client);
    public delegate void OnDisconnectDelegate(Client client);
}
