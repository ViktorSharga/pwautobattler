namespace GameAutomation.Models.Spells
{
    public class SpellRequirements
    {
        public bool IsFormTransformation { get; set; } = false;
        public bool RequiresAnimalForm { get; set; } = false;
        public bool RequiresHumanForm { get; set; } = false;
        public GameClass? RequiredClass { get; set; } = null;
    }
}