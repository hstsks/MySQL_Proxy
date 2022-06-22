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
        private static readonly ILog clientLogger = LogManager.GetLogger("client");

        public bool checkQuery(byte[] data)
        {
            List<Packet> packetList = SlicePacket(data.ToList<byte>());
            bool isClosed = false;

            foreach ( Packet packet in packetList)
            {
                if (packet.payload[0] == 3 && packet.payload[1] == 0 && packet.payload[2] == 1)
                {
                    parseQuery(packet);
                } else if (packet.payloadLength == 1 && packet.seqNumber == 0 && packet.payload[0] == 1)
                {
                    isClosed = true;
                }
            }

            return isClosed;
        }
        public string parseQuery(Packet packet)
        {
            string queryString = Encoding.UTF8.GetString(packet.payload.Skip(3).ToArray());
            clientLogger.Info(queryString);

            Regex chequer = new Regex("CHEQUER",RegexOptions.IgnoreCase);
            if (chequer.IsMatch(queryString))
            {
                Packet errorPacket = PermissionErrorPacket(packet.seqNumber);

                throw new PermissionDeniedException(errorPacket);
            }
            return queryString;
        }

        public Packet PermissionErrorPacket(int seq)
        {
            Packet packet = new Packet();

            byte[] errorHeader = { 255, 37, 5, 35, 55, 48, 49, 48, 48 };
            List<byte> errorMessage = Encoding.UTF8.GetBytes(" No permission to access the CHEQUER").ToList<byte>();
            List<byte> payload = new List<byte>(errorHeader);
            payload.AddRange(errorMessage);

            packet.payloadLength = payload.Count;
            packet.seqNumber = seq + 1;
            packet.payload = payload.ToArray();

            return packet;
        }
        public Packet PreparedLoginPacket(HandShakeResponse originalLogin)
        {
            Packet packet = new Packet();
            List<byte> payload = new List<byte>();
            byte empty = 0;
            string preparedAccount = "hstsks";
            byte[] preparedAuthData =
            {
                32, 242, 125, 189, 44, 42, 156, 101, 228, 1, 114, 204, 3, 51, 127, 52, 131, 149, 46, 63, 255, 10, 84, 192, 106, 53, 144, 50, 168, 210, 172, 46, 253
            };

            payload.AddRange(originalLogin.capacityFlags);
            payload.AddRange(BitConverter.GetBytes(originalLogin.maxPacketSize));
            payload.Add(originalLogin.charSet);
            for (int i = 9; i < 23; i++)
            {
                payload.Add(empty);
            }
            payload.AddRange(Encoding.UTF8.GetBytes(preparedAccount));
            payload.Add(empty);
            payload.AddRange(preparedAuthData.ToList<byte>());
            payload.AddRange(Encoding.UTF8.GetBytes("caching_sha2_password"));

            List<byte> keyValuePair = new List<byte>();
            foreach (KeyValuePair<string,string> pair in originalLogin.keyValuePair)
            {
                keyValuePair.AddRange(Int32ToLenencInt(pair.Key.Length));
                keyValuePair.AddRange(Encoding.UTF8.GetBytes(pair.Key));
                keyValuePair.AddRange(Int32ToLenencInt(pair.Value.Length));
                keyValuePair.AddRange(Encoding.UTF8.GetBytes(pair.Value));
            }
            List<byte> kayValuePayload = Int32ToLenencInt(keyValuePair.Count);
            kayValuePayload.AddRange(keyValuePair);
            payload.AddRange(kayValuePayload);

            return packet;
        }

        public HandShakeResponse parseHandShakeResponse(byte[] packet)
        {
            HandShakeResponse response = new HandShakeResponse();
            List<byte> data = packet.Skip(36).ToList<byte>();

            response.capacityFlags = packet.Skip(4).Take(4).ToArray();
            response.maxPacketSize = BitConverter.ToInt64(packet,8);
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

                int lengthOfPair = (int)LenencInt(data);
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
