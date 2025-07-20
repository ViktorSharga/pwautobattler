using System.Collections.Generic;
using System.Threading.Tasks;
using GameAutomation.Models;
using GameAutomation.Models.Spells;

namespace GameAutomation.Services
{
    public interface ISpellService
    {
        Task<SpellResult> CastSpellAsync(string spellId, IGameWindow window);
        IEnumerable<ISpell> GetSpellsForClass(GameClass gameClass);
        IEnumerable<ISpell> GetAvailableSpells(IGameWindow window);
        Task ReloadSpellsAsync();
        ISpell? GetSpellById(string spellId);
    }
}