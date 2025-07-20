# Plugin Development Guide

## Overview

PW Autobattler supports a robust plugin system that allows developers to extend the spell system with custom implementations. This guide covers everything you need to know to create and deploy plugins.

## Quick Start

### 1. Create Plugin Project

Create a new Class Library project targeting .NET 6.0 or later:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  
  <ItemGroup>
    <ProjectReference Include="..\..\GameAutomation.csproj" />
  </ItemGroup>
</Project>
```

### 2. Implement ISpellPlugin

```csharp
using GameAutomation.Models.Spells;
using GameAutomation.Models;
using GameAutomation.Plugins;

public class MyCustomPlugin : ISpellPlugin
{
    public string Name => "My Custom Spell Plugin";
    public string Version => "1.0.0";
    
    private readonly List<ISpell> _customSpells = new();
    
    public async Task<bool> InitializeAsync(IServiceProvider serviceProvider)
    {
        // Initialize your plugin
        _customSpells.Add(new CustomSpell("custom_heal", "Healing Wave", TimeSpan.FromSeconds(8)));
        _customSpells.Add(new CustomSpell("custom_shield", "Magic Shield", TimeSpan.FromSeconds(12)));
        
        return await Task.FromResult(true);
    }
    
    public async Task<IEnumerable<ISpell>> GetSpellsAsync()
    {
        return await Task.FromResult(_customSpells);
    }
    
    public async Task<bool> ExecuteSpellAsync(ISpell spell, IGameWindow window)
    {
        // Custom spell execution logic
        if (spell is CustomSpell customSpell)
        {
            return await customSpell.ExecuteAsync(window);
        }
        
        return false;
    }
    
    public void Dispose()
    {
        _customSpells.Clear();
    }
}
```

### 3. Deploy Plugin

1. Build your plugin project
2. Copy the output DLL to the `plugins` directory
3. Restart PW Autobattler
4. Your spells will automatically appear in the UI

## Plugin Interface Reference

### ISpellPlugin Interface

```csharp
public interface ISpellPlugin
{
    /// <summary>
    /// Human-readable plugin name
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Plugin version for compatibility checking
    /// </summary>
    string Version { get; }
    
    /// <summary>
    /// Initialize the plugin with dependency injection container
    /// </summary>
    /// <param name="serviceProvider">Service provider for dependency injection</param>
    /// <returns>True if initialization succeeded</returns>
    Task<bool> InitializeAsync(IServiceProvider serviceProvider);
    
    /// <summary>
    /// Get all spells provided by this plugin
    /// </summary>
    /// <returns>Collection of spells</returns>
    Task<IEnumerable<ISpell>> GetSpellsAsync();
    
    /// <summary>
    /// Execute a spell provided by this plugin
    /// </summary>
    /// <param name="spell">The spell to execute</param>
    /// <param name="window">Target game window</param>
    /// <returns>True if execution succeeded</returns>
    Task<bool> ExecuteSpellAsync(ISpell spell, IGameWindow window);
    
    /// <summary>
    /// Clean up plugin resources
    /// </summary>
    void Dispose();
}
```

## Spell Implementation

### Basic Spell Implementation

```csharp
public class CustomSpell : ISpell
{
    public string Id { get; }
    public string DisplayName { get; }
    public TimeSpan Cooldown { get; }
    public string KeyCombination { get; set; }
    public string Description { get; set; }
    
    public CustomSpell(string id, string displayName, TimeSpan cooldown)
    {
        Id = id;
        DisplayName = displayName;
        Cooldown = cooldown;
        KeyCombination = "";
        Description = "";
    }
    
    public async Task<bool> ExecuteAsync(IGameWindow window)
    {
        // Custom execution logic
        Console.WriteLine($"Executing {DisplayName} on window {window.Title}");
        
        // Simulate async operation
        await Task.Delay(100);
        
        return true;
    }
}
```

### Advanced Spell with Input Service

```csharp
public class AdvancedSpell : ISpell
{
    private readonly IInputService _inputService;
    private readonly IConfigurationService _configService;
    
    public string Id { get; }
    public string DisplayName { get; }
    public TimeSpan Cooldown { get; }
    public string KeyCombination { get; set; }
    public string Description { get; set; }
    
    public AdvancedSpell(string id, string displayName, TimeSpan cooldown, 
        IInputService inputService, IConfigurationService configService)
    {
        Id = id;
        DisplayName = displayName;
        Cooldown = cooldown;
        _inputService = inputService;
        _configService = configService;
        KeyCombination = "";
        Description = "";
    }
    
    public async Task<bool> ExecuteAsync(IGameWindow window)
    {
        try
        {
            // Get configuration
            var inputMethod = _configService.GetString("input.defaultMethod", "KeyboardEventOptimized");
            var delay = _configService.GetInt("input.keyDelayMs", 50);
            
            // Execute key sequence with input service
            await _inputService.SendKeySequenceAsync(window, KeyCombination, inputMethod);
            
            // Wait for spell animation
            await Task.Delay(delay);
            
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Spell execution failed: {ex.Message}");
            return false;
        }
    }
}
```

## Accessing Core Services

### Service Provider Usage

During plugin initialization, you receive an `IServiceProvider` that gives access to core services:

```csharp
public async Task<bool> InitializeAsync(IServiceProvider serviceProvider)
{
    // Get required services
    var inputService = serviceProvider.GetService<IInputService>();
    var configService = serviceProvider.GetService<IConfigurationService>();
    var cooldownService = serviceProvider.GetService<ICooldownService>();
    var windowService = serviceProvider.GetService<IWindowService>();
    
    if (inputService == null || configService == null)
    {
        Console.WriteLine("Required services not available");
        return false;
    }
    
    // Store services for later use
    _inputService = inputService;
    _configService = configService;
    
    // Initialize plugin-specific resources
    await LoadPluginConfiguration();
    
    return true;
}
```

### Available Core Services

- **IInputService**: Send keyboard/mouse input to game windows
- **IConfigurationService**: Access application configuration
- **ICooldownService**: Manage spell cooldowns
- **IWindowService**: Access registered game windows
- **ISpellService**: Access loaded spells (read-only)

## Configuration Integration

### Plugin-Specific Configuration

Add plugin configuration to `app.json`:

```json
{
  "plugins": {
    "enableAutoLoad": true,
    "pluginDirectory": "plugins",
    "myCustomPlugin": {
      "healingPower": 150,
      "shieldDuration": 30,
      "debugMode": true
    }
  }
}
```

### Reading Plugin Configuration

```csharp
public async Task<bool> InitializeAsync(IServiceProvider serviceProvider)
{
    var configService = serviceProvider.GetService<IConfigurationService>();
    
    // Read plugin-specific configuration
    var healingPower = configService?.GetInt("plugins.myCustomPlugin.healingPower", 100) ?? 100;
    var shieldDuration = configService?.GetInt("plugins.myCustomPlugin.shieldDuration", 20) ?? 20;
    var debugMode = configService?.GetBool("plugins.myCustomPlugin.debugMode", false) ?? false;
    
    // Configure plugin behavior
    _healingPower = healingPower;
    _shieldDuration = shieldDuration;
    _debugMode = debugMode;
    
    return true;
}
```

## Error Handling

### Plugin Error Handling

```csharp
public async Task<bool> ExecuteSpellAsync(ISpell spell, IGameWindow window)
{
    try
    {
        if (spell is not CustomSpell customSpell)
        {
            return false;
        }
        
        // Validate window
        if (window?.WindowHandle == IntPtr.Zero)
        {
            throw new ArgumentException("Invalid window handle");
        }
        
        // Execute spell
        return await customSpell.ExecuteAsync(window);
    }
    catch (Exception ex)
    {
        // Log error (use your preferred logging framework)
        LogError($"Spell execution failed: {ex.Message}", ex);
        return false;
    }
}

private void LogError(string message, Exception? ex = null)
{
    // Implement your logging strategy
    Console.WriteLine($"[{Name}] ERROR: {message}");
    if (ex != null)
    {
        Console.WriteLine($"[{Name}] EXCEPTION: {ex}");
    }
}
```

### Graceful Degradation

```csharp
public async Task<IEnumerable<ISpell>> GetSpellsAsync()
{
    var spells = new List<ISpell>();
    
    try
    {
        // Try to load primary spells
        spells.AddRange(await LoadPrimarySpells());
    }
    catch (Exception ex)
    {
        LogError("Failed to load primary spells", ex);
    }
    
    try
    {
        // Try to load secondary spells
        spells.AddRange(await LoadSecondarySpells());
    }
    catch (Exception ex)
    {
        LogError("Failed to load secondary spells", ex);
        // Continue with what we have
    }
    
    // Always return something, even if empty
    return spells;
}
```

## Advanced Features

### Custom UI Integration

Plugins can integrate with the UI system by providing custom controls:

```csharp
public class UIIntegratedPlugin : ISpellPlugin
{
    private Panel? _customPanel;
    
    public async Task<bool> InitializeAsync(IServiceProvider serviceProvider)
    {
        // Create custom UI panel
        _customPanel = new Panel
        {
            Name = "CustomPluginPanel",
            Text = "Custom Plugin Controls",
            Size = new Size(200, 100)
        };
        
        // Add custom controls
        var button = new Button
        {
            Text = "Plugin Action",
            Location = new Point(10, 10),
            Size = new Size(100, 30)
        };
        
        button.Click += async (sender, args) => await ExecuteCustomAction();
        _customPanel.Controls.Add(button);
        
        // TODO: Integrate with main form
        // This would require extension of the plugin interface
        
        return true;
    }
    
    private async Task ExecuteCustomAction()
    {
        // Custom plugin action
        await Task.Delay(100);
    }
}
```

### Spell Chaining

Create spells that execute in sequence:

```csharp
public class ChainSpell : ISpell
{
    private readonly List<ISpell> _spellChain;
    private readonly IInputService _inputService;
    
    public ChainSpell(IEnumerable<ISpell> spellChain, IInputService inputService)
    {
        _spellChain = spellChain.ToList();
        _inputService = inputService;
        
        // Chain properties
        Id = "chain_" + string.Join("_", _spellChain.Select(s => s.Id));
        DisplayName = "Chain: " + string.Join(" → ", _spellChain.Select(s => s.DisplayName));
        Cooldown = TimeSpan.FromMilliseconds(_spellChain.Sum(s => s.Cooldown.TotalMilliseconds));
    }
    
    public string Id { get; }
    public string DisplayName { get; }
    public TimeSpan Cooldown { get; }
    public string KeyCombination { get; set; } = "";
    public string Description { get; set; } = "";
    
    public async Task<bool> ExecuteAsync(IGameWindow window)
    {
        foreach (var spell in _spellChain)
        {
            // Execute each spell in sequence
            if (!string.IsNullOrEmpty(spell.KeyCombination))
            {
                await _inputService.SendKeySequenceAsync(window, spell.KeyCombination, "KeyboardEventOptimized");
            }
            
            // Wait for spell animation/cooldown
            await Task.Delay(100);
        }
        
        return true;
    }
}
```

## Testing Plugins

### Unit Testing

```csharp
[TestClass]
public class CustomPluginTests
{
    private MyCustomPlugin _plugin = null!;
    private Mock<IServiceProvider> _serviceProvider = null!;
    private Mock<IInputService> _inputService = null!;
    
    [TestInitialize]
    public void Setup()
    {
        _plugin = new MyCustomPlugin();
        _serviceProvider = new Mock<IServiceProvider>();
        _inputService = new Mock<IInputService>();
        
        _serviceProvider.Setup(sp => sp.GetService<IInputService>())
            .Returns(_inputService.Object);
    }
    
    [TestMethod]
    public async Task InitializeAsync_ShouldReturnTrue()
    {
        // Act
        var result = await _plugin.InitializeAsync(_serviceProvider.Object);
        
        // Assert
        Assert.IsTrue(result);
    }
    
    [TestMethod]
    public async Task GetSpellsAsync_ShouldReturnSpells()
    {
        // Arrange
        await _plugin.InitializeAsync(_serviceProvider.Object);
        
        // Act
        var spells = await _plugin.GetSpellsAsync();
        
        // Assert
        Assert.IsTrue(spells.Any());
        Assert.IsTrue(spells.Any(s => s.Id == "custom_heal"));
    }
}
```

### Integration Testing

```csharp
[TestMethod]
public async Task Plugin_IntegrationTest_ShouldWorkWithCoreServices()
{
    // Arrange
    var serviceProvider = CreateRealServiceProvider(); // Use real services
    var plugin = new MyCustomPlugin();
    var window = new TestGameWindow(new IntPtr(12345), "Test Window");
    
    // Act
    await plugin.InitializeAsync(serviceProvider);
    var spells = await plugin.GetSpellsAsync();
    var testSpell = spells.First();
    var result = await plugin.ExecuteSpellAsync(testSpell, window);
    
    // Assert
    Assert.IsTrue(result);
}
```

## Best Practices

### 1. Plugin Lifecycle Management

- Always implement proper disposal
- Handle initialization failures gracefully
- Provide meaningful error messages

### 2. Performance Considerations

- Cache expensive operations
- Use async/await properly
- Avoid blocking operations in the UI thread

### 3. Compatibility

- Target the same .NET version as the main application
- Use semantic versioning for your plugins
- Document breaking changes

### 4. Security

- Validate all input parameters
- Don't execute arbitrary code from configuration
- Handle sensitive data appropriately

### 5. User Experience

- Provide clear spell names and descriptions
- Use appropriate cooldown times
- Handle edge cases gracefully

## Troubleshooting

### Common Issues

1. **Plugin Not Loading**
   - Check DLL is in the correct directory
   - Verify .NET version compatibility
   - Check for missing dependencies

2. **Services Not Available**
   - Ensure plugin initialization happens after core services
   - Check service registration in the main application
   - Verify interface compatibility

3. **Spell Execution Fails**
   - Validate window handles
   - Check input service configuration
   - Verify key combinations are valid

### Debugging

Enable debug mode in plugin configuration:

```json
{
  "plugins": {
    "myCustomPlugin": {
      "debugMode": true
    }
  }
}
```

Use conditional compilation for debug output:

```csharp
#if DEBUG
private void DebugLog(string message)
{
    Console.WriteLine($"[DEBUG] {Name}: {message}");
}
#endif
```

## Examples

See the `src/Examples/` directory for complete plugin examples:

- **BasicSpellPlugin**: Simple spell implementation
- **AdvancedSpellPlugin**: Integration with core services
- **ChainSpellPlugin**: Complex spell chaining
- **UIExtensionPlugin**: Custom UI integration

## Support

For plugin development support:

1. Check the main documentation in `docs/ARCHITECTURE.md`
2. Review existing plugin examples
3. Submit issues to the project repository
4. Join the developer community discussions