using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace NetJungleTimer
{
    class NetProto
    {
        String host;
        int port;

        TcpClient tcpClient;
        NetworkStream netStream;

        String dangling = "";

        public bool Connected { get { if (tcpClient == null) return false; return tcpClient.Connected; } }

        public NetProto(String host, int port)
        {
            this.host = host;
            this.port = port;
        }

        public void Connect()
        {
            tcpClient = new TcpClient(this.host, this.port);
            netStream = tcpClient.GetStream();

            netStream.Write(System.Text.Encoding.ASCII.GetBytes("CONN\n"), 0, 5);
        }

        public void Disconnect()
        {
            if (netStream != null)
                netStream.Close();
            if (tcpClient != null)
                tcpClient.Close();
            netStream = null;
            tcpClient = null;
        }

        public String ReadData()
        {
            if (tcpClient == null || netStream == null || !tcpClient.Connected || !netStream.CanRead)
                return "RECONNECT";

            if (!netStream.DataAvailable)
                return null;

            byte[] netReadBuff = new byte[1024];
            StringBuilder netReadBuffSb = new StringBuilder(dangling);
            dangling = "";
            int numBytes = 0;

            do
            {
                numBytes = netStream.Read(netReadBuff, 0, netReadBuff.Length);
                netReadBuffSb.AppendFormat("{0}", Encoding.ASCII.GetString(netReadBuff, 0, numBytes));
            }
            while (netStream.DataAvailable);

            String[] outs = netReadBuffSb.ToString().Split(new char[] {'\n'}, 2);

            if (outs.Length == 2)
                dangling = outs[1];

            return outs[0];
        }

        internal void SendMessage(string what)
        {
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
