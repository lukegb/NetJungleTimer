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
        private class User32
        {
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
    }
}
