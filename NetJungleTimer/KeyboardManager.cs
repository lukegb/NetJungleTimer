using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace NetJungleTimer
{
    public class KeyboardManager
    {
        Dictionary<int, int> currentKeyState = new Dictionary<int, int>();
        Dictionary<int, int> previousKeyState = new Dictionary<int, int>();

        MainWindow parent;

        public KeyboardManager(MainWindow parent)
        {
            this.parent = parent;
        }

        public void Update()
        {
            int[] arrayList = currentKeyState.Keys.ToArray();
            foreach (int k in arrayList)
            {
                previousKeyState[k] = currentKeyState[k];
                currentKeyState[k] = WindowsApi.GetKeyState(k);
                if (currentKeyState[k] < 0 && previousKeyState[k] >= 0)
                    parent.OnHotKeyHandler(KeyInterop.KeyFromVirtualKey(k));
            }
        }

        public void ListenToKey(Key newKeyKey)
        {
            int newKey = KeyInterop.VirtualKeyFromKey(newKeyKey);
            if (!currentKeyState.ContainsKey(newKey))
                currentKeyState[newKey] = previousKeyState[newKey] = 0;
        }
    }
}
