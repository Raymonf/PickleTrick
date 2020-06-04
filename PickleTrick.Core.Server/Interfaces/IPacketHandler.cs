using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace PickleTrick.Core.Server.Interfaces
{
    public interface IPacketHandler
    {
        public Task Handle(Client user, byte[] packet);
    }
}
