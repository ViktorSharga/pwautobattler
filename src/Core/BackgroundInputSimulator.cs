using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Text;
using GameAutomation.Models;

namespace GameAutomation.Core
{
    public class BackgroundInputSimulator : IDisposable
    {
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_CHAR = 0x0102;
        private const int WM_SYSCHAR = 0x0106;
        private const int WM_ACTIVATE = 0x0006;
        private const int WM_SETFOCUS = 0x0007;
        private const int WM_KILLFOCUS = 0x0008;
        private const int WM_COMMAND = 0x0111;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;
        
        private const int GW_CHILD = 5;
        private const int GW_HWNDNEXT = 2;
        private const int WS_VISIBLE = 0x10000000;
        private const int WS_CHILD = 0x40000000;

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpWindowText, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern bool EnumChildWindows(IntPtr hWndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);

        [DllImport("user32.dll")]
        private static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindowEx(IntPtr hWndParent, IntPtr hWndChildAfter, string lpszClass, string lpszWindow);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        private readonly Dictionary<IntPtr, List<IntPtr>> _windowControls = new();
        private readonly Dictionary<IntPtr, HashSet<VirtualKeyCode>> _activeKeys = new();
        private readonly System.Threading.Timer _stateTimer;
        private bool _disposed = false;

        public List<GameWindow> RegisteredWindows { get; set; } = new();
        public bool IsRealTimeBroadcastEnabled { get; set; } = false;

        public event Action<string>? OnStatusUpdate;

        public BackgroundInputSimulator()
        {
            _stateTimer = new System.Threading.Timer(MaintainKeyStates, null, Timeout.Infinite, Timeout.Infinite);
        }

        public void StartRealTimeBroadcast()
        {
            IsRealTimeBroadcastEnabled = true;
            _stateTimer.Change(0, 100); // Check every 100ms
            OnStatusUpdate?.Invoke("Background real-time broadcasting started");
        }

        public void StopRealTimeBroadcast()
        {
            IsRealTimeBroadcastEnabled = false;
            _stateTimer.Change(Timeout.Infinite, Timeout.Infinite);
            
            // Release all held keys
            foreach (var windowKeys in _activeKeys.ToArray())
            {
                foreach (var key in windowKeys.Value.ToArray())
                {
                    SendBackgroundKeyUpToWindow(windowKeys.Key, key);
                }
            }
            _activeKeys.Clear();
            
            OnStatusUpdate?.Invoke("Background real-time broadcasting stopped");
        }

        public void SendBackgroundKeyPress(VirtualKeyCode key)
        {
            foreach (var window in RegisteredWindows)
            {
                if (window.IsActive && IsWindow(window.WindowHandle))
                {
                    SendBackgroundKeyToWindow(window.WindowHandle, key);
                }
            }
        }

        public void SendBackgroundKeyDown(VirtualKeyCode key)
        {
            foreach (var window in RegisteredWindows)
            {
                if (window.IsActive && IsWindow(window.WindowHandle))
                {
                    if (!_activeKeys.ContainsKey(window.WindowHandle))
                        _activeKeys[window.WindowHandle] = new HashSet<VirtualKeyCode>();
                    
                    _activeKeys[window.WindowHandle].Add(key);
                    SendBackgroundKeyDownToWindow(window.WindowHandle, key);
                }
            }
        }

        public void SendBackgroundKeyUp(VirtualKeyCode key)
        {
            foreach (var window in RegisteredWindows)
            {
                if (window.IsActive && IsWindow(window.WindowHandle))
                {
                    if (_activeKeys.ContainsKey(window.WindowHandle))
                        _activeKeys[window.WindowHandle].Remove(key);
                    
                    SendBackgroundKeyUpToWindow(window.WindowHandle, key);
                }
            }
        }

        private void SendBackgroundKeyToWindow(IntPtr windowHandle, VirtualKeyCode key)
        {
            // Method 1: Try direct WM_CHAR to main window
            if (TryDirectCharMessage(windowHandle, key))
                return;

            // Method 2: Try child window enumeration and targeting
            if (TryChildWindowInput(windowHandle, key))
                return;

            // Method 3: Try game-specific control targeting
            if (TryGameControlInput(windowHandle, key))
                return;

            // Method 4: Fallback to enhanced message approach
            TryEnhancedMessageInput(windowHandle, key);
        }

        private void SendBackgroundKeyDownToWindow(IntPtr windowHandle, VirtualKeyCode key)
        {
            // Send key down to main window and all child windows
            SendKeyDownMessage(windowHandle, key);
            
            var childWindows = EnumerateChildWindows(windowHandle);
            foreach (var child in childWindows)
            {
                SendKeyDownMessage(child, key);
            }
        }

        private void SendBackgroundKeyUpToWindow(IntPtr windowHandle, VirtualKeyCode key)
        {
            // Send key up to main window and all child windows
            SendKeyUpMessage(windowHandle, key);
            
            var childWindows = EnumerateChildWindows(windowHandle);
            foreach (var child in childWindows)
            {
                SendKeyUpMessage(child, key);
            }
        }

        private bool TryDirectCharMessage(IntPtr windowHandle, VirtualKeyCode key)
        {
            try
            {
                char ch = GetCharFromVirtualKey(key);
                if (ch != '\0')
                {
                    // Send WM_CHAR directly without focus change
                    SendMessage(windowHandle, WM_CHAR, new IntPtr(ch), IntPtr.Zero);
                    return true;
                }
            }
            catch (Exception)
            {
                // Continue to next method
            }
            return false;
        }

        private bool TryChildWindowInput(IntPtr windowHandle, VirtualKeyCode key)
        {
            try
            {
                var childWindows = EnumerateChildWindows(windowHandle);
                char ch = GetCharFromVirtualKey(key);
                
                foreach (var child in childWindows)
                {
                    var className = GetWindowClassName(child);
                    
                    // Target specific control types that handle input
                    if (IsInputControl(className))
                    {
                        if (ch != '\0')
                        {
                            SendMessage(child, WM_CHAR, new IntPtr(ch), IntPtr.Zero);
                        }
                        
                        uint scanCode = MapVirtualKey((uint)key, 0);
                        IntPtr lParam = new IntPtr((scanCode << 16) | 1);
                        IntPtr lParamUp = new IntPtr((scanCode << 16) | 0xC0000001);
                        
                        SendMessage(child, WM_KEYDOWN, new IntPtr((int)key), lParam);
                        Thread.Sleep(10);
                        SendMessage(child, WM_KEYUP, new IntPtr((int)key), lParamUp);
                        return true;
                    }
                }
            }
            catch (Exception)
            {
                // Continue to next method
            }
            return false;
        }

        private bool TryGameControlInput(IntPtr windowHandle, VirtualKeyCode key)
        {
            try
            {
                // Look for game-specific window classes
                var gameWindows = FindGameControls(windowHandle);
                char ch = GetCharFromVirtualKey(key);
                
                foreach (var gameWindow in gameWindows)
                {
                    // Send to game rendering window
                    if (ch != '\0')
                    {
                        PostMessage(gameWindow, WM_CHAR, new IntPtr(ch), IntPtr.Zero);
                    }
                    
                    uint scanCode = MapVirtualKey((uint)key, 0);
                    IntPtr lParam = new IntPtr((scanCode << 16) | 1);
                    IntPtr lParamUp = new IntPtr((scanCode << 16) | 0xC0000001);
                    
                    PostMessage(gameWindow, WM_KEYDOWN, new IntPtr((int)key), lParam);
                    Thread.Sleep(10);
                    PostMessage(gameWindow, WM_KEYUP, new IntPtr((int)key), lParamUp);
                    return true;
                }
            }
            catch (Exception)
            {
                // Continue to next method
            }
            return false;
        }

        private void TryEnhancedMessageInput(IntPtr windowHandle, VirtualKeyCode key)
        {
            try
            {
                // Send fake focus message to trick the application
                PostMessage(windowHandle, WM_SETFOCUS, IntPtr.Zero, IntPtr.Zero);
                
                char ch = GetCharFromVirtualKey(key);
                if (ch != '\0')
                {
                    PostMessage(windowHandle, WM_CHAR, new IntPtr(ch), IntPtr.Zero);
                }
                
                uint scanCode = MapVirtualKey((uint)key, 0);
                IntPtr lParam = new IntPtr((scanCode << 16) | 1);
                IntPtr lParamUp = new IntPtr((scanCode << 16) | 0xC0000001);
                
                PostMessage(windowHandle, WM_KEYDOWN, new IntPtr((int)key), lParam);
                Thread.Sleep(10);
                PostMessage(windowHandle, WM_KEYUP, new IntPtr((int)key), lParamUp);
                
                // Send fake focus lost message
                PostMessage(windowHandle, WM_KILLFOCUS, IntPtr.Zero, IntPtr.Zero);
            }
            catch (Exception ex)
            {
                OnStatusUpdate?.Invoke($"Enhanced message input failed: {ex.Message}");
            }
        }

        private void SendKeyDownMessage(IntPtr windowHandle, VirtualKeyCode key)
        {
            uint scanCode = MapVirtualKey((uint)key, 0);
            IntPtr lParam = new IntPtr((scanCode << 16) | 1);
            PostMessage(windowHandle, WM_KEYDOWN, new IntPtr((int)key), lParam);
        }

        private void SendKeyUpMessage(IntPtr windowHandle, VirtualKeyCode key)
        {
            uint scanCode = MapVirtualKey((uint)key, 0);
            IntPtr lParam = new IntPtr((scanCode << 16) | 0xC0000001);
            PostMessage(windowHandle, WM_KEYUP, new IntPtr((int)key), lParam);
        }

        private List<IntPtr> EnumerateChildWindows(IntPtr parentWindow)
        {
            var children = new List<IntPtr>();
            
            EnumChildWindows(parentWindow, (hWnd, lParam) =>
            {
                if (IsWindowVisible(hWnd))
                {
                    children.Add(hWnd);
                }
                return true;
            }, IntPtr.Zero);
            
            return children;
        }

        private List<IntPtr> FindGameControls(IntPtr parentWindow)
        {
            var gameControls = new List<IntPtr>();
            var childWindows = EnumerateChildWindows(parentWindow);
            
            foreach (var child in childWindows)
            {
                var className = GetWindowClassName(child);
                
                // Look for common game control classes
                if (IsGameControl(className))
                {
                    gameControls.Add(child);
                }
            }
            
            return gameControls;
        }

        private string GetWindowClassName(IntPtr windowHandle)
        {
            var className = new StringBuilder(256);
            GetClassName(windowHandle, className, className.Capacity);
            return className.ToString();
        }

        private bool IsInputControl(string className)
        {
            var inputControls = new[]
            {
                "Edit", "RichEdit", "RichEdit20A", "RichEdit20W", "RichEdit50W",
                "RICHEDIT_CLASS", "Static", "Button", "ComboBox", "ListBox"
            };
            
            return Array.Exists(inputControls, control => 
                className.IndexOf(control, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private bool IsGameControl(string className)
        {
            var gameControls = new[]
            {
                "OpenGL", "DirectX", "D3D", "Unity", "Unreal", "Game", "Canvas",
                "Render", "3D", "Graphics", "Display", "Screen", "Window"
            };
            
            return Array.Exists(gameControls, control => 
                className.IndexOf(control, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private char GetCharFromVirtualKey(VirtualKeyCode key)
        {
            return key switch
            {
                VirtualKeyCode.VK_A => 'a',
                VirtualKeyCode.VK_S => 's',
                VirtualKeyCode.VK_D => 'd',
                VirtualKeyCode.VK_W => 'w',
                VirtualKeyCode.VK_Q => 'q',
                VirtualKeyCode.VK_1 => '1',
                VirtualKeyCode.VK_2 => '2',
                VirtualKeyCode.VK_3 => '3',
                _ => '\0'
            };
        }

        private void MaintainKeyStates(object? state)
        {
            if (!IsRealTimeBroadcastEnabled) return;

            // Re-send key down messages for all active keys to maintain state
            foreach (var windowKeys in _activeKeys.ToArray())
            {
                var windowHandle = windowKeys.Key;
                var keys = windowKeys.Value;

                if (!IsWindow(windowHandle))
                {
                    _activeKeys.Remove(windowHandle);
                    continue;
                }

                foreach (var key in keys.ToArray())
                {
                    SendKeyDownMessage(windowHandle, key);
                }
            }
        }

        // Support for external key state management
        public void HandleExternalKeyDown(VirtualKeyCode key)
        {
            if (IsRealTimeBroadcastEnabled)
            {
                SendBackgroundKeyDown(key);
            }
        }

        public void HandleExternalKeyUp(VirtualKeyCode key)
        {
            if (IsRealTimeBroadcastEnabled)
            {
                SendBackgroundKeyUp(key);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                StopRealTimeBroadcast();
                _stateTimer?.Dispose();
                _disposed = true;
            }
        }
    }
}