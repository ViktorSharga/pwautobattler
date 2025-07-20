using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GameAutomation.Models;
using GameAutomation.Models.Spells;

namespace GameAutomation.Services
{
    public class CooldownService : ICooldownService
    {
        private readonly ConcurrentDictionary<CooldownKey, DateTime> _cooldowns;
        private readonly Timer _cleanupTimer;
        private bool _disposed = false;

        public event EventHandler<CooldownEventArgs>? CooldownStarted;
        public event EventHandler<CooldownEventArgs>? CooldownExpired;

        public CooldownService()
        {
            _cooldowns = new ConcurrentDictionary<CooldownKey, DateTime>();
            
            // Cleanup timer runs every 30 seconds
            _cleanupTimer = new Timer(CleanupExpiredCooldowns, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
        }

        public bool IsOnCooldown(IGameWindow window, ISpell spell)
        {
            var key = new CooldownKey(window.WindowHandle, spell.Id);
            
            if (_cooldowns.TryGetValue(key, out var endTime))
            {
                if (DateTime.Now < endTime)
                {
                    return true;
                }
                else
                {
                    // Cooldown expired, remove it and raise event
                    if (_cooldowns.TryRemove(key, out _))
                    {
                        CooldownExpired?.Invoke(this, new CooldownEventArgs(window, spell));
                    }
                }
            }
            
            return false;
        }

        public TimeSpan? GetRemainingCooldown(IGameWindow window, ISpell spell)
        {
            var key = new CooldownKey(window.WindowHandle, spell.Id);
            
            if (_cooldowns.TryGetValue(key, out var endTime))
            {
                var remaining = endTime - DateTime.Now;
                if (remaining > TimeSpan.Zero)
                {
                    return remaining;
                }
                else
                {
                    // Cooldown expired, remove it and raise event
                    if (_cooldowns.TryRemove(key, out _))
                    {
                        CooldownExpired?.Invoke(this, new CooldownEventArgs(window, spell));
                    }
                }
            }
            
            return null;
        }

        public async Task StartCooldownAsync(IGameWindow window, ISpell spell)
        {
            await Task.CompletedTask; // For async interface compliance
            
            var key = new CooldownKey(window.WindowHandle, spell.Id);
            var endTime = DateTime.Now.Add(spell.Cooldown);
            
            _cooldowns.AddOrUpdate(key, endTime, (k, v) => endTime);
            
            CooldownStarted?.Invoke(this, new CooldownEventArgs(window, spell, spell.Cooldown));
        }

        public async Task ResetCooldownAsync(IGameWindow window, ISpell spell)
        {
            await Task.CompletedTask; // For async interface compliance
            
            var key = new CooldownKey(window.WindowHandle, spell.Id);
            
            if (_cooldowns.TryRemove(key, out _))
            {
                CooldownExpired?.Invoke(this, new CooldownEventArgs(window, spell));
            }
        }

        public async Task CleanupExpiredCooldownsAsync()
        {
            await Task.CompletedTask; // For async interface compliance
            
            var now = DateTime.Now;
            var expiredKeys = new List<CooldownKey>();
            
            foreach (var kvp in _cooldowns)
            {
                if (now >= kvp.Value)
                {
                    expiredKeys.Add(kvp.Key);
                }
            }
            
            foreach (var key in expiredKeys)
            {
                _cooldowns.TryRemove(key, out _);
            }
        }

        private void CleanupExpiredCooldowns(object? state)
        {
            _ = CleanupExpiredCooldownsAsync();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _cleanupTimer?.Dispose();
                _cooldowns.Clear();
                _disposed = true;
            }
        }

        private readonly struct CooldownKey : IEquatable<CooldownKey>
        {
            public readonly IntPtr WindowHandle;
            public readonly string SpellId;

            public CooldownKey(IntPtr windowHandle, string spellId)
            {
                WindowHandle = windowHandle;
                SpellId = spellId;
            }

            public bool Equals(CooldownKey other)
            {
                return WindowHandle.Equals(other.WindowHandle) && 
                       string.Equals(SpellId, other.SpellId, StringComparison.Ordinal);
            }

            public override bool Equals(object? obj)
            {
                return obj is CooldownKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(WindowHandle, SpellId);
            }
        }
    }
}