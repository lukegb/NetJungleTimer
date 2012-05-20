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
    class NetProto
    {
        String host;
        int port;

        TcpClient tcpClient;
        NetworkStream netStream;
        MainWindow parent;

        Thread myThread;

        public bool Connected { get { if (tcpClient == null) return false; return tcpClient.Connected; } }

        public NetProto(MainWindow parent, String host, int port)
        {
            this.parent = parent;

            this.host = host;
            this.port = port;
        }

        public void Go()
        {
            ThreadStart handleNetworking = delegate()
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

                    byte[] netReadBuff = new byte[1024];
                    StreamReader sr = new StreamReader(netStream);

                    String lineFromServer;
                    while ((lineFromServer = sr.ReadLine()) != null)
                    {
                        Console.WriteLine("<- {0}", lineFromServer);
                        NotifyUI(lineFromServer);
                    }

                    Thread.Sleep(300);
                }
            };
            myThread = new Thread(handleNetworking);
            myThread.Start();
        }

        private void NotifyUI(String what)
        {
            parent.Dispatcher.Invoke(DispatcherPriority.Normal,
                new Action<String>(parent.OnNetworkMessage),
                what);
        }


        private void Connect()
        {
            tcpClient = new TcpClient(this.host, this.port);
            netStream = tcpClient.GetStream();

            netStream.Write(System.Text.Encoding.ASCII.GetBytes("CONN\n"), 0, 5);
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
            Console.Write(what);

            if (tcpClient == null || netStream == null || !tcpClient.Connected || !netStream.CanWrite)
            {
                Disconnect();
                Connect();
            }

            byte[] gogo = Encoding.ASCII.GetBytes(what + "\n");
            netStream.Write(gogo, 0, gogo.Length);
        }
    }
}
