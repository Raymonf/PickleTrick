using System;
using System.Collections.Generic;
using System.Text;

namespace PickleTrick.Core.Server.Interfaces
{
    public interface IPacketHandler
    {
        public void Handle(Client user, Span<byte> packet);
    }
}
