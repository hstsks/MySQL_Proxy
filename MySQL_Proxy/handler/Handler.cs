using System;
using System.Text;
using System.Threading;
using log4net;
using MySQL_Proxy.connector;

namespace MySQL_Proxy.handler
{
    class Handler
    {
        private static readonly ILog clientLogger = LogManager.GetLogger("client");
        private static readonly ILog DataBaseLogger = LogManager.GetLogger("database");


        public static ManualResetEvent isClientReceive = new ManualResetEvent(false);
        public static ManualResetEvent isDataBaseReceive = new ManualResetEvent(true);

        private ClientConnector clientConnector;
        private DataBaseConnector dataBaseConnector;

        public Handler(IAsyncResult ar)
        {
            clientConnector = new ClientConnector(ar);
            dataBaseConnector = new DataBaseConnector();

            clientConnector.onMessage = new OnMessage(OnClientMessage);
            dataBaseConnector.onMessage = new OnMessage(OnDataBaseMessage);
        }

        private void OnClientMessage(byte[] data)
        {
            dataBaseConnector.Send(data);
        }
        private void OnDataBaseMessage(byte[] data)
        {
            clientConnector.Send(data);
        }
    }
}
