using System;
using System.Threading.Tasks;

namespace GameAutomation.Services
{
    public interface IConfigurationService
    {
        T GetValue<T>(string key, T defaultValue = default!);
        Task SetValueAsync<T>(string key, T value);
        Task SaveAsync();
        Task LoadAsync();
        
        event EventHandler<ConfigurationChangedEventArgs>? ConfigurationChanged;
    }

    public class ConfigurationChangedEventArgs : EventArgs
    {
        public string Key { get; }
        public object? OldValue { get; }
        public object? NewValue { get; }

        public ConfigurationChangedEventArgs(string key, object? oldValue, object? newValue)
        {
            Key = key;
            OldValue = oldValue;
            NewValue = newValue;
        }
    }
}