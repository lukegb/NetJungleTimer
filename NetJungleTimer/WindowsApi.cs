using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;

namespace NetJungleTimer
{
    class WindowsApi
    {
        internal class User32
        {
            public enum HookType : int
            {
                WH_JOURNALRECORD = 0,
                WH_JOURNALPLAYBACK = 1,
                WH_KEYBOARD = 2,
                WH_GETMESSAGE = 3,
                WH_CALLWNDPROC = 4,
                WH_CBT = 5,
                WH_SYSMSGFILTER = 6,
                WH_MOUSE = 7,
                WH_HARDWARE = 8,
                WH_DEBUG = 9,
                WH_SHELL = 10,
                WH_FOREGROUNDIDLE = 11,
                WH_CALLWNDPROCRET = 12,
                WH_KEYBOARD_LL = 13,
                WH_MOUSE_LL = 14
            }

            public enum KeyboardMessageType : int
            {
                WM_KEYDOWN = 256,
                WM_KEYUP = 257,
                WM_SYSKEYDOWN = 260,
                WM_SYSKEYUP = 261
            }

            [StructLayout(LayoutKind.Sequential)]
            public class KBDLLHOOKSTRUCT
            {
                public uint vkCode;
                public uint scanCode;
                public KBDLLHOOKSTRUCTFlags flags;
                public uint time;
                public UIntPtr dwExtraInfo;
            }

            [Flags]
            public enum KBDLLHOOKSTRUCTFlags : uint
            {
                LLKHF_EXTENDED = 0x01,
                LLKHF_INJECTED = 0x10,
                LLKHF_ALTDOWN = 0x20,
                LLKHF_UP = 0x80,
            }

            public delegate int HookProc(int code, IntPtr wParam, [In] KBDLLHOOKSTRUCT lParam);

            [DllImport("user32.dll")]
            public static extern IntPtr SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

            [DllImport("user32.dll")]
            public static extern IntPtr GetForegroundWindow();

            [DllImport("user32.dll")]
            public static extern IntPtr GetWindowText(IntPtr hWnd, StringBuilder text, IntPtr count);

            [DllImport("user32.dll")]
            public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

            [DllImport("user32.dll", SetLastError = true)]
            public static extern IntPtr GetWindowRect(IntPtr hWnd, ref WIN_RECT rect);

            [DllImport("user32.dll")]
            public static extern IntPtr GetClientRect(IntPtr hWnd, ref WIN_RECT rect);

            [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
            public static extern short GetAsyncKeyState(int vkey);

            [DllImport("user32.dll", SetLastError=true)]
            public static extern IntPtr SetWindowsHookEx(HookType code, HookProc func, IntPtr hInstance, int threadID);

            [DllImport("user32.dll")]
            public static extern int CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

            [DllImport("user32.dll")]
            public static extern int CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, KBDLLHOOKSTRUCT lParam);

            [DllImport("user32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool UnhookWindowsHookEx(IntPtr hhk);

            [DllImport("user32.dll", SetLastError = true)]
            internal static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);
        }

        internal class WindowStyle
        {
            // Window Styles
            public const int WS_EX_NOACTIVATE = 0x08000000;
            public const int WS_EX_TRANSPARENT = 0x00000020;
            public const int WS_EX_LAYERED = 0x00080000;

            // I want style info!
            public const int GWL_STYLE = -16;
        }

        public struct WIN_RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }


        // my stuff!
        public static void SetWindowNoActivate(IntPtr window)
        {
            User32.SetWindowLong(window, -20, User32.GetWindowLong(window, -20) | WindowStyle.WS_EX_NOACTIVATE | WindowStyle.WS_EX_TRANSPARENT);
        }

        public static int GetKeyState(int vkey)
        {
            return User32.GetAsyncKeyState(vkey);
        }

        internal static Rect GetWindowDims(IntPtr hndl)
        {
            Rect returnRect = new Rect();
            WIN_RECT winLoc = new WIN_RECT();
            WIN_RECT winDim = new WIN_RECT();
            User32.GetWindowRect(hndl, ref winLoc);
            User32.GetClientRect(hndl, ref winDim);

            returnRect.X = winLoc.left;
            returnRect.Y = winLoc.top;
            returnRect.Width = winLoc.right - winLoc.left;
            returnRect.Height = winLoc.bottom - winLoc.top;
            return returnRect;
            
        }

        internal static IntPtr GetForegroundWindowHandle()
        {
            return User32.GetForegroundWindow();
        }

        internal static string GetWindowCaption(IntPtr tempHandle)
        {
            StringBuilder sb = new StringBuilder(1024);
            User32.GetWindowText(tempHandle, sb, (IntPtr)sb.MaxCapacity);
            return sb.ToString();
        }

        internal static uint GetWindowStyle(IntPtr tempHandle)
        {
            return (uint)User32.GetWindowLong(tempHandle, WindowStyle.GWL_STYLE); // grab the style
        }

        internal static void MoveWindowToSensibleLocation(IntPtr leagueOfLegendsWindowHndl)
        {
            Rect windowDims = GetWindowDims(leagueOfLegendsWindowHndl);
            User32.MoveWindow(leagueOfLegendsWindowHndl, 0, 0, (int)windowDims.Width, (int)windowDims.Height, true);
        }
    }
}
