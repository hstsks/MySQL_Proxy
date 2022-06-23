using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MySQL_Proxy.type;

namespace MySQL_Proxy.parser
{
    class DataBasePacketParser : PacketParser
    {
        public InitHandShake ParseInitHandShake(byte[] packet)
        {
            InitHandShake handShake = new InitHandShake();
            List<byte> data = packet.Skip(5).ToList<byte>();

            handShake.protocolVer = packet[4];
            handShake.serverVer = NullTerminateStr(data);
            handShake.connectionID = data.Take(4).ToArray();

            List<byte> authPluginByte = data.Skip(4).Take(8).ToList<byte>();
            List<byte> capacityByte = data.Skip(13).Take(2).ToList<byte>();

            data.RemoveRange(0, 15);

            //if no more data in the packet
            if(data[0] == 0)
            {
                handShake.authPluginData = Encoding.UTF8.GetString(authPluginByte.ToArray());
                handShake.capacityFlags = capacityByte.ToArray();

                return handShake;
            }

            handShake.charSet = data[0];

            handShake.statusFlags = data.Skip(1).Take(2).ToArray();

            capacityByte.AddRange(data.Skip(3).Take(2).ToList<byte>());
            handShake.capacityFlags = capacityByte.ToArray();

            //length of plugin data
            data.RemoveRange(0, 5);
            int lengthOfAuthPluginData = 0;
            if (handShake.capacityFlags[3] % 16 > 8)
            {
                lengthOfAuthPluginData = lengthOfAuthPluginData + data[0];
            } 
            data.RemoveRange(0, 11);

            //auth-plugin-data-2
            if (handShake.capacityFlags[2] > 128)
            {
                int size;
                if (lengthOfAuthPluginData > 21) 
                {
                    size = 13;
                } else if (lengthOfAuthPluginData < 8)
                {
                    size = 0;
                }
                else
                {
                    size = lengthOfAuthPluginData - 8;
                }
                authPluginByte.AddRange(data.Take(size));
                data.RemoveRange(0, size);
            }

            handShake.authPluginData = Encoding.UTF8.GetString(authPluginByte.ToArray());

            //auth-plugin-name
            if (handShake.capacityFlags[3] % 16 > 8)
            {
                handShake.authPluginName = NullTerminateStr(data);
            }

            return handShake;
        }
    }
}
