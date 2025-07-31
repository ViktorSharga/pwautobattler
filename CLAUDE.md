# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

PW Autobattler is a Windows desktop application for automating keyboard and mouse input across multiple instances of "Asgard Perfect World" (ElementClient.exe). It uses low-level Windows APIs for input simulation and window management.

## Build Commands

```bash
# Build the application
dotnet build GameAutomation.csproj --configuration Release

# Publish self-contained executable
dotnet publish GameAutomation.csproj --configuration Release --runtime win-x64 --self-contained true --output ./publish

# Restore dependencies
dotnet restore GameAutomation.csproj
```

## Architecture Overview

### Core Components

1. **Window Management System** (`src/Core/WindowManager.cs`)
   - Enumerates ElementClient.exe windows using Win32 APIs
   - Manages window registration and validation
   - Tracks up to 10 game windows with hotkey registration (Ctrl+Shift+1-9/0)

2. **Input Simulation** (`src/Core/InputSimulator.cs`, `BackgroundMouseSimulator.cs`)
   - Multiple input methods: KeyboardEventOptimized (default), PostMessage, SendMessage, SendInput
   - Mouse mirroring and follow leader functionality
   - Handles both keyboard and mouse input broadcasting

3. **Hotkey System** (`src/Core/HotkeyManager.cs`, `LowLevelKeyboardHook.cs`)
   - Global hotkey registration for window management
   - Low-level keyboard hooks for input interception
   - Handles modifier keys and special key combinations

4. **Service Layer** (`src/Services/`)
   - IWindowService: Window registration and management
   - IInputService: Input broadcasting and simulation
   - ISpellService: Game-specific spell casting logic
   - ICooldownService: Cooldown management for spells
   - IConfigurationService: Settings and configuration

### Key Design Patterns

- **Interface-based architecture**: All major components use interfaces for abstraction
- **Spell System**: Modular spell system with configuration, requirements, and execution phases
- **Game Class Support**: Different configurations per game class (GameClass enum)
- **Async Operations**: Spell casting and other operations use async/await patterns

### Important Implementation Details

1. **Window Registration**: Windows are identified by process ID and handle, stored in GameWindow objects
2. **Input Methods**: Default method (KeyboardEventOptimized) requires brief window focus switching
3. **Background Input**: PostMessage/SendMessage methods have limited reliability with games
4. **Mouse Hook**: Uses low-level mouse hooks for cursor position tracking

## Development Notes

- Target Framework: .NET 9.0 Windows
- Platform: x64 only
- Windows Forms application
- No unit tests currently in the project
- GitHub Actions workflow configured for automated builds and releases