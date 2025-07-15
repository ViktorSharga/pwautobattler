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
        private readonly HotkeyManager _hotkeyManager;
        private readonly Dictionary<int, GameWindow> _registeredWindows;

        private ListBox _windowListBox = null!;
        private Button _refreshButton = null!;
        private Button _sendQButton = null!;
        private Button _send1Button = null!;
        private Button _startMovementButton = null!;
        private Button _stopMovementButton = null!;
        private Label _statusLabel = null!;

        public MainForm()
        {
            _windowManager = new WindowManager();
            _inputSimulator = new InputSimulator();
            _hotkeyManager = new HotkeyManager();
            _registeredWindows = new Dictionary<int, GameWindow>();

            InitializeComponent();
            SetupHotkeys();
        }

        private void InitializeComponent()
        {
            Text = "Game Multi-Window Controller";
            Size = new System.Drawing.Size(500, 400);
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
                Size = new System.Drawing.Size(460, 100)
            };

            _refreshButton = new Button
            {
                Text = "Refresh Available Windows",
                Location = new System.Drawing.Point(10, 145),
                Size = new System.Drawing.Size(150, 30)
            };
            _refreshButton.Click += RefreshButton_Click;

            // Test controls
            var testLabel = new Label
            {
                Text = "Test Controls (PostMessage/SendMessage):",
                Location = new System.Drawing.Point(10, 185),
                Size = new System.Drawing.Size(250, 20)
            };

            _sendQButton = new Button
            {
                Text = "Send Q to All",
                Location = new System.Drawing.Point(10, 210),
                Size = new System.Drawing.Size(100, 30)
            };
            _sendQButton.Click += SendQButton_Click;

            _send1Button = new Button
            {
                Text = "Send 1 to All",
                Location = new System.Drawing.Point(120, 210),
                Size = new System.Drawing.Size(100, 30)
            };
            _send1Button.Click += Send1Button_Click;

            _startMovementButton = new Button
            {
                Text = "Start Movement",
                Location = new System.Drawing.Point(10, 250),
                Size = new System.Drawing.Size(100, 30)
            };
            _startMovementButton.Click += StartMovementButton_Click;

            _stopMovementButton = new Button
            {
                Text = "Stop Movement",
                Location = new System.Drawing.Point(120, 250),
                Size = new System.Drawing.Size(100, 30)
            };
            _stopMovementButton.Click += StopMovementButton_Click;

            // Status
            _statusLabel = new Label
            {
                Text = "Status: Ready. Use Ctrl+Shift+1/2/3 to register windows.",
                Location = new System.Drawing.Point(10, 300),
                Size = new System.Drawing.Size(460, 40),
                BorderStyle = BorderStyle.Fixed3D
            };

            // Add controls to form
            Controls.Add(windowLabel);
            Controls.Add(_windowListBox);
            Controls.Add(_refreshButton);
            Controls.Add(testLabel);
            Controls.Add(_sendQButton);
            Controls.Add(_send1Button);
            Controls.Add(_startMovementButton);
            Controls.Add(_stopMovementButton);
            Controls.Add(_statusLabel);
        }

        private void SetupHotkeys()
        {
            _hotkeyManager.RegisterHotkey(Keys.D1, () => RegisterWindow(1));
            _hotkeyManager.RegisterHotkey(Keys.D2, () => RegisterWindow(2));
            _hotkeyManager.RegisterHotkey(Keys.D3, () => RegisterWindow(3));
            _hotkeyManager.StartListening();
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

        private void SendQButton_Click(object? sender, EventArgs e)
        {
            var windows = _registeredWindows.Values.Where(w => w.IsActive).ToList();
            _inputSimulator.BroadcastToAll(windows, hwnd => _inputSimulator.SendKeyPressMultiMethod(hwnd, VirtualKeyCode.VK_Q));
            UpdateStatus($"Sent Q key to {windows.Count} windows using multi-method approach.");
        }

        private void Send1Button_Click(object? sender, EventArgs e)
        {
            var windows = _registeredWindows.Values.Where(w => w.IsActive).ToList();
            _inputSimulator.BroadcastToAll(windows, hwnd => _inputSimulator.SendKeyPressMultiMethod(hwnd, VirtualKeyCode.VK_1));
            UpdateStatus($"Sent 1 key to {windows.Count} windows using multi-method approach.");
        }

        private void StartMovementButton_Click(object? sender, EventArgs e)
        {
            var windows = _registeredWindows.Values.Where(w => w.IsActive).ToList();
            _inputSimulator.BroadcastToAll(windows, hwnd => _inputSimulator.SendKeyDown(hwnd, VirtualKeyCode.VK_W));
            UpdateStatus($"Started movement (W key down) for {windows.Count} windows using PostMessage.");
        }

        private void StopMovementButton_Click(object? sender, EventArgs e)
        {
            var windows = _registeredWindows.Values.Where(w => w.IsActive).ToList();
            _inputSimulator.BroadcastToAll(windows, hwnd => _inputSimulator.SendKeyUp(hwnd, VirtualKeyCode.VK_W));
            UpdateStatus($"Stopped movement (W key up) for {windows.Count} windows using PostMessage.");
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
            _statusLabel.Text = $"Status: {message}";
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _hotkeyManager?.Dispose();
            base.OnFormClosed(e);
        }
    }
}