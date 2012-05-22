using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetJungleTimer
{
    class MockupNetProto : INetProto
    {
        public bool IsMaster
        {
            get
            {
                return true;
            }
        }

        public void Go() { }

        public void Stop() { }

        public void SendMessage(string message) { }

        public event NewNetworkMessageHandler NewNetworkMessage;
    }
}
