using System;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using MySQL_Proxy.state;
using MySQL_Proxy.type;

namespace MySQL_Proxy.connector
{
    delegate InitHandShake ParseHandShake(byte[] data);
    class DataBaseConnector : Connector
    {
        private const int port = 3306;
        public ParseHandShake ParseHandShake;

        public DataBaseConnector() : base()
        {
            try
            {
                IPHostEntry ipHostInfo = Dns.GetHostEntry("");
                IPAddress ipAddress = ipHostInfo.AddressList[0];
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);

                socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                socket.BeginConnect(remoteEP, new AsyncCallback(ConnectCallback), socket);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                socket = (Socket)ar.AsyncState;

                socket.EndConnect(ar);

                StateObject state = new StateObject();
                state.workSocket = socket;

                socket.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadHandShakeCallback), state);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private void ReadHandShakeCallback(IAsyncResult ar)
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

                    InitHandShake initHandShake = ParseHandShake(parsedBuffer);
                }

                isParseComplete.WaitOne();

                handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
            }
        }
    }
}
