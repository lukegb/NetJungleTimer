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
    public class NetProto
    {
        String host;
        int port;
        String username;
        String roomName;

        TcpClient tcpClient;
        NetworkStream netStream;
        public NetProtoUI parent;

        Thread recvThread;
        Thread sendThread;
        Thread pingThread;

        Queue<String> messageQueue;

        int reconnectAttempt;

        bool pingResponded = true;

        public bool IsMaster { get; private set; }


        public bool Connected { get { if (tcpClient == null) return false; return tcpClient.Connected; } }

        public NetProto(NetProtoUI parent, String host, int port, String username, String roomName)
        {
            this.parent = parent;

            this.host = host;
            this.port = port;
            this.username = username;
            this.roomName = roomName;
        }

        public void Go()
        {
            messageQueue = new Queue<string>();

            ThreadStart networkReceive = delegate()
            {
                while (true)
                {
                    if (tcpClient == null || netStream == null || !tcpClient.Connected || !netStream.CanRead)
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
                        StreamReader sr = new StreamReader(netStream);

                        String lineFromServer;
                        while ((lineFromServer = sr.ReadLine()) != null)
                        {
                            if (lineFromServer == "&PONG")
                            {
                                pingResponded = true;
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
            recvThread = new Thread(networkReceive);
            recvThread.Start();


            ThreadStart networkSend = delegate()
            {
                while (true)
                {
                    if (tcpClient == null || netStream == null || !tcpClient.Connected || !netStream.CanRead)
                    {
                        Thread.Sleep(300);
                        continue;
                    }

                    while (messageQueue.Count > 0)
                    {
                        string what = messageQueue.Dequeue();

                        byte[] gogo = Encoding.ASCII.GetBytes(what + "\n");
                        netStream.Write(gogo, 0, gogo.Length);
                    }

                    Thread.Sleep(20);
                }
            };
            sendThread = new Thread(networkSend);
            sendThread.Start();


            ThreadStart networkPing = delegate()
            {
                while (true)
                {
                    if (tcpClient == null || netStream == null || !tcpClient.Connected || !netStream.CanRead)
                    {
                        Thread.Sleep(300);
                        continue;
                    }

                    pingResponded = false;
                    messageQueue.Enqueue("&PING");
                    while (messageQueue.Count > 0)
                    {
                        Thread.Sleep(200);
                    }

                    Thread.Sleep(2500);

                    if (!pingResponded)
                    {
                        this.Disconnect();
                    }
                }
            };
            pingThread = new Thread(networkPing);
            pingThread.Start();
        }


        internal void Stop()
        {
            messageQueue.Enqueue("&DISCONNECT");
            while (messageQueue.Count > 0)
            {
                Thread.Sleep(20);
            }
            recvThread.Abort();
            sendThread.Abort();
            pingThread.Abort();
            this.Disconnect();
        }

        private void NotifyUI(String what)
        {
            parent.GetDispatcher().Invoke(DispatcherPriority.Normal,
                new Action<String>(parent.OnNetworkMessage),
                what);
        }


        private void Connect()
        {
            NotifyUI(String.Format("!RECONNECT {0}", ++this.reconnectAttempt));

            tcpClient = new TcpClient(this.host, this.port);
            netStream = tcpClient.GetStream();

            netStream.Write(System.Text.Encoding.ASCII.GetBytes("CONN\n"), 0, 5);

            String joinStr = String.Format("LOGIN {0} {1}\n", this.username, "-");
            byte[] joinBytes = System.Text.Encoding.ASCII.GetBytes(joinStr);
            netStream.Write(joinBytes, 0, joinBytes.Length);

            joinStr = String.Format("JOIN {0}\n", this.roomName);
            joinBytes = System.Text.Encoding.ASCII.GetBytes(joinStr);
            netStream.Write(joinBytes, 0, joinBytes.Length);

            this.reconnectAttempt = 0;
            NotifyUI("!CONNECTED");
        }

        private void Disconnect()
        {
            NotifyUI("!DISCONNECT");

            if (netStream != null)
                netStream.Close();
            if (tcpClient != null)
                tcpClient.Close();
            netStream = null;
            tcpClient = null;
        }

        internal void SendMessage(string what)
        {
            messageQueue.Enqueue(what);
        }
    }
}
