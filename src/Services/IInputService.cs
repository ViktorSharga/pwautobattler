using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameAutomation.Core;
using GameAutomation.Models;

namespace GameAutomation.Services
{
    public interface IInputService : IDisposable
    {
        Task<bool> SendKeySequenceAsync(IGameWindow window, VirtualKeyCode[] keys, int[] delays);
        Task<bool> SendKeySequenceAsync(IGameWindow window, string keySequence, string inputMethod);
        Task<bool> SendKeyPressAsync(IGameWindow window, VirtualKeyCode keyCode);
        Task<bool> SendMouseClickAsync(IGameWindow window, int x, int y);
        Task<bool> SendDoubleClickAsync(IGameWindow window, int x, int y);
        Task BroadcastKeyAsync(VirtualKeyCode key, IEnumerable<IGameWindow> windows);
        Task<bool> StartBroadcastModeAsync();
        Task StopBroadcastModeAsync();
        bool IsBroadcastModeEnabled { get; }
        InputMethod CurrentInputMethod { get; set; }
    }
}