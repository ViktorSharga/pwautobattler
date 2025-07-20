using GameAutomation.Core;

namespace GameAutomation.Models.Configuration
{
    public class AppSettings
    {
        public InputSettings Input { get; set; } = new();
        public UISettings UI { get; set; } = new();
        public GameSettings Game { get; set; } = new();
    }

    public class InputSettings
    {
        public InputMethod DefaultMethod { get; set; } = InputMethod.KeyboardEventOptimized;
        public int RetryAttempts { get; set; } = 3;
        public int TimeoutMs { get; set; } = 5000;
        public int DelayAfterActivation { get; set; } = 50;
    }

    public class UISettings
    {
        public int UpdateThrottleMs { get; set; } = 100;
        public int MaxSpellsPerRow { get; set; } = 3;
        public bool EnableDoubleBuffering { get; set; } = true;
        public int TableHeight { get; set; } = 0; // 0 = dynamic
    }

    public class GameSettings
    {
        public string ProcessName { get; set; } = "ElementClient";
        public int MaxWindows { get; set; } = 10;
        public int WindowActivationDelay { get; set; } = 50;
    }
}