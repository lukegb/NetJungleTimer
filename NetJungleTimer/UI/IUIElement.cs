using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetJungleTimer.UI
{
    interface IUIElement
    {
        bool GotKey(KeyboardManager.KMKey Key);
        void UpdateComponent(DateTime now);
        void GotMessage(String message);
        void SyncData();
    }
}
