using System;

namespace GameAutomation.Models
{
    public class GameWindow : IGameWindow
    {
        public int ProcessId { get; set; }
        public IntPtr WindowHandle { get; set; }
        public string WindowTitle { get; set; } = string.Empty;
        public int RegistrationSlot { get; set; }
        public bool IsActive { get; set; }
        public DateTime RegisteredAt { get; set; }
        public GameClass CharacterClass { get; set; } = GameClass.None;
        
        // IGameWindow interface implementation
        public int SlotNumber => RegistrationSlot;
        public GameClass GameClass => CharacterClass;
        public bool IsValid => WindowHandle != IntPtr.Zero && !string.IsNullOrEmpty(WindowTitle);
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