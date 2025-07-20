using System;

namespace GameAutomation.Models
{
    public interface IGameWindow
    {
        IntPtr WindowHandle { get; }
        int SlotNumber { get; }
        GameClass GameClass { get; }
        bool IsActive { get; set; }
        bool IsInAnimalForm { get; set; }
        string WindowTitle { get; }
        bool IsValid { get; }
    }
}