using System.Collections.Generic;

namespace MySQL_Proxy.type
{
    class Packet
    {
        public int payloadLength;
        public int seqNumber;
        public byte[] payload;
    }

    class InitHandShake
    {
        public byte protocolVer;
        public string serverVer;
        public byte[] connectionID;
        public string authPluginData;
        public byte[] statusFlags;
        public byte[] capacityFlags;
        public byte charSet;
        public string authPluginName;
    }

    class HandShakeResponse
    {
        public byte[] capacityFlags;
        public long maxPacketSize;
        public byte charSet;
        public string userName;
        public string database;
        public string authResponse;
        public string authPluginName;
        public Dictionary<string, string> keyValuePair;
    }
}
