using System;
using System.Threading;
using log4net;
using System.Text;
using MySQL_Proxy.connector;
using MySQL_Proxy.parser;
using MySQL_Proxy.exception;
using MySQL_Proxy.type;

namespace MySQL_Proxy.handler
{
    delegate void OnClose(string id);
    class Handler
    {
        public string handlerID
        {
            get { return clientConnector.clientID; }
        }

        public OnClose onClose;

        private ClientPacketParser clientPacketParser = new ClientPacketParser();

        private ClientConnector clientConnector;
        private DataBaseConnector dataBaseConnector;

        //private static readonly ILog clientLogger = LogManager.GetLogger("client");
        //private static readonly ILog dataBaseLogger = LogManager.GetLogger("database");

        public Handler(IAsyncResult ar)
        {
            clientConnector = new ClientConnector(ar);
            dataBaseConnector = new DataBaseConnector();

            clientConnector.onMessage = new OnMessage(OnClientMessage);
            dataBaseConnector.onMessage = new OnMessage(OnDataBaseMessage);

            clientConnector.ParseResponse = new ParseResponse(ParseHandShakeRes);
        }

        private void OnClientMessage(byte[] data)
        {
            try
            {
                bool isClosed = clientPacketParser.checkQuery(data);
                dataBaseConnector.Send(data);
                if (isClosed) OnClose();
            }
            catch (PermissionDeniedException e)
            {
                Console.WriteLine("permission denied exception");
                dataBaseConnector.Send(e.errorPacket);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                throw e;
            }
        }
        private void OnDataBaseMessage(byte[] data)
        {
            clientConnector.Send(data);
        }

        private HandShakeResponse ParseHandShakeRes(byte[] input)
        {
            HandShakeResponse response = clientPacketParser.parseHandShakeResponse(input);
            if (response.authPluginName == "mysql_clear_password")
            {
                dataBaseConnector.Send(clientPacketParser.PreparedLoginPacket(response));
            } else
            {
                OnClientMessage(input);
            }

            return response;
        }

        private void OnClose()
        {
            clientConnector.Close();
            dataBaseConnector.Close();
            onClose(handlerID);
        }
    }
}
