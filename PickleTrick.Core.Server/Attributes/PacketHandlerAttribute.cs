using System;
using System.Collections.Generic;
using System.Text;

namespace PickleTrick.Core.Server.Attributes
{
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class HandlesPacket : Attribute
    {
        public ushort opcode;

        public HandlesPacket(object opcode)
        {
            this.opcode = Convert.ToUInt16(opcode as Enum);
        }

        public HandlesPacket(ushort opcode)
        {
            this.opcode = opcode;
        }
    }
}
