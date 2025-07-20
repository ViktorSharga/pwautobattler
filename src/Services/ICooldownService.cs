using System;
using System.Threading.Tasks;
using GameAutomation.Models;
using GameAutomation.Models.Spells;

namespace GameAutomation.Services
{
    public interface ICooldownService : IDisposable
    {
        bool IsOnCooldown(IGameWindow window, ISpell spell);
        TimeSpan? GetRemainingCooldown(IGameWindow window, ISpell spell);
        Task StartCooldownAsync(IGameWindow window, ISpell spell);
        Task ResetCooldownAsync(IGameWindow window, ISpell spell);
        Task CleanupExpiredCooldownsAsync();
        
        event EventHandler<CooldownEventArgs>? CooldownStarted;
        event EventHandler<CooldownEventArgs>? CooldownExpired;
    }

    public class CooldownEventArgs : EventArgs
    {
        public IGameWindow Window { get; }
        public ISpell Spell { get; }
        public TimeSpan? RemainingTime { get; }

        public CooldownEventArgs(IGameWindow window, ISpell spell, TimeSpan? remainingTime = null)
        {
            Window = window;
            Spell = spell;
            RemainingTime = remainingTime;
        }
    }
}