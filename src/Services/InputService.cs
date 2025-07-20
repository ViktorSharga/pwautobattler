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
        private readonly IConfigurationService? _configurationService;
        private bool _broadcastModeEnabled = false;
        private bool _disposed = false;

        public bool IsBroadcastModeEnabled => _broadcastModeEnabled;
        
        public InputMethod CurrentInputMethod 
        { 
            get => _inputSimulator.CurrentMethod;
            set => _inputSimulator.CurrentMethod = value;
        }

        public InputService(IConfigurationService? configurationService = null)
        {
            _configurationService = configurationService;
            _inputSimulator = new InputSimulator();
            _keyboardHook = new LowLevelKeyboardHook();
            _simulators = new Dictionary<InputMethod, InputSimulator>
            {
                { InputMethod.KeyboardEventOptimized, _inputSimulator }
            };
            
            // Set default input method from configuration
            if (_configurationService != null)
            {
                var defaultMethodName = _configurationService.GetString("input.defaultMethod", "KeyboardEventOptimized");
                if (Enum.TryParse<InputMethod>(defaultMethodName, out var method))
                {
                    CurrentInputMethod = method;
                }
                
                _broadcastModeEnabled = _configurationService.GetBool("input.enableBroadcastMode", false);
            }
            
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
                    else if (delays.Length == 0 && _configurationService != null)
                    {
                        // Use default delay from configuration if no delays specified
                        var defaultDelay = _configurationService.GetInt("input.keyDelayMs", 50);
                        if (defaultDelay > 0)
                        {
                            await Task.Delay(defaultDelay);
                        }
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