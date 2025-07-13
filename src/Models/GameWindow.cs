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

        public GameWindow()
        {
            RegisteredAt = DateTime.Now;
        }

        public override string ToString()
        {
            return $"[{RegistrationSlot}] PID: {ProcessId} - {WindowTitle}";
        }
    }
}