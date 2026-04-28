using System;
using System.Runtime.InteropServices;

namespace AutoTyper
{
    public static class KeyboardSimulator
    {
        [DllImport("user32.dll")]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        private static extern short VkKeyScanEx(char ch, IntPtr dwhkl);

        [DllImport("user32.dll")]
        private static extern IntPtr GetKeyboardLayout(uint idThread);

        private const int INPUT_KEYBOARD = 1;
        private const uint KEYEVENTF_UNICODE = 0x0004;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const ushort VK_RETURN = 0x0D;
        private const ushort VK_SPACE = 0x20;
        private const ushort VK_SHIFT = 0x10;

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public int type;
            public InputUnion u;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)]
            public MOUSEINPUT mi;
            [FieldOffset(0)]
            public KEYBDINPUT ki;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        public static void SendChar(char c, IntPtr targetWindow = default)
        {
            // If target window is specified, use PostMessage instead
            if (targetWindow != IntPtr.Zero)
            {
                SendCharToWindow(c, targetWindow);
                return;
            }

            // Skip \r, only process \n for newlines
            if (c == '\r')
            {
                return; // Skip carriage return
            }
            
            // Handle newline
            if (c == '\n')
            {
                SendShiftEnter();
                return;
            }
            
            if (c == ' ')
            {
                SendKey(VK_SPACE);
                return;
            }

            INPUT[] inputs = new INPUT[2];

            // Key down
            inputs[0] = new INPUT
            {
                type = INPUT_KEYBOARD,
                u = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = 0,
                        wScan = c,
                        dwFlags = KEYEVENTF_UNICODE,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            // Key up
            inputs[1] = new INPUT
            {
                type = INPUT_KEYBOARD,
                u = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = 0,
                        wScan = c,
                        dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        private static void SendCharToWindow(char c, IntPtr targetWindow)
        {
            // Skip \r
            if (c == '\r')
            {
                return;
            }

            // Handle newline with Shift+Enter
            if (c == '\n')
            {
                NativeMethods.PostMessage(targetWindow, NativeMethods.WM_KEYDOWN, (IntPtr)VK_SHIFT, IntPtr.Zero);
                NativeMethods.PostMessage(targetWindow, NativeMethods.WM_KEYDOWN, (IntPtr)VK_RETURN, IntPtr.Zero);
                NativeMethods.PostMessage(targetWindow, NativeMethods.WM_KEYUP, (IntPtr)VK_RETURN, IntPtr.Zero);
                NativeMethods.PostMessage(targetWindow, NativeMethods.WM_KEYUP, (IntPtr)VK_SHIFT, IntPtr.Zero);
                return;
            }

            // Send character directly to window
            NativeMethods.PostMessage(targetWindow, NativeMethods.WM_CHAR, (IntPtr)c, IntPtr.Zero);
        }

        private static void SendShiftEnter()
        {
            INPUT[] inputs = new INPUT[4];

            // Shift down
            inputs[0] = new INPUT
            {
                type = INPUT_KEYBOARD,
                u = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = VK_SHIFT,
                        wScan = 0,
                        dwFlags = 0,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            // Enter down
            inputs[1] = new INPUT
            {
                type = INPUT_KEYBOARD,
                u = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = VK_RETURN,
                        wScan = 0,
                        dwFlags = 0,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            // Enter up
            inputs[2] = new INPUT
            {
                type = INPUT_KEYBOARD,
                u = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = VK_RETURN,
                        wScan = 0,
                        dwFlags = KEYEVENTF_KEYUP,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            // Shift up
            inputs[3] = new INPUT
            {
                type = INPUT_KEYBOARD,
                u = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = VK_SHIFT,
                        wScan = 0,
                        dwFlags = KEYEVENTF_KEYUP,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        private static void SendKey(ushort vkCode)
        {
            INPUT[] inputs = new INPUT[2];

            // Key down
            inputs[0] = new INPUT
            {
                type = INPUT_KEYBOARD,
                u = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = vkCode,
                        wScan = 0,
                        dwFlags = 0,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            // Key up
            inputs[1] = new INPUT
            {
                type = INPUT_KEYBOARD,
                u = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = vkCode,
                        wScan = 0,
                        dwFlags = KEYEVENTF_KEYUP,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
        }
    }
}
