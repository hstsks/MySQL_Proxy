using System;
using System.Threading;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Linq;
using MySQL_Proxy.state;
using MySQL_Proxy.parser;
using MySQL_Proxy.type;

namespace MySQL_Proxy.connector
{
    delegate void OnMessage(byte[] message);

    class Connector
    {
        protected Socket socket { get; set; }
        public OnMessage onMessage;
        public static ManualResetEvent isParseComplete = new ManualResetEvent(true);
        protected PacketCollector packetCollector { get; set; } = new PacketCollector();

        public Connector ()
        {
        }
        public void ReadCallback(IAsyncResult ar)
        {
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.workSocket;

            try
            {
                int byteRead = handler.EndReceive(ar);

                if (byteRead > 0)
                {
                    isParseComplete.WaitOne();
                    isParseComplete.Reset();

                    if (packetCollector.RefinePacket(state.buffer))
                    {
                        byte[] parsedBuffer = packetCollector.GetData();
                        packetCollector.ClearData();
                        this.onMessage(parsedBuffer);
                    }

                    isParseComplete.WaitOne();

                    handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
                }
            } 
            catch (ObjectDisposedException e)
            {

            } catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                Close();
            }
        }
        public void Close()
        {
            try
            {
                socket.Shutdown(SocketShutdown.Both);
            }
            finally
            {
                socket.Close();
            }
        }
        public void Send(byte[] data)
        {
            try
            {
                socket.BeginSend(data, 0, data.Length, 0, new AsyncCallback(SendCallback), socket);
            }
            catch (ObjectDisposedException e)
            {

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                Close();
            }
        }
        public void Send(Packet packet)
        {
            List<byte> payloadLength = BitConverter.GetBytes(packet.payloadLength).Take(3).ToList<byte>();
            List<byte> seqNumber = BitConverter.GetBytes(packet.seqNumber).Take(1).ToList<byte>();
            List<byte> payload = packet.payload.ToList<byte>();

            List<byte> packetData = new List<byte>(payloadLength);
            packetData.AddRange(seqNumber);
            packetData.AddRange(payload);

            Send(packetData.ToArray());
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
