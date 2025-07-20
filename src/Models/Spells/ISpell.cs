using System;
using System.Threading;
using System.Threading.Tasks;

namespace GameAutomation.Models.Spells
{
    public interface ISpell
    {
        string Id { get; }
        string DisplayName { get; }
        TimeSpan Cooldown { get; }
        SpellRequirements Requirements { get; }
        Task<SpellResult> ExecuteAsync(IGameWindow window, CancellationToken cancellationToken);
    }
}