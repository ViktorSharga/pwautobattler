using System.Collections.Generic;

namespace GameAutomation.Models.Spells
{
    public class SpellConfiguration
    {
        public Dictionary<string, ClassSpells> Classes { get; set; } = new();
    }

    public class ClassSpells
    {
        public List<SpellData> Spells { get; set; } = new();
    }

    public class SpellData
    {
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Cooldown { get; set; } = "00:00:00";
        public SpellExecutionData Execution { get; set; } = new();
        public SpellRequirementsData Requirements { get; set; } = new();
        public bool IsFormTransformation { get; set; } = false;
    }

    public class SpellExecutionData
    {
        public List<string> KeySequence { get; set; } = new();
        public List<int> Delays { get; set; } = new();
    }

    public class SpellRequirementsData
    {
        public string? FormRequired { get; set; }
    }
}