using System;

namespace GameAutomation.Models
{
    public class GameWindow
    {
        public int ProcessId { get; set; }
        public IntPtr WindowHandle { get; set; }
        public string WindowTitle { get; set; } = string.Empty;
        public int RegistrationSlot { get; set; }
        public bool IsActive { get; set; }
        public DateTime RegisteredAt { get; set; }
        public GameClass CharacterClass { get; set; } = GameClass.None;
        public bool IsMainWindow { get; set; } = false;
        public bool IsInAnimalForm { get; set; } = false; // Track transformation state

        public GameWindow()
        {
            RegisteredAt = DateTime.Now;
        }

        public override string ToString()
        {
            var classText = CharacterClass == GameClass.None ? "" : $" ({CharacterClass})";
            var mainText = IsMainWindow ? " [MAIN]" : "";
            return $"[{RegistrationSlot}] PID: {ProcessId} - {WindowTitle}{classText}{mainText}";
        }
    }
}