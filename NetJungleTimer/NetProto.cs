using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetJungleTimer
{
    public interface NetProto
    {
        bool IsMaster { get; }

        void Go();
        void Stop();

        void SendMessage(String message);

        NetProtoUI Parent { get; set; }
    }
}
