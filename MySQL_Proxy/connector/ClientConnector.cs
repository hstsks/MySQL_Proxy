using System;
using System.Net.Sockets;
using MySQL_Proxy.state;
using MySQL_Proxy.type;

namespace MySQL_Proxy.connector
{
    delegate HandShakeResponse ParseResponse(byte[] data);
    class ClientConnector : Connector
    {
        public string clientID;
        public ParseResponse ParseResponse;
        public ClientConnector(IAsyncResult ar) : base()
        {
            server.Server.allDone.Set();

            Socket listener = (Socket)ar.AsyncState;
            socket = listener.EndAccept(ar);
            Console.WriteLine("Socket connected to : {0}\n", socket.RemoteEndPoint.ToString());

            StateObject state = new StateObject() { workSocket = socket };

            socket.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadResponseCallback), state);
        }

        public void ReadResponseCallback(IAsyncResult ar)
        {
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.workSocket;

            int byteRead = handler.EndReceive(ar);

            if (byteRead > 0)
            {
                isParseComplete.WaitOne();
                isParseComplete.Reset();

                if (packetCollector.RefinePacket(state.buffer))
                {
                    byte[] parsedBuffer = packetCollector.GetData();
                    packetCollector.ClearData();

                    HandShakeResponse handShakeResponse = ParseResponse(parsedBuffer);
                    clientID = handShakeResponse.userName + DateTime.Now.ToString();
                }

                isParseComplete.WaitOne();

                handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
            }
        }
    }
}
