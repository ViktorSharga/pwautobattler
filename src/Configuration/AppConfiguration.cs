using System;
using System.Collections.Generic;

namespace GameAutomation.Configuration
{
    public class AppConfiguration
    {
        public InputSettings Input { get; set; } = new();
        public UISettings UI { get; set; } = new();
        public GameSettings Game { get; set; } = new();
        public CooldownSettings Cooldowns { get; set; } = new();
        public HotkeySettings Hotkeys { get; set; } = new();
        public LoggingSettings Logging { get; set; } = new();
    }

    public class InputSettings
    {
        public string DefaultMethod { get; set; } = "KeyboardEventOptimized";
        public int RetryAttempts { get; set; } = 3;
        public int RetryDelayMs { get; set; } = 100;
        public int TimeoutMs { get; set; } = 5000;
        public int KeyDelayMs { get; set; } = 50;
        public bool EnableBroadcastMode { get; set; } = false;
    }

    public class UISettings
    {
        public int UpdateThrottleMs { get; set; } = 100;
        public int MaxSpellsPerRow { get; set; } = 6;
        public bool EnableDoubleBuffering { get; set; } = true;
        public int RefreshIntervalMs { get; set; } = 1000;
        public bool WindowListAutoRefresh { get; set; } = true;
    }

    public class GameSettings
    {
        public string ProcessName { get; set; } = "ElementClient";
        public int MaxWindows { get; set; } = 10;
        public bool AutoScanOnStartup { get; set; } = false;
        public int ValidateWindowsInterval { get; set; } = 5000;
    }

    public class CooldownSettings
    {
        public bool EnableGlobalCooldowns { get; set; } = true;
        public int CleanupIntervalMinutes { get; set; } = 30;
        public bool PersistCooldowns { get; set; } = false;
    }

    public class HotkeySettings
    {
        public bool EnableGlobalHotkeys { get; set; } = true;
        public List<string> RegistrationModifiers { get; set; } = new() { "Ctrl", "Shift" };
        public List<string> BroadcastKeys { get; set; } = new() { "1", "2", "3", "4", "5", "6", "7", "8", "9" };
    }

    public class LoggingSettings
    {
        public bool EnableLogging { get; set; } = true;
        public string LogLevel { get; set; } = "Info";
        public string LogFilePath { get; set; } = "logs/game-automation.log";
        public int MaxLogFileSizeMB { get; set; } = 10;
    }
}