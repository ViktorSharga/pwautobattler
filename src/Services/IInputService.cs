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
        Task BroadcastKeyAsync(VirtualKeyCode key, IEnumerable<IGameWindow> windows);
        Task<bool> StartBroadcastModeAsync();
        Task StopBroadcastModeAsync();
        bool IsBroadcastModeEnabled { get; }
        InputMethod CurrentInputMethod { get; set; }
    }
}