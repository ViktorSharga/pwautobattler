using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace GameAutomation.Core
{
    public class BackgroundMouseSimulator
    {
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_LBUTTONDBLCLK = 0x0203;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int WM_RBUTTONUP = 0x0205;
        private const int WM_RBUTTONDBLCLK = 0x0206;
        private const int WM_MOUSEMOVE = 0x0200;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        public enum MouseButton
        {
            Left,
            Right
        }

        public enum MouseInputMethod
        {
            SendMessage,
            PostMessage,
            DirectClientCoordinates
        }

        /// <summary>
        /// Sends a mouse click to a window without activating it
        /// </summary>
        /// <param name="windowHandle">Target window handle</param>
        /// <param name="screenX">Screen X coordinate</param>
        /// <param name="screenY">Screen Y coordinate</param>
        /// <param name="button">Mouse button to click</param>
        /// <param name="method">Input method to use</param>
        /// <returns>True if successful</returns>
        public bool SendMouseClick(IntPtr windowHandle, int screenX, int screenY, MouseButton button, MouseInputMethod method = MouseInputMethod.PostMessage)
        {
            try
            {
                // Convert screen coordinates to client coordinates
                var clientPoint = new POINT { x = screenX, y = screenY };
                if (!ScreenToClient(windowHandle, ref clientPoint))
                {
                    return false;
                }

                // Create lParam with coordinates
                var lParam = (IntPtr)((clientPoint.y << 16) | (clientPoint.x & 0xFFFF));

                switch (method)
                {
                    case MouseInputMethod.SendMessage:
                        return SendMouseClickSendMessage(windowHandle, lParam, button);
                    
                    case MouseInputMethod.PostMessage:
                        return SendMouseClickPostMessage(windowHandle, lParam, button);
                    
                    case MouseInputMethod.DirectClientCoordinates:
                        return SendMouseClickDirectClient(windowHandle, clientPoint.x, clientPoint.y, button);
                    
                    default:
                        return false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Mouse click failed: {ex.Message}");
                return false;
            }
        }

        private bool SendMouseClickSendMessage(IntPtr windowHandle, IntPtr lParam, MouseButton button)
        {
            var downMsg = button == MouseButton.Left ? WM_LBUTTONDOWN : WM_RBUTTONDOWN;
            var upMsg = button == MouseButton.Left ? WM_LBUTTONUP : WM_RBUTTONUP;

            // Send mouse down and up messages
            SendMessage(windowHandle, (uint)downMsg, IntPtr.Zero, lParam);
            Thread.Sleep(10); // Small delay between down and up
            SendMessage(windowHandle, (uint)upMsg, IntPtr.Zero, lParam);

            return true;
        }

        private bool SendMouseClickPostMessage(IntPtr windowHandle, IntPtr lParam, MouseButton button)
        {
            var downMsg = button == MouseButton.Left ? WM_LBUTTONDOWN : WM_RBUTTONDOWN;
            var upMsg = button == MouseButton.Left ? WM_LBUTTONUP : WM_RBUTTONUP;

            // Post mouse down and up messages
            PostMessage(windowHandle, (uint)downMsg, IntPtr.Zero, lParam);
            Thread.Sleep(10); // Small delay between down and up
            PostMessage(windowHandle, (uint)upMsg, IntPtr.Zero, lParam);

            return true;
        }

        private bool SendMouseClickDirectClient(IntPtr windowHandle, int clientX, int clientY, MouseButton button)
        {
            // Try direct client coordinate approach
            var lParam = (IntPtr)((clientY << 16) | (clientX & 0xFFFF));
            var downMsg = button == MouseButton.Left ? WM_LBUTTONDOWN : WM_RBUTTONDOWN;
            var upMsg = button == MouseButton.Left ? WM_LBUTTONUP : WM_RBUTTONUP;

            // Send mouse move first
            PostMessage(windowHandle, (uint)WM_MOUSEMOVE, IntPtr.Zero, lParam);
            Thread.Sleep(5);

            // Then send click
            PostMessage(windowHandle, (uint)downMsg, IntPtr.Zero, lParam);
            Thread.Sleep(10);
            PostMessage(windowHandle, (uint)upMsg, IntPtr.Zero, lParam);

            return true;
        }

        /// <summary>
        /// Sends a proper double-click to a window without activating it
        /// </summary>
        /// <param name="windowHandle">Target window handle</param>
        /// <param name="screenX">Screen X coordinate</param>
        /// <param name="screenY">Screen Y coordinate</param>
        /// <param name="button">Mouse button to double-click</param>
        /// <param name="method">Input method to use</param>
        /// <returns>True if successful</returns>
        public bool SendMouseDoubleClick(IntPtr windowHandle, int screenX, int screenY, MouseButton button, MouseInputMethod method = MouseInputMethod.PostMessage)
        {
            try
            {
                // Convert screen coordinates to client coordinates
                var clientPoint = new POINT { x = screenX, y = screenY };
                if (!ScreenToClient(windowHandle, ref clientPoint))
                {
                    return false;
                }

                // Create lParam with coordinates
                var lParam = (IntPtr)((clientPoint.y << 16) | (clientPoint.x & 0xFFFF));

                switch (method)
                {
                    case MouseInputMethod.SendMessage:
                        return SendMouseDoubleClickSendMessage(windowHandle, lParam, button);
                    
                    case MouseInputMethod.PostMessage:
                        return SendMouseDoubleClickPostMessage(windowHandle, lParam, button);
                    
                    case MouseInputMethod.DirectClientCoordinates:
                        return SendMouseDoubleClickDirectClient(windowHandle, clientPoint.x, clientPoint.y, button);
                    
                    default:
                        return false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Mouse double-click failed: {ex.Message}");
                return false;
            }
        }

        private bool SendMouseDoubleClickSendMessage(IntPtr windowHandle, IntPtr lParam, MouseButton button)
        {
            var downMsg = button == MouseButton.Left ? WM_LBUTTONDOWN : WM_RBUTTONDOWN;
            var upMsg = button == MouseButton.Left ? WM_LBUTTONUP : WM_RBUTTONUP;
            var dblclkMsg = button == MouseButton.Left ? WM_LBUTTONDBLCLK : WM_RBUTTONDBLCLK;

            // Proper double-click sequence: DOWN -> UP -> DBLCLK -> UP
            SendMessage(windowHandle, (uint)downMsg, IntPtr.Zero, lParam);
            Thread.Sleep(10);
            SendMessage(windowHandle, (uint)upMsg, IntPtr.Zero, lParam);
            Thread.Sleep(10); // Timing is important for double-click recognition
            SendMessage(windowHandle, (uint)dblclkMsg, IntPtr.Zero, lParam);
            Thread.Sleep(10);
            SendMessage(windowHandle, (uint)upMsg, IntPtr.Zero, lParam);

            return true;
        }

        private bool SendMouseDoubleClickPostMessage(IntPtr windowHandle, IntPtr lParam, MouseButton button)
        {
            var downMsg = button == MouseButton.Left ? WM_LBUTTONDOWN : WM_RBUTTONDOWN;
            var upMsg = button == MouseButton.Left ? WM_LBUTTONUP : WM_RBUTTONUP;
            var dblclkMsg = button == MouseButton.Left ? WM_LBUTTONDBLCLK : WM_RBUTTONDBLCLK;

            // Proper double-click sequence: DOWN -> UP -> DBLCLK -> UP
            PostMessage(windowHandle, (uint)downMsg, IntPtr.Zero, lParam);
            Thread.Sleep(10);
            PostMessage(windowHandle, (uint)upMsg, IntPtr.Zero, lParam);
            Thread.Sleep(10); // Timing is important for double-click recognition
            PostMessage(windowHandle, (uint)dblclkMsg, IntPtr.Zero, lParam);
            Thread.Sleep(10);
            PostMessage(windowHandle, (uint)upMsg, IntPtr.Zero, lParam);

            return true;
        }

        private bool SendMouseDoubleClickDirectClient(IntPtr windowHandle, int clientX, int clientY, MouseButton button)
        {
            var lParam = (IntPtr)((clientY << 16) | (clientX & 0xFFFF));
            var downMsg = button == MouseButton.Left ? WM_LBUTTONDOWN : WM_RBUTTONDOWN;
            var upMsg = button == MouseButton.Left ? WM_LBUTTONUP : WM_RBUTTONUP;
            var dblclkMsg = button == MouseButton.Left ? WM_LBUTTONDBLCLK : WM_RBUTTONDBLCLK;

            // Send mouse move first
            PostMessage(windowHandle, (uint)WM_MOUSEMOVE, IntPtr.Zero, lParam);
            Thread.Sleep(5);

            // Proper double-click sequence: DOWN -> UP -> DBLCLK -> UP
            PostMessage(windowHandle, (uint)downMsg, IntPtr.Zero, lParam);
            Thread.Sleep(10);
            PostMessage(windowHandle, (uint)upMsg, IntPtr.Zero, lParam);
            Thread.Sleep(10);
            PostMessage(windowHandle, (uint)dblclkMsg, IntPtr.Zero, lParam);
            Thread.Sleep(10);
            PostMessage(windowHandle, (uint)upMsg, IntPtr.Zero, lParam);

            return true;
        }

        /// <summary>
        /// Broadcasts a double-click to all windows
        /// </summary>
        /// <param name="windows">List of windows to send to</param>
        /// <param name="screenX">Screen X coordinate</param>
        /// <param name="screenY">Screen Y coordinate</param>
        /// <param name="button">Mouse button to double-click</param>
        /// <param name="method">Input method to use</param>
        public void BroadcastMouseDoubleClick(System.Collections.Generic.List<Models.GameWindow> windows, int screenX, int screenY, MouseButton button, MouseInputMethod method = MouseInputMethod.PostMessage)
        {
            foreach (var window in windows)
            {
                if (window.IsActive)
                {
                    SendMouseDoubleClick(window.WindowHandle, screenX, screenY, button, method);
                }
            }
        }

        /// <summary>
        /// Tests all mouse input methods on a window
        /// </summary>
        /// <param name="windowHandle">Target window handle</param>
        /// <param name="screenX">Screen X coordinate</param>
        /// <param name="screenY">Screen Y coordinate</param>
        /// <param name="button">Mouse button to test</param>
        /// <returns>True if any method worked</returns>
        public bool TryAllMouseMethods(IntPtr windowHandle, int screenX, int screenY, MouseButton button)
        {
            var methods = Enum.GetValues<MouseInputMethod>();
            
            foreach (var method in methods)
            {
                System.Diagnostics.Debug.WriteLine($"Testing mouse method: {method}");
                
                try
                {
                    if (SendMouseClick(windowHandle, screenX, screenY, button, method))
                    {
                        System.Diagnostics.Debug.WriteLine($"Mouse method {method} succeeded!");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Mouse method {method} failed: {ex.Message}");
                }
                
                Thread.Sleep(100); // Small delay between attempts
            }
            
            return false;
        }

        /// <summary>
        /// Broadcasts a mouse click to all windows
        /// </summary>
        /// <param name="windows">List of windows to send to</param>
        /// <param name="screenX">Screen X coordinate</param>
        /// <param name="screenY">Screen Y coordinate</param>
        /// <param name="button">Mouse button to click</param>
        /// <param name="method">Input method to use</param>
        public void BroadcastMouseClick(System.Collections.Generic.List<Models.GameWindow> windows, int screenX, int screenY, MouseButton button, MouseInputMethod method = MouseInputMethod.PostMessage)
        {
            foreach (var window in windows)
            {
                if (window.IsActive)
                {
                    SendMouseClick(window.WindowHandle, screenX, screenY, button, method);
                }
            }
        }
    }
}