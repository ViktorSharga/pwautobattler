using System;
using System.Threading.Tasks;
using GameAutomation.Configuration;

namespace GameAutomation.Services
{
    public interface IConfigurationService
    {
        /// <summary>
        /// Loads configuration from file
        /// </summary>
        Task LoadConfigurationAsync();

        /// <summary>
        /// Saves current configuration to file
        /// </summary>
        Task SaveConfigurationAsync();

        /// <summary>
        /// Gets a configuration value by key (section.property format)
        /// </summary>
        T GetValue<T>(string key, T defaultValue = default!);

        /// <summary>
        /// Sets a configuration value by key (section.property format)
        /// </summary>
        void SetValue<T>(string key, T value);

        /// <summary>
        /// Gets the complete configuration object
        /// </summary>
        AppConfiguration GetConfiguration();

        /// <summary>
        /// Updates the entire configuration object
        /// </summary>
        void UpdateConfiguration(AppConfiguration configuration);

        /// <summary>
        /// Convenience methods for common types
        /// </summary>
        bool GetBool(string key, bool defaultValue = false);
        int GetInt(string key, int defaultValue = 0);
        string GetString(string key, string defaultValue = "");
        double GetDouble(string key, double defaultValue = 0.0);

        /// <summary>
        /// Reloads configuration from file
        /// </summary>
        void ReloadConfiguration();
    }
}