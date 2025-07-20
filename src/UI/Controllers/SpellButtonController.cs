using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using GameAutomation.Models;
using GameAutomation.Models.Spells;
using GameAutomation.Services;

namespace GameAutomation.UI.Controllers
{
    public class SpellButtonController
    {
        private readonly ISpellService _spellService;
        private readonly ICooldownService _cooldownService;
        private readonly Panel _classTablePanel;
        private readonly Dictionary<string, Button> _spellButtons;
        private readonly Dictionary<string, Label> _cooldownLabels;
        private readonly System.Windows.Forms.Timer _cooldownTimer;
        private DateTime _lastTableUpdate = DateTime.MinValue;
        
        public event Action<string>? StatusChanged;

        public SpellButtonController(ISpellService spellService, ICooldownService cooldownService, Panel classTablePanel)
        {
            _spellService = spellService ?? throw new ArgumentNullException(nameof(spellService));
            _cooldownService = cooldownService ?? throw new ArgumentNullException(nameof(cooldownService));
            _classTablePanel = classTablePanel ?? throw new ArgumentNullException(nameof(classTablePanel));
            _spellButtons = new Dictionary<string, Button>();
            _cooldownLabels = new Dictionary<string, Label>();
            
            _cooldownTimer = new System.Windows.Forms.Timer();
            _cooldownTimer.Interval = 1000; // Update every second
            _cooldownTimer.Tick += CooldownTimer_Tick;
            _cooldownTimer.Start();
        }

        public void UpdateClassTable(IReadOnlyDictionary<int, GameWindow> registeredWindows)
        {
            // Throttle updates to prevent excessive redraws (max 10 per second)
            var now = DateTime.Now;
            if ((now - _lastTableUpdate).TotalMilliseconds < 100)
            {
                return;
            }
            _lastTableUpdate = now;
            
            _classTablePanel.SuspendLayout();
            
            try
            {
                UpdateClassTableInternal(registeredWindows);
            }
            finally
            {
                _classTablePanel.ResumeLayout(true);
            }
        }

        private void UpdateClassTableInternal(IReadOnlyDictionary<int, GameWindow> registeredWindows)
        {
            _classTablePanel.Controls.Clear();
            _spellButtons.Clear();
            _cooldownLabels.Clear();
            
            var classWindows = registeredWindows
                .Where(kvp => kvp.Value.IsActive && kvp.Value.GameClass != GameClass.None)
                .OrderBy(kvp => kvp.Key)
                .ToList();
            
            if (classWindows.Count == 0)
            {
                var noClassLabel = new Label
                {
                    Text = "No windows with assigned classes",
                    Location = new Point(10, 10),
                    Size = new Size(300, 20),
                    ForeColor = Color.Gray
                };
                _classTablePanel.Controls.Add(noClassLabel);
                return;
            }
            
            // Track class counts for duplicates
            var classCount = new Dictionary<GameClass, int>();
            var classNumbers = new Dictionary<GameWindow, int>();
            
            foreach (var kvp in classWindows)
            {
                var gameClass = kvp.Value.GameClass;
                if (!classCount.ContainsKey(gameClass))
                    classCount[gameClass] = 0;
                classCount[gameClass]++;
                classNumbers[kvp.Value] = classCount[gameClass];
            }
            
            int yPos = 10;
            const int rowHeight = 40;
            const int spellButtonWidth = 80;
            const int spellButtonHeight = 30;
            const int spacing = 5;
            
            foreach (var kvp in classWindows)
            {
                var slot = kvp.Key;
                var window = kvp.Value;
                var classNumber = classNumbers[window];
                
                CreateClassRowControls(slot, window, classNumber, yPos, spellButtonWidth, spellButtonHeight, spacing);
                yPos += rowHeight;
            }
            
            // Update panel height to fit all controls
            var totalHeight = Math.Max(200, yPos + 10);
            if (_classTablePanel.Height != totalHeight)
            {
                _classTablePanel.Height = totalHeight;
                
                // Adjust form height if needed
                var form = _classTablePanel.FindForm();
                if (form != null)
                {
                    var newFormHeight = _classTablePanel.Bottom + 150;
                    if (form.Height < newFormHeight)
                    {
                        form.Height = newFormHeight;
                    }
                }
            }
        }

        private void CreateClassRowControls(int slot, GameWindow window, int classNumber, int yPos, 
            int spellButtonWidth, int spellButtonHeight, int spacing)
        {
            // Window info label
            var windowLabel = new Label
            {
                Text = $"[{slot}] {window.GameClass}",
                Location = new Point(10, yPos + 5),
                Size = new Size(120, 20),
                BackColor = window.IsActive ? Color.LightGreen : Color.LightGray,
                Font = new Font("Microsoft Sans Serif", 8F, FontStyle.Bold)
            };
            _classTablePanel.Controls.Add(windowLabel);

            // Get spells for this class
            var spells = _spellService.GetSpellsForClass(window.GameClass).ToList();
            var universalSpells = _spellService.GetSpellsForClass(GameClass.None).ToList();
            
            int xPos = 140;
            
            // Add class-specific spells
            foreach (var spell in spells)
            {
                CreateSpellButton(spell, window, xPos, yPos, spellButtonWidth, spellButtonHeight);
                xPos += spellButtonWidth + spacing;
            }
            
            // Add universal spells
            foreach (var spell in universalSpells)
            {
                CreateSpellButton(spell, window, xPos, yPos, spellButtonWidth, spellButtonHeight);
                xPos += spellButtonWidth + spacing;
            }
        }

        private void CreateSpellButton(ISpell spell, GameWindow window, int xPos, int yPos, 
            int buttonWidth, int buttonHeight)
        {
            var buttonKey = $"{window.ProcessId}_{spell.Id}";
            
            var button = new Button
            {
                Text = spell.DisplayName,
                Location = new Point(xPos, yPos),
                Size = new Size(buttonWidth, buttonHeight),
                Tag = new { Spell = spell, Window = window },
                Font = new Font("Microsoft Sans Serif", 7F),
                UseVisualStyleBackColor = true
            };
            
            button.Click += async (s, e) => await SpellButton_Click(spell, window);
            
            // Create cooldown label
            var cooldownLabel = new Label
            {
                Text = "",
                Location = new Point(xPos, yPos + buttonHeight + 2),
                Size = new Size(buttonWidth, 15),
                Font = new Font("Microsoft Sans Serif", 6F),
                ForeColor = Color.Red,
                TextAlign = ContentAlignment.MiddleCenter
            };
            
            _spellButtons[buttonKey] = button;
            _cooldownLabels[buttonKey] = cooldownLabel;
            
            _classTablePanel.Controls.Add(button);
            _classTablePanel.Controls.Add(cooldownLabel);
            
            UpdateSpellButtonState(spell, window);
        }

        private async Task SpellButton_Click(ISpell spell, GameWindow window)
        {
            try
            {
                var result = await _spellService.CastSpellAsync(spell.Id, window);
                
                if (result.Success)
                {
                    StatusChanged?.Invoke($"Cast {spell.DisplayName} on window {window.RegistrationSlot}");
                }
                else
                {
                    StatusChanged?.Invoke($"Failed to cast {spell.DisplayName}: {result.ErrorMessage}");
                }
                
                UpdateSpellButtonState(spell, window);
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"Error casting {spell.DisplayName}: {ex.Message}");
            }
        }

        private void UpdateSpellButtonState(ISpell spell, GameWindow window)
        {
            var buttonKey = $"{window.ProcessId}_{spell.Id}";
            
            if (!_spellButtons.TryGetValue(buttonKey, out var button) || 
                !_cooldownLabels.TryGetValue(buttonKey, out var cooldownLabel))
                return;
            
            var isOnCooldown = _cooldownService.IsOnCooldown(window, spell);
            var remainingCooldown = _cooldownService.GetRemainingCooldown(window, spell);
            
            button.Enabled = !isOnCooldown;
            
            if (isOnCooldown && remainingCooldown.HasValue)
            {
                var totalSeconds = (int)remainingCooldown.Value.TotalSeconds;
                if (totalSeconds > 60)
                {
                    var minutes = totalSeconds / 60;
                    var seconds = totalSeconds % 60;
                    cooldownLabel.Text = $"{minutes}:{seconds:D2}";
                }
                else
                {
                    cooldownLabel.Text = $"{totalSeconds}s";
                }
                
                button.BackColor = Color.LightCoral;
            }
            else
            {
                cooldownLabel.Text = "";
                button.BackColor = SystemColors.Control;
            }
        }

        private void CooldownTimer_Tick(object? sender, EventArgs e)
        {
            // Update all spell button states
            foreach (var kvp in _spellButtons)
            {
                var buttonKey = kvp.Key;
                var parts = buttonKey.Split('_');
                if (parts.Length >= 2 && int.TryParse(parts[0], out var processId))
                {
                    var spellId = string.Join("_", parts.Skip(1));
                    var spell = _spellService.GetSpellById(spellId);
                    
                    if (spell != null)
                    {
                        // Find window by process ID
                        var window = FindWindowByProcessId(processId);
                        if (window != null)
                        {
                            UpdateSpellButtonState(spell, window);
                        }
                    }
                }
            }
        }

        private GameWindow? FindWindowByProcessId(int processId)
        {
            // This would need access to the registered windows
            // For now, return null - this will be handled in the main form integration
            return null;
        }

        public void Dispose()
        {
            _cooldownTimer?.Stop();
            _cooldownTimer?.Dispose();
        }
    }
}