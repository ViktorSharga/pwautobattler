# PW Autobattler - Architecture Documentation

## Overview

PW Autobattler has been refactored from a monolithic God Object architecture into a clean, maintainable, and extensible system. This document describes the current architecture after the 7-phase refactoring process.

## Architecture Principles

### 1. Separation of Concerns
- **Services**: Business logic and core functionality
- **Controllers**: UI management and user interaction
- **Infrastructure**: Cross-cutting concerns (memory, pooling, caching)
- **Models**: Data structures and interfaces
- **Configuration**: External configuration management

### 2. Dependency Injection
- Services are injected into controllers and other services
- Loose coupling between components
- Easy testing with mock implementations

### 3. Event-Driven Architecture
- Services communicate through events
- Loose coupling between system components
- Extensible through event subscriptions

### 4. Plugin Architecture
- Extensible spell system through plugins
- Runtime plugin loading and management
- Clean separation between core and extensions

## System Components

### Core Services

#### IWindowService / WindowService
**Location**: `src/Services/WindowService.cs`

Manages game window registration and tracking.

```csharp
public interface IWindowService
{
    void RegisterWindow(IntPtr handle, string title, Rectangle rect);
    bool UnregisterWindow(IntPtr handle);
    IGameWindow? GetWindow(IntPtr handle);
    IEnumerable<IGameWindow> GetAllWindows();
    void ClearAllWindows();
}
```

**Key Features**:
- Thread-safe window collection management
- Automatic window state tracking
- Integration with input services

#### ICooldownService / CooldownService
**Location**: `src/Services/CooldownService.cs`

Manages spell cooldowns with performance optimizations.

```csharp
public interface ICooldownService
{
    bool IsOnCooldown(IGameWindow window, ISpell spell);
    TimeSpan? GetRemainingCooldown(IGameWindow window, ISpell spell);
    Task StartCooldownAsync(IGameWindow window, ISpell spell);
    Task ResetCooldownAsync(IGameWindow window, ISpell spell);
    Task CleanupExpiredCooldownsAsync();
}
```

**Key Features**:
- LRU (Least Recently Used) eviction strategy
- Configurable maximum cooldown limits
- Automatic cleanup and memory management
- Thread-safe concurrent operations
- Event notifications for cooldown state changes

#### ISpellService / SpellService
**Location**: `src/Services/SpellService.cs`

Data-driven spell management system.

```csharp
public interface ISpellService
{
    Task LoadSpellsAsync(string? configPath = null);
    Task<IEnumerable<ISpell>> GetAllSpellsAsync();
    Task<ISpell?> GetSpellAsync(string spellId);
    Task ReloadSpellsAsync();
}
```

**Key Features**:
- JSON-based spell configuration
- Hot-reload capability
- Async spell loading
- Integration with cooldown system

#### IConfigurationService / ConfigurationService
**Location**: `src/Services/ConfigurationService.cs`

Centralized configuration management.

```csharp
public interface IConfigurationService
{
    Task LoadConfigurationAsync(string? configPath = null);
    string GetString(string key, string defaultValue = "");
    int GetInt(string key, int defaultValue = 0);
    bool GetBool(string key, bool defaultValue = false);
    void SetValue(string key, object value);
}
```

**Key Features**:
- Hierarchical configuration keys (e.g., "input.defaultMethod")
- Type-safe value retrieval
- Runtime configuration updates
- JSON-based configuration files

### UI Controllers

#### GameWindowController
**Location**: `src/UI/Controllers/GameWindowController.cs`

Manages game window UI controls and user interactions.

**Responsibilities**:
- Window registration interface
- Window list display and management
- Window state visualization
- Integration with WindowService

#### SpellButtonController
**Location**: `src/UI/Controllers/SpellButtonController.cs`

Dynamic spell button generation and management.

**Responsibilities**:
- Dynamic button creation from spell configuration
- Button layout and styling
- Click event handling
- Cooldown state visualization

#### InputMethodController
**Location**: `src/UI/Controllers/InputMethodController.cs`

Input method selection and configuration.

**Responsibilities**:
- Input method dropdown management
- Input configuration UI
- Method switching logic
- Integration with InputService

### Infrastructure Components

#### ObjectPool<T>
**Location**: `src/Infrastructure/ObjectPool.cs`

Thread-safe generic object pooling for performance optimization.

```csharp
public class ObjectPool<T> : IDisposable where T : class
{
    public T Get();
    public void Return(T item);
    public int Count { get; }
    public int MaxSize { get; }
}
```

**Key Features**:
- Configurable pool size limits
- Automatic object reset on return
- Thread-safe operations
- Disposable wrapper for automatic return

#### MemoryManager
**Location**: `src/Infrastructure/MemoryManager.cs`

Automatic memory management and optimization.

```csharp
public class MemoryManager : IDisposable
{
    public void ScheduleCleanup<T>(T target) where T : class;
    public void ForceCleanup();
    public MemoryStats GetMemoryStats();
    public Task OptimizeMemoryAsync();
}
```

**Key Features**:
- Weak reference tracking
- Automatic garbage collection triggers
- Memory usage monitoring
- Configurable cleanup intervals

#### StringCache
**Location**: `src/Infrastructure/StringCache.cs`

String interning and caching for memory optimization.

```csharp
public class StringCache : IDisposable
{
    public string Intern(string value);
    public string Format(string format, params object[] args);
    public string CreateCooldownKey(IntPtr windowHandle, string spellId);
}
```

**Key Features**:
- String interning to reduce memory usage
- Formatted string caching
- Specialized key generation methods
- Configurable cache size limits

### Plugin System

#### ISpellPlugin
**Location**: `src/Plugins/ISpellPlugin.cs`

Interface for spell system extensions.

```csharp
public interface ISpellPlugin
{
    string Name { get; }
    string Version { get; }
    Task<bool> InitializeAsync(IServiceProvider serviceProvider);
    Task<IEnumerable<ISpell>> GetSpellsAsync();
    Task<bool> ExecuteSpellAsync(ISpell spell, IGameWindow window);
    void Dispose();
}
```

#### PluginManager
**Location**: `src/Plugins/PluginManager.cs`

Runtime plugin loading and lifecycle management.

**Key Features**:
- Dynamic assembly loading
- Plugin dependency injection
- Lifecycle management (Initialize/Dispose)
- Error handling and isolation

## Data Flow

### Spell Execution Flow

1. **User Input**: User clicks spell button in UI
2. **Controller**: SpellButtonController receives click event
3. **Cooldown Check**: Controller queries CooldownService
4. **Spell Execution**: If available, spell is executed via InputService
5. **Cooldown Start**: CooldownService starts tracking cooldown
6. **UI Update**: Controller updates button state
7. **Event Notification**: Services emit events for state changes

### Configuration Flow

1. **Startup**: ConfigurationService loads app.json
2. **Service Initialization**: Services receive configuration via dependency injection
3. **Runtime Updates**: Configuration changes propagate to services
4. **Persistence**: Changes are saved back to configuration files

### Memory Management Flow

1. **Object Creation**: Services create objects normally
2. **Pool Registration**: Objects are returned to pools when possible
3. **Weak Tracking**: MemoryManager tracks objects with weak references
4. **Cleanup Triggers**: Automatic cleanup based on time or memory pressure
5. **Optimization**: Memory optimization during low-activity periods

## Performance Characteristics

### Scalability Metrics

- **Window Management**: O(1) lookup, O(n) iteration
- **Cooldown Tracking**: O(1) average case with LRU eviction
- **Spell Loading**: O(n) initial load, O(1) subsequent access
- **Object Pooling**: O(1) get/return operations
- **String Caching**: O(1) average case with periodic cleanup

### Memory Optimization

- **Object Pooling**: Reduces GC pressure for frequently created objects
- **String Interning**: Eliminates duplicate string allocations
- **Weak References**: Allows automatic cleanup without explicit management
- **LRU Eviction**: Prevents unbounded memory growth in long-running sessions

## Configuration Schema

### app.json Structure

```json
{
  "input": {
    "defaultMethod": "KeyboardEventOptimized",
    "retryAttempts": 3,
    "keyDelayMs": 50
  },
  "cooldowns": {
    "cleanupIntervalMinutes": 30,
    "maxCooldowns": 1000
  },
  "spells": {
    "configPath": "src/Data/Spells/spells.json"
  },
  "memory": {
    "cleanupIntervalMinutes": 5,
    "highMemoryThresholdMB": 500
  },
  "plugins": {
    "enableAutoLoad": true,
    "pluginDirectory": "plugins"
  }
}
```

### spells.json Structure

```json
[
  {
    "id": "bramble_vortex",
    "displayName": "Bramble Vortex",
    "keyCombination": "F1",
    "cooldown": "00:00:06",
    "description": "Создает вихрь из колючих лоз"
  }
]
```

## Testing Strategy

### Unit Tests
**Location**: `src/Tests/UnitTests/`

- Individual service testing with mocks
- Controller logic validation
- Infrastructure component testing
- Error handling and edge cases

### Integration Tests
**Location**: `src/Tests/IntegrationTests/`

- Service interaction testing
- Configuration integration
- End-to-end workflow validation
- Plugin system integration

### Performance Tests
**Location**: `src/Tests/PerformanceTests/`

- Scalability benchmarks
- Memory usage profiling
- Concurrent access testing
- Performance regression detection

## Future Extensibility

### Planned Extensions

1. **Network Synchronization**: Multi-client spell coordination
2. **AI Integration**: Intelligent spell timing and selection
3. **Advanced Plugins**: Custom UI extensions and game integrations
4. **Configuration UI**: Runtime configuration management interface
5. **Telemetry**: Performance monitoring and usage analytics

### Extension Points

- **ISpellPlugin**: Custom spell implementations
- **Event System**: Custom event handlers and processors
- **Configuration**: Custom configuration providers
- **Input Methods**: Custom input simulation strategies
- **UI Controllers**: Custom UI component management

## Dependencies

### External Dependencies
- **System.Text.Json**: Configuration and spell data serialization
- **Microsoft.VisualStudio.TestTools.UnitTesting**: Unit testing framework
- **System.Collections.Concurrent**: Thread-safe collections

### Internal Dependencies
- Services depend on interfaces, not concrete implementations
- Controllers depend on services through dependency injection
- Infrastructure components are shared across all layers
- Models define contracts between components

This architecture provides a solid foundation for continued development while maintaining high performance, testability, and extensibility.