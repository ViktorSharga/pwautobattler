using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using GameAutomation.Configuration;

namespace GameAutomation.Services
{
    public class ConfigurationService : IConfigurationService
    {
        private readonly string _configFilePath;
        private AppConfiguration _configuration;
        private readonly JsonSerializerOptions _jsonOptions;

        public ConfigurationService(string? configFilePath = null)
        {
            _configFilePath = configFilePath ?? Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, 
                "src", "Configuration", "app.json");
            
            _configuration = new AppConfiguration();
            
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true,
                AllowTrailingCommas = true
            };
        }

        public async Task LoadConfigurationAsync()
        {
            try
            {
                if (File.Exists(_configFilePath))
                {
                    var jsonContent = await File.ReadAllTextAsync(_configFilePath);
                    var loadedConfig = JsonSerializer.Deserialize<AppConfiguration>(jsonContent, _jsonOptions);
                    
                    if (loadedConfig != null)
                    {
                        _configuration = loadedConfig;
                    }
                }
                else
                {
                    // Create default configuration file
                    await SaveConfigurationAsync();
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load configuration from {_configFilePath}: {ex.Message}", ex);
            }
        }

        public async Task SaveConfigurationAsync()
        {
            try
            {
                var directory = Path.GetDirectoryName(_configFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var jsonContent = JsonSerializer.Serialize(_configuration, _jsonOptions);
                await File.WriteAllTextAsync(_configFilePath, jsonContent);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save configuration to {_configFilePath}: {ex.Message}", ex);
            }
        }

        public T GetValue<T>(string key, T defaultValue = default!)
        {
            try
            {
                var keyParts = key.Split('.');
                if (keyParts.Length != 2)
                {
                    return defaultValue;
                }

                var section = keyParts[0].ToLower();
                var property = keyParts[1];

                object? sectionObject = section switch
                {
                    "input" => _configuration.Input,
                    "ui" => _configuration.UI,
                    "game" => _configuration.Game,
                    "cooldowns" => _configuration.Cooldowns,
                    "hotkeys" => _configuration.Hotkeys,
                    "logging" => _configuration.Logging,
                    _ => null
                };

                if (sectionObject == null)
                {
                    return defaultValue;
                }

                var propertyInfo = sectionObject.GetType().GetProperty(property);
                if (propertyInfo == null)
                {
                    return defaultValue;
                }

                var value = propertyInfo.GetValue(sectionObject);
                if (value is T typedValue)
                {
                    return typedValue;
                }

                // Try to convert the value
                if (value != null && typeof(T) != typeof(object))
                {
                    try
                    {
                        return (T)Convert.ChangeType(value, typeof(T));
                    }
                    catch
                    {
                        return defaultValue;
                    }
                }

                return defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        public void SetValue<T>(string key, T value)
        {
            try
            {
                var keyParts = key.Split('.');
                if (keyParts.Length != 2)
                {
                    return;
                }

                var section = keyParts[0].ToLower();
                var property = keyParts[1];

                object? sectionObject = section switch
                {
                    "input" => _configuration.Input,
                    "ui" => _configuration.UI,
                    "game" => _configuration.Game,
                    "cooldowns" => _configuration.Cooldowns,
                    "hotkeys" => _configuration.Hotkeys,
                    "logging" => _configuration.Logging,
                    _ => null
                };

                if (sectionObject == null)
                {
                    return;
                }

                var propertyInfo = sectionObject.GetType().GetProperty(property);
                if (propertyInfo != null && propertyInfo.CanWrite)
                {
                    propertyInfo.SetValue(sectionObject, value);
                }
            }
            catch
            {
                // Silently fail for now - could add logging later
            }
        }

        public AppConfiguration GetConfiguration()
        {
            return _configuration;
        }

        public void UpdateConfiguration(AppConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public bool GetBool(string key, bool defaultValue = false)
        {
            return GetValue(key, defaultValue);
        }

        public int GetInt(string key, int defaultValue = 0)
        {
            return GetValue(key, defaultValue);
        }

        public string GetString(string key, string defaultValue = "")
        {
            return GetValue(key, defaultValue);
        }

        public double GetDouble(string key, double defaultValue = 0.0)
        {
            return GetValue(key, defaultValue);
        }

        public void ReloadConfiguration()
        {
            _ = Task.Run(LoadConfigurationAsync);
        }
    }
}