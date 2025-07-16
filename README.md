# Game Multi-Window Controller

A powerful automation tool for controlling multiple game windows simultaneously. Features include keyboard broadcasting, mouse mirroring, and follow leader functionality.

## Features

### 🎮 Multi-Window Management
- **Window Registration**: Register up to 10 game windows using `Ctrl+Shift+1-9/0`
- **Active Window Detection**: Automatically detects "ElementClient" game windows
- **Window Validation**: Real-time validation of window states

### ⌨️ Keyboard Broadcasting
- **Broadcast Mode**: Press `1-9` keys to broadcast to all registered windows
- **Multiple Input Methods**: Support for various input simulation techniques
- **Key Broadcasting**: Send Q, movement commands, and more to all windows

### 🖱️ Mouse Mirroring
- **Ctrl+Click Mirroring**: Mirror mouse clicks to all registered windows
- **Background Clicking**: Send clicks without activating target windows
- **Multiple Methods**: Fallback support for different click simulation methods

### 👥 Follow Leader System
- **Leader Window**: First registered window acts as the leader
- **Coordinate Calibration**: Set two target coordinates using `Ctrl+Click`
- **Automatic Sequence**: Executes `Shift+1` → Right-click coord1 → Left-click coord2
- **Hotkey Activation**: Press `Ctrl+F` to execute follow leader sequence

## Download & Installation

### Latest Release
Download the latest pre-built executable from [GitHub Releases](../../releases):
- **GameAutomation-SelfContained-win-x64.zip** (Recommended - ~50MB, no dependencies)
- **GameAutomation-FrameworkDependent-win-x64.zip** (Smaller, requires .NET 9.0)

### Requirements
- **Windows x64** operating system
- **Self-contained build**: No additional requirements
- **Framework-dependent build**: .NET 9.0 Desktop Runtime

## Quick Start

1. **Download** and extract the latest release
2. **Run** `GameAutomation.exe` as Administrator (recommended for input simulation)
3. **Register windows**:
   - Focus on your first game window
   - Press `Ctrl+Shift+1` to register as leader
   - Focus on second game window  
   - Press `Ctrl+Shift+2` to register as follower
   - Repeat for additional windows
4. **Test functionality**:
   - Enable "Broadcast Mode" and press `1-9` keys
   - Enable "Mouse Mirroring" and use `Ctrl+Click`
   - Calibrate coordinates and use `Ctrl+F` for follow leader

## Usage Guide

### Window Registration
- **Hotkeys**: `Ctrl+Shift+1` through `Ctrl+Shift+0` (for slots 1-10)
- **Process**: Focus the game window, then press the hotkey
- **Verification**: Check the window list in the application

### Broadcast Mode
1. Enable "Broadcast Mode (Listen 1-9)" checkbox
2. Press number keys `1-9` to broadcast to all registered windows
3. The key will be sent to all active registered windows
4. Window 1 will be brought to focus after broadcasting

### Mouse Mirroring
1. Enable "Mouse Mirroring (Ctrl+Click)" checkbox  
2. Use `Ctrl+Left Click` or `Ctrl+Right Click` anywhere
3. The click will be mirrored to all registered windows
4. Clicks are sent without activating target windows

### Follow Leader System
1. **Calibrate coordinates**:
   - Click "Calibrate Coord 1" button
   - `Ctrl+Click` on the first target location
   - Click "Calibrate Coord 2" button  
   - `Ctrl+Click` on the second target location
2. **Execute sequence**:
   - Press `Ctrl+F` hotkey, OR
   - Click "Follow Leader" button
3. **Sequence performed**: For each follower window:
   - Sends `Shift+1` keypress
   - Right-clicks on coordinate 1
   - Left-clicks on coordinate 2

## Input Methods

The application supports multiple input simulation methods:
- **KeyboardEventOptimized** (Recommended)
- **PostMessage**
- **SendMessage** 
- **SendInput**
- **KeyboardEvent**
- **ScanCode**

Use "Test All Methods" to find the best method for your game.

## Building from Source

See [BUILD.md](BUILD.md) for detailed build instructions.

### Development Build
```bash
git clone https://github.com/ViktorSharga/pwautobattler.git
cd pwautobattler
dotnet build autobattler.sln
```

## Troubleshooting

### Common Issues
- **Input not working**: Try different input methods using the dropdown
- **Windows not detected**: Ensure game process is named "ElementClient"
- **Access denied**: Run as Administrator for better input simulation
- **Clicks not registering**: Increase delays or try different mouse input methods

### Debug Information
- Status messages show detailed execution logs
- Each step of follow leader is logged with success/failure
- Window validation occurs in real-time

## Technical Details

- **Framework**: .NET 9.0 Windows Forms
- **Platform**: Windows x64
- **Input Simulation**: Win32 API (SendInput, PostMessage, SendMessage)
- **Window Management**: Windows API (EnumWindows, FindWindow)
- **Global Hooks**: Low-level keyboard and mouse hooks

## License

This project is for educational and personal use. Use responsibly and in accordance with game terms of service.