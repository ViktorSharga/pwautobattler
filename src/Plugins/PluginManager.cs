using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using GameAutomation.Models.Spells;

namespace GameAutomation.Plugins
{
    /// <summary>
    /// Manages loading and lifecycle of spell plugins
    /// </summary>
    public class PluginManager : IDisposable
    {
        private readonly List<ISpellPlugin> _loadedPlugins;
        private readonly IServiceProvider _serviceProvider;
        private readonly string _pluginsDirectory;
        private bool _disposed = false;

        public PluginManager(IServiceProvider serviceProvider, string? pluginsDirectory = null)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _pluginsDirectory = pluginsDirectory ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins");
            _loadedPlugins = new List<ISpellPlugin>();
        }

        /// <summary>
        /// Gets all loaded plugins
        /// </summary>
        public IReadOnlyList<ISpellPlugin> LoadedPlugins => _loadedPlugins.AsReadOnly();

        /// <summary>
        /// Loads all plugins from the plugins directory
        /// </summary>
        public async Task LoadPluginsAsync()
        {
            try
            {
                if (!Directory.Exists(_pluginsDirectory))
                {
                    Directory.CreateDirectory(_pluginsDirectory);
                    return;
                }

                var pluginFiles = Directory.GetFiles(_pluginsDirectory, "*.dll", SearchOption.AllDirectories);
                
                foreach (var pluginFile in pluginFiles)
                {
                    try
                    {
                        await LoadPluginFromFileAsync(pluginFile);
                    }
                    catch (Exception ex)
                    {
                        // Log plugin loading error but continue with other plugins
                        Console.WriteLine($"Failed to load plugin from {pluginFile}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load plugins from directory {_pluginsDirectory}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Loads a specific plugin from a file
        /// </summary>
        public async Task LoadPluginFromFileAsync(string filePath)
        {
            try
            {
                var assembly = Assembly.LoadFrom(filePath);
                var pluginTypes = assembly.GetTypes()
                    .Where(type => typeof(ISpellPlugin).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                    .ToList();

                foreach (var pluginType in pluginTypes)
                {
                    var plugin = Activator.CreateInstance(pluginType) as ISpellPlugin;
                    if (plugin != null)
                    {
                        await plugin.InitializeAsync(_serviceProvider);
                        _loadedPlugins.Add(plugin);
                        Console.WriteLine($"Loaded plugin: {plugin.Name} v{plugin.Version}");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load plugin from {filePath}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Registers a plugin instance directly (useful for built-in plugins)
        /// </summary>
        public async Task RegisterPluginAsync(ISpellPlugin plugin)
        {
            if (plugin == null)
                throw new ArgumentNullException(nameof(plugin));

            if (_loadedPlugins.Any(p => p.Name == plugin.Name))
            {
                throw new InvalidOperationException($"Plugin with name '{plugin.Name}' is already registered");
            }

            await plugin.InitializeAsync(_serviceProvider);
            _loadedPlugins.Add(plugin);
        }

        /// <summary>
        /// Unregisters a plugin
        /// </summary>
        public void UnregisterPlugin(string pluginName)
        {
            var plugin = _loadedPlugins.FirstOrDefault(p => p.Name == pluginName);
            if (plugin != null)
            {
                plugin.Dispose();
                _loadedPlugins.Remove(plugin);
            }
        }

        /// <summary>
        /// Gets all spells from all loaded plugins
        /// </summary>
        public IEnumerable<ISpell> GetAllSpells()
        {
            return _loadedPlugins.SelectMany(plugin => plugin.GetSpells());
        }

        /// <summary>
        /// Gets spells for a specific game class from all loaded plugins
        /// </summary>
        public IEnumerable<ISpell> GetSpellsForClass(Models.GameClass gameClass)
        {
            return _loadedPlugins
                .Where(plugin => plugin.SupportsClass(gameClass))
                .SelectMany(plugin => plugin.GetSpellsForClass(gameClass));
        }

        /// <summary>
        /// Gets plugins that support a specific game class
        /// </summary>
        public IEnumerable<ISpellPlugin> GetPluginsForClass(Models.GameClass gameClass)
        {
            return _loadedPlugins.Where(plugin => plugin.SupportsClass(gameClass));
        }

        /// <summary>
        /// Finds a plugin by name
        /// </summary>
        public ISpellPlugin? FindPlugin(string name)
        {
            return _loadedPlugins.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Reloads all plugins
        /// </summary>
        public async Task ReloadPluginsAsync()
        {
            // Dispose existing plugins
            foreach (var plugin in _loadedPlugins.ToList())
            {
                plugin.Dispose();
            }
            _loadedPlugins.Clear();

            // Reload plugins
            await LoadPluginsAsync();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                foreach (var plugin in _loadedPlugins)
                {
                    try
                    {
                        plugin.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error disposing plugin {plugin.Name}: {ex.Message}");
                    }
                }
                _loadedPlugins.Clear();
                _disposed = true;
            }
        }
    }
}