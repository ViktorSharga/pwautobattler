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
        private CheckBox _broadcastModeCheckBox = null!;
        private bool _broadcastMode = false;
        private LowLevelKeyboardHook? _keyboardHook;

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
        }

        private void InitializeComponent()
        {
            Text = "Game Multi-Window Controller";
            Size = new System.Drawing.Size(550, 500);
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
                Size = new System.Drawing.Size(510, 100)
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

            // Test controls
            var testLabel = new Label
            {
                Text = "Test Controls:",
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

            // Status
            _statusLabel = new Label
            {
                Text = "Status: Ready. Use Ctrl+Shift+1-9/0 to register windows.",
                Location = new System.Drawing.Point(10, 330),
                Size = new System.Drawing.Size(510, 100),
                BorderStyle = BorderStyle.Fixed3D
            };

            // Instructions
            var instructionsLabel = new Label
            {
                Text = "Note: KeyboardEventOptimized is recommended for most games.\nIt minimizes window flickering and supports proper movement.",
                Location = new System.Drawing.Point(10, 440),
                Size = new System.Drawing.Size(510, 40),
                ForeColor = System.Drawing.Color.DarkGreen
            };

            // Add controls to form
            Controls.Add(windowLabel);
            Controls.Add(_windowListBox);
            Controls.Add(_refreshButton);
            Controls.Add(_methodLabel);
            Controls.Add(_methodComboBox);
            Controls.Add(_testAllMethodsButton);
            Controls.Add(_broadcastModeCheckBox);
            Controls.Add(testLabel);
            Controls.Add(_sendQButton);
            Controls.Add(_send1Button);
            Controls.Add(_startMovementButton);
            Controls.Add(_stopMovementButton);
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

        private void Send1Button_Click(object? sender, EventArgs e)
        {
            var windows = _registeredWindows.Values.Where(w => w.IsActive).ToList();
            var method = _inputSimulator.CurrentMethod;
            
            _inputSimulator.BroadcastToAll(windows, hwnd => 
                _inputSimulator.SendKeyPress(hwnd, VirtualKeyCode.VK_1, method));
            
            UpdateStatus($"Sent 1 key to {windows.Count} windows using {method} method.");
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
            
            for (int i = 1; i <= 10; i++)
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
                
                // Return to window 1 if it exists and is active
                if (_registeredWindows.ContainsKey(1) && _registeredWindows[1].IsActive)
                {
                    SetForegroundWindow(_registeredWindows[1].WindowHandle);
                }
                else
                {
                    // If window 1 doesn't exist, return to original window
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

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _keyboardHook?.Dispose();
            _hotkeyManager?.Dispose();
            base.OnFormClosed(e);
        }
    }
}