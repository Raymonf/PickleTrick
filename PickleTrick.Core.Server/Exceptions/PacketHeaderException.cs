using System;
using System.Collections.Generic;
using System.Text;

namespace PickleTrick.Core.Server.Exceptions
{
    class PacketHeaderException : Exception
    {
        public PacketHeaderException() {}

        public PacketHeaderException(string message)
            : base(message) {}

        public PacketHeaderException(string message, Exception inner)
            : base(message, inner) {}
    }
}
