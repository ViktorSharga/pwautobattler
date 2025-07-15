using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using GameAutomation.Core;
using GameAutomation.Models;

namespace GameAutomation.UI
{
    public partial class MainForm : Form
    {
        private readonly WindowManager _windowManager;
        private readonly InputSimulator _inputSimulator;
        private readonly LowLevelInputSimulator _lowLevelInputSimulator;
        private readonly HotkeyManager _hotkeyManager;
        private readonly Dictionary<int, GameWindow> _registeredWindows;

        private ListBox _windowListBox = null!;
        private Button _refreshButton = null!;
        private Button _sendQButton = null!;
        private Button _send1Button = null!;
        private Button _startMovementButton = null!;
        private Button _stopMovementButton = null!;
        private Label _statusLabel = null!;
        private ComboBox _methodComboBox = null!;
        private Button _testAllMethodsButton = null!;
        private Label _methodLabel = null!;
        
        // Low-level controls
        private GroupBox _lowLevelGroupBox = null!;
        private Button _startHookButton = null!;
        private Button _stopHookButton = null!;
        private Button _startRealTimeButton = null!;
        private Button _stopRealTimeButton = null!;
        private CheckBox _enableLowLevelCheckBox = null!;
        private Label _lowLevelStatusLabel = null!;

        public MainForm()
        {
            _windowManager = new WindowManager();
            _inputSimulator = new InputSimulator();
            _lowLevelInputSimulator = new LowLevelInputSimulator();
            _hotkeyManager = new HotkeyManager();
            _registeredWindows = new Dictionary<int, GameWindow>();

            InitializeComponent();
            SetupHotkeys();
            SetupLowLevelSimulator();
        }

        private void InitializeComponent()
        {
            Text = "Game Multi-Window Controller - Low Level Edition";
            Size = new System.Drawing.Size(600, 650);
            StartPosition = FormStartPosition.CenterScreen;

            // Window list
            var windowLabel = new Label
            {
                Text = "Registered Windows:",
                Location = new System.Drawing.Point(10, 10),
                Size = new System.Drawing.Size(200, 20)
            };

            _windowListBox = new ListBox
            {
                Location = new System.Drawing.Point(10, 35),
                Size = new System.Drawing.Size(560, 100)
            };

            _refreshButton = new Button
            {
                Text = "Refresh Available Windows",
                Location = new System.Drawing.Point(10, 145),
                Size = new System.Drawing.Size(150, 30)
            };
            _refreshButton.Click += RefreshButton_Click;

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
            _methodComboBox.SelectedItem = "KeyboardEventOptimized";
            _methodComboBox.SelectedIndexChanged += MethodComboBox_SelectedIndexChanged;

            _testAllMethodsButton = new Button
            {
                Text = "Test All Methods",
                Location = new System.Drawing.Point(225, 180),
                Size = new System.Drawing.Size(100, 25),
                Font = new System.Drawing.Font("Microsoft Sans Serif", 8F)
            };
            _testAllMethodsButton.Click += TestAllMethodsButton_Click;

            // Test controls
            var testLabel = new Label
            {
                Text = "Standard Test Controls:",
                Location = new System.Drawing.Point(10, 215),
                Size = new System.Drawing.Size(200, 20)
            };

            _sendQButton = new Button
            {
                Text = "Send Q to All",
                Location = new System.Drawing.Point(10, 240),
                Size = new System.Drawing.Size(100, 30)
            };
            _sendQButton.Click += SendQButton_Click;

            _send1Button = new Button
            {
                Text = "Send 1 to All",
                Location = new System.Drawing.Point(120, 240),
                Size = new System.Drawing.Size(100, 30)
            };
            _send1Button.Click += Send1Button_Click;

            _startMovementButton = new Button
            {
                Text = "Start Movement",
                Location = new System.Drawing.Point(10, 280),
                Size = new System.Drawing.Size(100, 30)
            };
            _startMovementButton.Click += StartMovementButton_Click;

            _stopMovementButton = new Button
            {
                Text = "Stop Movement",
                Location = new System.Drawing.Point(120, 280),
                Size = new System.Drawing.Size(100, 30)
            };
            _stopMovementButton.Click += StopMovementButton_Click;

            // Low-level controls group
            _lowLevelGroupBox = new GroupBox
            {
                Text = "Low-Level Input Simulation",
                Location = new System.Drawing.Point(10, 320),
                Size = new System.Drawing.Size(560, 120),
                ForeColor = System.Drawing.Color.DarkRed
            };

            _enableLowLevelCheckBox = new CheckBox
            {
                Text = "Enable Low-Level Mode (Real-time WASD broadcasting)",
                Location = new System.Drawing.Point(10, 20),
                Size = new System.Drawing.Size(350, 20),
                ForeColor = System.Drawing.Color.DarkRed
            };
            _enableLowLevelCheckBox.CheckedChanged += EnableLowLevelCheckBox_CheckedChanged;

            _startHookButton = new Button
            {
                Text = "Start Keyboard Hook",
                Location = new System.Drawing.Point(10, 50),
                Size = new System.Drawing.Size(120, 30),
                Enabled = false
            };
            _startHookButton.Click += StartHookButton_Click;

            _stopHookButton = new Button
            {
                Text = "Stop Keyboard Hook",
                Location = new System.Drawing.Point(140, 50),
                Size = new System.Drawing.Size(120, 30),
                Enabled = false
            };
            _stopHookButton.Click += StopHookButton_Click;

            _startRealTimeButton = new Button
            {
                Text = "Start Real-Time",
                Location = new System.Drawing.Point(270, 50),
                Size = new System.Drawing.Size(100, 30),
                Enabled = false,
                BackColor = System.Drawing.Color.LightGreen
            };
            _startRealTimeButton.Click += StartRealTimeButton_Click;

            _stopRealTimeButton = new Button
            {
                Text = "Stop Real-Time",
                Location = new System.Drawing.Point(380, 50),
                Size = new System.Drawing.Size(100, 30),
                Enabled = false,
                BackColor = System.Drawing.Color.LightCoral
            };
            _stopRealTimeButton.Click += StopRealTimeButton_Click;

            _lowLevelStatusLabel = new Label
            {
                Text = "Low-level mode disabled",
                Location = new System.Drawing.Point(10, 85),
                Size = new System.Drawing.Size(540, 20),
                ForeColor = System.Drawing.Color.Gray
            };

            _lowLevelGroupBox.Controls.Add(_enableLowLevelCheckBox);
            _lowLevelGroupBox.Controls.Add(_startHookButton);
            _lowLevelGroupBox.Controls.Add(_stopHookButton);
            _lowLevelGroupBox.Controls.Add(_startRealTimeButton);
            _lowLevelGroupBox.Controls.Add(_stopRealTimeButton);
            _lowLevelGroupBox.Controls.Add(_lowLevelStatusLabel);

            // Status
            _statusLabel = new Label
            {
                Text = "Status: Ready. Use Ctrl+Shift+1/2/3 to register windows.",
                Location = new System.Drawing.Point(10, 450),
                Size = new System.Drawing.Size(560, 80),
                BorderStyle = BorderStyle.Fixed3D
            };

            // Instructions
            var instructionsLabel = new Label
            {
                Text = "Low-Level Mode: Captures your actual WASD key presses and broadcasts them to all registered windows in real-time.\nWarning: This mode requires careful handling and may interfere with normal keyboard usage.",
                Location = new System.Drawing.Point(10, 540),
                Size = new System.Drawing.Size(560, 40),
                ForeColor = System.Drawing.Color.DarkBlue
            };

            // Add controls to form
            Controls.Add(windowLabel);
            Controls.Add(_windowListBox);
            Controls.Add(_refreshButton);
            Controls.Add(_methodLabel);
            Controls.Add(_methodComboBox);
            Controls.Add(_testAllMethodsButton);
            Controls.Add(testLabel);
            Controls.Add(_sendQButton);
            Controls.Add(_send1Button);
            Controls.Add(_startMovementButton);
            Controls.Add(_stopMovementButton);
            Controls.Add(_lowLevelGroupBox);
            Controls.Add(_statusLabel);
            Controls.Add(instructionsLabel);
        }

        private void SetupHotkeys()
        {
            _hotkeyManager.RegisterHotkey(Keys.D1, () => RegisterWindow(1));
            _hotkeyManager.RegisterHotkey(Keys.D2, () => RegisterWindow(2));
            _hotkeyManager.RegisterHotkey(Keys.D3, () => RegisterWindow(3));
            _hotkeyManager.StartListening();
        }

        private void SetupLowLevelSimulator()
        {
            _lowLevelInputSimulator.OnStatusUpdate += (message) =>
            {
                Invoke(new Action(() =>
                {
                    _lowLevelStatusLabel.Text = message;
                    UpdateStatus($"Low-level: {message}");
                }));
            };
        }

        private void EnableLowLevelCheckBox_CheckedChanged(object? sender, EventArgs e)
        {
            bool enabled = _enableLowLevelCheckBox.Checked;
            
            _startHookButton.Enabled = enabled;
            _stopHookButton.Enabled = enabled;
            _startRealTimeButton.Enabled = enabled;
            _stopRealTimeButton.Enabled = enabled;

            if (enabled)
            {
                _lowLevelStatusLabel.Text = "Low-level mode enabled - ready to start";
                _lowLevelStatusLabel.ForeColor = System.Drawing.Color.DarkGreen;
                UpdateStatus("Low-level mode enabled. Click 'Start Keyboard Hook' to begin.");
            }
            else
            {
                _lowLevelInputSimulator.StopRealTimeBroadcast();
                _lowLevelInputSimulator.StopKeyboardHook();
                _lowLevelStatusLabel.Text = "Low-level mode disabled";
                _lowLevelStatusLabel.ForeColor = System.Drawing.Color.Gray;
                UpdateStatus("Low-level mode disabled.");
            }
        }

        private void StartHookButton_Click(object? sender, EventArgs e)
        {
            _lowLevelInputSimulator.RegisteredWindows = _registeredWindows.Values.ToList();
            _lowLevelInputSimulator.StartKeyboardHook();
            _startHookButton.Enabled = false;
            _stopHookButton.Enabled = true;
        }

        private void StopHookButton_Click(object? sender, EventArgs e)
        {
            _lowLevelInputSimulator.StopKeyboardHook();
            _lowLevelInputSimulator.StopRealTimeBroadcast();
            _startHookButton.Enabled = true;
            _stopHookButton.Enabled = false;
            _startRealTimeButton.Enabled = true;
            _stopRealTimeButton.Enabled = false;
        }

        private void StartRealTimeButton_Click(object? sender, EventArgs e)
        {
            _lowLevelInputSimulator.RegisteredWindows = _registeredWindows.Values.ToList();
            _lowLevelInputSimulator.StartRealTimeBroadcast();
            _startRealTimeButton.Enabled = false;
            _stopRealTimeButton.Enabled = true;
            UpdateStatus("Real-time broadcasting active! Press WASD to move in all windows.");
        }

        private void StopRealTimeButton_Click(object? sender, EventArgs e)
        {
            _lowLevelInputSimulator.StopRealTimeBroadcast();
            _startRealTimeButton.Enabled = true;
            _stopRealTimeButton.Enabled = false;
            UpdateStatus("Real-time broadcasting stopped.");
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
                
                // Update low-level simulator if active
                if (_enableLowLevelCheckBox.Checked)
                {
                    _lowLevelInputSimulator.RegisteredWindows = _registeredWindows.Values.ToList();
                }
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
            if (_enableLowLevelCheckBox.Checked)
            {
                _lowLevelInputSimulator.BroadcastSingleKey(LowLevelInputSimulator.VirtualKeyCode.VK_Q);
                UpdateStatus("Sent Q key using low-level method.");
            }
            else
            {
                var windows = _registeredWindows.Values.Where(w => w.IsActive).ToList();
                var method = _inputSimulator.CurrentMethod;
                
                _inputSimulator.BroadcastToAll(windows, hwnd => 
                    _inputSimulator.SendKeyPress(hwnd, VirtualKeyCode.VK_Q, method));
                
                UpdateStatus($"Sent Q key to {windows.Count} windows using {method} method.");
            }
        }

        private void Send1Button_Click(object? sender, EventArgs e)
        {
            if (_enableLowLevelCheckBox.Checked)
            {
                _lowLevelInputSimulator.BroadcastSingleKey(LowLevelInputSimulator.VirtualKeyCode.VK_1);
                UpdateStatus("Sent 1 key using low-level method.");
            }
            else
            {
                var windows = _registeredWindows.Values.Where(w => w.IsActive).ToList();
                var method = _inputSimulator.CurrentMethod;
                
                _inputSimulator.BroadcastToAll(windows, hwnd => 
                    _inputSimulator.SendKeyPress(hwnd, VirtualKeyCode.VK_1, method));
                
                UpdateStatus($"Sent 1 key to {windows.Count} windows using {method} method.");
            }
        }

        private void StartMovementButton_Click(object? sender, EventArgs e)
        {
            var windows = _registeredWindows.Values.Where(w => w.IsActive).ToList();
            _inputSimulator.BroadcastToAll(windows, hwnd => _inputSimulator.SendKeyDown(hwnd, VirtualKeyCode.VK_W));
            UpdateStatus($"Started movement (W key down) for {windows.Count} windows.");
        }

        private void StopMovementButton_Click(object? sender, EventArgs e)
        {
            var windows = _registeredWindows.Values.Where(w => w.IsActive).ToList();
            _inputSimulator.BroadcastToAll(windows, hwnd => _inputSimulator.SendKeyUp(hwnd, VirtualKeyCode.VK_W));
            UpdateStatus($"Stopped movement (W key up) for {windows.Count} windows.");
        }

        private void UpdateWindowList()
        {
            _windowListBox.Items.Clear();
            
            for (int i = 1; i <= 3; i++)
            {
                if (_registeredWindows.TryGetValue(i, out var window))
                {
                    // Validate window is still active
                    if (_windowManager.ValidateWindow(window.WindowHandle))
                    {
                        _windowListBox.Items.Add(window.ToString());
                    }
                    else
                    {
                        window.IsActive = false;
                        _windowListBox.Items.Add($"[{i}] [INACTIVE] - Window closed");
                    }
                }
                else
                {
                    _windowListBox.Items.Add($"[{i}] [Empty Slot]");
                }
            }
        }

        private void UpdateStatus(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            _statusLabel.Text = $"[{timestamp}] Status: {message}";
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _lowLevelInputSimulator?.Dispose();
            _hotkeyManager?.Dispose();
            base.OnFormClosed(e);
        }
    }
}