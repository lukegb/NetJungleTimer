using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetJungleTimer
{
    enum DXScanCode : ushort
    {
        Escape = 1,
        D1 = 2,
        D2 = 3,
        D3 = 4,
        D4 = 5,
        D5 = 6,
        D6 = 7,
        D7 = 8,
        D8 = 9,
        D9 = 10,
        D0 = 11,
        Minus = 12,
        Equals = 13,
        Back = 14,
        Tab = 15,
        Q = 16,
        W = 17,
        E = 18,
        R = 19,
        T = 20,
        Y = 21,
        U = 22,
        I = 23,
        O = 24,
        P = 25,
        LeftBracket = 26,
        RightBracket = 27,
        Return = 28,
        LeftControl = 29,
        A = 30,
        S = 31,
        D = 32,
        F = 33,
        G = 34,
        H = 35,
        J = 36,
        K = 37,
        L = 38,
        SemiColon = 39,
        Apostrophe = 40,
        Grave = 41,
        LeftShift = 42,
        BackSlash = 43,
        Z = 44,
        X = 45,
        C = 46,
        V = 47,
        B = 48,
        N = 49,
        M = 50,
        Comma = 51,
        Period = 52,
        Slash = 53,
        RightShift = 54,
        Multiply = 55,
        LeftMenu = 56,
        Space = 57,
        Capital = 58,
        F1 = 59,
        F2 = 60,
        F3 = 61,
        F4 = 62,
        F5 = 63,
        F6 = 64,
        F7 = 65,
        F8 = 66,
        F9 = 67,
        F10 = 68,
        Numlock = 69,
        Scroll = 70,
        NumPad7 = 71,
        NumPad8 = 72,
        NumPad9 = 73,
        Subtract = 74,
        NumPad4 = 75,
        NumPad5 = 76,
        NumPad6 = 77,
        Add = 78,
        NumPad1 = 79,
        NumPad2 = 80,
        NumPad3 = 81,
        NumPad0 = 82,
        NumPadPeriod = 83,
        OEM102 = 86,
        F11 = 87,
        F12 = 88,
        F13 = 100,
        F14 = 101,
        F15 = 102,
        Kana = 112,
        AbntC1 = 115,
        Convert = 121,
        NoConvert = 123,
        Yen = 125,
        AbntC2 = 126,
        NumPadEquals = 141,
        PrevTrack = 144,
        At = 145,
        Colon = 146,
        Underline = 147,
        Kanji = 148,
        Stop = 149,
        AX = 150,
        Unlabeled = 151,
        NextTrack = 153,
        NumPadEnter = 156,
        RightControl = 157,
        Mute = 160,
        Calculator = 161,
        PlayPause = 162,
        MediaStop = 164,
        VolumeDown = 174,
        VolumeUp = 176,
        WebHome = 178,
        NumPadComma = 179,
        Divide = 181,
        SysRq = 183,
        RightMenu = 184,
        Pause = 197,
        Home = 199,
        Up = 200,
        Prior = 201,
        Left = 203,
        Right = 205,
        End = 207,
        Down = 208,
        PageDown = 209,
        Insert = 210,
        Delete = 211,
        LeftWindows = 219,
        RightWindows = 220,
        Apps = 221,
        Power = 222,
        Sleep = 223,
        Wake = 227,
        WebSearch = 229,
        WebFavorites = 230,
        WebRefresh = 231,
        WebStop = 232,
        WebForward = 233,
        WebBack = 234,
        MyComputer = 235,
        Mail = 236,
        MediaSelect = 237,
    }

    class DXScanCodeHelper
    {

        public struct DXScanCodeHelperStruct
        {
            public DXScanCode KeyCode { get; internal set; }
            public bool ShiftPressed { get; internal set; }
            public bool AltPressed { get; internal set; }
            public bool CtrlPressed { get; internal set; }
        }


        public static DXScanCodeHelperStruct GetDXScanCode(char datChar)
        {
            var ret = new DXScanCodeHelperStruct();
            ret.ShiftPressed = false;
            ret.AltPressed = false;
            ret.CtrlPressed = false;
            switch (datChar)
            {
                case 'a':
                    ret.KeyCode = DXScanCode.A;
                    break;
                case 'b':
                    ret.KeyCode = DXScanCode.B;
                    break;
                case 'c':
                    ret.KeyCode = DXScanCode.C;
                    break;
                case 'd':
                    ret.KeyCode = DXScanCode.D;
                    break;
                case 'e':
                    ret.KeyCode = DXScanCode.E;
                    break;
                case 'f':
                    ret.KeyCode = DXScanCode.F;
                    break;
                case 'g':
                    ret.KeyCode = DXScanCode.G;
                    break;
                case 'h':
                    ret.KeyCode = DXScanCode.H;
                    break;
                case 'i':
                    ret.KeyCode = DXScanCode.I;
                    break;
                case 'j':
                    ret.KeyCode = DXScanCode.J;
                    break;
                case 'k':
                    ret.KeyCode = DXScanCode.K;
                    break;
                case 'l':
                    ret.KeyCode = DXScanCode.L;
                    break;
                case 'm':
                    ret.KeyCode = DXScanCode.M;
                    break;
                case 'n':
                    ret.KeyCode = DXScanCode.N;
                    break;
                case 'o':
                    ret.KeyCode = DXScanCode.O;
                    break;
                case 'p':
                    ret.KeyCode = DXScanCode.P;
                    break;
                case 'q':
                    ret.KeyCode = DXScanCode.Q;
                    break;
                case 'r':
                    ret.KeyCode = DXScanCode.R;
                    break;
                case 's':
                    ret.KeyCode = DXScanCode.S;
                    break;
                case 't':
                    ret.KeyCode = DXScanCode.T;
                    break;
                case 'u':
                    ret.KeyCode = DXScanCode.U;
                    break;
                case 'v':
                    ret.KeyCode = DXScanCode.V;
                    break;
                case 'w':
                    ret.KeyCode = DXScanCode.W;
                    break;
                case 'x':
                    ret.KeyCode = DXScanCode.X;
                    break;
                case 'y':
                    ret.KeyCode = DXScanCode.Y;
                    break;
                case 'z':
                    ret.KeyCode = DXScanCode.Z;
                    break;
                case 'A':
                    ret.KeyCode = DXScanCode.A;
                    ret.ShiftPressed = true;
                    break;
                case 'B':
                    ret.KeyCode = DXScanCode.B;
                    ret.ShiftPressed = true;
                    break;
                case 'C':
                    ret.KeyCode = DXScanCode.C;
                    ret.ShiftPressed = true;
                    break;
                case 'D':
                    ret.KeyCode = DXScanCode.D;
                    ret.ShiftPressed = true;
                    break;
                case 'E':
                    ret.KeyCode = DXScanCode.E;
                    ret.ShiftPressed = true;
                    break;
                case 'F':
                    ret.KeyCode = DXScanCode.F;
                    ret.ShiftPressed = true;
                    break;
                case 'G':
                    ret.KeyCode = DXScanCode.G;
                    ret.ShiftPressed = true;
                    break;
                case 'H':
                    ret.KeyCode = DXScanCode.H;
                    ret.ShiftPressed = true;
                    break;
                case 'I':
                    ret.KeyCode = DXScanCode.I;
                    ret.ShiftPressed = true;
                    break;
                case 'J':
                    ret.KeyCode = DXScanCode.J;
                    ret.ShiftPressed = true;
                    break;
                case 'K':
                    ret.KeyCode = DXScanCode.K;
                    ret.ShiftPressed = true;
                    break;
                case 'L':
                    ret.KeyCode = DXScanCode.L;
                    ret.ShiftPressed = true;
                    break;
                case 'M':
                    ret.KeyCode = DXScanCode.M;
                    ret.ShiftPressed = true;
                    break;
                case 'N':
                    ret.KeyCode = DXScanCode.N;
                    ret.ShiftPressed = true;
                    break;
                case 'O':
                    ret.KeyCode = DXScanCode.O;
                    ret.ShiftPressed = true;
                    break;
                case 'P':
                    ret.KeyCode = DXScanCode.P;
                    ret.ShiftPressed = true;
                    break;
                case 'Q':
                    ret.KeyCode = DXScanCode.Q;
                    ret.ShiftPressed = true;
                    break;
                case 'R':
                    ret.KeyCode = DXScanCode.R;
                    ret.ShiftPressed = true;
                    break;
                case 'S':
                    ret.KeyCode = DXScanCode.S;
                    ret.ShiftPressed = true;
                    break;
                case 'T':
                    ret.KeyCode = DXScanCode.T;
                    ret.ShiftPressed = true;
                    break;
                case 'U':
                    ret.KeyCode = DXScanCode.U;
                    ret.ShiftPressed = true;
                    break;
                case 'V':
                    ret.KeyCode = DXScanCode.V;
                    ret.ShiftPressed = true;
                    break;
                case 'W':
                    ret.KeyCode = DXScanCode.W;
                    ret.ShiftPressed = true;
                    break;
                case 'X':
                    ret.KeyCode = DXScanCode.X;
                    ret.ShiftPressed = true;
                    break;
                case 'Y':
                    ret.KeyCode = DXScanCode.Y;
                    ret.ShiftPressed = true;
                    break;
                case 'Z':
                    ret.KeyCode = DXScanCode.Z;
                    ret.ShiftPressed = true;
                    break;
                case '0':
                    ret.KeyCode = DXScanCode.D0;
                    break;
                case '1':
                    ret.KeyCode = DXScanCode.D1;
                    break;
                case '2':
                    ret.KeyCode = DXScanCode.D2;
                    break;
                case '3':
                    ret.KeyCode = DXScanCode.D3;
                    break;
                case '4':
                    ret.KeyCode = DXScanCode.D4;
                    break;
                case '5':
                    ret.KeyCode = DXScanCode.D5;
                    break;
                case '6':
                    ret.KeyCode = DXScanCode.D6;
                    break;
                case '7':
                    ret.KeyCode = DXScanCode.D7;
                    break;
                case '8':
                    ret.KeyCode = DXScanCode.D8;
                    break;
                case '9':
                    ret.KeyCode = DXScanCode.D9;
                    break;
                case '\t':
                    ret.KeyCode = DXScanCode.Tab;
                    break;
                case ' ':
                    ret.KeyCode = DXScanCode.Space;
                    break;
                case '/':
                    ret.KeyCode = DXScanCode.Slash;
                    break;
                case '\\':
                    ret.KeyCode = DXScanCode.BackSlash;
                    break;
                case ';':
                    ret.KeyCode = DXScanCode.SemiColon;
                    break;
                case ':':
                    ret.KeyCode = DXScanCode.Colon;
                    break;
                case '\n':
                    ret.KeyCode = DXScanCode.Return;
                    break;
            }
            return ret;
        }
    }
}
