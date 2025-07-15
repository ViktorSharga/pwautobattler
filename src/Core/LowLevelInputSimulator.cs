using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using GameAutomation.Models;

namespace GameAutomation.Core
{
    public class LowLevelInputSimulator : IDisposable
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;
        
        private const int HC_ACTION = 0;
        private const int VK_LSHIFT = 0xA0;
        private const int VK_RSHIFT = 0xA1;
        private const int VK_LCONTROL = 0xA2;
        private const int VK_RCONTROL = 0xA3;
        private const int VK_LMENU = 0xA4;
        private const int VK_RMENU = 0xA5;

        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
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
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct INPUT_UNION
        {
            [FieldOffset(0)]
            public MOUSEINPUT mi;
            [FieldOffset(0)]
            public KEYBDINPUT ki;
            [FieldOffset(0)]
            public HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint type;
            public INPUT_UNION u;
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);

        [DllImport("user32.dll")]
        private static extern IntPtr SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        private const uint INPUT_KEYBOARD = 1;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const uint KEYEVENTF_SCANCODE = 0x0008;

        private IntPtr _hookId = IntPtr.Zero;
        private LowLevelKeyboardProc _proc;
        private readonly Dictionary<IntPtr, HashSet<VirtualKeyCode>> _activeKeys = new();
        private readonly System.Threading.Timer _broadcastTimer;
        private bool _disposed = false;

        public List<GameWindow> RegisteredWindows { get; set; } = new();
        public bool IsRealTimeBroadcastEnabled { get; set; } = false;

        public event Action<string>? OnStatusUpdate;

        public LowLevelInputSimulator()
        {
            _proc = HookCallback;
            
            // Create a timer for real-time broadcasting
            _broadcastTimer = new System.Threading.Timer(BroadcastActiveKeys, null, Timeout.Infinite, Timeout.Infinite);
        }

        public void StartKeyboardHook()
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule? curModule = curProcess.MainModule)
            {
                _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(curModule?.ModuleName), 0);
                
                if (_hookId == IntPtr.Zero)
                {
                    OnStatusUpdate?.Invoke("Failed to install keyboard hook");
                    return;
                }
                
                OnStatusUpdate?.Invoke("Low-level keyboard hook installed successfully");
            }
        }

        public void StopKeyboardHook()
        {
            if (_hookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookId);
                _hookId = IntPtr.Zero;
                OnStatusUpdate?.Invoke("Keyboard hook removed");
            }
        }

        public void StartRealTimeBroadcast()
        {
            IsRealTimeBroadcastEnabled = true;
            _broadcastTimer.Change(0, 16); // ~60 FPS update rate
            OnStatusUpdate?.Invoke("Real-time broadcasting started");
        }

        public void StopRealTimeBroadcast()
        {
            IsRealTimeBroadcastEnabled = false;
            _broadcastTimer.Change(Timeout.Infinite, Timeout.Infinite);
            
            // Release all held keys
            foreach (var windowKeys in _activeKeys.ToArray())
            {
                foreach (var key in windowKeys.Value.ToArray())
                {
                    SendKeyUpToWindow(windowKeys.Key, key);
                }
            }
            _activeKeys.Clear();
            
            OnStatusUpdate?.Invoke("Real-time broadcasting stopped");
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && IsRealTimeBroadcastEnabled && RegisteredWindows.Count > 0)
            {
                var vkCode = (VirtualKeyCode)Marshal.ReadInt32(lParam);
                
                // Only process movement keys in real-time mode
                if (IsMovementKey(vkCode))
                {
                    bool isKeyDown = wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN;
                    bool isKeyUp = wParam == (IntPtr)WM_KEYUP || wParam == (IntPtr)WM_SYSKEYUP;

                    if (isKeyDown)
                    {
                        HandleKeyDown(vkCode);
                    }
                    else if (isKeyUp)
                    {
                        HandleKeyUp(vkCode);
                    }
                }
            }

            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        private bool IsMovementKey(VirtualKeyCode vkCode)
        {
            return vkCode == VirtualKeyCode.VK_W || 
                   vkCode == VirtualKeyCode.VK_A || 
                   vkCode == VirtualKeyCode.VK_S || 
                   vkCode == VirtualKeyCode.VK_D;
        }

        private void HandleKeyDown(VirtualKeyCode vkCode)
        {
            foreach (var window in RegisteredWindows)
            {
                if (window.IsActive && IsWindow(window.WindowHandle))
                {
                    if (!_activeKeys.ContainsKey(window.WindowHandle))
                        _activeKeys[window.WindowHandle] = new HashSet<VirtualKeyCode>();
                    
                    if (!_activeKeys[window.WindowHandle].Contains(vkCode))
                    {
                        _activeKeys[window.WindowHandle].Add(vkCode);
                        SendKeyDownToWindow(window.WindowHandle, vkCode);
                    }
                }
            }
        }

        private void HandleKeyUp(VirtualKeyCode vkCode)
        {
            foreach (var window in RegisteredWindows)
            {
                if (window.IsActive && IsWindow(window.WindowHandle))
                {
                    if (_activeKeys.ContainsKey(window.WindowHandle) && 
                        _activeKeys[window.WindowHandle].Contains(vkCode))
                    {
                        _activeKeys[window.WindowHandle].Remove(vkCode);
                        SendKeyUpToWindow(window.WindowHandle, vkCode);
                    }
                }
            }
        }

        private void BroadcastActiveKeys(object? state)
        {
            if (!IsRealTimeBroadcastEnabled) return;

            // This method ensures continuous key states are maintained
            // Useful for games that might lose key state during focus changes
            foreach (var windowKeys in _activeKeys.ToArray())
            {
                var windowHandle = windowKeys.Key;
                var keys = windowKeys.Value;

                if (!IsWindow(windowHandle) || !IsWindowVisible(windowHandle))
                {
                    _activeKeys.Remove(windowHandle);
                    continue;
                }

                // Re-send key down for all active keys to maintain state
                foreach (var key in keys.ToArray())
                {
                    SendKeyDownToWindow(windowHandle, key, false); // Don't log repetitive actions
                }
            }
        }

        private void SendKeyDownToWindow(IntPtr windowHandle, VirtualKeyCode vkCode, bool logAction = true)
        {
            try
            {
                uint currentThreadId = GetCurrentThreadId();
                GetWindowThreadProcessId(windowHandle, out uint windowThreadId);

                // Attach to the window's thread
                bool attached = false;
                if (currentThreadId != windowThreadId)
                {
                    attached = AttachThreadInput(currentThreadId, windowThreadId, true);
                }

                // Brief focus to ensure input is received
                IntPtr originalForeground = GetForegroundWindow();
                SetForegroundWindow(windowHandle);
                Thread.Sleep(1); // Minimal delay

                // Send the key down event
                var inputs = new INPUT[1];
                inputs[0].type = INPUT_KEYBOARD;
                inputs[0].u.ki.wVk = (ushort)vkCode;
                inputs[0].u.ki.wScan = (ushort)MapVirtualKey((uint)vkCode, 0);
                inputs[0].u.ki.dwFlags = 0;
                inputs[0].u.ki.time = 0;
                inputs[0].u.ki.dwExtraInfo = IntPtr.Zero;

                SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));

                // Restore original focus
                SetForegroundWindow(originalForeground);

                // Detach from thread
                if (attached)
                {
                    AttachThreadInput(currentThreadId, windowThreadId, false);
                }

                if (logAction)
                {
                    OnStatusUpdate?.Invoke($"Key down: {vkCode} sent to window");
                }
            }
            catch (Exception ex)
            {
                OnStatusUpdate?.Invoke($"Error sending key down: {ex.Message}");
            }
        }

        private void SendKeyUpToWindow(IntPtr windowHandle, VirtualKeyCode vkCode, bool logAction = true)
        {
            try
            {
                uint currentThreadId = GetCurrentThreadId();
                GetWindowThreadProcessId(windowHandle, out uint windowThreadId);

                // Attach to the window's thread
                bool attached = false;
                if (currentThreadId != windowThreadId)
                {
                    attached = AttachThreadInput(currentThreadId, windowThreadId, true);
                }

                // Brief focus to ensure input is received
                IntPtr originalForeground = GetForegroundWindow();
                SetForegroundWindow(windowHandle);
                Thread.Sleep(1); // Minimal delay

                // Send the key up event
                var inputs = new INPUT[1];
                inputs[0].type = INPUT_KEYBOARD;
                inputs[0].u.ki.wVk = (ushort)vkCode;
                inputs[0].u.ki.wScan = (ushort)MapVirtualKey((uint)vkCode, 0);
                inputs[0].u.ki.dwFlags = KEYEVENTF_KEYUP;
                inputs[0].u.ki.time = 0;
                inputs[0].u.ki.dwExtraInfo = IntPtr.Zero;

                SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));

                // Restore original focus
                SetForegroundWindow(originalForeground);

                // Detach from thread
                if (attached)
                {
                    AttachThreadInput(currentThreadId, windowThreadId, false);
                }

                if (logAction)
                {
                    OnStatusUpdate?.Invoke($"Key up: {vkCode} sent to window");
                }
            }
            catch (Exception ex)
            {
                OnStatusUpdate?.Invoke($"Error sending key up: {ex.Message}");
            }
        }

        // Add A, S, D keys to the enum
        public enum VirtualKeyCode : ushort
        {
            VK_A = 0x41,
            VK_S = 0x53,
            VK_D = 0x44,
            VK_W = 0x57,
            VK_Q = 0x51,
            VK_1 = 0x31,
            VK_2 = 0x32,
            VK_3 = 0x33
        }

        // Method for single key testing
        public bool SendSingleKey(IntPtr windowHandle, VirtualKeyCode vkCode)
        {
            try
            {
                SendKeyDownToWindow(windowHandle, vkCode);
                Thread.Sleep(50);
                SendKeyUpToWindow(windowHandle, vkCode);
                return true;
            }
            catch (Exception ex)
            {
                OnStatusUpdate?.Invoke($"Error sending single key: {ex.Message}");
                return false;
            }
        }

        public void BroadcastSingleKey(VirtualKeyCode vkCode)
        {
            foreach (var window in RegisteredWindows)
            {
                if (window.IsActive && IsWindow(window.WindowHandle))
                {
                    SendSingleKey(window.WindowHandle, vkCode);
                    Thread.Sleep(10); // Small delay between windows
                }
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                StopRealTimeBroadcast();
                StopKeyboardHook();
                _broadcastTimer?.Dispose();
                _disposed = true;
            }
        }
    }
}