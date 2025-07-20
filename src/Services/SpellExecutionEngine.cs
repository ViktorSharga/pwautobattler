using System;
using System.Threading;
using System.Threading.Tasks;
using GameAutomation.Models;
using GameAutomation.Models.Spells;

namespace GameAutomation.Services
{
    public class SpellExecutionEngine
    {
        private readonly IInputService _inputService;
        private readonly ICooldownService _cooldownService;
        private readonly IConfigurationService? _configurationService;
        private bool _broadcastDisabled = false;

        public SpellExecutionEngine(IInputService inputService, ICooldownService cooldownService, IConfigurationService? configurationService = null)
        {
            _inputService = inputService ?? throw new ArgumentNullException(nameof(inputService));
            _cooldownService = cooldownService ?? throw new ArgumentNullException(nameof(cooldownService));
            _configurationService = configurationService;
        }

        public async Task<SpellResult> ExecuteSpellAsync(ISpell spell, IGameWindow window)
        {
            return await ExecuteSpellAsync(spell, window, CancellationToken.None);
        }

        public async Task<SpellResult> ExecuteSpellAsync(ISpell spell, IGameWindow window, CancellationToken cancellationToken)
        {
            using var timeoutCancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            using var combinedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, timeoutCancellation.Token);

            try
            {
                // Pre-execution validation
                var validationResult = ValidateExecution(spell, window);
                if (!validationResult.Success)
                    return validationResult;

                // Disable broadcast mode temporarily if needed
                await DisableBroadcastTemporarily();

                // Execute spell with error handling
                return await ExecuteWithRetry(spell, window, combinedCancellation.Token);
            }
            catch (OperationCanceledException) when (timeoutCancellation.Token.IsCancellationRequested)
            {
                return SpellResult.Failed("Spell execution timed out");
            }
            catch (Exception ex)
            {
                return SpellResult.Failed($"Unexpected error: {ex.Message}");
            }
            finally
            {
                await RestoreBroadcastMode();
            }
        }

        public async Task<SpellResult> ExecuteSpellSafelyAsync(ISpell spell, IGameWindow window)
        {
            // This method provides additional safety for broadcast mode conflicts
            var wasBroadcastEnabled = _inputService.IsBroadcastModeEnabled;
            
            try
            {
                if (wasBroadcastEnabled)
                {
                    await _inputService.StopBroadcastModeAsync();
                }

                return await ExecuteSpellAsync(spell, window);
            }
            finally
            {
                if (wasBroadcastEnabled)
                {
                    await _inputService.StartBroadcastModeAsync();
                }
            }
        }

        private SpellResult ValidateExecution(ISpell spell, IGameWindow window)
        {
            // Check if window is valid
            if (!window.IsValid)
            {
                return SpellResult.Failed("Invalid window handle");
            }

            // Check cooldown
            if (_cooldownService.IsOnCooldown(window, spell))
            {
                var remaining = _cooldownService.GetRemainingCooldown(window, spell);
                return SpellResult.Failed($"Spell on cooldown for {remaining?.TotalSeconds:F0} seconds");
            }

            // Check form requirements
            if (spell.Requirements.RequiresAnimalForm && !window.IsInAnimalForm)
            {
                return SpellResult.Failed("Requires animal form");
            }

            if (spell.Requirements.RequiresHumanForm && window.IsInAnimalForm)
            {
                return SpellResult.Failed("Requires human form");
            }

            // Check if spell is for the correct class
            if (spell.Requirements.RequiredClass.HasValue && 
                spell.Requirements.RequiredClass.Value != window.GameClass &&
                spell.Requirements.RequiredClass.Value != GameClass.None)
            {
                return SpellResult.Failed($"Spell not available for {window.GameClass}");
            }

            return SpellResult.Successful();
        }

        private async Task<SpellResult> ExecuteWithRetry(ISpell spell, IGameWindow window, CancellationToken cancellationToken)
        {
            var maxRetries = _configurationService?.GetValue("input.retryAttempts", 3) ?? 3;
            var delayMs = _configurationService?.GetValue("input.retryDelayMs", 100) ?? 100;

            Exception? lastException = null;

            for (int attempt = 0; attempt <= maxRetries; attempt++)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var startTime = DateTime.Now;
                    var result = await spell.ExecuteAsync(window, cancellationToken);
                    var executionTime = DateTime.Now - startTime;

                    if (result.Success)
                    {
                        // Start cooldown
                        await _cooldownService.StartCooldownAsync(window, spell);

                        // Handle form transformation
                        if (spell.Requirements.IsFormTransformation)
                        {
                            window.IsInAnimalForm = !window.IsInAnimalForm;
                        }

                        return SpellResult.Successful(executionTime);
                    }

                    // If this was the last attempt, return the failed result
                    if (attempt == maxRetries)
                    {
                        return result;
                    }

                    // Wait before retry
                    await Task.Delay(delayMs, cancellationToken);
                }
                catch (Exception ex) when (!(ex is OperationCanceledException))
                {
                    lastException = ex;
                    
                    if (attempt == maxRetries)
                    {
                        break;
                    }

                    await Task.Delay(delayMs, cancellationToken);
                }
            }

            return SpellResult.Failed($"Failed after {maxRetries + 1} attempts. Last error: {lastException?.Message}");
        }

        private async Task DisableBroadcastTemporarily()
        {
            if (_inputService.IsBroadcastModeEnabled)
            {
                await _inputService.StopBroadcastModeAsync();
                _broadcastDisabled = true;
            }
        }

        private async Task RestoreBroadcastMode()
        {
            if (_broadcastDisabled)
            {
                await _inputService.StartBroadcastModeAsync();
                _broadcastDisabled = false;
            }
        }

        public async Task<SpellResult> BroadcastSpellAsync(ISpell spell, IGameWindow[] windows)
        {
            var results = new List<SpellResult>();
            var tasks = new List<Task<SpellResult>>();

            foreach (var window in windows)
            {
                if (window.IsValid && window.IsActive)
                {
                    tasks.Add(ExecuteSpellAsync(spell, window));
                }
            }

            var allResults = await Task.WhenAll(tasks);
            
            var successCount = 0;
            var errors = new List<string>();

            foreach (var result in allResults)
            {
                if (result.Success)
                {
                    successCount++;
                }
                else
                {
                    errors.Add(result.ErrorMessage ?? "Unknown error");
                }
            }

            if (successCount == allResults.Length)
            {
                return SpellResult.Successful();
            }
            else if (successCount > 0)
            {
                return SpellResult.Failed($"Partial success: {successCount}/{allResults.Length} windows. Errors: {string.Join(", ", errors)}");
            }
            else
            {
                return SpellResult.Failed($"Complete failure. Errors: {string.Join(", ", errors)}");
            }
        }
    }
}