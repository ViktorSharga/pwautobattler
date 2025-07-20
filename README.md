# PW Autobattler

A Windows desktop application for automating keyboard and mouse input across multiple instances of "Asgard Perfect World" (ElementClient.exe). 

## Features
- Register up to 10 game windows using hotkeys Ctrl+Shift+1-9/0
- Broadcast keyboard/mouse input to multiple game windows
- Multiple input simulation methods
- Test functions for common game actions
- Mouse mirroring and follow leader functionality

## Known Issues
- Background input methods (PostMessage/SendMessage) have limited reliability due to Windows API limitations and game-specific input handling
- KeyboardEventOptimized (default method) requires brief window focus switching for reliable input delivery