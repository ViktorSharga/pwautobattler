using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using GameAutomation.Core;
using GameAutomation.Models;

namespace GameAutomation.UI
{
    public partial class MainForm : Form
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
        private readonly WindowManager _windowManager;
        private readonly InputSimulator _inputSimulator;
        private readonly HotkeyManager _hotkeyManager;
        private readonly Dictionary<int, GameWindow> _registeredWindows;

        private Panel _windowPanel = null!;
        private readonly List<Label> _windowLabels = new();
        private readonly List<Button> _testButtons = new();
        private readonly List<ComboBox> _classDropdowns = new();
        private readonly List<CheckBox> _mainCheckboxes = new();
        private Panel _classTablePanel = null!;
        private readonly CooldownManager _cooldownManager = new();
        private System.Windows.Forms.Timer _cooldownTimer = null!;
        private DateTime _lastTableUpdate = DateTime.MinValue;
        private Button _refreshButton = null!;
        private Button _autoScanButton = null!;
        private Button _sendQButton = null!;
        private Label _statusLabel = null!;
        private ComboBox _methodComboBox = null!;
        private Button _testAllMethodsButton = null!;
        private Label _methodLabel = null!;
        private CheckBox _broadcastModeCheckBox = null!;
        private bool _broadcastMode = false;
        private LowLevelKeyboardHook? _keyboardHook;
        private CheckBox _mouseMirroringCheckBox = null!;
        private bool _mouseMirroringMode = false;
        private CheckBox _shiftDoubleClickCheckBox = null!;
        private bool _shiftDoubleClickMode = false;
        private MouseHook? _mouseHook;
        private BackgroundMouseSimulator? _backgroundMouseSimulator;
        

        public MainForm()
        {
            _windowManager = new WindowManager();
            _inputSimulator = new InputSimulator();
            _hotkeyManager = new HotkeyManager();
            _registeredWindows = new Dictionary<int, GameWindow>();

            InitializeComponent();
            SetupHotkeys();
            _hotkeyManager.StartListening();
            
            // Initialize keyboard hook
            _keyboardHook = new LowLevelKeyboardHook();
            _keyboardHook.KeyDown += OnGlobalKeyDown;
            
            // Initialize mouse hook and simulator
            _mouseHook = new MouseHook();
            _mouseHook.CtrlLeftClick += OnCtrlLeftClick;
            _mouseHook.CtrlRightClick += OnCtrlRightClick;
            _mouseHook.ShiftLeftClick += OnShiftLeftClick;
            _mouseHook.StartListening(); // Always listen for calibration
            _backgroundMouseSimulator = new BackgroundMouseSimulator();
            
            // Initialize cooldown timer
            _cooldownTimer = new System.Windows.Forms.Timer();
            _cooldownTimer.Interval = 1000; // Update every second
            _cooldownTimer.Tick += CooldownTimer_Tick;
            _cooldownTimer.Start();
        }

        private void InitializeComponent()
        {
            Text = "Game Multi-Window Controller";
            Size = new System.Drawing.Size(750, 650);
            StartPosition = FormStartPosition.CenterScreen;

            // Window list
            var windowLabel = new Label
            {
                Text = "Registered Windows:",
                Location = new System.Drawing.Point(10, 10),
                Size = new System.Drawing.Size(200, 20)
            };

            _windowPanel = new Panel
            {
                Location = new System.Drawing.Point(10, 35),
                Size = new System.Drawing.Size(710, 100),
                BorderStyle = BorderStyle.Fixed3D,
                AutoScroll = true
            };
            
            // Initialize window labels, test buttons, class dropdowns, and main checkboxes
            for (int i = 0; i < 10; i++)
            {
                var label = new Label
                {
                    Location = new System.Drawing.Point(5, i * 20),
                    Size = new System.Drawing.Size(320, 18),
                    Text = $"[{i + 1}] [Empty Slot]",
                    Font = new System.Drawing.Font("Microsoft Sans Serif", 8F)
                };
                _windowLabels.Add(label);
                _windowPanel.Controls.Add(label);
                
                var classDropdown = new ComboBox
                {
                    Location = new System.Drawing.Point(330, i * 20 - 2),
                    Size = new System.Drawing.Size(90, 20),
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Font = new System.Drawing.Font("Microsoft Sans Serif", 7F),
                    Enabled = false
                };
                classDropdown.Items.AddRange(Enum.GetNames(typeof(GameClass)));
                classDropdown.SelectedItem = GameClass.None.ToString();
                var slotNumber = i + 1; // Capture for closure
                classDropdown.SelectedIndexChanged += (s, e) => ClassDropdown_SelectedIndexChanged(slotNumber, classDropdown);
                _classDropdowns.Add(classDropdown);
                _windowPanel.Controls.Add(classDropdown);
                
                var testButton = new Button
                {
                    Location = new System.Drawing.Point(430, i * 20 - 2),
                    Size = new System.Drawing.Size(80, 20),
                    Text = "Test Focus",
                    Font = new System.Drawing.Font("Microsoft Sans Serif", 7F),
                    Enabled = false
                };
                testButton.Click += (s, e) => TestConnection_Click(slotNumber);
                _testButtons.Add(testButton);
                _windowPanel.Controls.Add(testButton);
                
                var mainCheckbox = new CheckBox
                {
                    Location = new System.Drawing.Point(520, i * 20),
                    Size = new System.Drawing.Size(50, 18),
                    Text = "Main",
                    Font = new System.Drawing.Font("Microsoft Sans Serif", 7F),
                    Enabled = false
                };
                mainCheckbox.CheckedChanged += (s, e) => MainCheckbox_CheckedChanged(slotNumber, mainCheckbox);
                _mainCheckboxes.Add(mainCheckbox);
                _windowPanel.Controls.Add(mainCheckbox);
            }

            _refreshButton = new Button
            {
                Text = "Refresh Available Windows",
                Location = new System.Drawing.Point(10, 145),
                Size = new System.Drawing.Size(150, 30)
            };
            _refreshButton.Click += RefreshButton_Click;

            _autoScanButton = new Button
            {
                Text = "Auto-Scan & Assign",
                Location = new System.Drawing.Point(170, 145),
                Size = new System.Drawing.Size(130, 30)
            };
            _autoScanButton.Click += AutoScanButton_Click;

            // Input method selector
            _methodLabel = new Label
            {
                Text = "Input Method:",
                Location = new System.Drawing.Point(10, 185),
                Size = new System.Drawing.Size(80, 20)
            };

            _methodComboBox = new ComboBox
            {
                Location = new System.Drawing.Point(95, 182),
                Size = new System.Drawing.Size(120, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _methodComboBox.Items.AddRange(Enum.GetNames(typeof(InputMethod)));
            _methodComboBox.SelectedItem = "KeyboardEventOptimized"; // Set optimized as default
            _methodComboBox.SelectedIndexChanged += MethodComboBox_SelectedIndexChanged;

            _testAllMethodsButton = new Button
            {
                Text = "Test All Methods",
                Location = new System.Drawing.Point(225, 180),
                Size = new System.Drawing.Size(100, 25),
                Font = new System.Drawing.Font("Microsoft Sans Serif", 8F)
            };
            _testAllMethodsButton.Click += TestAllMethodsButton_Click;

            // Broadcast mode
            _broadcastModeCheckBox = new CheckBox
            {
                Text = "Broadcast Mode (Listen 1-9)",
                Location = new System.Drawing.Point(340, 182),
                Size = new System.Drawing.Size(150, 25),
                Font = new System.Drawing.Font("Microsoft Sans Serif", 8F)
            };
            _broadcastModeCheckBox.CheckedChanged += BroadcastModeCheckBox_CheckedChanged;

            // Mouse mirroring mode
            _mouseMirroringCheckBox = new CheckBox
            {
                Text = "Mouse Mirroring (Ctrl+Click)",
                Location = new System.Drawing.Point(340, 210),
                Size = new System.Drawing.Size(150, 25),
                Font = new System.Drawing.Font("Microsoft Sans Serif", 8F)
            };
            _mouseMirroringCheckBox.CheckedChanged += MouseMirroringCheckBox_CheckedChanged;

            // Shift double-click mode
            _shiftDoubleClickCheckBox = new CheckBox
            {
                Text = "Shift+Click Double-Click",
                Location = new System.Drawing.Point(340, 238),
                Size = new System.Drawing.Size(150, 25),
                Font = new System.Drawing.Font("Microsoft Sans Serif", 8F)
            };
            _shiftDoubleClickCheckBox.CheckedChanged += ShiftDoubleClickCheckBox_CheckedChanged;

            // Test controls
            var testLabel = new Label
            {
                Text = "Test Controls:",
                Location = new System.Drawing.Point(10, 273),
                Size = new System.Drawing.Size(200, 20)
            };

            _sendQButton = new Button
            {
                Text = "Send Q to All",
                Location = new System.Drawing.Point(10, 298),
                Size = new System.Drawing.Size(100, 30)
            };
            _sendQButton.Click += SendQButton_Click;

            // Class table panel
            var classTableLabel = new Label
            {
                Text = "Assigned Classes:",
                Location = new System.Drawing.Point(10, 330),
                Size = new System.Drawing.Size(200, 20)
            };
            
            _classTablePanel = new Panel
            {
                Location = new System.Drawing.Point(10, 355),
                Size = new System.Drawing.Size(710, 200), // Increased initial height
                BorderStyle = BorderStyle.Fixed3D,
                AutoScroll = false, // Disable scroll, let it expand
                BackColor = System.Drawing.Color.WhiteSmoke,
                AutoSize = false // We'll manage size manually
            };
            
            // Enable double buffering to reduce flickering
            typeof(Panel).InvokeMember("DoubleBuffered",
                System.Reflection.BindingFlags.SetProperty | 
                System.Reflection.BindingFlags.Instance | 
                System.Reflection.BindingFlags.NonPublic,
                null, _classTablePanel, new object[] { true });
            
            // Status
            _statusLabel = new Label
            {
                Text = "Status: Ready. Use Ctrl+Shift+1-9/0 to register windows.",
                Location = new System.Drawing.Point(10, 485),
                Size = new System.Drawing.Size(710, 100),
                BorderStyle = BorderStyle.Fixed3D
            };

            // Instructions
            var instructionsLabel = new Label
            {
                Text = "Note: KeyboardEventOptimized is recommended for most games.\nIt minimizes window flickering and supports proper movement.",
                Location = new System.Drawing.Point(10, 595),
                Size = new System.Drawing.Size(710, 40),
                ForeColor = System.Drawing.Color.DarkGreen
            };

            // Add controls to form
            Controls.Add(windowLabel);
            Controls.Add(_windowPanel);
            Controls.Add(_refreshButton);
            Controls.Add(_autoScanButton);
            Controls.Add(_methodLabel);
            Controls.Add(_methodComboBox);
            Controls.Add(_testAllMethodsButton);
            Controls.Add(_broadcastModeCheckBox);
            Controls.Add(_mouseMirroringCheckBox);
            Controls.Add(_shiftDoubleClickCheckBox);
            Controls.Add(testLabel);
            Controls.Add(_sendQButton);
            Controls.Add(classTableLabel);
            Controls.Add(_classTablePanel);
            Controls.Add(_statusLabel);
            Controls.Add(instructionsLabel);
        }

        private void SetupHotkeys()
        {
            // Ctrl+Shift+1-9/0 for window registration
            _hotkeyManager.RegisterHotkey(Keys.D1, () => RegisterWindow(1));
            _hotkeyManager.RegisterHotkey(Keys.D2, () => RegisterWindow(2));
            _hotkeyManager.RegisterHotkey(Keys.D3, () => RegisterWindow(3));
            _hotkeyManager.RegisterHotkey(Keys.D4, () => RegisterWindow(4));
            _hotkeyManager.RegisterHotkey(Keys.D5, () => RegisterWindow(5));
            _hotkeyManager.RegisterHotkey(Keys.D6, () => RegisterWindow(6));
            _hotkeyManager.RegisterHotkey(Keys.D7, () => RegisterWindow(7));
            _hotkeyManager.RegisterHotkey(Keys.D8, () => RegisterWindow(8));
            _hotkeyManager.RegisterHotkey(Keys.D9, () => RegisterWindow(9));
            _hotkeyManager.RegisterHotkey(Keys.D0, () => RegisterWindow(10));
            
        }

        private void RegisterWindow(int slot)
        {
            var activeWindow = _windowManager.GetActiveWindow();
            if (activeWindow != null)
            {
                activeWindow.RegistrationSlot = slot;
                _registeredWindows[slot] = activeWindow;
                UpdateWindowList();
                UpdateStatus($"Window registered to slot {slot}: PID {activeWindow.ProcessId}");
            }
            else
            {
                UpdateStatus("No valid ElementClient window is currently active.");
            }
        }

        private void RefreshButton_Click(object? sender, EventArgs e)
        {
            UpdateWindowList();
            UpdateStatus("Window list refreshed.");
        }

        private void AutoScanButton_Click(object? sender, EventArgs e)
        {
            try
            {
                var availableWindows = _windowManager.EnumerateGameWindows().ToList();
                int assignedCount = 0;
                
                // Clear existing registrations
                _registeredWindows.Clear();
                
                // Auto-assign windows to slots 1-10
                for (int i = 0; i < Math.Min(availableWindows.Count, 10); i++)
                {
                    var window = availableWindows[i];
                    window.RegistrationSlot = i + 1;
                    window.IsActive = true;
                    window.RegisteredAt = DateTime.Now;
                    _registeredWindows[i + 1] = window;
                    assignedCount++;
                }
                
                UpdateWindowList();
                UpdateStatus($"Auto-scan completed. Assigned {assignedCount} ElementClient windows to slots 1-{assignedCount}.");
            }
            catch (Exception ex)
            {
                UpdateStatus($"Auto-scan failed: {ex.Message}");
            }
        }

        private void MethodComboBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_methodComboBox.SelectedItem != null)
            {
                _inputSimulator.CurrentMethod = (InputMethod)Enum.Parse(typeof(InputMethod), _methodComboBox.SelectedItem.ToString()!);
                UpdateStatus($"Input method changed to: {_inputSimulator.CurrentMethod}");
            }
        }

        private void TestAllMethodsButton_Click(object? sender, EventArgs e)
        {
            var windows = _registeredWindows.Values.Where(w => w.IsActive).ToList();
            if (windows.Count == 0)
            {
                UpdateStatus("No active windows to test.");
                return;
            }

            var testWindow = windows.First();
            UpdateStatus("Testing all input methods on first registered window...");
            
            if (_inputSimulator.TryAllMethods(testWindow.WindowHandle, VirtualKeyCode.VK_Q))
            {
                UpdateStatus("Found working method! Check debug output for details.");
            }
            else
            {
                UpdateStatus("No method worked. Game might require special handling.");
            }
        }

        private void SendQButton_Click(object? sender, EventArgs e)
        {
            var windows = _registeredWindows.Values.Where(w => w.IsActive).ToList();
            var method = _inputSimulator.CurrentMethod;
            
            _inputSimulator.BroadcastToAll(windows, hwnd => 
                _inputSimulator.SendKeyPress(hwnd, VirtualKeyCode.VK_Q, method));
            
            UpdateStatus($"Sent Q key to {windows.Count} windows using {method} method.");
        }


        private void UpdateWindowList()
        {
            for (int i = 1; i <= 10; i++)
            {
                var labelIndex = i - 1;
                var label = _windowLabels[labelIndex];
                var testButton = _testButtons[labelIndex];
                var classDropdown = _classDropdowns[labelIndex];
                var mainCheckbox = _mainCheckboxes[labelIndex];
                
                if (_registeredWindows.TryGetValue(i, out var window))
                {
                    // Validate window is still active
                    if (_windowManager.ValidateWindow(window.WindowHandle))
                    {
                        label.Text = window.ToString();
                        label.ForeColor = System.Drawing.Color.Green;
                        testButton.Enabled = true;
                        classDropdown.Enabled = true;
                        mainCheckbox.Enabled = true;
                        
                        // Update dropdown selection to match window's class
                        classDropdown.SelectedItem = window.CharacterClass.ToString();
                        
                        // Update main checkbox
                        mainCheckbox.Checked = window.IsMainWindow;
                    }
                    else
                    {
                        window.IsActive = false;
                        label.Text = $"[{i}] [INACTIVE] - Window closed";
                        label.ForeColor = System.Drawing.Color.Red;
                        testButton.Enabled = false;
                        classDropdown.Enabled = false;
                        classDropdown.SelectedItem = GameClass.None.ToString();
                        mainCheckbox.Enabled = false;
                        mainCheckbox.Checked = false;
                    }
                }
                else
                {
                    label.Text = $"[{i}] [Empty Slot]";
                    label.ForeColor = System.Drawing.Color.Gray;
                    testButton.Enabled = false;
                    classDropdown.Enabled = false;
                    classDropdown.SelectedItem = GameClass.None.ToString();
                    mainCheckbox.Enabled = false;
                    mainCheckbox.Checked = false;
                }
            }
            
            UpdateClassTable(); // Update the class table whenever window list changes
        }

        private void ClassDropdown_SelectedIndexChanged(int slotNumber, ComboBox dropdown)
        {
            if (_registeredWindows.TryGetValue(slotNumber, out var window))
            {
                if (Enum.TryParse<GameClass>(dropdown.SelectedItem?.ToString(), out var selectedClass))
                {
                    window.CharacterClass = selectedClass;
                    UpdateWindowList(); // Refresh display to show class change
                    UpdateClassTable(); // Update the class table
                    UpdateStatus($"Set window {slotNumber} class to {selectedClass}.");
                }
            }
        }
        
        private void MainCheckbox_CheckedChanged(int slotNumber, CheckBox checkbox)
        {
            if (checkbox.Checked)
            {
                // Uncheck all other main checkboxes
                foreach (var kvp in _registeredWindows)
                {
                    if (kvp.Key != slotNumber)
                    {
                        kvp.Value.IsMainWindow = false;
                    }
                }
                
                // Update all checkboxes
                for (int i = 0; i < _mainCheckboxes.Count; i++)
                {
                    if (i != slotNumber - 1)
                    {
                        _mainCheckboxes[i].Checked = false;
                    }
                }
                
                // Set this window as main
                if (_registeredWindows.TryGetValue(slotNumber, out var window))
                {
                    window.IsMainWindow = true;
                    UpdateStatus($"Set window {slotNumber} as main window.");
                }
            }
            else
            {
                // Unchecking main window
                if (_registeredWindows.TryGetValue(slotNumber, out var window))
                {
                    window.IsMainWindow = false;
                }
            }
            
            UpdateWindowList(); // Refresh display to show main status
        }

        private void TestConnection_Click(int slotNumber)
        {
            if (_registeredWindows.TryGetValue(slotNumber, out var window) && window.IsActive)
            {
                try
                {
                    // Bring the window to focus to test connection
                    SetForegroundWindow(window.WindowHandle);
                    UpdateStatus($"Brought window {slotNumber} (PID: {window.ProcessId}) to focus for testing.");
                }
                catch (Exception ex)
                {
                    UpdateStatus($"Failed to bring window {slotNumber} to focus: {ex.Message}");
                }
            }
            else
            {
                UpdateStatus($"No active window registered in slot {slotNumber}.");
            }
        }

        private void UpdateClassTable()
        {
            // Throttle updates to prevent excessive redraws (max 10 per second)
            var now = DateTime.Now;
            if ((now - _lastTableUpdate).TotalMilliseconds < 100)
            {
                return;
            }
            _lastTableUpdate = now;
            
            // Suspend layout to prevent flickering
            _classTablePanel.SuspendLayout();
            
            try
            {
                UpdateClassTableInternal();
            }
            finally
            {
                _classTablePanel.ResumeLayout(true);
            }
        }
        
        private void UpdateClassTableInternal()
        {
            // Clear existing controls
            _classTablePanel.Controls.Clear();
            
            // Create list to batch add controls for better performance
            var controlsToAdd = new List<Control>();
            
            // Get windows with assigned classes (not None) and sort by slot
            var classWindows = _registeredWindows
                .Where(kvp => kvp.Value.IsActive && kvp.Value.CharacterClass != GameClass.None)
                .OrderBy(kvp => kvp.Key)
                .ToList();
            
            if (classWindows.Count == 0)
            {
                var noClassLabel = new Label
                {
                    Text = "No windows with assigned classes",
                    Location = new System.Drawing.Point(10, 10),
                    Size = new System.Drawing.Size(300, 20),
                    ForeColor = System.Drawing.Color.Gray
                };
                _classTablePanel.Controls.Add(noClassLabel);
                return;
            }
            
            // Track class counts for duplicates
            var classCount = new Dictionary<GameClass, int>();
            var classNumbers = new Dictionary<GameWindow, int>();
            
            // First pass - count classes
            foreach (var kvp in classWindows)
            {
                var gameClass = kvp.Value.CharacterClass;
                if (!classCount.ContainsKey(gameClass))
                    classCount[gameClass] = 0;
                classCount[gameClass]++;
                classNumbers[kvp.Value] = classCount[gameClass];
            }
            
            // Reset counts for second pass
            classCount.Clear();
            
            // Create rows with new 4-column layout
            int currentY = 10; // Starting Y position
            foreach (var kvp in classWindows)
            {
                var window = kvp.Value;
                var gameClass = window.CharacterClass;
                
                // Increment class count
                if (!classCount.ContainsKey(gameClass))
                    classCount[gameClass] = 0;
                classCount[gameClass]++;
                
                // Calculate row height needed for this window
                var allActions = GetActionsForClass(window.CharacterClass);
                var classSpecificActions = allActions.Where(a => a != GameActions.AutoAttack).ToList();
                int spellRows = Math.Max(1, (int)Math.Ceiling(classSpecificActions.Count / 3.0));
                int rowHeight = Math.Max(40, spellRows * 35 + 15); // Minimum 40px, 35px per spell row + 15px padding
                
                // Column 1: Class Name
                var className = gameClass.ToString();
                if (classNumbers[window] > 1)
                {
                    className += $" {classCount[gameClass]}";
                }
                
                var classLabel = new Label
                {
                    Text = className,
                    Location = new System.Drawing.Point(10, currentY + 5),
                    Size = new System.Drawing.Size(80, 20),
                    Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold),
                    TextAlign = System.Drawing.ContentAlignment.MiddleLeft
                };
                controlsToAdd.Add(classLabel);
                
                // Column 2: AutoAttack Button
                CreateActionButton(window, GameActions.AutoAttack, new System.Drawing.Point(100, currentY + 3), controlsToAdd);
                
                // Column 3: Class-Specific Spells Grid (3 columns wide) - filter by form
                var availableSpells = FilterSpellsByForm(window, classSpecificActions);
                var filteredSpellRows = Math.Max(1, (int)Math.Ceiling(availableSpells.Count / 3.0));
                CreateSpellGrid(window, availableSpells, new System.Drawing.Point(180, currentY + 3), filteredSpellRows, controlsToAdd);
                
                // Column 4: TP Out Button (rightmost)
                CreateActionButton(window, GameActions.TpOut, new System.Drawing.Point(580, currentY + 3), controlsToAdd);
                
                // Move to next row position
                currentY += rowHeight;
            }
            
            // Batch add all controls at once for better performance
            if (controlsToAdd.Count > 0)
            {
                _classTablePanel.Controls.AddRange(controlsToAdd.ToArray());
            }
            
            // Resize the table panel to fit all content
            int tableHeight = Math.Max(50, currentY + 20); // Minimum 50px, current position + 20px padding
            _classTablePanel.Size = new System.Drawing.Size(710, tableHeight);
            
            // Adjust status label position below the table
            _statusLabel.Location = new System.Drawing.Point(10, _classTablePanel.Location.Y + tableHeight + 10);
        }
        
        private void CreateActionButton(GameWindow window, GameAction action, System.Drawing.Point location, List<Control>? controlsToAdd = null)
        {
            var button = new Button
            {
                Text = action.DisplayName,
                Location = location,
                Size = new System.Drawing.Size(70, 28), // Increased size for consistency
                Tag = new Tuple<GameWindow, GameAction>(window, action),
                Font = new System.Drawing.Font("Microsoft Sans Serif", 8F), // Increased font size
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                FlatStyle = FlatStyle.Standard
            };
            button.Click += ActionButton_Click;
            
            // Check if button should be disabled due to cooldown
            if (_cooldownManager.IsOnCooldown(window, action))
            {
                button.Enabled = false;
            }
            
            // Add button to panel or batch list
            if (controlsToAdd != null)
            {
                controlsToAdd.Add(button);
            }
            else
            {
                _classTablePanel.Controls.Add(button);
            }
            
            // Only create cooldown label for actions that have cooldowns
            if (action.Cooldown > TimeSpan.Zero)
            {
                var cooldownLabel = new Label
                {
                    Text = _cooldownManager.GetCooldownDisplay(window, action),
                    Location = new System.Drawing.Point(location.X + 75, location.Y + 4), // Adjusted for new button size
                    Size = new System.Drawing.Size(40, 20),
                    Font = new System.Drawing.Font("Microsoft Sans Serif", 7F), // Increased font size
                    ForeColor = System.Drawing.Color.Red,
                    TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                    Tag = new Tuple<GameWindow, GameAction>(window, action)
                };
                
                if (controlsToAdd != null)
                {
                    controlsToAdd.Add(cooldownLabel);
                }
                else
                {
                    _classTablePanel.Controls.Add(cooldownLabel);
                }
            }
        }
        
        private void CreateSpellGrid(GameWindow window, List<GameAction> spells, System.Drawing.Point startLocation, int rows, List<Control>? controlsToAdd = null)
        {
            const int buttonWidth = 70; // Increased from 60 to 70 for better text display
            const int buttonHeight = 28;
            const int cooldownWidth = 40;
            const int columnSpacing = buttonWidth + cooldownWidth + 8; // Button + cooldown + gap
            const int rowSpacing = 35;
            
            for (int i = 0; i < spells.Count; i++)
            {
                int col = i % 3; // 3 columns
                int row = i / 3; // Calculate row
                
                var spell = spells[i];
                var buttonLocation = new System.Drawing.Point(
                    startLocation.X + (col * columnSpacing),
                    startLocation.Y + (row * rowSpacing)
                );
                
                // Create spell button
                var button = new Button
                {
                    Text = spell.DisplayName,
                    Location = buttonLocation,
                    Size = new System.Drawing.Size(buttonWidth, buttonHeight),
                    Tag = new Tuple<GameWindow, GameAction>(window, spell),
                    Font = new System.Drawing.Font("Microsoft Sans Serif", 8F), // Increased from 6F to 8F
                    TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                    FlatStyle = FlatStyle.Standard // Ensure proper text rendering
                };
                
                // Apply special styling for form transformation buttons
                if (spell.IsFormTransformation)
                {
                    if (window.IsInAnimalForm)
                    {
                        button.BackColor = System.Drawing.Color.Yellow; // Yellow = Animal form
                        button.ForeColor = System.Drawing.Color.Black;
                    }
                    else
                    {
                        button.BackColor = System.Drawing.SystemColors.Control; // Default = Human form
                        button.ForeColor = System.Drawing.Color.Black;
                    }
                }
                button.Click += ActionButton_Click;
                
                // Check if button should be disabled due to cooldown
                if (_cooldownManager.IsOnCooldown(window, spell))
                {
                    button.Enabled = false;
                }
                
                // Add button to panel or batch list
                if (controlsToAdd != null)
                {
                    controlsToAdd.Add(button);
                }
                else
                {
                    _classTablePanel.Controls.Add(button);
                }
                
                // Create cooldown label for spell
                var cooldownLabel = new Label
                {
                    Text = _cooldownManager.GetCooldownDisplay(window, spell),
                    Location = new System.Drawing.Point(buttonLocation.X + buttonWidth + 2, buttonLocation.Y + 4),
                    Size = new System.Drawing.Size(cooldownWidth, 20),
                    Font = new System.Drawing.Font("Microsoft Sans Serif", 7F), // Increased from 6F to 7F
                    ForeColor = System.Drawing.Color.Red,
                    TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                    Tag = new Tuple<GameWindow, GameAction>(window, spell)
                };
                
                if (controlsToAdd != null)
                {
                    controlsToAdd.Add(cooldownLabel);
                }
                else
                {
                    _classTablePanel.Controls.Add(cooldownLabel);
                }
            }
        }
        
        private void ActionButton_Click(object? sender, EventArgs e)
        {
            if (sender is Button button && button.Tag is Tuple<GameWindow, GameAction> data)
            {
                var window = data.Item1;
                var action = data.Item2;
                
                // Check if on cooldown
                if (_cooldownManager.IsOnCooldown(window, action))
                {
                    var remaining = _cooldownManager.GetCooldownDisplay(window, action);
                    UpdateStatus($"{action.DisplayName} is on cooldown for {window.CharacterClass}: {remaining} remaining.");
                    return;
                }
                
                // Execute the action
                if (action == GameActions.AutoAttack)
                {
                    ExecuteAutoAttack(window);
                }
                else if (action == GameActions.TpOut)
                {
                    ExecuteTpOut(window);
                }
                else if (action == GameActions.ShamanStun)
                {
                    ExecuteShamanStun(window);
                }
                else if (action == GameActions.ShamanImmunity)
                {
                    ExecuteShamanImmunity(window);
                }
                else if (action == GameActions.ShamanHeal)
                {
                    ExecuteShamanHeal(window);
                }
                else if (action == GameActions.ShamanDetarget)
                {
                    ExecuteShamanDetarget(window);
                }
                else if (action == GameActions.DruidForm)
                {
                    ExecuteFormTransformation(window, action);
                }
                else if (action == GameActions.TankForm)
                {
                    ExecuteFormTransformation(window, action);
                }
                else if (action == GameActions.PriestForm)
                {
                    ExecuteFormTransformation(window, action);
                }
                else if (action == GameActions.PriestBeam)
                {
                    ExecutePriestBeam(window);
                }
                else if (action == GameActions.PriestSeal)
                {
                    ExecutePriestSeal(window);
                }
                else if (action == GameActions.PriestSleep)
                {
                    ExecutePriestSleep(window);
                }
                else if (action == GameActions.PriestDebuff)
                {
                    ExecutePriestDebuff(window);
                }
                else if (action == GameActions.PriestHeal)
                {
                    ExecutePriestHeal(window);
                }
                else if (action == GameActions.TankStun)
                {
                    ExecuteTankStun(window);
                }
                else if (action == GameActions.TankAgro)
                {
                    ExecuteTankAgro(window);
                }
                else if (action == GameActions.TankChi)
                {
                    ExecuteTankChi(window);
                }
                else if (action == GameActions.DruidWound)
                {
                    ExecuteDruidWound(window);
                }
                else if (action == GameActions.DruidClear)
                {
                    ExecuteDruidClear(window);
                }
                else if (action == GameActions.DruidParasitAnimal)
                {
                    ExecuteDruidParasitAnimal(window);
                }
                else if (action == GameActions.DruidParasitHuman)
                {
                    ExecuteDruidParasitHuman(window);
                }
                else if (action == GameActions.DruidStun)
                {
                    ExecuteDruidStun(window);
                }
                else if (action == GameActions.DruidSara)
                {
                    ExecuteDruidSara(window);
                }
                else if (action == GameActions.SeekerSeal)
                {
                    ExecuteSeekerSeal(window);
                }
                else if (action == GameActions.SeekerSpark)
                {
                    ExecuteSeekerSpark(window);
                }
                else if (action == GameActions.SeekerStun)
                {
                    ExecuteSeekerStun(window);
                }
                else if (action == GameActions.SeekerRefresh)
                {
                    ExecuteSeekerRefresh(window);
                }
                else if (action == GameActions.SeekerDisarm)
                {
                    ExecuteSeekerDisarm(window);
                }
                else if (action == GameActions.AssassinOtvod)
                {
                    ExecuteAssassinOtvod(window);
                }
                else if (action == GameActions.AssassinStun)
                {
                    ExecuteAssassinStun(window);
                }
                else if (action == GameActions.AssassinPrison)
                {
                    ExecuteAssassinPrison(window);
                }
                else if (action == GameActions.AssassinSalo)
                {
                    ExecuteAssassinSalo(window);
                }
                else if (action == GameActions.AssassinSleep)
                {
                    ExecuteAssassinSleep(window);
                }
                else if (action == GameActions.AssassinChi)
                {
                    ExecuteAssassinChi(window);
                }
                else if (action == GameActions.AssassinObman)
                {
                    ExecuteAssassinObman(window);
                }
                else if (action == GameActions.AssassinPtp)
                {
                    ExecuteAssassinPtp(window);
                }
                else if (action == GameActions.WarriorDraki)
                {
                    ExecuteWarriorDraki(window);
                }
                else if (action == GameActions.WarriorParal)
                {
                    ExecuteWarriorParal(window);
                }
                else if (action == GameActions.WarriorStun)
                {
                    ExecuteWarriorStun(window);
                }
                else if (action == GameActions.WarriorDisarm)
                {
                    ExecuteWarriorDisarm(window);
                }
                else if (action == GameActions.WarriorAlmaz)
                {
                    ExecuteWarriorAlmaz(window);
                }
                else if (action == GameActions.MageDebuf)
                {
                    ExecuteMageDebuf(window);
                }
                else if (action == GameActions.MageSalo)
                {
                    ExecuteMageSalo(window);
                }
                else if (action == GameActions.MageStun)
                {
                    ExecuteMageStun(window);
                }
                else if (action == GameActions.MagePoison)
                {
                    ExecuteMagePoison(window);
                }
                else if (action == GameActions.ArcherStun)
                {
                    ExecuteArcherStun(window);
                }
                else if (action == GameActions.ArcherParal)
                {
                    ExecuteArcherParal(window);
                }
                else if (action == GameActions.ArcherBlood)
                {
                    ExecuteArcherBlood(window);
                }
                else if (action == GameActions.ArcherRoscol)
                {
                    ExecuteArcherRoscol(window);
                }
                
                // Start cooldown
                _cooldownManager.StartCooldown(window, action);
                
                // Update UI immediately
                UpdateClassTable();
            }
        }
        
        private void ExecuteSpellSafely(GameWindow window, string spellName, Action spellAction)
        {
            // Find main window
            var mainWindow = _registeredWindows.Values.FirstOrDefault(w => w.IsMainWindow && w.IsActive);
            
            // Temporarily disable global key listener to prevent broadcast interference
            _keyboardHook?.StopListening();
            
            try
            {
                // Focus target window
                SetForegroundWindow(window.WindowHandle);
                System.Threading.Thread.Sleep(50); // Delay after window activation
                
                // Execute the spell action
                spellAction();
                
                // Return to main window if exists
                if (mainWindow != null && mainWindow != window)
                {
                    System.Threading.Thread.Sleep(50);
                    SetForegroundWindow(mainWindow.WindowHandle);
                    UpdateStatus($"Executed {spellName} for {window.CharacterClass} and returned to main window.");
                }
                else
                {
                    UpdateStatus($"Executed {spellName} for {window.CharacterClass}.");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Failed to execute {spellName}: {ex.Message}");
            }
            finally
            {
                // Re-enable global key listener if broadcast mode is still on
                if (_broadcastMode)
                {
                    _keyboardHook?.StartListening();
                }
            }
        }
        
        private void ExecuteAutoAttack(GameWindow window)
        {
            ExecuteSpellSafely(window, "AUTO ATTACK", () =>
            {
                // Press 1, wait 50ms, then press 3
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_1, _inputSimulator.CurrentMethod);
                System.Threading.Thread.Sleep(50);
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_3, _inputSimulator.CurrentMethod);
            });
        }
        
        private void ExecuteTpOut(GameWindow window)
        {
            // Find main window
            var mainWindow = _registeredWindows.Values.FirstOrDefault(w => w.IsMainWindow && w.IsActive);
            
            try
            {
                // Focus target window
                SetForegroundWindow(window.WindowHandle);
                System.Threading.Thread.Sleep(50); // Delay after window activation
                
                // Press F8
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_F8, _inputSimulator.CurrentMethod);
                
                // Return to main window if exists
                if (mainWindow != null && mainWindow != window)
                {
                    System.Threading.Thread.Sleep(50);
                    SetForegroundWindow(mainWindow.WindowHandle);
                    UpdateStatus($"Executed TP Out for {window.CharacterClass} and returned to main window.");
                }
                else
                {
                    UpdateStatus($"Executed TP Out for {window.CharacterClass}.");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Failed to execute TP Out: {ex.Message}");
            }
        }
        
        private void ExecuteShamanStun(GameWindow window)
        {
            // Find main window
            var mainWindow = _registeredWindows.Values.FirstOrDefault(w => w.IsMainWindow && w.IsActive);
            
            try
            {
                // Focus target window
                SetForegroundWindow(window.WindowHandle);
                System.Threading.Thread.Sleep(50); // Delay after window activation
                
                // Press 1, wait 50ms, then press F7
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_1, _inputSimulator.CurrentMethod);
                System.Threading.Thread.Sleep(50);
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_F7, _inputSimulator.CurrentMethod);
                
                // Return to main window if exists
                if (mainWindow != null && mainWindow != window)
                {
                    System.Threading.Thread.Sleep(50);
                    SetForegroundWindow(mainWindow.WindowHandle);
                    UpdateStatus($"Executed STUN for {window.CharacterClass} and returned to main window.");
                }
                else
                {
                    UpdateStatus($"Executed STUN for {window.CharacterClass}.");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Failed to execute STUN: {ex.Message}");
            }
        }
        
        private void ExecuteShamanImmunity(GameWindow window)
        {
            // Find main window
            var mainWindow = _registeredWindows.Values.FirstOrDefault(w => w.IsMainWindow && w.IsActive);
            
            try
            {
                // Focus target window
                SetForegroundWindow(window.WindowHandle);
                System.Threading.Thread.Sleep(50); // Delay after window activation
                
                // Press F6, wait 50ms, then press 1
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_F6, _inputSimulator.CurrentMethod);
                System.Threading.Thread.Sleep(50);
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_1, _inputSimulator.CurrentMethod);
                
                // Return to main window if exists
                if (mainWindow != null && mainWindow != window)
                {
                    System.Threading.Thread.Sleep(50);
                    SetForegroundWindow(mainWindow.WindowHandle);
                    UpdateStatus($"Executed IMMUNITY for {window.CharacterClass} and returned to main window.");
                }
                else
                {
                    UpdateStatus($"Executed IMMUNITY for {window.CharacterClass}.");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Failed to execute IMMUNITY: {ex.Message}");
            }
        }
        
        private void ExecuteShamanHeal(GameWindow window)
        {
            // Find main window
            var mainWindow = _registeredWindows.Values.FirstOrDefault(w => w.IsMainWindow && w.IsActive);
            
            try
            {
                // Focus target window
                SetForegroundWindow(window.WindowHandle);
                System.Threading.Thread.Sleep(50); // Delay after window activation
                
                // Press F4
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_F4, _inputSimulator.CurrentMethod);
                System.Threading.Thread.Sleep(50);
                
                // Return to main window if exists
                if (mainWindow != null && mainWindow != window)
                {
                    System.Threading.Thread.Sleep(50);
                    SetForegroundWindow(mainWindow.WindowHandle);
                    UpdateStatus($"Executed HEAL for {window.CharacterClass} and returned to main window.");
                }
                else
                {
                    UpdateStatus($"Executed HEAL for {window.CharacterClass}.");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Failed to execute HEAL: {ex.Message}");
            }
        }
        
        private void ExecuteShamanDetarget(GameWindow window)
        {
            // Find main window
            var mainWindow = _registeredWindows.Values.FirstOrDefault(w => w.IsMainWindow && w.IsActive);
            
            try
            {
                // Focus target window
                SetForegroundWindow(window.WindowHandle);
                System.Threading.Thread.Sleep(50); // Delay after window activation
                
                // Press F5
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_F5, _inputSimulator.CurrentMethod);
                System.Threading.Thread.Sleep(50);
                
                // Return to main window if exists
                if (mainWindow != null && mainWindow != window)
                {
                    System.Threading.Thread.Sleep(50);
                    SetForegroundWindow(mainWindow.WindowHandle);
                    UpdateStatus($"Executed DETARGET for {window.CharacterClass} and returned to main window.");
                }
                else
                {
                    UpdateStatus($"Executed DETARGET for {window.CharacterClass}.");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Failed to execute DETARGET: {ex.Message}");
            }
        }
        
        private void ExecuteFormTransformation(GameWindow window, GameAction action)
        {
            // Find main window
            var mainWindow = _registeredWindows.Values.FirstOrDefault(w => w.IsMainWindow && w.IsActive);
            
            try
            {
                // Focus target window
                SetForegroundWindow(window.WindowHandle);
                System.Threading.Thread.Sleep(50); // Delay after window activation
                
                // Press F1 (all form transformations use F1)
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_F1, _inputSimulator.CurrentMethod);
                System.Threading.Thread.Sleep(50);
                
                // Toggle form state
                window.IsInAnimalForm = !window.IsInAnimalForm;
                
                // Update the class table to reflect new form state
                UpdateClassTable();
                
                // Return to main window if exists
                if (mainWindow != null && mainWindow != window)
                {
                    System.Threading.Thread.Sleep(50);
                    SetForegroundWindow(mainWindow.WindowHandle);
                    UpdateStatus($"Executed FORM TRANSFORMATION for {window.CharacterClass} - now in {(window.IsInAnimalForm ? "ANIMAL" : "HUMAN")} form and returned to main window.");
                }
                else
                {
                    UpdateStatus($"Executed FORM TRANSFORMATION for {window.CharacterClass} - now in {(window.IsInAnimalForm ? "ANIMAL" : "HUMAN")} form.");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Failed to execute FORM TRANSFORMATION: {ex.Message}");
            }
        }
        
        private void ExecutePriestBeam(GameWindow window)
        {
            ExecuteSpellSafely(window, "PRIEST BEAM", () =>
            {
                // Press 1, wait 50ms, then press F2
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_1, _inputSimulator.CurrentMethod);
                System.Threading.Thread.Sleep(50);
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_F2, _inputSimulator.CurrentMethod);
            });
        }
        
        private void ExecutePriestSeal(GameWindow window)
        {
            ExecuteSpellSafely(window, "PRIEST SEAL", () =>
            {
                // Press 1, wait 50ms, then press F3
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_1, _inputSimulator.CurrentMethod);
                System.Threading.Thread.Sleep(50);
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_F3, _inputSimulator.CurrentMethod);
            });
        }
        
        private void ExecutePriestSleep(GameWindow window)
        {
            ExecuteSpellSafely(window, "PRIEST SLEEP", () =>
            {
                // Press 1, wait 50ms, then press F4
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_1, _inputSimulator.CurrentMethod);
                System.Threading.Thread.Sleep(50);
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_F4, _inputSimulator.CurrentMethod);
            });
        }
        
        private void ExecutePriestDebuff(GameWindow window)
        {
            ExecuteSpellSafely(window, "PRIEST DEBUFF", () =>
            {
                // Press 1, wait 50ms, then press F5
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_1, _inputSimulator.CurrentMethod);
                System.Threading.Thread.Sleep(50);
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_F5, _inputSimulator.CurrentMethod);
            });
        }
        
        private void ExecutePriestHeal(GameWindow window)
        {
            ExecuteSpellSafely(window, "PRIEST HEAL", () =>
            {
                // Press Shift+1, wait 50ms, then press F6
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_1, _inputSimulator.CurrentMethod, true, false, false); // Shift + 1
                System.Threading.Thread.Sleep(50);
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_F6, _inputSimulator.CurrentMethod);
            });
        }
        
        private void ExecuteTankStun(GameWindow window)
        {
            ExecuteSpellSafely(window, "TANK STUN", () =>
            {
                // Execute Tank Stun - sequence: 1 50ms wait f2
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_1, _inputSimulator.CurrentMethod);
                System.Threading.Thread.Sleep(50);
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_F2, _inputSimulator.CurrentMethod);
            });
        }
        
        private void ExecuteTankAgro(GameWindow window)
        {
            ExecuteSpellSafely(window, "TANK AGRO", () =>
            {
                // Execute Tank Agro - sequence: 1 50ms wait f3
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_1, _inputSimulator.CurrentMethod);
                System.Threading.Thread.Sleep(50);
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_F3, _inputSimulator.CurrentMethod);
            });
        }
        
        private void ExecuteTankChi(GameWindow window)
        {
            ExecuteSpellSafely(window, "TANK CHI", () =>
            {
                // Execute Tank Chi - sequence: 50ms wait f4
                System.Threading.Thread.Sleep(50);
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_F4, _inputSimulator.CurrentMethod);
            });
        }
        
        private void ExecuteDruidWound(GameWindow window)
        {
            ExecuteSpellSafely(window, "DRUID WOUND", () =>
            {
                // Execute Druid Wound - sequence: 1 50ms wait f2
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_1, _inputSimulator.CurrentMethod);
                System.Threading.Thread.Sleep(50);
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_F2, _inputSimulator.CurrentMethod);
            });
        }
        
        private void ExecuteDruidClear(GameWindow window)
        {
            ExecuteSpellSafely(window, "DRUID CLEAR", () =>
            {
                // Execute Druid Clear - sequence: 1 50ms wait f3
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_1, _inputSimulator.CurrentMethod);
                System.Threading.Thread.Sleep(50);
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_F3, _inputSimulator.CurrentMethod);
            });
        }
        
        private void ExecuteDruidParasitAnimal(GameWindow window)
        {
            ExecuteSpellSafely(window, "DRUID PARASIT ANIMAL", () =>
            {
                // Execute Druid Parasit Animal - sequence: 1 50ms wait f4
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_1, _inputSimulator.CurrentMethod);
                System.Threading.Thread.Sleep(50);
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_F4, _inputSimulator.CurrentMethod);
            });
        }
        
        private void ExecuteDruidParasitHuman(GameWindow window)
        {
            ExecuteSpellSafely(window, "DRUID PARASIT HUMAN", () =>
            {
                // Execute Druid Parasit Human - sequence: 1 50ms wait f5
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_1, _inputSimulator.CurrentMethod);
                System.Threading.Thread.Sleep(50);
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_F5, _inputSimulator.CurrentMethod);
            });
        }
        
        private void ExecuteDruidStun(GameWindow window)
        {
            ExecuteSpellSafely(window, "DRUID STUN", () =>
            {
                // Execute Druid Stun - sequence: 1 50ms wait f6
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_1, _inputSimulator.CurrentMethod);
                System.Threading.Thread.Sleep(50);
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_F6, _inputSimulator.CurrentMethod);
            });
        }
        
        private void ExecuteDruidSara(GameWindow window)
        {
            ExecuteSpellSafely(window, "DRUID SARA", () =>
            {
                // Execute Druid Sara - sequence: 1 50ms wait f7
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_1, _inputSimulator.CurrentMethod);
                System.Threading.Thread.Sleep(50);
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_F7, _inputSimulator.CurrentMethod);
            });
        }
        
        private void ExecuteSeekerSeal(GameWindow window)
        {
            ExecuteSpellSafely(window, "SEEKER SEAL", () =>
            {
                // Execute Seeker Seal - sequence: 1 50ms wait f1
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_1, _inputSimulator.CurrentMethod);
                System.Threading.Thread.Sleep(50);
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_F1, _inputSimulator.CurrentMethod);
            });
        }
        
        private void ExecuteSeekerSpark(GameWindow window)
        {
            ExecuteSpellSafely(window, "SEEKER SPARK", () =>
            {
                // Execute Seeker Spark - sequence: 1 50ms wait f2
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_1, _inputSimulator.CurrentMethod);
                System.Threading.Thread.Sleep(50);
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_F2, _inputSimulator.CurrentMethod);
            });
        }
        
        private void ExecuteSeekerStun(GameWindow window)
        {
            ExecuteSpellSafely(window, "SEEKER STUN", () =>
            {
                // Execute Seeker Stun - sequence: 1 50ms wait f3
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_1, _inputSimulator.CurrentMethod);
                System.Threading.Thread.Sleep(50);
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_F3, _inputSimulator.CurrentMethod);
            });
        }
        
        private void ExecuteSeekerRefresh(GameWindow window)
        {
            ExecuteSpellSafely(window, "SEEKER REFRESH", () =>
            {
                // Execute Seeker Refresh - sequence: 50ms wait f4
                System.Threading.Thread.Sleep(50);
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_F4, _inputSimulator.CurrentMethod);
            });
            
            // Special behavior: Reset cooldown on SeekerStun
            _cooldownManager.ResetCooldown(window, GameActions.SeekerStun);
            
            // Update UI immediately to show unlocked Stun button
            UpdateClassTable();
        }
        
        private void ExecuteSeekerDisarm(GameWindow window)
        {
            ExecuteSpellSafely(window, "SEEKER DISARM", () =>
            {
                // Execute Seeker Disarm - sequence: 1 50ms wait f5
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_1, _inputSimulator.CurrentMethod);
                System.Threading.Thread.Sleep(50);
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_F5, _inputSimulator.CurrentMethod);
            });
        }
        
        private void ExecuteAssassinOtvod(GameWindow window)
        {
            ExecuteSpellSafely(window, "ASSASSIN OTVOD", () =>
            {
                // Execute Assassin Otvod - sequence: 1 50ms wait f1
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_1, _inputSimulator.CurrentMethod);
                System.Threading.Thread.Sleep(50);
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_F1, _inputSimulator.CurrentMethod);
            });
        }
        
        private void ExecuteAssassinStun(GameWindow window)
        {
            ExecuteSpellSafely(window, "ASSASSIN STUN", () =>
            {
                // Execute Assassin Stun - sequence: 1 50ms wait f2
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_1, _inputSimulator.CurrentMethod);
                System.Threading.Thread.Sleep(50);
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_F2, _inputSimulator.CurrentMethod);
            });
        }
        
        private void ExecuteAssassinPrison(GameWindow window)
        {
            ExecuteSpellSafely(window, "ASSASSIN PRISON", () =>
            {
                // Execute Assassin Prison - sequence: 1 50ms wait f3
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_1, _inputSimulator.CurrentMethod);
                System.Threading.Thread.Sleep(50);
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_F3, _inputSimulator.CurrentMethod);
            });
        }
        
        private void ExecuteAssassinSalo(GameWindow window)
        {
            ExecuteSpellSafely(window, "ASSASSIN SALO", () =>
            {
                // Execute Assassin Salo - sequence: 50ms wait f4
                System.Threading.Thread.Sleep(50);
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_F4, _inputSimulator.CurrentMethod);
            });
        }
        
        private void ExecuteAssassinSleep(GameWindow window)
        {
            ExecuteSpellSafely(window, "ASSASSIN SLEEP", () =>
            {
                // Execute Assassin Sleep - sequence: 1 50ms wait f5
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_1, _inputSimulator.CurrentMethod);
                System.Threading.Thread.Sleep(50);
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_F5, _inputSimulator.CurrentMethod);
            });
        }
        
        private void ExecuteAssassinChi(GameWindow window)
        {
            ExecuteSpellSafely(window, "ASSASSIN CHI", () =>
            {
                // Execute Assassin Chi - sequence: 50ms wait f6
                System.Threading.Thread.Sleep(50);
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_F6, _inputSimulator.CurrentMethod);
            });
        }
        
        private void ExecuteAssassinObman(GameWindow window)
        {
            ExecuteSpellSafely(window, "ASSASSIN OBMAN", () =>
            {
                // Execute Assassin Obman - sequence: 50ms wait f7
                System.Threading.Thread.Sleep(50);
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_F7, _inputSimulator.CurrentMethod);
            });
        }
        
        private void ExecuteAssassinPtp(GameWindow window)
        {
            ExecuteSpellSafely(window, "ASSASSIN PTP", () =>
            {
                // Execute Assassin Ptp - sequence: 50ms wait f8
                System.Threading.Thread.Sleep(50);
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_F8, _inputSimulator.CurrentMethod);
            });
        }
        
        private void ExecuteWarriorDraki(GameWindow window)
        {
            ExecuteSpellSafely(window, "WARRIOR DRAKI", () =>
            {
                // Execute Warrior Draki - sequence: 1 100ms wait f1
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_1, _inputSimulator.CurrentMethod);
                System.Threading.Thread.Sleep(100);
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_F1, _inputSimulator.CurrentMethod);
            });
        }
        
        private void ExecuteWarriorParal(GameWindow window)
        {
            ExecuteSpellSafely(window, "WARRIOR PARAL", () =>
            {
                // Execute Warrior Paral - sequence: 1 100ms wait f2
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_1, _inputSimulator.CurrentMethod);
                System.Threading.Thread.Sleep(100);
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_F2, _inputSimulator.CurrentMethod);
            });
        }
        
        private void ExecuteWarriorStun(GameWindow window)
        {
            ExecuteSpellSafely(window, "WARRIOR STUN", () =>
            {
                // Execute Warrior Stun - sequence: 1 100ms wait f3
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_1, _inputSimulator.CurrentMethod);
                System.Threading.Thread.Sleep(100);
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_F3, _inputSimulator.CurrentMethod);
            });
        }
        
        private void ExecuteWarriorDisarm(GameWindow window)
        {
            ExecuteSpellSafely(window, "WARRIOR DISARM", () =>
            {
                // Execute Warrior Disarm - sequence: 100ms wait f4
                System.Threading.Thread.Sleep(100);
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_F4, _inputSimulator.CurrentMethod);
            });
        }
        
        private void ExecuteWarriorAlmaz(GameWindow window)
        {
            ExecuteSpellSafely(window, "WARRIOR ALMAZ", () =>
            {
                // Execute Warrior Almaz - sequence: 50ms wait f5
                System.Threading.Thread.Sleep(50);
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_F5, _inputSimulator.CurrentMethod);
            });
        }
        
        private void ExecuteMageDebuf(GameWindow window)
        {
            ExecuteSpellSafely(window, "MAGE DEBUF", () =>
            {
                // Execute Mage Debuf - sequence: 1 100ms wait f1
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_1, _inputSimulator.CurrentMethod);
                System.Threading.Thread.Sleep(100);
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_F1, _inputSimulator.CurrentMethod);
            });
        }
        
        private void ExecuteMageSalo(GameWindow window)
        {
            ExecuteSpellSafely(window, "MAGE SALO", () =>
            {
                // Execute Mage Salo - sequence: 1 100ms wait f2
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_1, _inputSimulator.CurrentMethod);
                System.Threading.Thread.Sleep(100);
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_F2, _inputSimulator.CurrentMethod);
            });
        }
        
        private void ExecuteMageStun(GameWindow window)
        {
            ExecuteSpellSafely(window, "MAGE STUN", () =>
            {
                // Execute Mage Stun - sequence: 1 100ms wait f3
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_1, _inputSimulator.CurrentMethod);
                System.Threading.Thread.Sleep(100);
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_F3, _inputSimulator.CurrentMethod);
            });
        }
        
        private void ExecuteMagePoison(GameWindow window)
        {
            ExecuteSpellSafely(window, "MAGE POISON", () =>
            {
                // Execute Mage Poison - sequence: 100ms wait f4
                System.Threading.Thread.Sleep(100);
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_F4, _inputSimulator.CurrentMethod);
            });
        }
        
        private void ExecuteArcherStun(GameWindow window)
        {
            ExecuteSpellSafely(window, "ARCHER STUN", () =>
            {
                // Execute Archer Stun - sequence: 1 100ms wait f1
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_1, _inputSimulator.CurrentMethod);
                System.Threading.Thread.Sleep(100);
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_F1, _inputSimulator.CurrentMethod);
            });
        }
        
        private void ExecuteArcherParal(GameWindow window)
        {
            ExecuteSpellSafely(window, "ARCHER PARAL", () =>
            {
                // Execute Archer Paral - sequence: 1 100ms wait f2
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_1, _inputSimulator.CurrentMethod);
                System.Threading.Thread.Sleep(100);
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_F2, _inputSimulator.CurrentMethod);
            });
        }
        
        private void ExecuteArcherBlood(GameWindow window)
        {
            ExecuteSpellSafely(window, "ARCHER BLOOD", () =>
            {
                // Execute Archer Blood - sequence: 1 100ms wait f3
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_1, _inputSimulator.CurrentMethod);
                System.Threading.Thread.Sleep(100);
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_F3, _inputSimulator.CurrentMethod);
            });
        }
        
        private void ExecuteArcherRoscol(GameWindow window)
        {
            ExecuteSpellSafely(window, "ARCHER ROSCOL", () =>
            {
                // Execute Archer Roscol - sequence: 100ms wait f4
                System.Threading.Thread.Sleep(100);
                _inputSimulator.SendKeyPress(window.WindowHandle, VirtualKeyCode.VK_F4, _inputSimulator.CurrentMethod);
            });
        }
        
        private List<GameAction> GetActionsForClass(GameClass characterClass)
        {
            var actions = new List<GameAction>();
            
            // All classes get AutoAttack as first action
            actions.Add(GameActions.AutoAttack);
            
            // Class-specific actions (TP Out will be handled separately)
            switch (characterClass)
            {
                case GameClass.Shaman:
                    actions.Add(GameActions.ShamanStun);
                    actions.Add(GameActions.ShamanImmunity);
                    actions.Add(GameActions.ShamanHeal);
                    actions.Add(GameActions.ShamanDetarget);
                    break;
                case GameClass.Druid:
                    actions.Add(GameActions.DruidForm);
                    actions.Add(GameActions.DruidWound);
                    actions.Add(GameActions.DruidClear);
                    actions.Add(GameActions.DruidParasitAnimal);
                    actions.Add(GameActions.DruidParasitHuman);
                    actions.Add(GameActions.DruidStun);
                    actions.Add(GameActions.DruidSara);
                    break;
                case GameClass.Tank:
                    actions.Add(GameActions.TankForm);
                    actions.Add(GameActions.TankStun);
                    actions.Add(GameActions.TankAgro);
                    actions.Add(GameActions.TankChi);
                    break;
                case GameClass.Priest:
                    actions.Add(GameActions.PriestForm);
                    actions.Add(GameActions.PriestBeam);
                    actions.Add(GameActions.PriestSeal);
                    actions.Add(GameActions.PriestSleep);
                    actions.Add(GameActions.PriestDebuff);
                    actions.Add(GameActions.PriestHeal);
                    break;
                case GameClass.Seeker:
                    actions.Add(GameActions.SeekerSeal);
                    actions.Add(GameActions.SeekerSpark);
                    actions.Add(GameActions.SeekerStun);
                    actions.Add(GameActions.SeekerRefresh);
                    actions.Add(GameActions.SeekerDisarm);
                    break;
                case GameClass.Assassin:
                    actions.Add(GameActions.AssassinOtvod);
                    actions.Add(GameActions.AssassinStun);
                    actions.Add(GameActions.AssassinPrison);
                    actions.Add(GameActions.AssassinSalo);
                    actions.Add(GameActions.AssassinSleep);
                    actions.Add(GameActions.AssassinChi);
                    actions.Add(GameActions.AssassinObman);
                    actions.Add(GameActions.AssassinPtp);
                    break;
                case GameClass.Warrior:
                    actions.Add(GameActions.WarriorDraki);
                    actions.Add(GameActions.WarriorParal);
                    actions.Add(GameActions.WarriorStun);
                    actions.Add(GameActions.WarriorDisarm);
                    actions.Add(GameActions.WarriorAlmaz);
                    break;
                case GameClass.Mage:
                    actions.Add(GameActions.MageDebuf);
                    actions.Add(GameActions.MageSalo);
                    actions.Add(GameActions.MageStun);
                    actions.Add(GameActions.MagePoison);
                    break;
                case GameClass.Archer:
                    actions.Add(GameActions.ArcherStun);
                    actions.Add(GameActions.ArcherParal);
                    actions.Add(GameActions.ArcherBlood);
                    actions.Add(GameActions.ArcherRoscol);
                    break;
                // TODO: Add other class-specific actions here
            }
            
            return actions;
        }
        
        private List<GameAction> FilterSpellsByForm(GameWindow window, List<GameAction> spells)
        {
            var filteredSpells = new List<GameAction>();
            
            foreach (var spell in spells)
            {
                // Always show form transformation spells
                if (spell.IsFormTransformation)
                {
                    filteredSpells.Add(spell);
                    continue;
                }
                
                // Check form requirements
                if (spell.RequiresAnimalForm && !window.IsInAnimalForm)
                {
                    // Spell requires animal form but character is in human form - hide it
                    continue;
                }
                
                if (spell.RequiresHumanForm && window.IsInAnimalForm)
                {
                    // Spell requires human form but character is in animal form - hide it
                    continue;
                }
                
                // If no specific form requirement or requirements are met, show the spell
                filteredSpells.Add(spell);
            }
            
            return filteredSpells;
        }
        
        private void CooldownTimer_Tick(object? sender, EventArgs e)
        {
            // Update all cooldown labels in the class table
            foreach (Control control in _classTablePanel.Controls)
            {
                if (control is Label label && label.Tag is Tuple<GameWindow, GameAction> data)
                {
                    var window = data.Item1;
                    var action = data.Item2;
                    
                    var cooldownText = _cooldownManager.GetCooldownDisplay(window, action);
                    label.Text = cooldownText;
                    
                    // Also update button enabled state
                    var button = _classTablePanel.Controls.OfType<Button>()
                        .FirstOrDefault(b => b.Tag is Tuple<GameWindow, GameAction> btnData 
                                           && btnData.Item1 == window 
                                           && btnData.Item2 == action);
                    
                    if (button != null)
                    {
                        button.Enabled = string.IsNullOrEmpty(cooldownText);
                    }
                }
            }
        }

        private void UpdateStatus(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            _statusLabel.Text = $"[{timestamp}] Status: {message}";
        }

        private void BroadcastModeCheckBox_CheckedChanged(object? sender, EventArgs e)
        {
            _broadcastMode = _broadcastModeCheckBox.Checked;
            
            if (_broadcastMode)
            {
                // Enable global key listener for 1-9 keys
                SetupGlobalKeyListener();
            }
            else
            {
                // Disable global key listener
                RemoveGlobalKeyListener();
            }
            
            UpdateStatus($"Broadcast mode {(_broadcastMode ? "enabled" : "disabled")}. {(_broadcastMode ? "Press 1-9 to broadcast keys." : "Use Ctrl+Shift+1-9/0 to register windows.")}");
        }


        private void BroadcastKeyToAllWindows(int keyNumber)
        {
            var windows = _registeredWindows.Values.Where(w => w.IsActive).ToList();
            if (windows.Count == 0)
            {
                UpdateStatus("No active windows to broadcast to.");
                return;
            }

            VirtualKeyCode keyCode = keyNumber switch
            {
                1 => VirtualKeyCode.VK_1,
                2 => VirtualKeyCode.VK_2,
                3 => VirtualKeyCode.VK_3,
                4 => VirtualKeyCode.VK_4,
                5 => VirtualKeyCode.VK_5,
                6 => VirtualKeyCode.VK_6,
                7 => VirtualKeyCode.VK_7,
                8 => VirtualKeyCode.VK_8,
                9 => VirtualKeyCode.VK_9,
                _ => VirtualKeyCode.VK_1
            };

            // Store current foreground window
            var currentWindow = GetForegroundWindow();
            
            // Temporarily disable global key listener to prevent infinite loop
            _keyboardHook?.StopListening();
            
            try
            {
                // Use the selected method from dropdown
                var method = _inputSimulator.CurrentMethod;
                foreach (var window in windows)
                {
                    if (window.IsActive)
                    {
                        _inputSimulator.SendKeyPress(window.WindowHandle, keyCode, method);
                    }
                }
                
                // Find and return to main window
                var mainWindow = _registeredWindows.Values.FirstOrDefault(w => w.IsMainWindow && w.IsActive);
                if (mainWindow != null)
                {
                    SetForegroundWindow(mainWindow.WindowHandle);
                }
                else if (_registeredWindows.ContainsKey(1) && _registeredWindows[1].IsActive)
                {
                    // Fallback to window 1 if no main window is set
                    SetForegroundWindow(_registeredWindows[1].WindowHandle);
                }
                else
                {
                    // If no main window or window 1, return to original window
                    SetForegroundWindow(currentWindow);
                }
                
                UpdateStatus($"Broadcasted key {keyNumber} to {windows.Count} windows using {method} method.");
            }
            finally
            {
                // Re-enable global key listener if broadcast mode is still on
                if (_broadcastMode)
                {
                    _keyboardHook?.StartListening();
                }
            }
        }

        private void SetupGlobalKeyListener()
        {
            _keyboardHook?.StartListening();
        }

        private void RemoveGlobalKeyListener()
        {
            _keyboardHook?.StopListening();
        }

        private void OnGlobalKeyDown(object? sender, Keys key)
        {
            if (!_broadcastMode) return;
            
            // Only handle keys 1-9 when broadcast mode is enabled
            int keyNumber = key switch
            {
                Keys.D1 => 1,
                Keys.D2 => 2,
                Keys.D3 => 3,
                Keys.D4 => 4,
                Keys.D5 => 5,
                Keys.D6 => 6,
                Keys.D7 => 7,
                Keys.D8 => 8,
                Keys.D9 => 9,
                _ => 0
            };
            
            if (keyNumber > 0)
            {
                // Broadcast the key to all windows
                BroadcastKeyToAllWindows(keyNumber);
            }
        }

        private void MouseMirroringCheckBox_CheckedChanged(object? sender, EventArgs e)
        {
            _mouseMirroringMode = _mouseMirroringCheckBox.Checked;
            
            if (_mouseMirroringMode)
            {
                _mouseHook?.StartListening();
            }
            else
            {
                // Only stop if shift double-click is also disabled
                if (!_shiftDoubleClickMode)
                {
                    _mouseHook?.StopListening();
                }
            }
            
            UpdateStatus($"Mouse mirroring {(_mouseMirroringMode ? "enabled" : "disabled")}. {(_mouseMirroringMode ? "Ctrl+Click to mirror to all windows." : "")}");
        }

        private void ShiftDoubleClickCheckBox_CheckedChanged(object? sender, EventArgs e)
        {
            _shiftDoubleClickMode = _shiftDoubleClickCheckBox.Checked;
            
            if (_shiftDoubleClickMode)
            {
                _mouseHook?.StartListening();
            }
            else
            {
                // Only stop if mouse mirroring is also disabled
                if (!_mouseMirroringMode)
                {
                    _mouseHook?.StopListening();
                }
            }
            
            UpdateStatus($"Shift+Click double-click {(_shiftDoubleClickMode ? "enabled" : "disabled")}. {(_shiftDoubleClickMode ? "Shift+Click to send double-click to all windows." : "")}");
        }

        private void OnCtrlLeftClick(object? sender, MouseHook.MouseEventArgs e)
        {
            if (!_mouseMirroringMode) return;
            
            var windows = _registeredWindows.Values.Where(w => w.IsActive).ToList();
            if (windows.Count == 0)
            {
                UpdateStatus("No active windows to mirror mouse clicks to.");
                return;
            }

            _backgroundMouseSimulator?.BroadcastMouseClick(windows, e.X, e.Y, BackgroundMouseSimulator.MouseButton.Left);
            UpdateStatus($"Mirrored left click at ({e.X}, {e.Y}) to {windows.Count} windows.");
        }

        private void OnCtrlRightClick(object? sender, MouseHook.MouseEventArgs e)
        {
            if (!_mouseMirroringMode) return;
            
            var windows = _registeredWindows.Values.Where(w => w.IsActive).ToList();
            if (windows.Count == 0)
            {
                UpdateStatus("No active windows to mirror mouse clicks to.");
                return;
            }

            _backgroundMouseSimulator?.BroadcastMouseClick(windows, e.X, e.Y, BackgroundMouseSimulator.MouseButton.Right);
            UpdateStatus($"Mirrored right click at ({e.X}, {e.Y}) to {windows.Count} windows.");
        }

        private void OnShiftLeftClick(object? sender, MouseHook.MouseEventArgs e)
        {
            if (!_shiftDoubleClickMode) return;
            
            var windows = _registeredWindows.Values.Where(w => w.IsActive).ToList();
            if (windows.Count == 0)
            {
                UpdateStatus("No active windows to send double-click to.");
                return;
            }

            // Send proper double-click to all registered windows using the broadcast method
            _backgroundMouseSimulator?.BroadcastMouseDoubleClick(windows, e.X, e.Y, BackgroundMouseSimulator.MouseButton.Left);
            
            UpdateStatus($"Sent proper double-click at ({e.X}, {e.Y}) to {windows.Count} windows.");
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _cooldownTimer?.Stop();
            _cooldownTimer?.Dispose();
            _keyboardHook?.Dispose();
            _mouseHook?.Dispose();
            _hotkeyManager?.Dispose();
            base.OnFormClosed(e);
        }
    }
}