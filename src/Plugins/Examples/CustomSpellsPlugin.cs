using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GameAutomation.Core;
using GameAutomation.Models;
using GameAutomation.Models.Spells;
using GameAutomation.Services;

namespace GameAutomation.Plugins.Examples
{
    /// <summary>
    /// Example plugin demonstrating how to create custom spells
    /// </summary>
    public class CustomSpellsPlugin : SpellPluginBase
    {
        private IInputService? _inputService;
        private List<ISpell> _customSpells = new();

        public override string Name => "CustomSpells";
        public override Version Version => new(1, 0, 0);
        public override string Description => "Example plugin with custom spell implementations";
        public override string Author => "Example Developer";

        public override IEnumerable<GameClass> SupportedClasses => 
            new[] { GameClass.None }; // Universal spells

        protected override async Task OnInitializeAsync()
        {
            if (ServiceProvider == null)
                throw new InvalidOperationException("Service provider not available");

            _inputService = ServiceProvider.GetService(typeof(IInputService)) as IInputService;
            if (_inputService == null)
                throw new InvalidOperationException("IInputService not found in service provider");

            // Create custom spells
            CreateCustomSpells();
            
            await Task.CompletedTask;
        }

        public override IEnumerable<ISpell> GetSpells()
        {
            EnsureInitialized();
            return _customSpells;
        }

        private void CreateCustomSpells()
        {
            // Example: Emergency logout spell
            _customSpells.Add(new CustomSpell(
                "emergency_logout",
                "Emergency Logout",
                TimeSpan.Zero,
                new SpellRequirements(),
                async (window, token) =>
                {
                    // Alt+F4 to close window
                    if (_inputService != null)
                    {
                        var success = await _inputService.SendKeySequenceAsync(window, 
                            new[] { VirtualKeyCode.VK_MENU, VirtualKeyCode.VK_F4 }, 
                            new[] { 0, 100 });
                        
                        return success 
                            ? SpellResult.Successful()
                            : SpellResult.Failed("Failed to send Alt+F4");
                    }
                    return SpellResult.Failed("InputService not available");
                }
            ));

            // Example: Screenshot spell
            _customSpells.Add(new CustomSpell(
                "take_screenshot",
                "Take Screenshot",
                TimeSpan.FromSeconds(1),
                new SpellRequirements(),
                async (window, token) =>
                {
                    // Print Screen key
                    if (_inputService != null)
                    {
                        var success = await _inputService.SendKeyPressAsync(window, VirtualKeyCode.VK_SNAPSHOT);
                        return success 
                            ? SpellResult.Successful()
                            : SpellResult.Failed("Failed to take screenshot");
                    }
                    return SpellResult.Failed("InputService not available");
                }
            ));

            // Example: Quick save spell (Ctrl+S)
            _customSpells.Add(new CustomSpell(
                "quick_save",
                "Quick Save",
                TimeSpan.FromSeconds(5),
                new SpellRequirements(),
                async (window, token) =>
                {
                    if (_inputService != null)
                    {
                        var success = await _inputService.SendKeySequenceAsync(window,
                            new[] { VirtualKeyCode.VK_CONTROL, VirtualKeyCode.VK_S },
                            new[] { 0, 50 });
                        
                        return success
                            ? SpellResult.Successful()
                            : SpellResult.Failed("Failed to save");
                    }
                    return SpellResult.Failed("InputService not available");
                }
            ));
        }

        protected override void OnDispose()
        {
            _customSpells.Clear();
            _inputService = null;
        }
    }

    /// <summary>
    /// Implementation of a custom spell
    /// </summary>
    public class CustomSpell : ISpell
    {
        private readonly Func<IGameWindow, CancellationToken, Task<SpellResult>> _executeFunc;

        public string Id { get; }
        public string DisplayName { get; }
        public TimeSpan Cooldown { get; }
        public SpellRequirements Requirements { get; }
        public SpellExecution Execution { get; }

        public CustomSpell(string id, string displayName, TimeSpan cooldown, 
            SpellRequirements requirements, 
            Func<IGameWindow, CancellationToken, Task<SpellResult>> executeFunc)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            Cooldown = cooldown;
            Requirements = requirements ?? throw new ArgumentNullException(nameof(requirements));
            Execution = new SpellExecution(); // Empty execution for custom logic
            _executeFunc = executeFunc ?? throw new ArgumentNullException(nameof(executeFunc));
        }

        public async Task<SpellResult> ExecuteAsync(IGameWindow window, CancellationToken cancellationToken)
        {
            try
            {
                return await _executeFunc(window, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return SpellResult.Failed("Operation was cancelled");
            }
            catch (Exception ex)
            {
                return SpellResult.Failed($"Execution failed: {ex.Message}");
            }
        }
    }
}