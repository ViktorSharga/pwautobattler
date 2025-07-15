using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Diagnostics;
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

    public enum InputMethod
    {
        PostMessage,
        SendMessage,
        SendInput,
        KeyboardEvent,
        ScanCode
    }

    public class InputSimulator
    {
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_CHAR = 0x0102;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;
        private const int WM_COMMAND = 0x0111;
        
        private const int INPUT_KEYBOARD = 1;
        private const int KEYEVENTF_KEYUP = 0x0002;
        private const int KEYEVENTF_SCANCODE = 0x0008;
        private const int KEYEVENTF_EXTENDEDKEY = 0x0001;
        private const int KEYEVENTF_UNICODE = 0x0004;

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

        [DllImport("user32.dll")]
        private static extern IntPtr PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);

        [DllImport("user32.dll")]
        private static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern short VkKeyScan(char ch);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetParent(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam, uint fuFlags, uint uTimeout, out IntPtr lpdwResult);

        private const uint SMTO_NORMAL = 0x0000;
        private const uint GW_CHILD = 5;

        public InputMethod CurrentMethod { get; set; } = InputMethod.PostMessage;

        public bool SendKeyPress(IntPtr windowHandle, VirtualKeyCode key, InputMethod method = InputMethod.PostMessage)
        {
            if (!ValidateWindow(windowHandle))
                return false;

            switch (method)
            {
                case InputMethod.PostMessage:
                    return SendKeyPressPostMessage(windowHandle, key);
                case InputMethod.SendMessage:
                    return SendKeyPressSendMessage(windowHandle, key);
                case InputMethod.SendInput:
                    return SendKeyPressSendInput(windowHandle, key);
                case InputMethod.KeyboardEvent:
                    return SendKeyPressKeyboardEvent(windowHandle, key);
                case InputMethod.ScanCode:
                    return SendKeyPressScanCode(windowHandle, key);
                default:
                    return SendKeyPressPostMessage(windowHandle, key);
            }
        }

        private bool SendKeyPressPostMessage(IntPtr windowHandle, VirtualKeyCode key)
        {
            try
            {
                uint scanCode = MapVirtualKey((uint)key, 0);
                
                // Try sending to child windows as well
                IntPtr childWindow = GetWindow(windowHandle, GW_CHILD);
                while (childWindow != IntPtr.Zero)
                {
                    if (IsWindowVisible(childWindow))
                    {
                        IntPtr lParam = new IntPtr((scanCode << 16) | 1);
                        IntPtr lParamUp = new IntPtr((scanCode << 16) | 0xC0000001);
                        
                        PostMessage(childWindow, WM_KEYDOWN, new IntPtr((int)key), lParam);
                        Thread.Sleep(30);
                        PostMessage(childWindow, WM_KEYUP, new IntPtr((int)key), lParamUp);
                    }
                    childWindow = GetWindow(childWindow, GW_CHILD);
                }

                // Also send to main window
                IntPtr lParamMain = new IntPtr((scanCode << 16) | 1);
                IntPtr lParamUpMain = new IntPtr((scanCode << 16) | 0xC0000001);
                
                PostMessage(windowHandle, WM_KEYDOWN, new IntPtr((int)key), lParamMain);
                Thread.Sleep(50);
                PostMessage(windowHandle, WM_KEYUP, new IntPtr((int)key), lParamUpMain);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool SendKeyPressSendMessage(IntPtr windowHandle, VirtualKeyCode key)
        {
            try
            {
                uint scanCode = MapVirtualKey((uint)key, 0);
                IntPtr lParam = new IntPtr((scanCode << 16) | 1);
                IntPtr lParamUp = new IntPtr((scanCode << 16) | 0xC0000001);

                IntPtr result;
                SendMessageTimeout(windowHandle, WM_KEYDOWN, new IntPtr((int)key), lParam, SMTO_NORMAL, 100, out result);
                Thread.Sleep(50);
                SendMessageTimeout(windowHandle, WM_KEYUP, new IntPtr((int)key), lParamUp, SMTO_NORMAL, 100, out result);

                // Also try WM_CHAR for character keys
                if (key == VirtualKeyCode.VK_Q || key == VirtualKeyCode.VK_W || 
                    key == VirtualKeyCode.VK_1 || key == VirtualKeyCode.VK_2 || key == VirtualKeyCode.VK_3)
                {
                    char ch = GetCharFromVirtualKey(key);
                    SendMessage(windowHandle, WM_CHAR, new IntPtr(ch), IntPtr.Zero);
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool SendKeyPressSendInput(IntPtr windowHandle, VirtualKeyCode key)
        {
            try
            {
                IntPtr originalForeground = GetForegroundWindow();
                SetForegroundWindow(windowHandle);
                Thread.Sleep(100);

                uint scanCode = MapVirtualKey((uint)key, 0);
                
                var inputs = new INPUT[]
                {
                    new INPUT
                    {
                        type = INPUT_KEYBOARD,
                        u = new InputUnion
                        {
                            ki = new KEYBDINPUT
                            {
                                wVk = (ushort)key,
                                wScan = (ushort)scanCode,
                                dwFlags = 0,
                                time = 0,
                                dwExtraInfo = IntPtr.Zero
                            }
                        }
                    },
                    new INPUT
                    {
                        type = INPUT_KEYBOARD,
                        u = new InputUnion
                        {
                            ki = new KEYBDINPUT
                            {
                                wVk = (ushort)key,
                                wScan = (ushort)scanCode,
                                dwFlags = KEYEVENTF_KEYUP,
                                time = 0,
                                dwExtraInfo = IntPtr.Zero
                            }
                        }
                    }
                };

                uint result = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
                
                Thread.Sleep(50);
                SetForegroundWindow(originalForeground);

                return result == inputs.Length;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool SendKeyPressKeyboardEvent(IntPtr windowHandle, VirtualKeyCode key)
        {
            try
            {
                IntPtr originalForeground = GetForegroundWindow();
                SetForegroundWindow(windowHandle);
                Thread.Sleep(100);

                byte vk = (byte)key;
                byte scan = (byte)MapVirtualKey((uint)key, 0);

                keybd_event(vk, scan, 0, UIntPtr.Zero);
                Thread.Sleep(50);
                keybd_event(vk, scan, KEYEVENTF_KEYUP, UIntPtr.Zero);

                Thread.Sleep(50);
                SetForegroundWindow(originalForeground);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool SendKeyPressScanCode(IntPtr windowHandle, VirtualKeyCode key)
        {
            try
            {
                IntPtr originalForeground = GetForegroundWindow();
                SetForegroundWindow(windowHandle);
                Thread.Sleep(100);

                uint scanCode = MapVirtualKey((uint)key, 0);
                
                var inputs = new INPUT[]
                {
                    new INPUT
                    {
                        type = INPUT_KEYBOARD,
                        u = new InputUnion
                        {
                            ki = new KEYBDINPUT
                            {
                                wVk = 0,
                                wScan = (ushort)scanCode,
                                dwFlags = KEYEVENTF_SCANCODE,
                                time = 0,
                                dwExtraInfo = IntPtr.Zero
                            }
                        }
                    },
                    new INPUT
                    {
                        type = INPUT_KEYBOARD,
                        u = new InputUnion
                        {
                            ki = new KEYBDINPUT
                            {
                                wVk = 0,
                                wScan = (ushort)scanCode,
                                dwFlags = KEYEVENTF_SCANCODE | KEYEVENTF_KEYUP,
                                time = 0,
                                dwExtraInfo = IntPtr.Zero
                            }
                        }
                    }
                };

                uint result = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
                
                Thread.Sleep(50);
                SetForegroundWindow(originalForeground);

                return result == inputs.Length;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool SendKeyDown(IntPtr windowHandle, VirtualKeyCode key)
        {
            if (!ValidateWindow(windowHandle))
                return false;

            try
            {
                uint scanCode = MapVirtualKey((uint)key, 0);
                IntPtr lParam = new IntPtr((scanCode << 16) | 1);

                PostMessage(windowHandle, WM_KEYDOWN, new IntPtr((int)key), lParam);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool SendKeyUp(IntPtr windowHandle, VirtualKeyCode key)
        {
            if (!ValidateWindow(windowHandle))
                return false;

            try
            {
                uint scanCode = MapVirtualKey((uint)key, 0);
                IntPtr lParam = new IntPtr((scanCode << 16) | 0xC0000001);

                PostMessage(windowHandle, WM_KEYUP, new IntPtr((int)key), lParam);
                return true;
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
                if (window.IsActive && ValidateWindow(window.WindowHandle))
                {
                    inputAction(window.WindowHandle);
                    Thread.Sleep(100);
                }
            }
        }

        public bool TryAllMethods(IntPtr windowHandle, VirtualKeyCode key)
        {
            var methods = Enum.GetValues<InputMethod>();
            foreach (var method in methods)
            {
                Debug.WriteLine($"Trying method: {method}");
                if (SendKeyPress(windowHandle, key, method))
                {
                    Debug.WriteLine($"Success with method: {method}");
                    return true;
                }
                Thread.Sleep(100);
            }
            return false;
        }

        private bool ValidateWindow(IntPtr windowHandle)
        {
            return windowHandle != IntPtr.Zero && IsWindow(windowHandle) && IsWindowVisible(windowHandle);
        }

        private char GetCharFromVirtualKey(VirtualKeyCode key)
        {
            switch (key)
            {
                case VirtualKeyCode.VK_Q: return 'q';
                case VirtualKeyCode.VK_W: return 'w';
                case VirtualKeyCode.VK_1: return '1';
                case VirtualKeyCode.VK_2: return '2';
                case VirtualKeyCode.VK_3: return '3';
                default: return '\0';
            }
        }
    }
}