using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GameAutomation.Infrastructure;
using GameAutomation.Models;
using GameAutomation.Models.Spells;

namespace GameAutomation.Services
{
    public class CooldownService : ICooldownService
    {
        private readonly ConcurrentDictionary<CooldownKey, CooldownEntry> _cooldowns;
        private readonly System.Threading.Timer _cleanupTimer;
        private readonly IConfigurationService? _configurationService;
        private readonly MemoryManager? _memoryManager;
        private bool _disposed = false;
        private int _cleanupCount = 0;
        private readonly int _maxCooldowns;

        public event EventHandler<CooldownEventArgs>? CooldownStarted;
        public event EventHandler<CooldownEventArgs>? CooldownExpired;

        public CooldownService(IConfigurationService? configurationService = null, MemoryManager? memoryManager = null)
        {
            _configurationService = configurationService;
            _memoryManager = memoryManager;
            _cooldowns = new ConcurrentDictionary<CooldownKey, CooldownEntry>();
            
            // Get configuration values or use defaults
            var cleanupIntervalMinutes = _configurationService?.GetInt("cooldowns.cleanupIntervalMinutes", 30) ?? 30;
            var cleanupInterval = TimeSpan.FromMinutes(cleanupIntervalMinutes);
            _maxCooldowns = _configurationService?.GetInt("cooldowns.maxCooldowns", 1000) ?? 1000;
            
            _cleanupTimer = new System.Threading.Timer(CleanupExpiredCooldowns, null, cleanupInterval, cleanupInterval);
        }

        public bool IsOnCooldown(IGameWindow window, ISpell spell)
        {
            var key = new CooldownKey(window.WindowHandle, spell.Id);
            
            if (_cooldowns.TryGetValue(key, out var entry))
            {
                var now = DateTime.UtcNow;
                if (now < entry.EndTime)
                {
                    // Update last access time for LRU
                    entry.LastAccessTime = now;
                    _cooldowns.TryUpdate(key, entry, entry);
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
            
            if (_cooldowns.TryGetValue(key, out var entry))
            {
                var now = DateTime.UtcNow;
                var remaining = entry.EndTime - now;
                if (remaining > TimeSpan.Zero)
                {
                    // Update last access time for LRU
                    entry.LastAccessTime = now;
                    _cooldowns.TryUpdate(key, entry, entry);
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
            
            // Check if we're approaching max cooldowns limit
            if (_cooldowns.Count >= _maxCooldowns)
            {
                await CleanupExpiredCooldownsAsync();
                
                // If still at limit after cleanup, remove oldest entries using LRU
                if (_cooldowns.Count >= _maxCooldowns)
                {
                    CleanupOldestEntries();
                }
            }
            
            var key = new CooldownKey(window.WindowHandle, spell.Id);
            var now = DateTime.UtcNow;
            var entry = new CooldownEntry
            {
                EndTime = now.Add(spell.Cooldown),
                LastAccessTime = now,
                SpellId = spell.Id
            };
            
            _cooldowns.AddOrUpdate(key, entry, (k, v) => entry);
            
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
            
            var now = DateTime.UtcNow;
            var expiredKeys = new List<CooldownKey>();
            var removedCount = 0;
            
            foreach (var kvp in _cooldowns)
            {
                if (now >= kvp.Value.EndTime)
                {
                    expiredKeys.Add(kvp.Key);
                }
            }
            
            foreach (var key in expiredKeys)
            {
                if (_cooldowns.TryRemove(key, out _))
                {
                    removedCount++;
                }
            }
            
            _cleanupCount++;
            
            // Trigger memory optimization if we're using memory manager and removed many items
            if (removedCount > 100 && _memoryManager != null)
            {
                _memoryManager.ScheduleCleanup(this);
            }
        }

        private void CleanupExpiredCooldowns(object? state)
        {
            _ = CleanupExpiredCooldownsAsync();
        }

        private void CleanupOldestEntries()
        {
            const int entriesToRemove = 100; // Remove 10% of max capacity
            
            var entriesToCleanup = _cooldowns
                .OrderBy(kvp => kvp.Value.LastAccessTime)
                .Take(entriesToRemove)
                .Select(kvp => kvp.Key)
                .ToList();
            
            foreach (var key in entriesToCleanup)
            {
                _cooldowns.TryRemove(key, out _);
            }
        }

        public int GetCooldownCount() => _cooldowns.Count;
        public int GetCleanupCount() => _cleanupCount;

        public void Dispose()
        {
            if (!_disposed)
            {
                _cleanupTimer?.Dispose();
                _cooldowns.Clear();
                _disposed = true;
            }
        }

        private struct CooldownEntry
        {
            public DateTime EndTime { get; set; }
            public DateTime LastAccessTime { get; set; }
            public string SpellId { get; set; }
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