using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GameAutomation.Core;
using GameAutomation.Models;
using GameAutomation.Models.Spells;

namespace GameAutomation.Services
{
    public class SpellService : ISpellService
    {
        private readonly IInputService _inputService;
        private readonly ICooldownService _cooldownService;
        private readonly Dictionary<string, ISpell> _spells;
        private readonly Dictionary<GameClass, List<ISpell>> _spellsByClass;
        private readonly string _spellsFilePath;

        public SpellService(IInputService inputService, ICooldownService cooldownService)
        {
            _inputService = inputService ?? throw new ArgumentNullException(nameof(inputService));
            _cooldownService = cooldownService ?? throw new ArgumentNullException(nameof(cooldownService));
            _spells = new Dictionary<string, ISpell>();
            _spellsByClass = new Dictionary<GameClass, List<ISpell>>();
            _spellsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "src", "Data", "Spells", "spells.json");
        }

        public async Task<SpellResult> CastSpellAsync(string spellId, IGameWindow window)
        {
            if (!_spells.TryGetValue(spellId, out var spell))
            {
                return SpellResult.Failed($"Spell with ID '{spellId}' not found");
            }

            // Check cooldown
            if (_cooldownService.IsOnCooldown(window, spell))
            {
                var remaining = _cooldownService.GetRemainingCooldown(window, spell);
                return SpellResult.Failed($"Spell on cooldown for {remaining?.TotalSeconds:F0} seconds");
            }

            // Check form requirements
            if (!CheckFormRequirements(spell, window))
            {
                return SpellResult.Failed("Form requirements not met");
            }

            // Execute spell
            var startTime = DateTime.Now;
            var result = await spell.ExecuteAsync(window, default);
            
            if (result.Success)
            {
                // Start cooldown
                await _cooldownService.StartCooldownAsync(window, spell);
                
                // Handle form transformation
                if (spell.Requirements.IsFormTransformation)
                {
                    window.IsInAnimalForm = !window.IsInAnimalForm;
                }
            }

            return result;
        }

        public IEnumerable<ISpell> GetSpellsForClass(GameClass gameClass)
        {
            _spellsByClass.TryGetValue(gameClass, out var spells);
            return spells ?? Enumerable.Empty<ISpell>();
        }

        public IEnumerable<ISpell> GetAvailableSpells(IGameWindow window)
        {
            var classSpells = GetSpellsForClass(window.GameClass).ToList();
            var universalSpells = GetSpellsForClass(GameClass.None).ToList();
            
            var availableSpells = new List<ISpell>();
            
            // Filter class spells by form requirements
            foreach (var spell in classSpells)
            {
                if (CheckFormRequirements(spell, window))
                {
                    availableSpells.Add(spell);
                }
            }
            
            // Add universal spells
            availableSpells.AddRange(universalSpells);
            
            return availableSpells;
        }

        public async Task ReloadSpellsAsync()
        {
            try
            {
                if (!File.Exists(_spellsFilePath))
                {
                    throw new FileNotFoundException($"Spells configuration file not found: {_spellsFilePath}");
                }

                var jsonContent = await File.ReadAllTextAsync(_spellsFilePath);
                var spellData = JsonSerializer.Deserialize<SpellConfiguration>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (spellData?.Classes == null)
                {
                    throw new InvalidOperationException("Invalid spell configuration format");
                }

                _spells.Clear();
                _spellsByClass.Clear();

                foreach (var classEntry in spellData.Classes)
                {
                    var gameClass = ParseGameClass(classEntry.Key);
                    var spells = new List<ISpell>();

                    foreach (var spellInfo in classEntry.Value.Spells)
                    {
                        var spell = CreateSpellFromData(spellInfo);
                        _spells[spell.Id] = spell;
                        spells.Add(spell);
                    }

                    _spellsByClass[gameClass] = spells;
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load spells: {ex.Message}", ex);
            }
        }

        public ISpell? GetSpellById(string spellId)
        {
            _spells.TryGetValue(spellId, out var spell);
            return spell;
        }

        private bool CheckFormRequirements(ISpell spell, IGameWindow window)
        {
            if (spell.Requirements.RequiresAnimalForm && !window.IsInAnimalForm)
                return false;
            
            if (spell.Requirements.RequiresHumanForm && window.IsInAnimalForm)
                return false;
            
            return true;
        }

        private ISpell CreateSpellFromData(SpellData spellData)
        {
            var requirements = new SpellRequirements
            {
                IsFormTransformation = spellData.IsFormTransformation,
                RequiresAnimalForm = spellData.Requirements?.FormRequired == "animal",
                RequiresHumanForm = spellData.Requirements?.FormRequired == "human"
            };

            var keySequence = spellData.Execution.KeySequence
                .Select(ParseVirtualKeyCode)
                .ToArray();

            var execution = new SpellExecution
            {
                KeySequence = keySequence,
                Delays = spellData.Execution.Delays,
                RequiresFocus = true
            };

            return new ConfigurableSpell(
                spellData.Id,
                spellData.DisplayName,
                TimeSpan.Parse(spellData.Cooldown),
                requirements,
                execution,
                _inputService
            );
        }

        private VirtualKeyCode ParseVirtualKeyCode(string keyString)
        {
            return keyString.ToUpper() switch
            {
                "VK_1" => VirtualKeyCode.VK_1,
                "VK_2" => VirtualKeyCode.VK_2,
                "VK_3" => VirtualKeyCode.VK_3,
                "VK_4" => VirtualKeyCode.VK_4,
                "VK_5" => VirtualKeyCode.VK_5,
                "VK_6" => VirtualKeyCode.VK_6,
                "VK_7" => VirtualKeyCode.VK_7,
                "VK_8" => VirtualKeyCode.VK_8,
                "VK_9" => VirtualKeyCode.VK_9,
                "VK_F1" => VirtualKeyCode.VK_F1,
                "VK_F2" => VirtualKeyCode.VK_F2,
                "VK_F3" => VirtualKeyCode.VK_F3,
                "VK_F4" => VirtualKeyCode.VK_F4,
                "VK_F5" => VirtualKeyCode.VK_F5,
                "VK_F6" => VirtualKeyCode.VK_F6,
                "VK_F7" => VirtualKeyCode.VK_F7,
                "VK_F8" => VirtualKeyCode.VK_F8,
                "VK_A" => VirtualKeyCode.VK_A,
                "VK_S" => VirtualKeyCode.VK_S,
                "VK_D" => VirtualKeyCode.VK_D,
                "VK_W" => VirtualKeyCode.VK_W,
                "VK_Q" => VirtualKeyCode.VK_Q,
                _ => throw new ArgumentException($"Unknown virtual key code: {keyString}")
            };
        }

        private GameClass ParseGameClass(string className)
        {
            return className.ToLower() switch
            {
                "shaman" => GameClass.Shaman,
                "seeker" => GameClass.Seeker,
                "assassin" => GameClass.Assassin,
                "warrior" => GameClass.Warrior,
                "mage" => GameClass.Mage,
                "archer" => GameClass.Archer,
                "tank" => GameClass.Tank,
                "druid" => GameClass.Druid,
                "priest" => GameClass.Priest,
                "universal" => GameClass.None,
                _ => throw new ArgumentException($"Unknown game class: {className}")
            };
        }

        // JSON model classes
        private class SpellConfiguration
        {
            public Dictionary<string, ClassData> Classes { get; set; } = new();
        }

        private class ClassData
        {
            public List<SpellData> Spells { get; set; } = new();
        }

        private class SpellData
        {
            public string Id { get; set; } = "";
            public string DisplayName { get; set; } = "";
            public string Cooldown { get; set; } = "00:00:00";
            public ExecutionData Execution { get; set; } = new();
            public RequirementsData? Requirements { get; set; }
            public bool IsFormTransformation { get; set; } = false;
        }

        private class ExecutionData
        {
            public List<string> KeySequence { get; set; } = new();
            public int[] Delays { get; set; } = Array.Empty<int>();
            public List<string>? Modifiers { get; set; }
        }

        private class RequirementsData
        {
            public string? FormRequired { get; set; }
        }
    }

    // Configurable spell implementation
    public class ConfigurableSpell : ISpell
    {
        private readonly IInputService _inputService;

        public string Id { get; }
        public string DisplayName { get; }
        public TimeSpan Cooldown { get; }
        public SpellRequirements Requirements { get; }
        public SpellExecution Execution { get; }

        public ConfigurableSpell(string id, string displayName, TimeSpan cooldown, 
            SpellRequirements requirements, SpellExecution execution, IInputService inputService)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            Cooldown = cooldown;
            Requirements = requirements ?? throw new ArgumentNullException(nameof(requirements));
            Execution = execution ?? throw new ArgumentNullException(nameof(execution));
            _inputService = inputService ?? throw new ArgumentNullException(nameof(inputService));
        }

        public async Task<SpellResult> ExecuteAsync(IGameWindow window, CancellationToken cancellationToken)
        {
            try
            {
                var startTime = DateTime.Now;
                
                // Execute key sequence with delays
                var success = await _inputService.SendKeySequenceAsync(window, Execution.KeySequence, Execution.Delays);
                
                var executionTime = DateTime.Now - startTime;
                
                return success 
                    ? SpellResult.Successful(executionTime)
                    : SpellResult.Failed("Failed to send key sequence");
            }
            catch (Exception ex)
            {
                return SpellResult.Failed($"Execution error: {ex.Message}");
            }
        }
    }
}