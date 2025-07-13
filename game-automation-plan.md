# Game Multi-Window Automation Implementation Plan

## Project Overview
Build a Windows application that can send low-level input commands to multiple instances of "Asgard Perfect World" (ElementClient.exe) running in windowed mode, even when the windows are in the background.

## Technical Requirements

### Core Requirements
- **Language**: C# with Windows Forms or WPF (recommended) or C++ with Win32 API
- **Input Method**: Low-level input simulation (not Windows messaging)
- **Target Process**: ElementClient.exe
- **Window Management**: Handle multiple instances with identical window names
- **Registration**: Use CTRL-SHIFT-1/2/3 hotkeys to register up to 3 windows
- **Background Operation**: Send inputs to windows without bringing them to foreground

### Key Features
1. Window registration system (up to 3 windows)
2. Process ID and window handle storage
3. Low-level keyboard input simulation
4. Multi-window command broadcasting
5. Test methods for different input types

## Implementation Strategy

### Phase 1: Project Setup and Core Infrastructure

#### 1.1 Create Project Structure
```
GameAutomation/
├── src/
│   ├── Core/
│   │   ├── WindowManager.cs
│   │   ├── InputSimulator.cs
│   │   └── HotkeyManager.cs
│   ├── Models/
│   │   └── GameWindow.cs
│   ├── UI/
│   │   └── MainForm.cs
│   └── Program.cs
├── libs/
│   └── (any required native DLLs)
└── GameAutomation.csproj
```

#### 1.2 Technology Stack
- **Framework**: .NET 6+ with Windows Forms/WPF
- **Libraries**: 
  - InputSimulator.NET or custom P/Invoke implementation
  - Global hotkey library or custom implementation
- **APIs**: Win32 API via P/Invoke for window management

### Phase 2: Window Management System

#### 2.1 Window Detection and Enumeration
```csharp
// Pseudocode structure
class WindowManager {
    - EnumerateGameWindows() // Find all ElementClient.exe windows
    - GetWindowByProcessId(int pid)
    - ValidateWindow(IntPtr hwnd)
    - GetWindowInfo(IntPtr hwnd) // Title, process ID, etc.
}
```

#### 2.2 Window Registration Model
```csharp
class GameWindow {
    - ProcessId: int
    - WindowHandle: IntPtr
    - WindowTitle: string
    - RegistrationSlot: int (1-3)
    - IsActive: bool
}
```

### Phase 3: Input Simulation System

#### 3.1 Low-Level Input Methods
Choose one of these approaches:
1. **SendInput API** (Recommended)
   - Most reliable for games
   - Works with DirectInput games
   - Requires window focus manipulation

2. **PostMessage with WM_KEYDOWN/UP**
   - May not work with all games
   - Easier to implement
   - Test first with target game

3. **Direct Input Injection**
   - Using Windows hooks
   - Most complex but most reliable

#### 3.2 Input Simulator Implementation
```csharp
class InputSimulator {
    - SendKeyPress(IntPtr hwnd, VirtualKeyCode key)
    - SendKeyDown(IntPtr hwnd, VirtualKeyCode key)
    - SendKeyUp(IntPtr hwnd, VirtualKeyCode key)
    - BroadcastToAll(Action<IntPtr> inputAction)
}
```

### Phase 4: Hotkey Registration System

#### 4.1 Global Hotkey Implementation
```csharp
class HotkeyManager {
    - RegisterHotkey(ModifierKeys modifiers, Keys key, Action callback)
    - UnregisterHotkey(int id)
    - HandleHotkeyPress(int hotkeyId)
}
```

#### 4.2 Window Registration Hotkeys
- CTRL+SHIFT+1: Register active window to slot 1
- CTRL+SHIFT+2: Register active window to slot 2
- CTRL+SHIFT+3: Register active window to slot 3

### Phase 5: Test Methods Implementation

#### 5.1 Test Method 1: Send 'Q' Key
```csharp
void TestSendQ() {
    foreach (var window in RegisteredWindows) {
        InputSimulator.SendKeyPress(window.Handle, VK_Q);
        Thread.Sleep(50); // Small delay between windows
    }
}
```

#### 5.2 Test Method 2: Send '1' Key
```csharp
void TestSend1() {
    foreach (var window in RegisteredWindows) {
        InputSimulator.SendKeyPress(window.Handle, VK_1);
        Thread.Sleep(50);
    }
}
```

#### 5.3 Test Method 3: Movement Simulation (Hold 'W')
```csharp
void TestMovementStart() {
    foreach (var window in RegisteredWindows) {
        InputSimulator.SendKeyDown(window.Handle, VK_W);
    }
}

void TestMovementStop() {
    foreach (var window in RegisteredWindows) {
        InputSimulator.SendKeyUp(window.Handle, VK_W);
    }
}
```

### Phase 6: User Interface Design

#### 6.1 Main Window Layout
```
┌─────────────────────────────────────┐
│  Game Multi-Window Controller       │
├─────────────────────────────────────┤
│ Registered Windows:                 │
│ [1] PID: 12345 - ElementClient.exe │
│ [2] PID: 23456 - ElementClient.exe │
│ [3] [Empty Slot]                   │
├─────────────────────────────────────┤
│ Test Controls:                      │
│ [Send Q to All] [Send 1 to All]   │
│ [Start Movement] [Stop Movement]    │
├─────────────────────────────────────┤
│ Status: Ready                       │
└─────────────────────────────────────┘
```

#### 6.2 UI Components
- Window list with process information
- Test buttons for each method
- Status bar for feedback
- Clear/unregister options

### Phase 7: Advanced Features

#### 7.1 Window Focus Management
```csharp
class FocusManager {
    - SaveCurrentFocus()
    - RestoreFocus()
    - TemporarilyFocus(IntPtr hwnd, Action action)
}
```

#### 7.2 Input Queue System
```csharp
class InputQueue {
    - QueueInput(GameWindow window, InputAction action)
    - ProcessQueue()
    - ClearQueue()
}
```

### Phase 8: Error Handling and Validation

#### 8.1 Common Issues to Handle
- Window closed after registration
- Process terminated
- Permission issues
- Anti-cheat detection

#### 8.2 Validation Methods
- Verify window exists before sending input
- Check process is still running
- Validate window belongs to correct game

### Phase 9: Testing Strategy

#### 9.1 Unit Tests
- Window detection accuracy
- Input simulation reliability
- Hotkey registration/unregistration

#### 9.2 Integration Tests
- Multi-window command broadcasting
- Timing and synchronization
- Resource cleanup

### Phase 10: Deployment and Documentation

#### 10.1 Build Configuration
- x64 build for modern systems
- Include all dependencies
- Code signing (if needed)

#### 10.2 User Documentation
- Installation guide
- Usage instructions
- Troubleshooting section
- Known limitations

## Implementation Notes

### Critical Considerations

1. **Anti-Cheat Systems**: Some games have anti-cheat that may detect automated inputs. This tool is for testing only.

2. **Input Method Selection**: Start with SendInput API. If it doesn't work, try:
   - SetForegroundWindow + SendInput
   - PostMessage/SendMessage
   - Low-level keyboard hooks

3. **Timing**: Games may have input rate limits. Add configurable delays between inputs.

4. **Window Identification**: Since windows have identical names, use:
   - Process ID
   - Window creation time
   - Window position/size

### Code Snippets for Claude Code

#### Window Enumeration (C#)
```csharp
[DllImport("user32.dll")]
static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

[DllImport("user32.dll")]
static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

// Find all ElementClient.exe windows
List<IntPtr> FindGameWindows() {
    var windows = new List<IntPtr>();
    EnumWindows((hwnd, lParam) => {
        GetWindowThreadProcessId(hwnd, out uint pid);
        var process = Process.GetProcessById((int)pid);
        if (process.ProcessName == "ElementClient") {
            windows.Add(hwnd);
        }
        return true;
    }, IntPtr.Zero);
    return windows;
}
```

#### SendInput Example (C#)
```csharp
[DllImport("user32.dll")]
static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

void SendKeyPress(VirtualKeyCode key) {
    var inputs = new INPUT[] {
        CreateKeyInput(key, false), // Key down
        CreateKeyInput(key, true)    // Key up
    };
    SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
}
```

## Next Steps for Claude Code

1. **Initial Setup**: Create the project structure and basic window detection
2. **Window Management**: Implement window enumeration and registration
3. **Input Testing**: Start with simple SendInput implementation
4. **Hotkey System**: Add global hotkey registration
5. **UI Development**: Create the user interface
6. **Testing**: Test with actual game instances
7. **Refinement**: Adjust based on game behavior

## Success Criteria

- ✅ Can detect and register multiple ElementClient.exe windows
- ✅ Global hotkeys work reliably for window registration
- ✅ Input commands reach background windows
- ✅ All three test methods work correctly
- ✅ Stable operation without crashes
- ✅ Clear UI feedback for all operations