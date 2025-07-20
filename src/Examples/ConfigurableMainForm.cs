using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using GameAutomation.Configuration;
using GameAutomation.Plugins;
using GameAutomation.Plugins.Examples;
using GameAutomation.Services;

namespace GameAutomation.Examples
{
    /// <summary>
    /// Example demonstrating how to use the new configuration and plugin system
    /// </summary>
    public partial class ConfigurableMainForm : Form
    {
        private readonly IConfigurationService _configurationService;
        private readonly PluginManager _pluginManager;
        private readonly ISpellService _spellService;
        private readonly ICooldownService _cooldownService;
        private readonly IInputService _inputService;
        private readonly IWindowService _windowService;

        private Label _configStatusLabel = null!;
        private Label _pluginStatusLabel = null!;
        private Button _reloadConfigButton = null!;
        private Button _reloadPluginsButton = null!;
        private ListBox _pluginListBox = null!;
        private TextBox _configTextBox = null!;

        public ConfigurableMainForm()
        {
            // Initialize services with dependency injection pattern
            _configurationService = new ConfigurationService();
            _windowService = new WindowService();
            _inputService = new InputService(_configurationService);
            _cooldownService = new CooldownService();
            _spellService = new SpellService(_inputService, _cooldownService);
            
            // Create simple service provider for plugins
            var serviceProvider = new SimpleServiceProvider();
            serviceProvider.RegisterInstance<IConfigurationService>(_configurationService);
            serviceProvider.RegisterInstance<ISpellService>(_spellService);
            serviceProvider.RegisterInstance<IInputService>(_inputService);
            serviceProvider.RegisterInstance<ICooldownService>(_cooldownService);
            serviceProvider.RegisterInstance<IWindowService>(_windowService);

            _pluginManager = new PluginManager(serviceProvider);

            InitializeComponent();
            InitializeAsync();
        }

        private void InitializeComponent()
        {
            Text = "Configurable Game Automation - Phase 5 Demo";
            Size = new Size(800, 600);
            StartPosition = FormStartPosition.CenterScreen;

            CreateConfigurationControls();
            CreatePluginControls();
            LayoutControls();
        }

        private void CreateConfigurationControls()
        {
            // Configuration status
            var configLabel = new Label
            {
                Text = "Configuration:",
                Location = new Point(10, 10),
                Size = new Size(100, 20),
                Font = new Font("Microsoft Sans Serif", 8F, FontStyle.Bold)
            };

            _configStatusLabel = new Label
            {
                Text = "Loading...",
                Location = new Point(120, 10),
                Size = new Size(200, 20),
                ForeColor = Color.Gray
            };

            _reloadConfigButton = new Button
            {
                Text = "Reload Config",
                Location = new Point(330, 8),
                Size = new Size(100, 25)
            };
            _reloadConfigButton.Click += ReloadConfigButton_Click;

            // Configuration display
            _configTextBox = new TextBox
            {
                Location = new Point(10, 40),
                Size = new Size(760, 200),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                Font = new Font("Consolas", 9F)
            };

            Controls.Add(configLabel);
            Controls.Add(_configStatusLabel);
            Controls.Add(_reloadConfigButton);
            Controls.Add(_configTextBox);
        }

        private void CreatePluginControls()
        {
            // Plugin status
            var pluginLabel = new Label
            {
                Text = "Plugins:",
                Location = new Point(10, 250),
                Size = new Size(100, 20),
                Font = new Font("Microsoft Sans Serif", 8F, FontStyle.Bold)
            };

            _pluginStatusLabel = new Label
            {
                Text = "Loading...",
                Location = new Point(120, 250),
                Size = new Size(200, 20),
                ForeColor = Color.Gray
            };

            _reloadPluginsButton = new Button
            {
                Text = "Reload Plugins",
                Location = new Point(330, 248),
                Size = new Size(100, 25)
            };
            _reloadPluginsButton.Click += ReloadPluginsButton_Click;

            // Plugin list
            _pluginListBox = new ListBox
            {
                Location = new Point(10, 280),
                Size = new Size(760, 200),
                Font = new Font("Consolas", 9F)
            };

            Controls.Add(pluginLabel);
            Controls.Add(_pluginStatusLabel);
            Controls.Add(_reloadPluginsButton);
            Controls.Add(_pluginListBox);
        }

        private void LayoutControls()
        {
            // Create instruction label
            var instructionLabel = new Label
            {
                Text = "This demo shows the new configuration system (app.json) and plugin architecture.\n" +
                       "Configuration values are loaded from src/Configuration/app.json.\n" +
                       "Plugins can be loaded dynamically to extend spell functionality.",
                Location = new Point(10, 490),
                Size = new Size(760, 60),
                ForeColor = Color.DarkGreen
            };

            Controls.Add(instructionLabel);
        }

        private async void InitializeAsync()
        {
            try
            {
                // Load configuration
                await _configurationService.LoadConfigurationAsync();
                UpdateConfigurationDisplay();
                _configStatusLabel.Text = "Loaded successfully";
                _configStatusLabel.ForeColor = Color.Green;

                // Load spells
                await _spellService.ReloadSpellsAsync();

                // Load plugins
                await LoadPluginsAsync();
                
            }
            catch (Exception ex)
            {
                _configStatusLabel.Text = $"Error: {ex.Message}";
                _configStatusLabel.ForeColor = Color.Red;
            }
        }

        private async Task LoadPluginsAsync()
        {
            try
            {
                // Register built-in plugin
                var builtInPlugin = new BuiltInSpellsPlugin();
                await _pluginManager.RegisterPluginAsync(builtInPlugin);

                // Register example custom plugin
                var customPlugin = new CustomSpellsPlugin();
                await _pluginManager.RegisterPluginAsync(customPlugin);

                // Load external plugins from directory
                await _pluginManager.LoadPluginsAsync();

                UpdatePluginDisplay();
                _pluginStatusLabel.Text = $"Loaded {_pluginManager.LoadedPlugins.Count} plugins";
                _pluginStatusLabel.ForeColor = Color.Green;
            }
            catch (Exception ex)
            {
                _pluginStatusLabel.Text = $"Error: {ex.Message}";
                _pluginStatusLabel.ForeColor = Color.Red;
            }
        }

        private void UpdateConfigurationDisplay()
        {
            var config = _configurationService.GetConfiguration();
            
            var configText = $"Input Settings:\n" +
                           $"  Default Method: {config.Input.DefaultMethod}\n" +
                           $"  Retry Attempts: {config.Input.RetryAttempts}\n" +
                           $"  Key Delay: {config.Input.KeyDelayMs}ms\n" +
                           $"  Broadcast Mode: {config.Input.EnableBroadcastMode}\n\n" +
                           
                           $"UI Settings:\n" +
                           $"  Update Throttle: {config.UI.UpdateThrottleMs}ms\n" +
                           $"  Max Spells Per Row: {config.UI.MaxSpellsPerRow}\n" +
                           $"  Double Buffering: {config.UI.EnableDoubleBuffering}\n\n" +
                           
                           $"Game Settings:\n" +
                           $"  Process Name: {config.Game.ProcessName}\n" +
                           $"  Max Windows: {config.Game.MaxWindows}\n" +
                           $"  Auto Scan: {config.Game.AutoScanOnStartup}\n\n" +
                           
                           $"Hotkey Settings:\n" +
                           $"  Global Hotkeys: {config.Hotkeys.EnableGlobalHotkeys}\n" +
                           $"  Modifiers: {string.Join(", ", config.Hotkeys.RegistrationModifiers)}\n";

            _configTextBox.Text = configText;
        }

        private void UpdatePluginDisplay()
        {
            _pluginListBox.Items.Clear();
            
            foreach (var plugin in _pluginManager.LoadedPlugins)
            {
                var spellCount = plugin.GetSpells().Count();
                var supportedClasses = string.Join(", ", plugin.SupportedClasses);
                
                _pluginListBox.Items.Add($"{plugin.Name} v{plugin.Version}");
                _pluginListBox.Items.Add($"  Description: {plugin.Description}");
                _pluginListBox.Items.Add($"  Author: {plugin.Author}");
                _pluginListBox.Items.Add($"  Spells: {spellCount}");
                _pluginListBox.Items.Add($"  Supports: {supportedClasses}");
                _pluginListBox.Items.Add("");
            }
        }

        private async void ReloadConfigButton_Click(object? sender, EventArgs e)
        {
            try
            {
                _configStatusLabel.Text = "Reloading...";
                _configStatusLabel.ForeColor = Color.Gray;
                
                await _configurationService.LoadConfigurationAsync();
                UpdateConfigurationDisplay();
                
                _configStatusLabel.Text = "Reloaded successfully";
                _configStatusLabel.ForeColor = Color.Green;
            }
            catch (Exception ex)
            {
                _configStatusLabel.Text = $"Error: {ex.Message}";
                _configStatusLabel.ForeColor = Color.Red;
            }
        }

        private async void ReloadPluginsButton_Click(object? sender, EventArgs e)
        {
            try
            {
                _pluginStatusLabel.Text = "Reloading...";
                _pluginStatusLabel.ForeColor = Color.Gray;
                
                await _pluginManager.ReloadPluginsAsync();
                
                // Re-register built-in plugins
                await LoadPluginsAsync();
            }
            catch (Exception ex)
            {
                _pluginStatusLabel.Text = $"Error: {ex.Message}";
                _pluginStatusLabel.ForeColor = Color.Red;
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _pluginManager?.Dispose();
            base.OnFormClosed(e);
        }
    }

    /// <summary>
    /// Simple service provider implementation for dependency injection
    /// </summary>
    public class SimpleServiceProvider : IServiceProvider
    {
        private readonly Dictionary<Type, object> _services = new();

        public void RegisterInstance<T>(T instance) where T : class
        {
            _services[typeof(T)] = instance;
        }

        public object? GetService(Type serviceType)
        {
            _services.TryGetValue(serviceType, out var service);
            return service;
        }
    }
}