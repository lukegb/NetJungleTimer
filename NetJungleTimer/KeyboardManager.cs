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
                    other.Key.Equals(Key) &&
                    other.CtrlDown == this.CtrlDown &&
                    other.AltDown == this.AltDown &&
                    other.ShiftDown == this.ShiftDown
                    );
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

            public static bool operator ==(KMKey kmk1, KMKey kmk2)
            {
                if ((object)kmk1 == null || (object)kmk2 == null)
                    return Object.Equals(kmk1, kmk2);

                return kmk1.Equals(kmk2);
            }

            public static bool operator !=(KMKey kmk1, KMKey kmk2)
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

        IntPtr kbdHook;

        private WindowsApi.User32.HookProc keyPressDelegate = null;

        public KeyboardManager(MainWindow parent)
        {
            this.parent = parent;

            this.keyPressDelegate = new WindowsApi.User32.HookProc(this.keyboardCallback);

            kbdHook = WindowsApi.User32.SetWindowsHookEx(WindowsApi.User32.HookType.WH_KEYBOARD_LL, this.keyPressDelegate, IntPtr.Zero, 0);
        }

        ~KeyboardManager()
        {
            WindowsApi.User32.UnhookWindowsHookEx(this.kbdHook);
        }

        private int keyboardCallback(int code, IntPtr wParam, [In] WindowsApi.User32.KBDLLHOOKSTRUCT lParam)
        {
            // if it's not a key being pressed
            if (code < 0) // we need to return callnexthookex
                return WindowsApi.User32.CallNextHookEx(IntPtr.Zero, code, wParam, lParam);

            Key pressedKey = KeyInterop.KeyFromVirtualKey((int)lParam.vkCode); // let's convert the key to a key :P

            bool suppress = false;

            switch (wParam.ToInt32())
            {
                case (Int32)WindowsApi.User32.KeyboardMessageType.WM_KEYDOWN:
                case (Int32)WindowsApi.User32.KeyboardMessageType.WM_SYSKEYDOWN:
                    suppress = KeyDown(pressedKey);
                    break;
                case (Int32)WindowsApi.User32.KeyboardMessageType.WM_KEYUP:
                case (Int32)WindowsApi.User32.KeyboardMessageType.WM_SYSKEYUP:
                    suppress = KeyUp(pressedKey);
                    break;
            }

            if (suppress)
                return 1;

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

        private bool KeyDown(Key whatKey)
        {
            if (whatKey == Key.LeftCtrl || whatKey == Key.RightCtrl)
                return false;
            else if (whatKey == Key.LeftAlt || whatKey == Key.RightAlt)
                return false;
            else if (whatKey == Key.LeftShift || whatKey == Key.RightShift)
                return false;
            else
            {
                foreach (KMKey kmk in keysPressed)
                {
                    if (kmk.Key.Equals(whatKey))
                    {
                        return false;
                    }
                }

                ResetControlKeys();
                KMKey akmk = new KMKey(whatKey, ctrlDown, altDown, shiftDown);
                keysPressed.Add(akmk);

                if (runningKeys.Contains(akmk))
                {
                    return parent.OnHotKeyHandler(akmk);
                }
                return false;
            }
        }

        private bool KeyUp(Key whatKey)
        {
            if (whatKey == Key.LeftCtrl || whatKey == Key.RightCtrl)
                return false;
            else if (whatKey == Key.LeftAlt || whatKey == Key.RightAlt)
                return false;
            else if (whatKey == Key.LeftShift || whatKey == Key.RightShift)
                return false;
            else
            {
                KMKey[] keysPressedA = new KMKey[keysPressed.Count()];
                keysPressed.CopyTo(keysPressedA, 0);
                foreach (KMKey kmk in keysPressedA)
                {
                    if (kmk.Key.Equals(whatKey))
                    {
                        keysPressed.Remove(kmk);
                        break;
                    }
                }
                return false;
            }
        }

        public void ListenToKey(KMKey newKeyKey)
        {
            runningKeys.Add(newKeyKey);
        }

        public void EnsureNumLockEnabled()
        {
            int keystate = WindowsApi.GetKeyState(KeyInterop.VirtualKeyFromKey(Key.NumLock));

            if (!Console.NumberLock)
            {
                Console.WriteLine("SENDING INPUT");
                var inputSet = new[]
                   {
                       new WindowsApi.User32.INPUT()
                       {
                           type = (int)WindowsApi.User32.INPUT_KEYBOARD,
                           u = new WindowsApi.User32.InputUnion()
                           {
                               ki = new WindowsApi.User32.KEYBDINPUT
                               {
                                   wScan = 0,
                                   wVk = (ushort)WindowsApi.User32.VirtualKeyShort.NUMLOCK,
                                   dwFlags = 0,
                                   dwExtraInfo = WindowsApi.User32.GetMessageExtraInfo(),
                                   time = 0
                               }
                           }
                       },
                       new WindowsApi.User32.INPUT()
                       {
                           type = (int)WindowsApi.User32.INPUT_KEYBOARD,
                           u = new WindowsApi.User32.InputUnion()
                           {
                               ki = new WindowsApi.User32.KEYBDINPUT
                               {
                                   wScan = 0,
                                   wVk = (ushort)WindowsApi.User32.VirtualKeyShort.NUMLOCK,
                                   dwFlags = (ushort)WindowsApi.User32.KEYEVENTF.KEYUP,
                                   dwExtraInfo = WindowsApi.User32.GetMessageExtraInfo(),
                                   time = 0
                               }
                           }
                       }
                   };
                WindowsApi.User32.SendInput((uint)inputSet.Length, inputSet, Marshal.SizeOf(typeof(WindowsApi.User32.INPUT)));
                int lasterr = Marshal.GetLastWin32Error();
                Console.WriteLine(lasterr);
            }
        }
    }
}
