# Configuration Reference

## Overview

PW Autobattler uses a JSON-based configuration system that allows runtime customization of behavior without recompiling the application. Configuration is hierarchical and supports type-safe access.

## Configuration Files

### app.json - Main Configuration

**Location**: `src/Configuration/app.json`

The main configuration file contains all application settings organized into logical sections.

```json
{
  "input": {
    "defaultMethod": "KeyboardEventOptimized",
    "retryAttempts": 3,
    "keyDelayMs": 50,
    "mouseDelayMs": 100,
    "broadcastMode": false
  },
  "cooldowns": {
    "cleanupIntervalMinutes": 30,
    "maxCooldowns": 1000,
    "enableEvents": true
  },
  "spells": {
    "configPath": "src/Data/Spells/spells.json",
    "enableHotReload": true,
    "autoLoadPluginSpells": true
  },
  "memory": {
    "cleanupIntervalMinutes": 5,
    "highMemoryThresholdMB": 500,
    "enableOptimization": true,
    "objectPooling": {
      "enabled": true,
      "maxPoolSize": 100
    },
    "stringCaching": {
      "enabled": true,
      "maxCacheSize": 1000
    }
  },
  "plugins": {
    "enableAutoLoad": true,
    "pluginDirectory": "plugins",
    "loadTimeout": 30000
  },
  "ui": {
    "theme": "default",
    "spellButtonSize": {
      "width": 100,
      "height": 30
    },
    "refreshInterval": 1000,
    "showCooldownTimers": true
  },
  "logging": {
    "level": "Information",
    "enableFileLogging": true,
    "logPath": "logs/app.log",
    "maxLogFileSizeMB": 10
  }
}
```

### spells.json - Spell Configuration

**Location**: `src/Data/Spells/spells.json`

Contains all spell definitions with their properties and metadata.

```json
[
  {
    "id": "bramble_vortex",
    "displayName": "Bramble Vortex",
    "keyCombination": "F1",
    "cooldown": "00:00:06",
    "description": "Создает вихрь из колючих лоз",
    "category": "nature",
    "manaCost": 25
  },
  {
    "id": "healing_spring",
    "displayName": "Healing Spring",
    "keyCombination": "F2", 
    "cooldown": "00:00:15",
    "description": "Восстанавливает здоровье",
    "category": "healing",
    "manaCost": 40
  }
]
```

## Configuration Sections

### Input Section

Controls how input is sent to game windows.

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `defaultMethod` | string | `"KeyboardEventOptimized"` | Input simulation method |
| `retryAttempts` | int | `3` | Number of retry attempts for failed input |
| `keyDelayMs` | int | `50` | Delay between key presses in milliseconds |
| `mouseDelayMs` | int | `100` | Delay between mouse actions in milliseconds |
| `broadcastMode` | bool | `false` | Enable broadcasting to multiple windows |

**Available Input Methods**:
- `"KeyboardEventOptimized"` - Optimized keyboard events (recommended)
- `"MouseEventOptimized"` - Optimized mouse events
- `"BackgroundInput"` - Background input simulation
- `"LowLevel"` - Low-level input hooks

### Cooldowns Section

Manages spell cooldown tracking and cleanup.

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `cleanupIntervalMinutes` | int | `30` | How often to cleanup expired cooldowns |
| `maxCooldowns` | int | `1000` | Maximum number of tracked cooldowns |
| `enableEvents` | bool | `true` | Enable cooldown event notifications |

### Spells Section

Controls spell loading and management.

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `configPath` | string | `"src/Data/Spells/spells.json"` | Path to spell configuration file |
| `enableHotReload` | bool | `true` | Allow runtime spell configuration reloading |
| `autoLoadPluginSpells` | bool | `true` | Automatically load spells from plugins |

### Memory Section

Performance and memory optimization settings.

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `cleanupIntervalMinutes` | int | `5` | Memory cleanup interval |
| `highMemoryThresholdMB` | int | `500` | High memory usage threshold |
| `enableOptimization` | bool | `true` | Enable automatic memory optimization |

#### Object Pooling Subsection

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `objectPooling.enabled` | bool | `true` | Enable object pooling |
| `objectPooling.maxPoolSize` | int | `100` | Maximum objects per pool |

#### String Caching Subsection

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `stringCaching.enabled` | bool | `true` | Enable string caching |
| `stringCaching.maxCacheSize` | int | `1000` | Maximum cached strings |

### Plugins Section

Plugin system configuration.

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `enableAutoLoad` | bool | `true` | Automatically load plugins at startup |
| `pluginDirectory` | string | `"plugins"` | Directory to scan for plugin DLLs |
| `loadTimeout` | int | `30000` | Plugin loading timeout in milliseconds |

### UI Section

User interface customization options.

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `theme` | string | `"default"` | UI theme name |
| `spellButtonSize.width` | int | `100` | Spell button width in pixels |
| `spellButtonSize.height` | int | `30` | Spell button height in pixels |
| `refreshInterval` | int | `1000` | UI refresh interval in milliseconds |
| `showCooldownTimers` | bool | `true` | Show cooldown countdown timers |

### Logging Section

Application logging configuration.

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `level` | string | `"Information"` | Minimum log level |
| `enableFileLogging` | bool | `true` | Write logs to file |
| `logPath` | string | `"logs/app.log"` | Log file path |
| `maxLogFileSizeMB` | int | `10` | Maximum log file size |

**Available Log Levels**:
- `"Trace"` - Most verbose
- `"Debug"` - Debug information
- `"Information"` - General information
- `"Warning"` - Warning messages
- `"Error"` - Error messages
- `"Critical"` - Critical errors only

## Spell Configuration Schema

### Spell Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `id` | string | Yes | Unique spell identifier |
| `displayName` | string | Yes | Human-readable spell name |
| `keyCombination` | string | Yes | Key sequence to execute spell |
| `cooldown` | string | Yes | Cooldown duration (HH:MM:SS format) |
| `description` | string | No | Spell description |
| `category` | string | No | Spell category for organization |
| `manaCost` | int | No | Mana cost (for future use) |
| `level` | int | No | Required level (for future use) |
| `icon` | string | No | Icon file path |

### Key Combination Format

Key combinations use standard Windows key names:

**Single Keys**: `"F1"`, `"A"`, `"1"`, `"Space"`, `"Enter"`

**Modifiers**: `"Ctrl"`, `"Alt"`, `"Shift"`, `"Win"`

**Combinations**: `"Ctrl+C"`, `"Alt+F4"`, `"Ctrl+Shift+A"`

**Sequences**: `"F1,F2"` (press F1, then F2)

**Examples**:
```json
{
  "keyCombination": "F1"           // Simple key
},
{
  "keyCombination": "Ctrl+Q"       // Key with modifier
},
{
  "keyCombination": "1,2,3"        // Key sequence
},
{
  "keyCombination": "Alt+Tab,Enter" // Complex sequence
}
```

### Cooldown Format

Cooldowns use TimeSpan format: `"HH:MM:SS"` or `"MM:SS"`

**Examples**:
- `"00:00:05"` - 5 seconds
- `"00:01:30"` - 1 minute 30 seconds
- `"01:05:00"` - 1 hour 5 minutes
- `"30"` - 30 seconds (shorthand)

## Accessing Configuration

### In C# Code

```csharp
// Inject configuration service
public MyService(IConfigurationService config)
{
    _config = config;
}

// Read configuration values
var inputMethod = _config.GetString("input.defaultMethod", "KeyboardEventOptimized");
var retryAttempts = _config.GetInt("input.retryAttempts", 3);
var enableOptimization = _config.GetBool("memory.enableOptimization", true);

// Update configuration at runtime
_config.SetValue("input.keyDelayMs", 75);
```

### Configuration Path Syntax

Use dot notation to access nested values:

- `"input.defaultMethod"` → `config.input.defaultMethod`
- `"memory.objectPooling.enabled"` → `config.memory.objectPooling.enabled`
- `"plugins.myPlugin.setting"` → `config.plugins.myPlugin.setting`

## Runtime Configuration

### Hot Reload

Configuration changes can be applied without restarting:

```csharp
// Reload configuration from file
await _configService.LoadConfigurationAsync();

// Reload spells specifically
await _spellService.ReloadSpellsAsync();
```

### Programmatic Updates

```csharp
// Update single values
_config.SetValue("input.keyDelayMs", 100);
_config.SetValue("cooldowns.maxCooldowns", 2000);

// Update complex objects
_config.SetValue("ui.spellButtonSize", new { width = 120, height = 35 });
```

## Environment Variables

Override configuration values using environment variables:

```bash
# Override input method
PWAUTOBATTLER_INPUT_DEFAULTMETHOD=LowLevel

# Override cooldown settings
PWAUTOBATTLER_COOLDOWNS_MAXCOOLDOWNS=500

# Override memory threshold
PWAUTOBATTLER_MEMORY_HIGHMEMORYDEFAULTMB=1000
```

**Naming Convention**: `PWAUTOBATTLER_SECTION_SUBSECTION_KEY`

- Convert to uppercase
- Replace dots with underscores
- Use `PWAUTOBATTLER_` prefix

## Configuration Validation

### Validation Rules

The configuration system validates values on load:

- **Required fields**: Must be present and non-empty
- **Type validation**: Values must match expected types
- **Range validation**: Numeric values must be within valid ranges
- **Enum validation**: String values must match allowed options

### Common Validation Errors

1. **Invalid time format**: `"cooldown": "invalid"` → Use `"HH:MM:SS"`
2. **Negative values**: `"retryAttempts": -1` → Use positive integers
3. **Invalid paths**: `"configPath": "nonexistent.json"` → Use valid file paths
4. **Unknown input methods**: `"defaultMethod": "unknown"` → Use supported methods

## Performance Considerations

### Configuration Loading

- Configuration is loaded once at startup
- Hot reload triggers re-validation
- Plugin configuration is cached after first access
- File watching is used for automatic reload

### Memory Usage

- Configuration is stored in memory for fast access
- String values are interned to reduce memory usage
- Complex objects are serialized on-demand

### Best Practices

1. **Use appropriate data types**: Don't store numbers as strings
2. **Group related settings**: Use nested objects for organization
3. **Provide sensible defaults**: Always specify fallback values
4. **Document custom settings**: Add comments to complex configurations
5. **Validate early**: Check configuration at startup

## Backup and Migration

### Automatic Backups

Configuration files are automatically backed up:

- Backup created before each update
- Stored in `backups/` directory
- Timestamped filename format: `app.json.backup.20240320-143022`

### Migration

When configuration schema changes:

1. Old configuration is preserved
2. New fields get default values
3. Migration warnings are logged
4. Manual migration may be required for breaking changes

### Example Migration

```csharp
// Migrate old configuration format
if (_config.GetString("input.method") != "")
{
    // Migrate old "method" to new "defaultMethod"
    var oldMethod = _config.GetString("input.method");
    _config.SetValue("input.defaultMethod", oldMethod);
    _config.SetValue("input.method", ""); // Clear old value
}
```

## Troubleshooting

### Common Issues

1. **Configuration not loading**
   - Check file path and permissions
   - Validate JSON syntax
   - Review validation errors in logs

2. **Values not updating**
   - Ensure hot reload is enabled
   - Check for cached values
   - Verify configuration path

3. **Invalid spell configuration**
   - Validate key combination format
   - Check cooldown time format
   - Ensure unique spell IDs

### Debug Configuration

Enable configuration debugging:

```json
{
  "logging": {
    "level": "Debug"
  },
  "debug": {
    "logConfigurationChanges": true,
    "validateOnAccess": true
  }
}
```

### Configuration Validation Tool

Use the validation tool to check configuration:

```bash
# Validate main configuration
GameAutomation.exe --validate-config

# Validate spell configuration
GameAutomation.exe --validate-spells

# Export effective configuration
GameAutomation.exe --export-config output.json
```

## Examples

### Minimal Configuration

```json
{
  "input": {
    "defaultMethod": "KeyboardEventOptimized"
  },
  "spells": {
    "configPath": "spells.json"
  }
}
```

### Development Configuration

```json
{
  "input": {
    "defaultMethod": "KeyboardEventOptimized",
    "keyDelayMs": 10
  },
  "cooldowns": {
    "cleanupIntervalMinutes": 1,
    "maxCooldowns": 100
  },
  "logging": {
    "level": "Debug",
    "enableFileLogging": true
  },
  "memory": {
    "enableOptimization": false
  }
}
```

### Production Configuration

```json
{
  "input": {
    "defaultMethod": "KeyboardEventOptimized",
    "retryAttempts": 5,
    "keyDelayMs": 50
  },
  "cooldowns": {
    "cleanupIntervalMinutes": 60,
    "maxCooldowns": 5000
  },
  "memory": {
    "cleanupIntervalMinutes": 15,
    "highMemoryThresholdMB": 1000,
    "enableOptimization": true
  },
  "logging": {
    "level": "Information",
    "enableFileLogging": true,
    "maxLogFileSizeMB": 50
  }
}
```