using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace TwitchBot
{
    class GlobalKeysend
    {
        [DllImport("user32.dll")]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [StructLayout(LayoutKind.Sequential)]
        struct INPUT
        {
            public uint type;
            public INPUTUNION inputUnion;
            public static int Size { get { return Marshal.SizeOf(typeof(INPUT)); } }
        }

        [StructLayout(LayoutKind.Explicit)]
        struct INPUTUNION
        {
            [FieldOffset(0)]
            public MOUSEINPUT mi;

            [FieldOffset(0)]
            public KEYBDINPUT ki;

            [FieldOffset(0)]
            public HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct MOUSEINPUT
        {
            public int dx, dy;
            public uint mouseData,  dwFlags, time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct KEYBDINPUT
        {
            public ushort wVk, wScan;
            public uint dwFlags, time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL, wParamH;
        }

        private const uint INPUT_KEYBOARD = 1;
        private const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const uint KEYEVENTF_SCANCODE = 0x0008;

        public void SendGlobalKey(char key)
        {
            INPUT input = new INPUT();
            input.type = INPUT_KEYBOARD;
            input.inputUnion.ki = new KEYBDINPUT();
            input.inputUnion.ki.wVk = 0;
            input.inputUnion.ki.wScan = (ushort)key;
            input.inputUnion.ki.dwFlags = KEYEVENTF_SCANCODE;
            input.inputUnion.ki.time = 0;
            input.inputUnion.ki.dwExtraInfo = IntPtr.Zero;

            INPUT[] inputs = new INPUT[] { input };
            SendInput(1, inputs, INPUT.Size);
        }
    }
}
