using System;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using MySQL_Proxy.state;

namespace MySQL_Proxy.connector
{
    class DataBaseConnector : Connector
    {
        private const int port = 3306;

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

                Console.WriteLine("Socket connected to : {0}\n", socket.RemoteEndPoint.ToString());

                StateObject state = new StateObject();
                state.workSocket = socket;

                socket.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
