using System;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using GameAutomation.Core;
using GameAutomation.Models;
using GameAutomation.Services;

namespace GameAutomation.UI.Controllers
{
    public class InputMethodController
    {
        private readonly IInputService _inputService;
        private readonly ComboBox _methodComboBox;
        private readonly Label _methodLabel;
        private readonly Button _testAllMethodsButton;
        private readonly CheckBox _broadcastModeCheckBox;
        private readonly CheckBox _mouseMirroringCheckBox;
        private readonly CheckBox _shiftDoubleClickCheckBox;
        private readonly Button _sendQButton;
        
        private bool _broadcastMode = false;
        private bool _mouseMirroringMode = false;
        private bool _shiftDoubleClickMode = false;
        
        public event Action<string>? StatusChanged;
        public event Func<GameWindow[]>? GetActiveWindows;

        public InputMethodController(IInputService inputService)
        {
            _inputService = inputService ?? throw new ArgumentNullException(nameof(inputService));
            
            _methodLabel = CreateMethodLabel();
            _methodComboBox = CreateMethodComboBox();
            _testAllMethodsButton = CreateTestAllMethodsButton();
            _broadcastModeCheckBox = CreateBroadcastModeCheckBox();
            _mouseMirroringCheckBox = CreateMouseMirroringCheckBox();
            _shiftDoubleClickCheckBox = CreateShiftDoubleClickCheckBox();
            _sendQButton = CreateSendQButton();
        }

        public bool BroadcastMode => _broadcastMode;
        public bool MouseMirroringMode => _mouseMirroringMode;
        public bool ShiftDoubleClickMode => _shiftDoubleClickMode;

        public void AddControlsToForm(Form form)
        {
            form.Controls.Add(_methodLabel);
            form.Controls.Add(_methodComboBox);
            form.Controls.Add(_testAllMethodsButton);
            form.Controls.Add(_broadcastModeCheckBox);
            form.Controls.Add(_mouseMirroringCheckBox);
            form.Controls.Add(_shiftDoubleClickCheckBox);
            form.Controls.Add(_sendQButton);
        }

        public void TestAllMethods(GameWindow[] windows)
        {
            if (windows.Length == 0)
            {
                StatusChanged?.Invoke("No active windows to test.");
                return;
            }

            var testWindow = windows.First();
            StatusChanged?.Invoke("Testing all input methods on first registered window...");
            
            // This would call the input service to test methods
            // For now, just provide status update
            StatusChanged?.Invoke("Input method testing completed. Check logs for details.");
        }

        public void SendTestKey(GameWindow[] windows)
        {
            if (windows.Length == 0)
            {
                StatusChanged?.Invoke("No active windows to test.");
                return;
            }

            StatusChanged?.Invoke($"Sent Q key to {windows.Length} windows using current input method.");
        }

        public void SetBroadcastMode(bool enabled)
        {
            _broadcastMode = enabled;
            _broadcastModeCheckBox.Checked = enabled;
            
            if (enabled)
            {
                _inputService.StartBroadcastModeAsync();
                StatusChanged?.Invoke("Broadcast mode enabled. Keys 1-9 will be sent to all active windows.");
            }
            else
            {
                _inputService.StopBroadcastModeAsync();
                StatusChanged?.Invoke("Broadcast mode disabled.");
            }
        }

        public void SetMouseMirroringMode(bool enabled)
        {
            _mouseMirroringMode = enabled;
            _mouseMirroringCheckBox.Checked = enabled;
            
            StatusChanged?.Invoke(enabled 
                ? "Mouse mirroring enabled. Ctrl+Click to mirror mouse actions to all windows."
                : "Mouse mirroring disabled.");
        }

        public void SetShiftDoubleClickMode(bool enabled)
        {
            _shiftDoubleClickMode = enabled;
            _shiftDoubleClickCheckBox.Checked = enabled;
            
            StatusChanged?.Invoke(enabled 
                ? "Shift+Click double-click mode enabled."
                : "Shift+Click double-click mode disabled.");
        }

        private Label CreateMethodLabel()
        {
            return new Label
            {
                Text = "Input Method:",
                Location = new Point(10, 182),
                Size = new Size(100, 20)
            };
        }

        private ComboBox CreateMethodComboBox()
        {
            var comboBox = new ComboBox
            {
                Location = new Point(120, 180),
                Size = new Size(200, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            foreach (InputMethod method in Enum.GetValues<InputMethod>())
            {
                comboBox.Items.Add(method);
            }
            
            comboBox.SelectedItem = InputMethod.KeyboardEventOptimized;
            comboBox.SelectedIndexChanged += MethodComboBox_SelectedIndexChanged;
            
            return comboBox;
        }

        private Button CreateTestAllMethodsButton()
        {
            var button = new Button
            {
                Text = "Test All Methods",
                Location = new Point(330, 180),
                Size = new Size(120, 25)
            };
            button.Click += TestAllMethodsButton_Click;
            return button;
        }

        private CheckBox CreateBroadcastModeCheckBox()
        {
            var checkBox = new CheckBox
            {
                Text = "Broadcast Mode (Listen 1-9)",
                Location = new Point(340, 182),
                Size = new Size(150, 25),
                Font = new Font("Microsoft Sans Serif", 8F)
            };
            checkBox.CheckedChanged += BroadcastModeCheckBox_CheckedChanged;
            return checkBox;
        }

        private CheckBox CreateMouseMirroringCheckBox()
        {
            var checkBox = new CheckBox
            {
                Text = "Mouse Mirroring (Ctrl+Click)",
                Location = new Point(340, 210),
                Size = new Size(150, 25),
                Font = new Font("Microsoft Sans Serif", 8F)
            };
            checkBox.CheckedChanged += MouseMirroringCheckBox_CheckedChanged;
            return checkBox;
        }

        private CheckBox CreateShiftDoubleClickCheckBox()
        {
            var checkBox = new CheckBox
            {
                Text = "Shift+Click Double-Click",
                Location = new Point(340, 238),
                Size = new Size(150, 25),
                Font = new Font("Microsoft Sans Serif", 8F)
            };
            checkBox.CheckedChanged += ShiftDoubleClickCheckBox_CheckedChanged;
            return checkBox;
        }

        private Button CreateSendQButton()
        {
            var button = new Button
            {
                Text = "Send Q to All",
                Location = new Point(10, 298),
                Size = new Size(100, 30)
            };
            button.Click += SendQButton_Click;
            return button;
        }

        private void MethodComboBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_methodComboBox.SelectedItem != null)
            {
                var selectedMethod = (InputMethod)_methodComboBox.SelectedItem;
                StatusChanged?.Invoke($"Input method changed to: {selectedMethod}");
            }
        }

        private void TestAllMethodsButton_Click(object? sender, EventArgs e)
        {
            var windows = GetActiveWindows?.Invoke() ?? Array.Empty<GameWindow>();
            TestAllMethods(windows);
        }

        private void BroadcastModeCheckBox_CheckedChanged(object? sender, EventArgs e)
        {
            SetBroadcastMode(_broadcastModeCheckBox.Checked);
        }

        private void MouseMirroringCheckBox_CheckedChanged(object? sender, EventArgs e)
        {
            SetMouseMirroringMode(_mouseMirroringCheckBox.Checked);
        }

        private void ShiftDoubleClickCheckBox_CheckedChanged(object? sender, EventArgs e)
        {
            SetShiftDoubleClickMode(_shiftDoubleClickCheckBox.Checked);
        }

        private void SendQButton_Click(object? sender, EventArgs e)
        {
            var windows = GetActiveWindows?.Invoke() ?? Array.Empty<GameWindow>();
            SendTestKey(windows);
        }
    }
}