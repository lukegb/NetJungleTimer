using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using System.Threading;
using System.Runtime.InteropServices;

namespace NetJungleTimer
{
    public class KeyboardManager
    {


        MainWindow parent;
        bool ctrlDown = false;
        bool altDown = false;
        bool shiftDown = false;

        public class KMKey : IEquatable<KMKey>
        {
            private bool _ctrlDown = false;
            private bool _altDown = false;
            private bool _shiftDown = false;

            private Key _key;

            public bool CtrlDown
            {
                get
                {
                    return _ctrlDown;
                }
                private set
                {
                    _ctrlDown = value;
                }
            }

            public bool AltDown
            {
                get
                {
                    return _altDown;
                }
                private set
                {
                    _altDown = value;
                }
            }

            public bool ShiftDown
            {
                get
                {
                    return _shiftDown;
                }
                private set
                {
                    _shiftDown = value;
                }
            }

            public Key Key
            {
                get
                {
                    return _key;
                }
                private set
                {
                    _key = value;
                }
            }

            public KMKey(Key datKey, bool datCtrlDown, bool datAltDown, bool datShiftDown)
            {
                Key = datKey;
                CtrlDown = datCtrlDown;
                AltDown = datAltDown;
                ShiftDown = datShiftDown;
            }

            public KMKey(Key datKey)
            {
                Key = datKey;
                CtrlDown = false;
                AltDown = false;
                ShiftDown = false;
            }

            public bool Equals(KMKey other)
            {
                return (
                    other.Key == this.Key &&
                    other.CtrlDown == this.CtrlDown &&
                    other.AltDown == this.AltDown &&
                    other.ShiftDown == this.CtrlDown
                    ) ;
            }

            public override bool Equals(object obj)
            {
                if (obj == null)
                    return false;

                KMKey kmkObj = obj as KMKey;
                if (kmkObj == null)
                    return false;
                else
                    return Equals(kmkObj);
            }

            public static bool operator == (KMKey kmk1, KMKey kmk2)
            {
                if ((object)kmk1 == null || (object)kmk2 == null)
                    return Object.Equals(kmk1, kmk2);

                return kmk1.Equals(kmk2);
            }

            public static bool operator != (KMKey kmk1, KMKey kmk2)
            {
                return !(kmk1 == kmk2);
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }

            public override String ToString()
            {
                return String.Format("<Key: {0}, CtrlDown: {1}, AltDown: {2}, ShiftDown: {3}>", Key, CtrlDown, AltDown, ShiftDown);
            }
        }

        List<KMKey> keysPressed = new List<KMKey>();
        List<KMKey> runningKeys = new List<KMKey>();

        private WindowsApi.User32.HookProc keyPressDelegate = null;

        public KeyboardManager(MainWindow parent)
        {
            this.parent = parent;

            this.keyPressDelegate = new WindowsApi.User32.HookProc(this.keyboardCallback);

            WindowsApi.User32.SetWindowsHookEx(WindowsApi.User32.HookType.WH_KEYBOARD_LL, this.keyPressDelegate, IntPtr.Zero, 0);
        }

        private int keyboardCallback(int code, IntPtr wParam, [In] WindowsApi.User32.KBDLLHOOKSTRUCT lParam)
        {
            // if it's not a key being pressed
            if (code < 0) // we need to return callnexthookex
                return WindowsApi.User32.CallNextHookEx(IntPtr.Zero, code, wParam, lParam);

            Key pressedKey = KeyInterop.KeyFromVirtualKey((int)lParam.vkCode); // let's convert the key to a key :P

            switch (wParam.ToInt32())
            {
                case (Int32)WindowsApi.User32.KeyboardMessageType.WM_KEYDOWN:
                case (Int32)WindowsApi.User32.KeyboardMessageType.WM_SYSKEYDOWN:
                    KeyDown(pressedKey);
                    break;
                case (Int32)WindowsApi.User32.KeyboardMessageType.WM_KEYUP:
                case (Int32)WindowsApi.User32.KeyboardMessageType.WM_SYSKEYUP:
                    KeyUp(pressedKey);
                    break;
            }
            //Console.WriteLine(pressedKey);

            return WindowsApi.User32.CallNextHookEx(IntPtr.Zero, code, wParam, lParam);
        }

        private void ResetControlKeys()
        {
            int[] keyList = new int[] {
                0x10, // VK_SHIFT
                0x11, // VK_CONTROL
                0x12, // VK_MENU (i.e. ALT)
            };

            foreach (int datKey in keyList)
            {
                int datVirtKey = WindowsApi.GetKeyState(datKey);
                int mostSigBit = (int)(datVirtKey & 0xFFFF0000);
                //int leastSigBit = (int)(datVirtKey & 0x0000FFFF);
                bool isSet = true;

                if (mostSigBit == 0)
                    isSet = false;

                switch (datKey)
                {
                    case 0x10:
                        shiftDown = isSet;
                        break;
                    case 0x11:
                        ctrlDown = isSet;
                        break;
                    case 0x12:
                        altDown = isSet;
                        break;
                }
            }
        }

        private void KeyDown(Key whatKey)
        {
            if (whatKey == Key.LeftCtrl || whatKey == Key.RightCtrl)
                return;
            else if (whatKey == Key.LeftAlt || whatKey == Key.RightAlt)
                return;
            else if (whatKey == Key.LeftShift || whatKey == Key.RightShift)
                return;
            else
            {
                foreach (KMKey kmk in keysPressed)
                {
                    if (kmk.Key.Equals(whatKey))
                    {
                        return;
                    }
                }

                ResetControlKeys();
                KMKey akmk = new KMKey(whatKey, ctrlDown, altDown, shiftDown);
                keysPressed.Add(akmk);
                
                Console.WriteLine("KEYDOWN: {0} (ctrl: {1}, alt: {2}, shift: {3})", whatKey, ctrlDown, altDown, shiftDown);
                Console.WriteLine(runningKeys.Contains(akmk));
                foreach (KMKey kmk in runningKeys)
                {
                    Console.WriteLine(kmk);
                }
                if (runningKeys.Contains(akmk))
                {
                    parent.OnHotKeyHandler(akmk);
                }
            }
        }

        private void KeyUp(Key whatKey)
        {
            if (whatKey == Key.LeftCtrl || whatKey == Key.RightCtrl)
                return;
            else if (whatKey == Key.LeftAlt || whatKey == Key.RightAlt)
                return;
            else if (whatKey == Key.LeftShift || whatKey == Key.RightShift)
                return;
            else
            {
                //Console.WriteLine(whatKey);
                KMKey[] keysPressedA = new KMKey[keysPressed.Count()];
                keysPressed.CopyTo(keysPressedA, 0);
                foreach (KMKey kmk in keysPressedA)
                {
                    if (kmk.Key.Equals(whatKey))
                    {
                        //Console.WriteLine("KEYUP: {0} (ctrl: {1}, alt: {2}, shift: {3})", whatKey, kmk.CtrlDown, kmk.AltDown, kmk.ShiftDown);
                        keysPressed.Remove(kmk);
                        break;
                    }
                }
            }
        }

        public void ListenToKey(KMKey newKeyKey)
        {
            runningKeys.Add(newKeyKey);
        }
    }
}
