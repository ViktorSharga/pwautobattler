using System;
using System.Collections.Generic;
using GameAutomation.Models;

namespace GameAutomation.Core
{
    public class CooldownManager
    {
        // Key: "WindowHandle_ActionName", Value: Cooldown end time
        private readonly Dictionary<string, DateTime> _cooldowns = new();
        
        public void StartCooldown(GameWindow window, GameAction action)
        {
            var key = GetKey(window, action);
            _cooldowns[key] = DateTime.Now.Add(action.Cooldown);
        }
        
        public bool IsOnCooldown(GameWindow window, GameAction action)
        {
            var key = GetKey(window, action);
            if (_cooldowns.TryGetValue(key, out var endTime))
            {
                if (DateTime.Now < endTime)
                {
                    return true;
                }
                else
                {
                    // Cooldown expired, remove it
                    _cooldowns.Remove(key);
                }
            }
            return false;
        }
        
        public TimeSpan? GetRemainingCooldown(GameWindow window, GameAction action)
        {
            var key = GetKey(window, action);
            if (_cooldowns.TryGetValue(key, out var endTime))
            {
                var remaining = endTime - DateTime.Now;
                if (remaining > TimeSpan.Zero)
                {
                    return remaining;
                }
                else
                {
                    // Cooldown expired, remove it
                    _cooldowns.Remove(key);
                }
            }
            return null;
        }
        
        public string GetCooldownDisplay(GameWindow window, GameAction action)
        {
            var remaining = GetRemainingCooldown(window, action);
            if (remaining.HasValue)
            {
                var totalSeconds = (int)remaining.Value.TotalSeconds;
                var minutes = totalSeconds / 60;
                var seconds = totalSeconds % 60;
                return $"{minutes}:{seconds:D2}";
            }
            return "";
        }
        
        private string GetKey(GameWindow window, GameAction action)
        {
            return $"{window.WindowHandle}_{action.Name}";
        }
        
        public void ClearCooldowns()
        {
            _cooldowns.Clear();
        }
        
        public void RemoveWindowCooldowns(GameWindow window)
        {
            var keysToRemove = new List<string>();
            foreach (var key in _cooldowns.Keys)
            {
                if (key.StartsWith($"{window.WindowHandle}_"))
                {
                    keysToRemove.Add(key);
                }
            }
            
            foreach (var key in keysToRemove)
            {
                _cooldowns.Remove(key);
            }
        }
        
        public void ResetCooldown(GameWindow window, GameAction action)
        {
            var key = GetKey(window, action);
            _cooldowns.Remove(key);
        }
    }
}