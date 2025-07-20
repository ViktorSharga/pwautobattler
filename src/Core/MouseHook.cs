using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace GameAutomation.Core
{
    public class MouseHook : IDisposable
    {
        private const int WH_MOUSE_LL = 14;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int HC_ACTION = 0;

        private readonly LowLevelMouseProc _proc;
        private IntPtr _hookId = IntPtr.Zero;
        private bool _disposed = false;

        public delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        public event EventHandler<MouseEventArgs>? CtrlLeftClick;
        public event EventHandler<MouseEventArgs>? CtrlRightClick;
        public event EventHandler<MouseEventArgs>? ShiftLeftClick;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        public class MouseEventArgs : EventArgs
        {
            public int X { get; }
            public int Y { get; }
            public DateTime Timestamp { get; }

            public MouseEventArgs(int x, int y)
            {
                X = x;
                Y = y;
                Timestamp = DateTime.Now;
            }
        }

        public MouseHook()
        {
            _proc = HookCallback;
        }

        public void StartListening()
        {
            if (_hookId == IntPtr.Zero)
            {
                _hookId = SetHook(_proc);
            }
        }

        public void StopListening()
        {
            if (_hookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookId);
                _hookId = IntPtr.Zero;
            }
        }

        private IntPtr SetHook(LowLevelMouseProc proc)
        {
            using (var curProcess = Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_MOUSE_LL, proc,
                    GetModuleHandle(curModule!.ModuleName), 0);
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= HC_ACTION)
            {
                // Check if Ctrl or Shift key is pressed
                bool ctrlPressed = (GetAsyncKeyState(0x11) & 0x8000) != 0; // VK_CONTROL
                bool shiftPressed = (GetAsyncKeyState(0x10) & 0x8000) != 0; // VK_SHIFT

                if (ctrlPressed || shiftPressed)
                {
                    var hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                    var mouseArgs = new MouseEventArgs(hookStruct.pt.x, hookStruct.pt.y);

                    if (wParam == (IntPtr)WM_LBUTTONDOWN)
                    {
                        if (ctrlPressed)
                        {
                            CtrlLeftClick?.Invoke(this, mouseArgs);
                        }
                        else if (shiftPressed)
                        {
                            ShiftLeftClick?.Invoke(this, mouseArgs);
                        }
                    }
                    else if (wParam == (IntPtr)WM_RBUTTONDOWN && ctrlPressed)
                    {
                        CtrlRightClick?.Invoke(this, mouseArgs);
                    }
                }
            }

            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                StopListening();
                _disposed = true;
            }
        }
    }
}