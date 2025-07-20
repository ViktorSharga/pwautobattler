using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GameAutomation.Core;
using GameAutomation.Models;

namespace GameAutomation.Services
{
    public class InputService : IInputService
    {
        private readonly InputSimulator _inputSimulator;
        private readonly LowLevelKeyboardHook _keyboardHook;
        private readonly Dictionary<InputMethod, InputSimulator> _simulators;
        private bool _broadcastModeEnabled = false;
        private bool _disposed = false;

        public bool IsBroadcastModeEnabled => _broadcastModeEnabled;
        
        public InputMethod CurrentInputMethod 
        { 
            get => _inputSimulator.CurrentMethod;
            set => _inputSimulator.CurrentMethod = value;
        }

        public InputService()
        {
            _inputSimulator = new InputSimulator();
            _keyboardHook = new LowLevelKeyboardHook();
            _simulators = new Dictionary<InputMethod, InputSimulator>
            {
                { InputMethod.KeyboardEventOptimized, _inputSimulator }
            };
            
            // Wire up keyboard hook for broadcast mode
            _keyboardHook.KeyDown += OnGlobalKeyDown;
        }

        public async Task<bool> SendKeySequenceAsync(IGameWindow window, VirtualKeyCode[] keys, int[] delays)
        {
            await Task.CompletedTask; // For async interface compliance
            
            if (!window.IsValid)
                return false;

            try
            {
                for (int i = 0; i < keys.Length; i++)
                {
                    var success = _inputSimulator.SendKeyPress(window.WindowHandle, keys[i], CurrentInputMethod);
                    if (!success)
                        return false;
                    
                    if (i < delays.Length && delays[i] > 0)
                    {
                        await Task.Delay(delays[i]);
                    }
                }
                
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task BroadcastKeyAsync(VirtualKeyCode key, IEnumerable<IGameWindow> windows)
        {
            await Task.CompletedTask; // For async interface compliance
            
            var activeWindows = windows.Where(w => w.IsActive && w.IsValid).ToList();
            
            _inputSimulator.BroadcastToAll(
                activeWindows.Cast<GameWindow>().ToList(),
                handle => _inputSimulator.SendKeyPress(handle, key, CurrentInputMethod)
            );
        }

        public async Task<bool> StartBroadcastModeAsync()
        {
            await Task.CompletedTask; // For async interface compliance
            
            if (_broadcastModeEnabled)
                return true;

            try
            {
                _keyboardHook.StartListening();
                _broadcastModeEnabled = true;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task StopBroadcastModeAsync()
        {
            await Task.CompletedTask; // For async interface compliance
            
            if (!_broadcastModeEnabled)
                return;

            _keyboardHook.StopListening();
            _broadcastModeEnabled = false;
        }

        private void OnGlobalKeyDown(object? sender, System.Windows.Forms.Keys key)
        {
            // This will be connected to broadcast functionality
            // For now, we'll leave it as a placeholder
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                StopBroadcastModeAsync().Wait();
                _keyboardHook?.Dispose();
                _disposed = true;
            }
        }
    }
}