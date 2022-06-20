using System;
using MySQL_Proxy.type;

namespace MySQL_Proxy.exception
{
    class PermissionDeniedException : Exception
    {
        public Packet errorPacket;

        public PermissionDeniedException(Packet packet) : base()
        {
            errorPacket = packet;
        }
    }
}
