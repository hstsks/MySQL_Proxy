using System.Collections.Generic;

namespace MySQL_Proxy.type
{
    class Packet
    {
        public int payloadLength;
        public int seqNumber;
        public byte[] payload;
    }

    class HandShakeResponse
    {
        public byte[] capacityFlags;
        public int maxPacketSize;
        public byte charSet;
        public string userName;
        public string database;
        public string authResponse;
        public string authPluginName;
        public Dictionary<string, string> keyValuePair;
    }
}
