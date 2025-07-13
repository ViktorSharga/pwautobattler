using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using GameAutomation.Models;

namespace GameAutomation.Core
{
    public class WindowManager
    {
        private const int MAX_TITLE_LENGTH = 256;

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        public List<GameWindow> EnumerateGameWindows()
        {
            var gameWindows = new List<GameWindow>();
            
            EnumWindows((hwnd, lParam) =>
            {
                if (IsWindowVisible(hwnd))
                {
                    GetWindowThreadProcessId(hwnd, out uint processId);
                    
                    try
                    {
                        var process = Process.GetProcessById((int)processId);
                        if (process.ProcessName.Equals("ElementClient", StringComparison.OrdinalIgnoreCase))
                        {
                            var title = GetWindowTitle(hwnd);
                            gameWindows.Add(new GameWindow
                            {
                                ProcessId = (int)processId,
                                WindowHandle = hwnd,
                                WindowTitle = title,
                                IsActive = true
                            });
                        }
                    }
                    catch (ArgumentException)
                    {
                        // Process no longer exists
                    }
                }
                return true;
            }, IntPtr.Zero);

            return gameWindows;
        }

        public GameWindow? GetWindowByProcessId(int processId)
        {
            var windows = EnumerateGameWindows();
            return windows.FirstOrDefault(w => w.ProcessId == processId);
        }

        public GameWindow? GetActiveWindow()
        {
            var foregroundWindow = GetForegroundWindow();
            if (foregroundWindow == IntPtr.Zero)
                return null;

            GetWindowThreadProcessId(foregroundWindow, out uint processId);
            
            try
            {
                var process = Process.GetProcessById((int)processId);
                if (process.ProcessName.Equals("ElementClient", StringComparison.OrdinalIgnoreCase))
                {
                    var title = GetWindowTitle(foregroundWindow);
                    return new GameWindow
                    {
                        ProcessId = (int)processId,
                        WindowHandle = foregroundWindow,
                        WindowTitle = title,
                        IsActive = true
                    };
                }
            }
            catch (ArgumentException)
            {
                // Process no longer exists
            }

            return null;
        }

        public bool ValidateWindow(IntPtr windowHandle)
        {
            if (windowHandle == IntPtr.Zero)
                return false;

            GetWindowThreadProcessId(windowHandle, out uint processId);
            
            try
            {
                var process = Process.GetProcessById((int)processId);
                return process.ProcessName.Equals("ElementClient", StringComparison.OrdinalIgnoreCase);
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        private string GetWindowTitle(IntPtr windowHandle)
        {
            var title = new StringBuilder(MAX_TITLE_LENGTH);
            GetWindowText(windowHandle, title, MAX_TITLE_LENGTH);
            return title.ToString();
        }
    }
}