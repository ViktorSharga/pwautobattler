using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GameAutomation.Core;
using GameAutomation.Models;

namespace GameAutomation.Services
{
    public class WindowService : IWindowService
    {
        private readonly WindowManager _windowManager;
        private readonly Dictionary<int, IGameWindow> _registeredWindows;
        private bool _disposed = false;

        public event EventHandler<WindowEventArgs>? WindowRegistered;
        public event EventHandler<WindowEventArgs>? WindowRemoved;

        public WindowService()
        {
            _windowManager = new WindowManager();
            _registeredWindows = new Dictionary<int, IGameWindow>();
        }

        public async Task<IGameWindow?> RegisterWindowAsync(int slot)
        {
            await Task.CompletedTask; // For async interface compliance
            
            // Check if slot is already occupied
            if (_registeredWindows.ContainsKey(slot))
            {
                return null;
            }

            // Get active window
            var activeWindow = _windowManager.GetActiveWindow();
            if (activeWindow == null)
            {
                return null;
            }

            // Check if this window is already registered in another slot
            var existingSlot = _registeredWindows.FirstOrDefault(kvp => 
                kvp.Value.WindowHandle == activeWindow.WindowHandle);
            
            if (existingSlot.Value != null)
            {
                return null; // Window already registered
            }

            // Register the window
            activeWindow.RegistrationSlot = slot;
            activeWindow.IsActive = true;
            _registeredWindows[slot] = activeWindow;

            // Raise event
            WindowRegistered?.Invoke(this, new WindowEventArgs(activeWindow, "Registered"));

            return activeWindow;
        }

        public async Task<bool> UnregisterWindowAsync(int slot)
        {
            await Task.CompletedTask; // For async interface compliance
            
            if (!_registeredWindows.TryGetValue(slot, out var window))
            {
                return false;
            }

            _registeredWindows.Remove(slot);
            
            // Raise event
            WindowRemoved?.Invoke(this, new WindowEventArgs(window, "Unregistered"));

            return true;
        }

        public IEnumerable<IGameWindow> GetActiveWindows()
        {
            // Clean up invalid windows
            var invalidSlots = _registeredWindows
                .Where(kvp => !_windowManager.ValidateWindow(kvp.Value.WindowHandle))
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var slot in invalidSlots)
            {
                var window = _registeredWindows[slot];
                _registeredWindows.Remove(slot);
                WindowRemoved?.Invoke(this, new WindowEventArgs(window, "InvalidWindow"));
            }

            return _registeredWindows.Values.Where(w => w.IsActive);
        }

        public async Task<IGameWindow?> GetMainWindowAsync()
        {
            await Task.CompletedTask; // For async interface compliance
            
            return _registeredWindows.Values.FirstOrDefault(w => w.IsMainWindow);
        }

        public IGameWindow? GetWindowBySlot(int slot)
        {
            _registeredWindows.TryGetValue(slot, out var window);
            return window;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _registeredWindows.Clear();
                _disposed = true;
            }
        }
    }
}