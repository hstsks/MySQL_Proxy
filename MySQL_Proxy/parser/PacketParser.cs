using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using MySQL_Proxy.type;

namespace MySQL_Proxy.parser
{
    class PacketParser
    {
        public List<Packet> SlicePacket(List<byte> data)
        {
            List<Packet> packetList = new List<Packet>();
            int param = 0;

            while (true)
            {
                Packet packet = new Packet();

                packet.payloadLength = HexArrToInt(data.Skip(param).Take(3).ToArray());
                packet.seqNumber = data[param + 3];
                packet.payload = data.Skip(param + 4).Take(packet.payloadLength).ToArray();

                packetList.Add(packet);

                param = param + 4 + packet.payloadLength;
                if (param + 1 > data.Count) break;
            }

            return packetList;
        }

        public int HexArrToInt(byte[] hex)
        {
            int result = 0;
            int pow = 1;
            for (int i = 0; i < hex.Length; i++)
            {
                result = result + hex[i] * pow;
                pow = pow * 256;
            }
            return result;
        }
        public List<byte> Int32ToLenencInt (int num)
        {
            List<byte> result = new List<byte>();
            byte[] data = BitConverter.GetBytes(num);

            if (num < 256)
            {
                result.Add(data[0]);
            }
            else if (num < 256 * 256)
            {
                byte size = 253;
                result.Add(size);
                result.AddRange(data.Take(2).ToList<byte>());
            } else
            {
                byte size = 254;
                result.Add(size);
                result.AddRange(data.Take(3).ToList<byte>());
            }
            return result;
        }

        public string NullTerminateStr(List<byte> input)
        {
            List<byte> str = new List<byte>();
            while (input[0] != 0)
            {
                str.Add(input[0]);
                input.RemoveAt(0);
            }
            input.RemoveAt(0);
            return Encoding.UTF8.GetString(str.ToArray());
        }
        public string LenencStr(List<byte> input)
        {
            int length = (int)LenencInt(input);
            if (length == 0) return "";
            byte[] str = input.Take(length).ToArray();
            input.RemoveRange(0, length);

            return Encoding.UTF8.GetString(str);
        }
        public ulong LenencInt(List<byte> input)
        {
            ulong result;
            if (input[0] < 251)
            {
                result = input[0];
                input.RemoveAt(0);
            } else if (input[0] == 253)
            {
                result = BitConverter.ToUInt16(input.Take(3).ToArray(), 1);
                input.RemoveRange(0, 3);
            } else if (input[0] == 254)
            {
                byte empty = 0;
                result = BitConverter.ToUInt32(input.Take(4).Append(empty).ToArray(), 1);
                input.RemoveRange(0, 5);

            } else if (input[0] == 255)
            {
                result = BitConverter.ToUInt64(input.Take(9).ToArray(), 1);
                input.RemoveRange(0, 9);
            } else
            {
                throw new NullReferenceException();
            }
            return result;
        }
    }
}
