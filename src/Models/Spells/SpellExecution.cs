using GameAutomation.Core;

namespace GameAutomation.Models.Spells
{
    public class SpellExecution
    {
        public VirtualKeyCode[] KeySequence { get; init; } = [];
        public int[] Delays { get; init; } = [];
        public bool RequiresFocus { get; init; } = true;
        public string? Description { get; init; }
    }
}