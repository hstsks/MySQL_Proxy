using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;
using System.Text.RegularExpressions;
using MySQL_Proxy.type;
using MySQL_Proxy.exception;
using log4net;

namespace MySQL_Proxy.parser
{
    class ClientPacketParser : PacketParser
    {
        private byte[] CLIENT_PLUGIN_AUTH_LENENC_CLIENT_DATA = { 0, 32, 0, 0 };
        private byte[] CLIENT_SECURE_CONNECTION = { 0, 0, 128, 0 };
        private byte[] CLIENT_CONNECT_WITH_DB = {  0, 0, 0 ,8 };
        private byte[] CLIENT_PLUGIN_AUTH = { 0, 8, 0, 0 };
        private byte[] CLIENT_CONNECT_ATTRS = { 0, 16, 0, 0 };
        private static readonly ILog clientLogger = LogManager.GetLogger("client");

        public void checkQuery(byte[] data)
        {
            List<Packet> packetList = SlicePacket(data.ToList<byte>());

            foreach ( Packet packet in packetList)
            {
                if (packet.payload[0] == 3 && packet.payload[1] == 0 && packet.payload[2] == 1)
                {
                    parseQuery(packet);
                } else if (packet.payloadLength == 1 && packet.seqNumber == 0 && packet.payload[0] == 1)
                {
                    Console.WriteLine("close");
                    // TODO close handler
                }
            }
        }
        public string parseQuery(Packet packet)
        {
            string queryString = Encoding.UTF8.GetString(packet.payload.Skip(3).ToArray());
            clientLogger.Info(queryString);

            Regex chequer = new Regex("chequer",RegexOptions.IgnoreCase);
            if (chequer.IsMatch(queryString))
            {
                Packet errorPacket = PermissionErrorPacket(packet.seqNumber);

                throw new PermissionDeniedException(errorPacket);
            }
            return queryString;
        }

        //TODO solve out of order error
        public Packet PermissionErrorPacket(int seq)
        {
            Packet packet = new Packet();

            byte[] errorHeader = { 255, 81, 4, 35, 52, 50, 48, 48, 48 };
            List<byte> errorMessage = Encoding.UTF8.GetBytes("No permission to access the CHEQUER").ToList<byte>();
            List<byte> payload = new List<byte>(errorHeader);
            payload.AddRange(errorMessage);

            packet.payloadLength = payload.Count;
            packet.seqNumber = seq + 1;
            packet.payload = payload.ToArray();

            return packet;
        }

        public HandShakeResponse parseHandShakeResponse(byte[] packet)
        {
            HandShakeResponse response = new HandShakeResponse();
            List<byte> data = packet.Skip(36).ToList<byte>();

            response.capacityFlags = packet.Skip(4).Take(4).ToArray();
            response.maxPacketSize = BitConverter.ToInt32(packet,8);
            response.charSet = packet[12];

            response.userName = NullTerminateStr(data);

            //auth-response
            if (response.capacityFlags[3] % 64 > 32)
            {
                response.authResponse = LenencStr(data);
            } 
            else if ( response.capacityFlags[2] > 128)
            {
                int length = data[0];
                byte[] str = data.Skip(1).Take(length).ToArray();
                data.RemoveRange(0, length + 1);

                response.authResponse = Encoding.UTF8.GetString(str);
            } else
            {
                response.authResponse = NullTerminateStr(data);
            }

            //auth-plugin-name
            if (response.capacityFlags[3] % 16 > 8)
            {
                response.authPluginName = NullTerminateStr(data);
            }

            //keys and value pairs
            if (response.capacityFlags[3] % 32 > 16)
            {
                response.keyValuePair = new Dictionary<string, string>();

                int lengthOfPair = LenencInt(data);
                int readData = 0;

                while (readData < lengthOfPair)
                {
                    string key = LenencStr(data);
                    string value = LenencStr(data);

                    if (key == "" || value == "") break;
                    response.keyValuePair.Add(key, value);
                    readData = readData + key.Length + value.Length + 2;
                }
            }
            return response;
        }

    }
}
