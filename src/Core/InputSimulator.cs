using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using GameAutomation.Models;

namespace GameAutomation.Core
{
    public enum VirtualKeyCode : ushort
    {
        VK_Q = 0x51,
        VK_W = 0x57,
        VK_1 = 0x31,
        VK_2 = 0x32,
        VK_3 = 0x33
    }

    public class InputSimulator
    {
        private const int INPUT_KEYBOARD = 1;
        private const int KEYEVENTF_KEYUP = 0x0002;
        private const int KEYEVENTF_SCANCODE = 0x0008;

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
            [FieldOffset(0)]
            public HARDWAREINPUT hi;
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

        [StructLayout(LayoutKind.Sequential)]
        private struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }

        [DllImport("user32.dll")]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);

        private IntPtr _originalForegroundWindow;

        public bool SendKeyPress(IntPtr windowHandle, VirtualKeyCode key)
        {
            try
            {
                // Save current foreground window
                _originalForegroundWindow = GetForegroundWindow();

                // Temporarily focus the target window
                SetForegroundWindow(windowHandle);
                Thread.Sleep(10); // Small delay to ensure focus

                // Create key down and key up inputs
                var inputs = new INPUT[]
                {
                    CreateKeyInput(key, false), // Key down
                    CreateKeyInput(key, true)   // Key up
                };

                // Send the input
                var result = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));

                // Restore original focus
                if (_originalForegroundWindow != IntPtr.Zero)
                {
                    SetForegroundWindow(_originalForegroundWindow);
                }

                return result == inputs.Length;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool SendKeyDown(IntPtr windowHandle, VirtualKeyCode key)
        {
            try
            {
                _originalForegroundWindow = GetForegroundWindow();
                SetForegroundWindow(windowHandle);
                Thread.Sleep(10);

                var input = CreateKeyInput(key, false);
                var result = SendInput(1, new[] { input }, Marshal.SizeOf(typeof(INPUT)));

                if (_originalForegroundWindow != IntPtr.Zero)
                {
                    SetForegroundWindow(_originalForegroundWindow);
                }

                return result == 1;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool SendKeyUp(IntPtr windowHandle, VirtualKeyCode key)
        {
            try
            {
                _originalForegroundWindow = GetForegroundWindow();
                SetForegroundWindow(windowHandle);
                Thread.Sleep(10);

                var input = CreateKeyInput(key, true);
                var result = SendInput(1, new[] { input }, Marshal.SizeOf(typeof(INPUT)));

                if (_originalForegroundWindow != IntPtr.Zero)
                {
                    SetForegroundWindow(_originalForegroundWindow);
                }

                return result == 1;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void BroadcastToAll(List<GameWindow> windows, Action<IntPtr> inputAction)
        {
            foreach (var window in windows)
            {
                if (window.IsActive)
                {
                    inputAction(window.WindowHandle);
                    Thread.Sleep(50); // Delay between windows
                }
            }
        }

        private INPUT CreateKeyInput(VirtualKeyCode key, bool isKeyUp)
        {
            return new INPUT
            {
                type = INPUT_KEYBOARD,
                u = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = (ushort)key,
                        wScan = (ushort)MapVirtualKey((uint)key, 0),
                        dwFlags = (uint)(isKeyUp ? KEYEVENTF_KEYUP : 0) | KEYEVENTF_SCANCODE,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };
        }
    }
}