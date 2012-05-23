using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using System.Threading;
using System.Runtime.InteropServices;

namespace NetJungleTimer
{
    public delegate void HotKeyPressedEventHandler(object sender, HotKeyPressedEventArgs e);

    public class HotKeyPressedEventArgs : EventArgs
    {
        public KeyboardManager.KMKey Key;

        public HotKeyPressedEventArgs(KeyboardManager.KMKey ThatKey)
        {
            Key = ThatKey;
        }
    }

    public class KeyboardManager : IDisposable
    {
        public class KMKey : IEquatable<KMKey>, ICloneable
        {

            public bool CtrlDown { get; private set; }
            public bool AltDown { get; private set; }
            public bool ShiftDown { get; private set; }

            public Key Key { get; private set; }

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

            public void InvertCtrlDown()
            {
                CtrlDown = !CtrlDown;
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

            public object Clone()
            {
                return new KMKey(this.Key, this.CtrlDown, this.AltDown, this.ShiftDown);
            }
        }

        bool CtrlDown = false;
        bool AltDown = false;
        bool ShiftDown = false;

        bool Disposed = false;

        List<KMKey> KeysPressed = new List<KMKey>();

        IntPtr KeyboardHook;

        private WindowsApi.User32.HookProc keyPressDelegate = null;

        public event HotKeyPressedEventHandler HotKeyPressed;


        private static readonly Lazy<KeyboardManager> _instance
        = new Lazy<KeyboardManager>(() => new KeyboardManager());

        public static KeyboardManager Instance
        {
            get
            {
                return _instance.Value;
            }
        }


        private KeyboardManager()
        {
            this.keyPressDelegate = new WindowsApi.User32.HookProc(this.KeyboardCallback);

            KeyboardHook = WindowsApi.User32.SetWindowsHookEx(WindowsApi.User32.HookType.WH_KEYBOARD_LL, this.keyPressDelegate, IntPtr.Zero, 0);
        }

        ~KeyboardManager()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        public void Dispose(bool disposing)
        {
            if (Disposed)
                return;
            Disposed = true;

            if (disposing) { }

            WindowsApi.User32.UnhookWindowsHookEx(this.KeyboardHook);
            this.KeyboardHook = IntPtr.Zero;

        }

        private int KeyboardCallback(int code, IntPtr wParam, [In] WindowsApi.User32.KBDLLHOOKSTRUCT lParam)
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
                        ShiftDown = isSet;
                        break;
                    case 0x11:
                        CtrlDown = isSet;
                        break;
                    case 0x12:
                        AltDown = isSet;
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
                foreach (KMKey kmk in KeysPressed)
                {
                    if (kmk.Key.Equals(whatKey))
                    {
                        return false;
                    }
                }

                ResetControlKeys();
                KMKey akmk = new KMKey(whatKey, CtrlDown, AltDown, ShiftDown);
                KeysPressed.Add(akmk);

                if (this.HotKeyPressed != null)
                    this.HotKeyPressed(this, new HotKeyPressedEventArgs(akmk));
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
                KMKey[] keysPressedA = new KMKey[KeysPressed.Count()];
                KeysPressed.CopyTo(keysPressedA, 0);
                foreach (KMKey kmk in keysPressedA)
                {
                    if (kmk.Key.Equals(whatKey))
                    {
                        KeysPressed.Remove(kmk);
                        break;
                    }
                }
                return false;
            }
        }

        public void EnsureNumLockEnabled()
        {
            int keystate = WindowsApi.GetKeyState(KeyInterop.VirtualKeyFromKey(Key.NumLock));

            if (!Console.NumberLock)
            {
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

        public void SendAlliedChatMessage(String whatMessage)
        {
            SendKeyStrokes(String.Format("\n{0}\n", whatMessage));
        }

        private WindowsApi.User32.INPUT _GenerateKeyPress(NetJungleTimer.DXScanCode datKey, bool pressDown)
        {
            Console.WriteLine("PRESSING KEY " + datKey);
            return new WindowsApi.User32.INPUT()
            {
                type = (int)WindowsApi.User32.INPUT_KEYBOARD,
                u = new WindowsApi.User32.InputUnion()
                {
                    ki = new WindowsApi.User32.KEYBDINPUT
                    {
                        wScan = (ushort)datKey,
                        wVk = 0,
                        dwFlags = (ushort)(WindowsApi.User32.KEYEVENTF.SCANCODE | (pressDown ? WindowsApi.User32.KEYEVENTF.NONE : WindowsApi.User32.KEYEVENTF.KEYUP)),
                        dwExtraInfo = WindowsApi.User32.GetMessageExtraInfo(),
                        time = 0
                    }
                }
            };
        }

        private WindowsApi.User32.INPUT _GenerateKeyPressUnicode(char datKey, bool pressDown)
        {
            return new WindowsApi.User32.INPUT()
            {
                type = (int)WindowsApi.User32.INPUT_KEYBOARD,
                u = new WindowsApi.User32.InputUnion()
                {
                    ki = new WindowsApi.User32.KEYBDINPUT
                    {
                        wScan = (ushort)datKey,
                        wVk = 0,
                        dwFlags = (ushort)(WindowsApi.User32.KEYEVENTF.UNICODE | (pressDown ? WindowsApi.User32.KEYEVENTF.NONE : WindowsApi.User32.KEYEVENTF.KEYUP)),
                        dwExtraInfo = WindowsApi.User32.GetMessageExtraInfo(),
                        time = 0
                    }
                }
            };
        }

        private void SendKeyStrokes(String whatStrokes)
        {
                var preInputSet = new List<WindowsApi.User32.INPUT>(); // run first
                var inputSet = new List<WindowsApi.User32.INPUT>(); // actual input
                var postInputSet = new List<WindowsApi.User32.INPUT>(); // run after

                if (Console.CapsLock)
                {
                    preInputSet.Add(_GenerateKeyPress(NetJungleTimer.DXScanCode.Capital, true));
                    preInputSet.Add(_GenerateKeyPress(NetJungleTimer.DXScanCode.Capital, false));
                    postInputSet.Add(_GenerateKeyPress(NetJungleTimer.DXScanCode.Capital, true));
                    postInputSet.Add(_GenerateKeyPress(NetJungleTimer.DXScanCode.Capital, false));
                }

                var respDict = new Dictionary<Key, NetJungleTimer.DXScanCode>
                {
                    {Key.LeftShift, NetJungleTimer.DXScanCode.LeftShift},
                    {Key.RightShift, NetJungleTimer.DXScanCode.RightShift},
                    {Key.LeftCtrl, NetJungleTimer.DXScanCode.LeftControl},
                    {Key.RightCtrl, NetJungleTimer.DXScanCode.RightControl},
                    {Key.LeftAlt, NetJungleTimer.DXScanCode.LeftMenu},
                    {Key.RightAlt, NetJungleTimer.DXScanCode.RightMenu},
                };

                foreach (KeyValuePair<Key, NetJungleTimer.DXScanCode> resp in respDict)
                {
                    if (Keyboard.IsKeyDown(resp.Key))
                    {
                        preInputSet.Add(_GenerateKeyPress(resp.Value, false));
                        postInputSet.Add(_GenerateKeyPress(resp.Value, true));
                    }
                }

                foreach (char datChar in whatStrokes)
                {
                    var virtKey = DXScanCodeHelper.GetDXScanCode(datChar);
                    if (virtKey.ShiftPressed)
                        inputSet.Add(_GenerateKeyPress(NetJungleTimer.DXScanCode.LeftShift, true));
                    if (virtKey.CtrlPressed)
                        inputSet.Add(_GenerateKeyPress(NetJungleTimer.DXScanCode.LeftControl, true));
                    if (virtKey.AltPressed)
                        inputSet.Add(_GenerateKeyPress(NetJungleTimer.DXScanCode.LeftMenu, true));

                    inputSet.Add(_GenerateKeyPress((NetJungleTimer.DXScanCode)virtKey.KeyCode, true));
                    inputSet.Add(_GenerateKeyPress((NetJungleTimer.DXScanCode)virtKey.KeyCode, false));

                    if (virtKey.AltPressed)
                        inputSet.Add(_GenerateKeyPress(NetJungleTimer.DXScanCode.LeftMenu, false));
                    if (virtKey.CtrlPressed)
                        inputSet.Add(_GenerateKeyPress(NetJungleTimer.DXScanCode.LeftControl, false));
                    if (virtKey.ShiftPressed)
                        inputSet.Add(_GenerateKeyPress(NetJungleTimer.DXScanCode.LeftShift, false));
                }

                // send sets
                //WindowsApi.User32.SendInput((uint)preInputSet.Count, preInputSet.ToArray(), Marshal.SizeOf(typeof(WindowsApi.User32.INPUT)));
                var finalInputSet = new List<WindowsApi.User32.INPUT>(); // run after
                finalInputSet.AddRange(preInputSet);
                finalInputSet.AddRange(inputSet);
                finalInputSet.AddRange(postInputSet);
                foreach (WindowsApi.User32.INPUT inp in finalInputSet)
                {
                    WindowsApi.User32.SendInput(1, new WindowsApi.User32.INPUT[] { inp }, Marshal.SizeOf(typeof(WindowsApi.User32.INPUT)));
                    Thread.Sleep(20);
                }
                //WindowsApi.User32.SendInput((uint)inputSet.Count, inputSet.ToArray(), Marshal.SizeOf(typeof(WindowsApi.User32.INPUT)));
                //WindowsApi.User32.SendInput((uint)postInputSet.Count, postInputSet.ToArray(), Marshal.SizeOf(typeof(WindowsApi.User32.INPUT)));

        }
    }
}
