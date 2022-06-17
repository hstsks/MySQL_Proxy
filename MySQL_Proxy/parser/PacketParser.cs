using System;
using System.Collections.Generic;
using System.Linq;
using log4net;

namespace MySQL_Proxy.parser
{
    class PacketParser
    {
        private readonly ILog parsingLogger = LogManager.GetLogger("parser");
        private readonly List<byte> fullData = new List<byte>();
        private List<byte> remainData = new List<byte>();


        public byte[] GetData()
        {
            return fullData.ToArray();
        }

        public void ClearData()
        {
            fullData.Clear();
        }

        public bool ParsePacket(byte[] input)
        {
            List<byte> inputData = input.ToList<byte>();
            for (int i = 0; i < remainData.Count; i++)
            {
                inputData.Insert(i, remainData[i]);
            }
            remainData.Clear();

            int seqNumber = inputData[3];
            int param = 0;

            while (true)
            {
                if (param + 4 > inputData.Count)
                {
                    remainData = inputData.Skip(param).Take(inputData.Count - param).ToList();
                    break;
                }

                int size = inputData[param] + inputData[param + 1] * 256 + inputData[param + 2] * 256 * 256 + 4;
                List<byte> dump = inputData.Skip(param).Take(size).ToList();

                if (seqNumber != inputData[param + 3])
                {
                    Array.Clear(input, 0, input.Length);
                    connector.Connector.isParseComplete.Set();

                    return true;
                }

                seqNumber = seqNumber == 255 ? 0 : seqNumber + 1;
                Console.WriteLine(seqNumber);

                if (size > dump.Count)
                {
                    remainData = dump;
                    break;
                }

                for (int j = 0; j < dump.Count; j++)
                {
                    fullData.Add(dump[j]);
                }

                param = param + size;

            }

            Array.Clear(input, 0, input.Length);
            connector.Connector.isParseComplete.Set();

            return false;
        }
    }
}
