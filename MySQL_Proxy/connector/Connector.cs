using System;
using System.Net.Sockets;
using MySQL_Proxy.state;
using MySQL_Proxy.parser;
using System.Threading;

namespace MySQL_Proxy.connector
{
    delegate void OnMessage(byte[] message);

    class Connector
    {
        protected Socket socket { get; set; }
        public OnMessage onMessage;
        public static ManualResetEvent isParseComplete = new ManualResetEvent(true);
        private PacketParser packetParser = new PacketParser();

        public Connector ()
        {
        }
        public void ReadCallback(IAsyncResult ar)
        {
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.workSocket;

            int byteRead = handler.EndReceive(ar);

            if (byteRead > 0)
            {
                isParseComplete.WaitOne();
                isParseComplete.Reset();

                if (packetParser.ParsePacket(state.buffer))
                {
                    byte[] parsedBuffer = packetParser.GetData();
                    packetParser.ClearData();
                    this.onMessage(parsedBuffer);
                }

                isParseComplete.WaitOne();

                handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
            }
        }
        public void Send(byte[] data)
        {
            socket.BeginSend(data, 0, data.Length, 0, new AsyncCallback(SendCallback), socket);
        }
        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                Socket handler = (Socket)ar.AsyncState;
                int bytesSent = handler.EndSend(ar);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

    }
}
