using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Threading;

namespace NetJungleTimer
{
    public class LiveNetProto : INetProto
    {
        String HostName;
        int Port;
        String UserName;
        String RoomName;

        TcpClient TcpClient;
        NetworkStream NetStream;

        Thread RecvThread;
        Thread SendThread;
        Thread PingThread;

        Queue<String> MessageQueue;

        int ReconnectAttempt;

        bool RespondedToPing = true;

        public bool IsMaster { get; private set; }

        public bool Connected { get { if (TcpClient == null) return false; return TcpClient.Connected; } }

        public event NewNetworkMessageHandler NewNetworkMessage;

        public LiveNetProto(String host, int port, String username, String roomName)
        {
            this.HostName = host;
            this.Port = port;
            this.UserName = username;
            this.RoomName = roomName;
        }

        public void Go()
        {
            MessageQueue = new Queue<string>();

            ThreadStart networkReceive = delegate()
            {
                while (true)
                {
                    if (TcpClient == null || NetStream == null || !TcpClient.Connected || !NetStream.CanRead)
                    {
                        try
                        {
                            this.Disconnect();
                            this.Connect();
                        }
                        catch
                        {
                        }

                        Thread.Sleep(300);
                        continue;
                    }

                    try
                    {

                        byte[] netReadBuff = new byte[1024];
                        StreamReader sr = new StreamReader(NetStream);

                        String lineFromServer;
                        while ((lineFromServer = sr.ReadLine()) != null)
                        {
                            if (lineFromServer == "&PONG")
                            {
                                RespondedToPing = true;
                                continue;
                            }
                            else if (lineFromServer.StartsWith("&NOTMASTER"))
                            {
                                IsMaster = false;
                            }
                            else if (lineFromServer.StartsWith("&NEWMASTER"))
                            {
                                IsMaster = true;
                            }
                            Console.WriteLine("<- {0}", lineFromServer);
                            NotifyUI(lineFromServer);
                        }
                    }
                    catch
                    {
                    }

                        Thread.Sleep(300);
                }
            };
            RecvThread = new Thread(networkReceive);
            RecvThread.Start();


            ThreadStart networkSend = delegate()
            {
                while (true)
                {
                    if (TcpClient == null || NetStream == null || !TcpClient.Connected || !NetStream.CanRead)
                    {
                        Thread.Sleep(300);
                        continue;
                    }

                    while (MessageQueue.Count > 0)
                    {
                        string what = MessageQueue.Dequeue();

                        byte[] gogo = Encoding.ASCII.GetBytes(what + "\n");
                        NetStream.Write(gogo, 0, gogo.Length);
                    }

                    Thread.Sleep(20);
                }
            };
            SendThread = new Thread(networkSend);
            SendThread.Start();


            ThreadStart networkPing = delegate()
            {
                while (true)
                {
                    if (TcpClient == null || NetStream == null || !TcpClient.Connected || !NetStream.CanRead)
                    {
                        Thread.Sleep(300);
                        continue;
                    }

                    RespondedToPing = false;
                    MessageQueue.Enqueue("&PING");
                    while (MessageQueue.Count > 0)
                    {
                        Thread.Sleep(200);
                    }

                    Thread.Sleep(2500);

                    if (!RespondedToPing)
                    {
                        this.Disconnect();
                    }
                }
            };
            PingThread = new Thread(networkPing);
            PingThread.Start();
        }


        public void Stop()
        {
            if (MessageQueue != null)
            {
                MessageQueue.Enqueue("&DISCONNECT");
                while (Connected && MessageQueue.Count > 0)
                {
                    Thread.Sleep(20);
                }
                MessageQueue.Clear();
            }

            if (RecvThread != null)
                RecvThread.Abort();
            RecvThread = null;

            if (SendThread != null)
                SendThread.Abort();
            SendThread = null;

            if (PingThread != null)
                PingThread.Abort();
            PingThread = null;

            this.Disconnect();
        }

        private void NotifyUI(String what)
        {
            if (this.NewNetworkMessage != null)
                this.NewNetworkMessage(this, new NewNetworkMessageEventArgs(what));
        }


        private void Connect()
        {
            NotifyUI(String.Format("!RECONNECT {0}", ++this.ReconnectAttempt));

            TcpClient = new TcpClient(this.HostName, this.Port);
            NetStream = TcpClient.GetStream();

            NetStream.Write(System.Text.Encoding.ASCII.GetBytes("CONN\n"), 0, 5);

            String joinStr = String.Format("LOGIN {0} {1}\n", this.UserName, "-");
            byte[] joinBytes = System.Text.Encoding.ASCII.GetBytes(joinStr);
            NetStream.Write(joinBytes, 0, joinBytes.Length);

            joinStr = String.Format("JOIN {0}\n", this.RoomName);
            joinBytes = System.Text.Encoding.ASCII.GetBytes(joinStr);
            NetStream.Write(joinBytes, 0, joinBytes.Length);

            this.ReconnectAttempt = 0;
            NotifyUI("!CONNECTED");
        }

        private void Disconnect()
        {
            NotifyUI("!DISCONNECT");

            if (NetStream != null)
                NetStream.Close();
            if (TcpClient != null)
                TcpClient.Close();
            NetStream = null;
            TcpClient = null;
        }

        public void SendMessage(string what)
        {
            MessageQueue.Enqueue(what);
        }
    }
}
