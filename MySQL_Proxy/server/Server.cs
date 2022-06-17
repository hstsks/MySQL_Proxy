using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using MySQL_Proxy.service;

namespace MySQL_Proxy.server
{
    class Server
    {
        public static ManualResetEvent allDone = new ManualResetEvent(false);

        public static void StartListening(int port)
        {
            Service service = new Service();

            byte[] localhost = { 127, 0, 0, 1 };
            IPAddress localhostAddress = new IPAddress(localhost);
            IPEndPoint localEndPoint = new IPEndPoint(localhostAddress, port);

            Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(100);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            new Thread(() =>
            {
                while (true)
                {
                    allDone.Reset();

                    Console.WriteLine("Waiting for a connection.");
                    listener.BeginAccept(new AsyncCallback(service.Create), listener);

                    allDone.WaitOne();
                }
            }).Start();

            Console.WriteLine("server sart on {0}",port);
        }
    }
}
