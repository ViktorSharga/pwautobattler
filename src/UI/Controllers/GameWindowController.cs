using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GameAutomation.Core;
using GameAutomation.Models;
using GameAutomation.Services;

namespace GameAutomation.UI.Controllers
{
    public class GameWindowController
    {
        private readonly IWindowService _windowService;
        private readonly Panel _windowPanel;
        private readonly Dictionary<int, GameWindow> _registeredWindows;
        private readonly List<Label> _windowLabels;
        private readonly List<Button> _testButtons;
        private readonly List<ComboBox> _classDropdowns;
        private readonly List<CheckBox> _mainCheckboxes;
        private readonly Button _refreshButton;
        private readonly Button _autoScanButton;
        
        public event Action<string>? StatusChanged;
        public event Action? WindowListChanged;

        public GameWindowController(IWindowService windowService, Panel windowPanel)
        {
            _windowService = windowService ?? throw new ArgumentNullException(nameof(windowService));
            _windowPanel = windowPanel ?? throw new ArgumentNullException(nameof(windowPanel));
            _registeredWindows = new Dictionary<int, GameWindow>();
            _windowLabels = new List<Label>();
            _testButtons = new List<Button>();
            _classDropdowns = new List<ComboBox>();
            _mainCheckboxes = new List<CheckBox>();

            _refreshButton = CreateRefreshButton();
            _autoScanButton = CreateAutoScanButton();
            
            InitializeControls();
        }

        public IReadOnlyDictionary<int, GameWindow> RegisteredWindows => _registeredWindows;

        public void RegisterWindow(int slot)
        {
            var activeWindow = _windowService.GetActiveWindow();
            if (activeWindow != null && activeWindow is GameWindow gameWindow)
            {
                gameWindow.RegistrationSlot = slot;
                _registeredWindows[slot] = gameWindow;
                UpdateWindowList();
                StatusChanged?.Invoke($"Window registered to slot {slot}: PID {gameWindow.ProcessId}");
            }
            else
            {
                StatusChanged?.Invoke("No valid ElementClient window is currently active.");
            }
        }

        public void RefreshWindows()
        {
            UpdateWindowList();
            StatusChanged?.Invoke("Window list refreshed.");
        }

        public void AutoScanWindows()
        {
            try
            {
                var availableWindows = _windowService.EnumerateGameWindows().ToList();
                int assignedCount = 0;
                
                _registeredWindows.Clear();
                
                for (int i = 0; i < Math.Min(availableWindows.Count, 10); i++)
                {
                    var window = availableWindows[i];
                    if (window is GameWindow gameWindow)
                    {
                        gameWindow.RegistrationSlot = i + 1;
                        gameWindow.IsActive = true;
                        gameWindow.RegisteredAt = DateTime.Now;
                        _registeredWindows[i + 1] = gameWindow;
                    }
                    assignedCount++;
                }
                
                UpdateWindowList();
                StatusChanged?.Invoke($"Auto-scan completed. Assigned {assignedCount} ElementClient windows to slots 1-{assignedCount}.");
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"Auto-scan failed: {ex.Message}");
            }
        }

        public void UpdateClassForWindow(int slot, GameClass gameClass)
        {
            if (_registeredWindows.TryGetValue(slot, out var window))
            {
                ((GameWindow)window).CharacterClass = gameClass;
                WindowListChanged?.Invoke();
            }
        }

        public void SetWindowActive(int slot, bool isActive)
        {
            if (_registeredWindows.TryGetValue(slot, out var window))
            {
                window.IsActive = isActive;
                WindowListChanged?.Invoke();
            }
        }

        public GameWindow[] GetActiveWindows()
        {
            return _registeredWindows.Values.Where(w => w.IsActive).ToArray();
        }

        private void InitializeControls()
        {
            var form = _windowPanel.FindForm();
            if (form != null)
            {
                form.Controls.Add(_refreshButton);
                form.Controls.Add(_autoScanButton);
            }
        }

        private Button CreateRefreshButton()
        {
            var button = new Button
            {
                Text = "Refresh List",
                Location = new Point(10, 145),
                Size = new Size(100, 30)
            };
            button.Click += (s, e) => RefreshWindows();
            return button;
        }

        private Button CreateAutoScanButton()
        {
            var button = new Button
            {
                Text = "Auto-Scan",
                Location = new Point(120, 145),
                Size = new Size(100, 30)
            };
            button.Click += (s, e) => AutoScanWindows();
            return button;
        }

        private void UpdateWindowList()
        {
            _windowPanel.SuspendLayout();
            
            // Clear existing controls
            foreach (var label in _windowLabels)
                _windowPanel.Controls.Remove(label);
            foreach (var button in _testButtons)
                _windowPanel.Controls.Remove(button);
            foreach (var combo in _classDropdowns)
                _windowPanel.Controls.Remove(combo);
            foreach (var checkbox in _mainCheckboxes)
                _windowPanel.Controls.Remove(checkbox);
            
            _windowLabels.Clear();
            _testButtons.Clear();
            _classDropdowns.Clear();
            _mainCheckboxes.Clear();

            // Add updated controls
            int yPos = 10;
            foreach (var kvp in _registeredWindows.OrderBy(w => w.Key))
            {
                var slot = kvp.Key;
                var window = kvp.Value;
                
                CreateWindowControls(slot, window, yPos);
                yPos += 30;
            }
            
            _windowPanel.ResumeLayout();
            WindowListChanged?.Invoke();
        }

        private void CreateWindowControls(int slot, GameWindow window, int yPos)
        {
            // Main checkbox
            var mainCheckbox = new CheckBox
            {
                Text = "",
                Location = new Point(5, yPos + 2),
                Size = new Size(20, 20),
                Checked = window.IsActive
            };
            mainCheckbox.CheckedChanged += (s, e) => SetWindowActive(slot, mainCheckbox.Checked);
            _mainCheckboxes.Add(mainCheckbox);
            _windowPanel.Controls.Add(mainCheckbox);

            // Window info label
            var label = new Label
            {
                Text = $"Slot {slot}: PID {window.ProcessId} - {window.WindowTitle}",
                Location = new Point(30, yPos),
                Size = new Size(300, 25),
                BackColor = window.IsActive ? Color.LightGreen : Color.LightGray
            };
            _windowLabels.Add(label);
            _windowPanel.Controls.Add(label);

            // Class dropdown
            var classCombo = new ComboBox
            {
                Location = new Point(340, yPos),
                Size = new Size(100, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            
            foreach (GameClass gameClass in Enum.GetValues<GameClass>())
            {
                classCombo.Items.Add(gameClass);
            }
            
            classCombo.SelectedItem = window.GameClass;
            classCombo.SelectedIndexChanged += (s, e) => 
            {
                if (classCombo.SelectedItem != null)
                {
                    UpdateClassForWindow(slot, (GameClass)classCombo.SelectedItem);
                }
            };
            
            _classDropdowns.Add(classCombo);
            _windowPanel.Controls.Add(classCombo);

            // Test button
            var testButton = new Button
            {
                Text = "Test",
                Location = new Point(450, yPos),
                Size = new Size(50, 25)
            };
            testButton.Click += (s, e) => TestWindow(window);
            _testButtons.Add(testButton);
            _windowPanel.Controls.Add(testButton);
        }

        private void TestWindow(GameWindow window)
        {
            // Implementation would call input service to test the window
            StatusChanged?.Invoke($"Testing window PID {window.ProcessId}...");
        }
    }
}