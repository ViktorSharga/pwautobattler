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
    public class EnhancedBackgroundInputSimulator : IDisposable
    {
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_CHAR = 0x0102;
        private const int WM_SYSCHAR = 0x0106;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;
        private const int WM_ACTIVATE = 0x0006;
        private const int WM_SETFOCUS = 0x0007;
        private const int WM_KILLFOCUS = 0x0008;
        private const int WM_COMMAND = 0x0111;
        private const int WM_USER = 0x0400;
        private const int WM_IME_KEYDOWN = 0x0290;
        private const int WM_IME_KEYUP = 0x0291;
        
        private const int INPUT_KEYBOARD = 1;
        private const int KEYEVENTF_KEYUP = 0x0002;
        private const int KEYEVENTF_SCANCODE = 0x0008;
        private const int KEYEVENTF_UNICODE = 0x0004;
        
        private const int GW_CHILD = 5;
        private const int GW_HWNDNEXT = 2;
        private const int GW_OWNER = 4;
        
        private const int PROCESS_ALL_ACCESS = 0x001F0FFF;
        private const int MEM_COMMIT = 0x1000;
        private const int PAGE_READWRITE = 0x04;

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint type;
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

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpWindowText, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern bool EnumChildWindows(IntPtr hWndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);

        [DllImport("user32.dll")]
        private static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindowEx(IntPtr hWndParent, IntPtr hWndChildAfter, string lpszClass, string lpszWindow);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll")]
        private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, out int lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int nSize, out int lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        private static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll")]
        private static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint dwFreeType);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        private readonly Dictionary<IntPtr, HashSet<VirtualKeyCode>> _activeKeys = new();
        private readonly Dictionary<IntPtr, ProcessInfo> _processInfoCache = new();
        private readonly System.Threading.Timer _stateTimer;
        private bool _disposed = false;

        public List<GameWindow> RegisteredWindows { get; set; } = new();
        public bool IsRealTimeBroadcastEnabled { get; set; } = false;

        public event Action<string>? OnStatusUpdate;

        private class ProcessInfo
        {
            public IntPtr ProcessHandle { get; set; }
            public string ProcessName { get; set; } = "";
            public List<IntPtr> ChildWindows { get; set; } = new();
            public string MainWindowClass { get; set; } = "";
            public bool IsDirectInputGame { get; set; } = false;
            public bool IsRawInputGame { get; set; } = false;
        }

        public EnhancedBackgroundInputSimulator()
        {
            _stateTimer = new System.Threading.Timer(MaintainKeyStates, null, Timeout.Infinite, Timeout.Infinite);
        }

        public void StartRealTimeBroadcast()
        {
            IsRealTimeBroadcastEnabled = true;
            _stateTimer.Change(0, 50); // Check every 50ms for better responsiveness
            AnalyzeRegisteredWindows();
            OnStatusUpdate?.Invoke("Enhanced background broadcasting started");
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
                    SendEnhancedKeyUpToWindow(windowKeys.Key, key);
                }
            }
            _activeKeys.Clear();
            
            OnStatusUpdate?.Invoke("Enhanced background broadcasting stopped");
        }

        private void AnalyzeRegisteredWindows()
        {
            foreach (var window in RegisteredWindows)
            {
                if (window.IsActive && IsWindow(window.WindowHandle))
                {
                    var processInfo = AnalyzeWindowProcess(window.WindowHandle);
                    _processInfoCache[window.WindowHandle] = processInfo;
                    
                    OnStatusUpdate?.Invoke($"Analyzed window: {processInfo.ProcessName} - DirectInput: {processInfo.IsDirectInputGame}, RawInput: {processInfo.IsRawInputGame}");
                }
            }
        }

        private ProcessInfo AnalyzeWindowProcess(IntPtr windowHandle)
        {
            var processInfo = new ProcessInfo();
            
            GetWindowThreadProcessId(windowHandle, out uint processId);
            processInfo.ProcessHandle = OpenProcess(PROCESS_ALL_ACCESS, false, (int)processId);
            
            try
            {
                var process = Process.GetProcessById((int)processId);
                processInfo.ProcessName = process.ProcessName;
            }
            catch
            {
                processInfo.ProcessName = "Unknown";
            }
            
            processInfo.MainWindowClass = GetWindowClassName(windowHandle);
            processInfo.ChildWindows = EnumerateAllChildWindows(windowHandle);
            
            // Analyze if game uses DirectInput/RawInput
            processInfo.IsDirectInputGame = IsDirectInputGame(processInfo);
            processInfo.IsRawInputGame = IsRawInputGame(processInfo);
            
            return processInfo;
        }

        private bool IsDirectInputGame(ProcessInfo processInfo)
        {
            // Check for DirectInput indicators
            var directInputIndicators = new[]
            {
                "dinput", "directinput", "dx", "d3d", "opengl", "vulkan", "unity", "unreal", "gamebryo"
            };
            
            string className = processInfo.MainWindowClass.ToLower();
            string processName = processInfo.ProcessName.ToLower();
            
            foreach (var indicator in directInputIndicators)
            {
                if (className.Contains(indicator) || processName.Contains(indicator))
                {
                    return true;
                }
            }
            
            return false;
        }

        private bool IsRawInputGame(ProcessInfo processInfo)
        {
            // Check for Raw Input indicators
            var rawInputIndicators = new[]
            {
                "raw", "hid", "input", "game", "engine"
            };
            
            string className = processInfo.MainWindowClass.ToLower();
            
            foreach (var indicator in rawInputIndicators)
            {
                if (className.Contains(indicator))
                {
                    return true;
                }
            }
            
            return false;
        }

        public void SendEnhancedKeyPress(VirtualKeyCode key)
        {
            foreach (var window in RegisteredWindows)
            {
                if (window.IsActive && IsWindow(window.WindowHandle))
                {
                    SendEnhancedKeyToWindow(window.WindowHandle, key);
                }
            }
        }

        public void SendEnhancedKeyDown(VirtualKeyCode key)
        {
            foreach (var window in RegisteredWindows)
            {
                if (window.IsActive && IsWindow(window.WindowHandle))
                {
                    if (!_activeKeys.ContainsKey(window.WindowHandle))
                        _activeKeys[window.WindowHandle] = new HashSet<VirtualKeyCode>();
                    
                    _activeKeys[window.WindowHandle].Add(key);
                    SendEnhancedKeyDownToWindow(window.WindowHandle, key);
                }
            }
        }

        public void SendEnhancedKeyUp(VirtualKeyCode key)
        {
            foreach (var window in RegisteredWindows)
            {
                if (window.IsActive && IsWindow(window.WindowHandle))
                {
                    if (_activeKeys.ContainsKey(window.WindowHandle))
                        _activeKeys[window.WindowHandle].Remove(key);
                    
                    SendEnhancedKeyUpToWindow(window.WindowHandle, key);
                }
            }
        }

        private void SendEnhancedKeyToWindow(IntPtr windowHandle, VirtualKeyCode key)
        {
            if (!_processInfoCache.TryGetValue(windowHandle, out var processInfo))
            {
                // Fallback to basic method
                SendBasicKeyToWindow(windowHandle, key);
                return;
            }

            // Try multiple methods based on game type
            if (TryDirectInputMethod(windowHandle, key, processInfo))
                return;
            
            if (TryRawInputMethod(windowHandle, key, processInfo))
                return;
            
            if (TryThreadAttachMethod(windowHandle, key, processInfo))
                return;
            
            if (TryAdvancedMessagingMethod(windowHandle, key, processInfo))
                return;
            
            // Fallback to basic method
            SendBasicKeyToWindow(windowHandle, key);
        }

        private void SendEnhancedKeyDownToWindow(IntPtr windowHandle, VirtualKeyCode key)
        {
            if (!_processInfoCache.TryGetValue(windowHandle, out var processInfo))
            {
                SendBasicKeyDownToWindow(windowHandle, key);
                return;
            }

            // Use the same enhanced methods for key down
            if (TryDirectInputKeyDown(windowHandle, key, processInfo))
                return;
            
            if (TryRawInputKeyDown(windowHandle, key, processInfo))
                return;
            
            if (TryThreadAttachKeyDown(windowHandle, key, processInfo))
                return;
            
            SendBasicKeyDownToWindow(windowHandle, key);
        }

        private void SendEnhancedKeyUpToWindow(IntPtr windowHandle, VirtualKeyCode key)
        {
            if (!_processInfoCache.TryGetValue(windowHandle, out var processInfo))
            {
                SendBasicKeyUpToWindow(windowHandle, key);
                return;
            }

            // Use the same enhanced methods for key up
            if (TryDirectInputKeyUp(windowHandle, key, processInfo))
                return;
            
            if (TryRawInputKeyUp(windowHandle, key, processInfo))
                return;
            
            if (TryThreadAttachKeyUp(windowHandle, key, processInfo))
                return;
            
            SendBasicKeyUpToWindow(windowHandle, key);
        }

        private bool TryDirectInputMethod(IntPtr windowHandle, VirtualKeyCode key, ProcessInfo processInfo)
        {
            if (!processInfo.IsDirectInputGame)
                return false;

            try
            {
                // For DirectInput games, try sending to all child windows
                foreach (var childWindow in processInfo.ChildWindows)
                {
                    if (IsWindowVisible(childWindow))
                    {
                        char ch = GetCharFromVirtualKey(key);
                        if (ch != '\0')
                        {
                            // Send both character and key messages
                            PostMessage(childWindow, WM_CHAR, new IntPtr(ch), IntPtr.Zero);
                            PostMessage(childWindow, WM_SYSCHAR, new IntPtr(ch), IntPtr.Zero);
                        }
                        
                        uint scanCode = MapVirtualKey((uint)key, 0);
                        IntPtr lParam = new IntPtr((scanCode << 16) | 1);
                        IntPtr lParamUp = new IntPtr((scanCode << 16) | 0xC0000001);
                        
                        PostMessage(childWindow, WM_KEYDOWN, new IntPtr((int)key), lParam);
                        PostMessage(childWindow, WM_SYSKEYDOWN, new IntPtr((int)key), lParam);
                        Thread.Sleep(10);
                        PostMessage(childWindow, WM_KEYUP, new IntPtr((int)key), lParamUp);
                        PostMessage(childWindow, WM_SYSKEYUP, new IntPtr((int)key), lParamUp);
                    }
                }
                
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool TryRawInputMethod(IntPtr windowHandle, VirtualKeyCode key, ProcessInfo processInfo)
        {
            if (!processInfo.IsRawInputGame)
                return false;

            try
            {
                // For Raw Input games, try sending to the main window with all message types
                char ch = GetCharFromVirtualKey(key);
                if (ch != '\0')
                {
                    PostMessage(windowHandle, WM_CHAR, new IntPtr(ch), IntPtr.Zero);
                    PostMessage(windowHandle, WM_SYSCHAR, new IntPtr(ch), IntPtr.Zero);
                    PostMessage(windowHandle, WM_IME_KEYDOWN, new IntPtr((int)key), IntPtr.Zero);
                    PostMessage(windowHandle, WM_IME_KEYUP, new IntPtr((int)key), IntPtr.Zero);
                }
                
                uint scanCode = MapVirtualKey((uint)key, 0);
                IntPtr lParam = new IntPtr((scanCode << 16) | 1);
                IntPtr lParamUp = new IntPtr((scanCode << 16) | 0xC0000001);
                
                PostMessage(windowHandle, WM_KEYDOWN, new IntPtr((int)key), lParam);
                PostMessage(windowHandle, WM_SYSKEYDOWN, new IntPtr((int)key), lParam);
                Thread.Sleep(10);
                PostMessage(windowHandle, WM_KEYUP, new IntPtr((int)key), lParamUp);
                PostMessage(windowHandle, WM_SYSKEYUP, new IntPtr((int)key), lParamUp);
                
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool TryThreadAttachMethod(IntPtr windowHandle, VirtualKeyCode key, ProcessInfo processInfo)
        {
            try
            {
                uint currentThreadId = GetCurrentThreadId();
                GetWindowThreadProcessId(windowHandle, out uint windowThreadId);

                if (currentThreadId == windowThreadId)
                    return false;

                // Attach to the window's thread
                bool attached = AttachThreadInput(currentThreadId, windowThreadId, true);
                if (!attached)
                    return false;

                // Send input using SendInput while attached
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
                
                // Detach from thread
                AttachThreadInput(currentThreadId, windowThreadId, false);
                
                return result == inputs.Length;
            }
            catch
            {
                return false;
            }
        }

        private bool TryAdvancedMessagingMethod(IntPtr windowHandle, VirtualKeyCode key, ProcessInfo processInfo)
        {
            try
            {
                // Try sending custom WM_USER messages that some games respond to
                uint baseMessage = WM_USER + 0x1000;
                
                PostMessage(windowHandle, baseMessage, new IntPtr((int)key), new IntPtr(1)); // Key down
                Thread.Sleep(10);
                PostMessage(windowHandle, baseMessage, new IntPtr((int)key), new IntPtr(0)); // Key up
                
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool TryDirectInputKeyDown(IntPtr windowHandle, VirtualKeyCode key, ProcessInfo processInfo)
        {
            if (!processInfo.IsDirectInputGame)
                return false;

            try
            {
                foreach (var childWindow in processInfo.ChildWindows)
                {
                    if (IsWindowVisible(childWindow))
                    {
                        uint scanCode = MapVirtualKey((uint)key, 0);
                        IntPtr lParam = new IntPtr((scanCode << 16) | 1);
                        
                        PostMessage(childWindow, WM_KEYDOWN, new IntPtr((int)key), lParam);
                        PostMessage(childWindow, WM_SYSKEYDOWN, new IntPtr((int)key), lParam);
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool TryRawInputKeyDown(IntPtr windowHandle, VirtualKeyCode key, ProcessInfo processInfo)
        {
            if (!processInfo.IsRawInputGame)
                return false;

            try
            {
                uint scanCode = MapVirtualKey((uint)key, 0);
                IntPtr lParam = new IntPtr((scanCode << 16) | 1);
                
                PostMessage(windowHandle, WM_KEYDOWN, new IntPtr((int)key), lParam);
                PostMessage(windowHandle, WM_SYSKEYDOWN, new IntPtr((int)key), lParam);
                
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool TryThreadAttachKeyDown(IntPtr windowHandle, VirtualKeyCode key, ProcessInfo processInfo)
        {
            try
            {
                uint currentThreadId = GetCurrentThreadId();
                GetWindowThreadProcessId(windowHandle, out uint windowThreadId);

                if (currentThreadId == windowThreadId)
                    return false;

                bool attached = AttachThreadInput(currentThreadId, windowThreadId, true);
                if (!attached)
                    return false;

                uint scanCode = MapVirtualKey((uint)key, 0);
                var input = new INPUT
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
                };

                uint result = SendInput(1, new[] { input }, Marshal.SizeOf(typeof(INPUT)));
                
                AttachThreadInput(currentThreadId, windowThreadId, false);
                
                return result == 1;
            }
            catch
            {
                return false;
            }
        }

        private bool TryDirectInputKeyUp(IntPtr windowHandle, VirtualKeyCode key, ProcessInfo processInfo)
        {
            if (!processInfo.IsDirectInputGame)
                return false;

            try
            {
                foreach (var childWindow in processInfo.ChildWindows)
                {
                    if (IsWindowVisible(childWindow))
                    {
                        uint scanCode = MapVirtualKey((uint)key, 0);
                        IntPtr lParam = new IntPtr((scanCode << 16) | 0xC0000001);
                        
                        PostMessage(childWindow, WM_KEYUP, new IntPtr((int)key), lParam);
                        PostMessage(childWindow, WM_SYSKEYUP, new IntPtr((int)key), lParam);
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool TryRawInputKeyUp(IntPtr windowHandle, VirtualKeyCode key, ProcessInfo processInfo)
        {
            if (!processInfo.IsRawInputGame)
                return false;

            try
            {
                uint scanCode = MapVirtualKey((uint)key, 0);
                IntPtr lParam = new IntPtr((scanCode << 16) | 0xC0000001);
                
                PostMessage(windowHandle, WM_KEYUP, new IntPtr((int)key), lParam);
                PostMessage(windowHandle, WM_SYSKEYUP, new IntPtr((int)key), lParam);
                
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool TryThreadAttachKeyUp(IntPtr windowHandle, VirtualKeyCode key, ProcessInfo processInfo)
        {
            try
            {
                uint currentThreadId = GetCurrentThreadId();
                GetWindowThreadProcessId(windowHandle, out uint windowThreadId);

                if (currentThreadId == windowThreadId)
                    return false;

                bool attached = AttachThreadInput(currentThreadId, windowThreadId, true);
                if (!attached)
                    return false;

                uint scanCode = MapVirtualKey((uint)key, 0);
                var input = new INPUT
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
                };

                uint result = SendInput(1, new[] { input }, Marshal.SizeOf(typeof(INPUT)));
                
                AttachThreadInput(currentThreadId, windowThreadId, false);
                
                return result == 1;
            }
            catch
            {
                return false;
            }
        }

        private void SendBasicKeyToWindow(IntPtr windowHandle, VirtualKeyCode key)
        {
            // Fallback to basic PostMessage
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
        }

        private void SendBasicKeyDownToWindow(IntPtr windowHandle, VirtualKeyCode key)
        {
            uint scanCode = MapVirtualKey((uint)key, 0);
            IntPtr lParam = new IntPtr((scanCode << 16) | 1);
            PostMessage(windowHandle, WM_KEYDOWN, new IntPtr((int)key), lParam);
        }

        private void SendBasicKeyUpToWindow(IntPtr windowHandle, VirtualKeyCode key)
        {
            uint scanCode = MapVirtualKey((uint)key, 0);
            IntPtr lParam = new IntPtr((scanCode << 16) | 0xC0000001);
            PostMessage(windowHandle, WM_KEYUP, new IntPtr((int)key), lParam);
        }

        private List<IntPtr> EnumerateAllChildWindows(IntPtr parentWindow)
        {
            var children = new List<IntPtr>();
            
            EnumChildWindows(parentWindow, (hWnd, lParam) =>
            {
                children.Add(hWnd);
                return true;
            }, IntPtr.Zero);
            
            return children;
        }

        private string GetWindowClassName(IntPtr windowHandle)
        {
            var className = new StringBuilder(256);
            GetClassName(windowHandle, className, className.Capacity);
            return className.ToString();
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

            // Re-send key down messages for all active keys
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
                    SendEnhancedKeyDownToWindow(windowHandle, key);
                }
            }
        }

        public void HandleExternalKeyDown(VirtualKeyCode key)
        {
            if (IsRealTimeBroadcastEnabled)
            {
                SendEnhancedKeyDown(key);
            }
        }

        public void HandleExternalKeyUp(VirtualKeyCode key)
        {
            if (IsRealTimeBroadcastEnabled)
            {
                SendEnhancedKeyUp(key);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                StopRealTimeBroadcast();
                
                // Clean up process handles
                foreach (var processInfo in _processInfoCache.Values)
                {
                    if (processInfo.ProcessHandle != IntPtr.Zero)
                    {
                        CloseHandle(processInfo.ProcessHandle);
                    }
                }
                _processInfoCache.Clear();
                
                _stateTimer?.Dispose();
                _disposed = true;
            }
        }
    }
}