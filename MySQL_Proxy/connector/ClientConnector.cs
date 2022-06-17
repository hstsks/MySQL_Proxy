using System;
using System.Net.Sockets;
using MySQL_Proxy.state;

namespace MySQL_Proxy.connector
{
    class ClientConnector : Connector
    {
        public ClientConnector(IAsyncResult ar) : base()
        {
            server.Server.allDone.Set();

            Socket listener = (Socket)ar.AsyncState;
            socket = listener.EndAccept(ar);

            StateObject state = new StateObject() { workSocket = socket };

            socket.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
        }
    }
}
