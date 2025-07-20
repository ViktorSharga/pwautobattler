using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameAutomation.Models;

namespace GameAutomation.Services
{
    public interface IWindowService : IDisposable
    {
        event EventHandler<WindowEventArgs>? WindowRegistered;
        event EventHandler<WindowEventArgs>? WindowRemoved;

        Task<IGameWindow?> RegisterWindowAsync(int slot);
        Task<bool> UnregisterWindowAsync(int slot);
        IEnumerable<IGameWindow> GetActiveWindows();
        Task<IGameWindow?> GetMainWindowAsync();
        IGameWindow? GetWindowBySlot(int slot);
    }

    public class WindowEventArgs : EventArgs
    {
        public IGameWindow Window { get; }
        public string Action { get; }

        public WindowEventArgs(IGameWindow window, string action)
        {
            Window = window;
            Action = action;
        }
    }
}