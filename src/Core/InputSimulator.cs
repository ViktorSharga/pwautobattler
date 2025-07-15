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
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_CHAR = 0x0102;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;

        [DllImport("user32.dll")]
        private static extern IntPtr PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);

        [DllImport("user32.dll")]
        private static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        public bool SendKeyPress(IntPtr windowHandle, VirtualKeyCode key)
        {
            if (!ValidateWindow(windowHandle))
                return false;

            try
            {
                // Get scan code for the key
                uint scanCode = MapVirtualKey((uint)key, 0);
                
                // Create lParam with scan code and other flags
                IntPtr lParam = new IntPtr((scanCode << 16) | 1);
                IntPtr lParamUp = new IntPtr((scanCode << 16) | 0xC0000001);

                // Send key down
                PostMessage(windowHandle, WM_KEYDOWN, new IntPtr((int)key), lParam);
                Thread.Sleep(50); // Small delay between down and up

                // Send key up
                PostMessage(windowHandle, WM_KEYUP, new IntPtr((int)key), lParamUp);

                return true;
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
                    Thread.Sleep(100); // Delay between windows
                }
            }
        }

        // Alternative method using SendMessage for games that require it
        public bool SendKeyPressWithSendMessage(IntPtr windowHandle, VirtualKeyCode key)
        {
            if (!ValidateWindow(windowHandle))
                return false;

            try
            {
                uint scanCode = MapVirtualKey((uint)key, 0);
                IntPtr lParam = new IntPtr((scanCode << 16) | 1);
                IntPtr lParamUp = new IntPtr((scanCode << 16) | 0xC0000001);

                // Use SendMessage instead of PostMessage for immediate processing
                SendMessage(windowHandle, WM_KEYDOWN, new IntPtr((int)key), lParam);
                Thread.Sleep(50);
                SendMessage(windowHandle, WM_KEYUP, new IntPtr((int)key), lParamUp);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // Method to try different approaches for stubborn games
        public bool SendKeyPressMultiMethod(IntPtr windowHandle, VirtualKeyCode key)
        {
            if (!ValidateWindow(windowHandle))
                return false;

            // Try PostMessage first (most compatible)
            if (SendKeyPress(windowHandle, key))
            {
                return true;
            }

            Thread.Sleep(100);

            // Try SendMessage as fallback
            return SendKeyPressWithSendMessage(windowHandle, key);
        }

        private bool ValidateWindow(IntPtr windowHandle)
        {
            return windowHandle != IntPtr.Zero && IsWindow(windowHandle) && IsWindowVisible(windowHandle);
        }
    }
}