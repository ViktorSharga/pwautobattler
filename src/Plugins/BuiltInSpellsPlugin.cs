using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GameAutomation.Models;
using GameAutomation.Models.Spells;
using GameAutomation.Services;

namespace GameAutomation.Plugins
{
    /// <summary>
    /// Built-in plugin that provides the core spells from spells.json
    /// </summary>
    public class BuiltInSpellsPlugin : SpellPluginBase
    {
        private ISpellService? _spellService;
        private List<ISpell> _cachedSpells = new();

        public override string Name => "BuiltInSpells";
        public override Version Version => new(1, 0, 0);
        public override string Description => "Built-in spells loaded from spells.json configuration";
        public override string Author => "Game Automation System";

        public override IEnumerable<GameClass> SupportedClasses => 
            Enum.GetValues<GameClass>().Where(c => c != GameClass.None);

        protected override async Task OnInitializeAsync()
        {
            if (ServiceProvider == null)
                throw new InvalidOperationException("Service provider not available");

            _spellService = ServiceProvider.GetService(typeof(ISpellService)) as ISpellService;
            if (_spellService == null)
                throw new InvalidOperationException("ISpellService not found in service provider");

            // Load spells from the spell service
            await RefreshSpellsAsync();
        }

        public override IEnumerable<ISpell> GetSpells()
        {
            EnsureInitialized();
            return _cachedSpells;
        }

        public override IEnumerable<ISpell> GetSpellsForClass(GameClass gameClass)
        {
            EnsureInitialized();
            
            if (_spellService == null)
                return Enumerable.Empty<ISpell>();

            return _spellService.GetSpellsForClass(gameClass);
        }

        public override bool SupportsClass(GameClass gameClass)
        {
            return gameClass != GameClass.None;
        }

        /// <summary>
        /// Refreshes the cached spells from the spell service
        /// </summary>
        public async Task RefreshSpellsAsync()
        {
            if (_spellService == null)
                return;

            try
            {
                await _spellService.ReloadSpellsAsync();
                
                // Cache all spells from all classes
                _cachedSpells.Clear();
                foreach (var gameClass in Enum.GetValues<GameClass>())
                {
                    if (gameClass != GameClass.None)
                    {
                        _cachedSpells.AddRange(_spellService.GetSpellsForClass(gameClass));
                    }
                }
                
                // Also add universal spells
                _cachedSpells.AddRange(_spellService.GetSpellsForClass(GameClass.None));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to refresh spells: {ex.Message}", ex);
            }
        }

        protected override void OnDispose()
        {
            _cachedSpells.Clear();
            _spellService = null;
        }
    }
}