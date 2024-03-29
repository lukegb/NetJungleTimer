﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetJungleTimer.Networking
{
    public class NewNetworkMessageEventArgs : EventArgs
    {
        public string NetworkMessage;

        public NewNetworkMessageEventArgs(string NetworkMessage)
        {
            this.NetworkMessage = NetworkMessage;
        }
    }

    public delegate void NewNetworkMessageHandler(object sender, NewNetworkMessageEventArgs e);

    public interface INetProto
    {
        bool IsMaster { get; }
        bool Connected { get; }

        void Go();
        void Stop();

        void SendMessage(String message);

        event NewNetworkMessageHandler NewNetworkMessage;
    }
}
