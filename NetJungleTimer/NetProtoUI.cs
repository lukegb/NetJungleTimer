using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Threading;

namespace NetJungleTimer
{
    public interface NetProtoUI
    {
        void OnNetworkMessage(String message);

        Dispatcher GetDispatcher();
    }
}
