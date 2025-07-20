using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using GameAutomation.Core;
using GameAutomation.Models;
using GameAutomation.Services;
using GameAutomation.UI.Controllers;

namespace GameAutomation.UI
{
    public partial class MainFormRefactored : Form
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        // Core services
        private readonly IWindowService _windowService;
        private readonly IInputService _inputService;
        private readonly ISpellService _spellService;
        private readonly ICooldownService _cooldownService;
        private readonly HotkeyManager _hotkeyManager;
        
        // UI Controllers
        private readonly GameWindowController _gameWindowController;
        private readonly SpellButtonController _spellButtonController;
        private readonly InputMethodController _inputMethodController;
        
        // UI Components
        private Panel _windowPanel = null!;
        private Panel _classTablePanel = null!;
        private Label _statusLabel = null!;
        
        // Hooks for global input handling
        private LowLevelKeyboardHook? _keyboardHook;
        private MouseHook? _mouseHook;
        private BackgroundMouseSimulator? _backgroundMouseSimulator;

        public MainFormRefactored()
        {
            // Initialize services (in a real implementation, these would be injected)
            _windowService = new WindowService();
            _inputService = new InputService(_windowService);
            _cooldownService = new CooldownService();
            _spellService = new SpellService(_inputService, _cooldownService);
            _hotkeyManager = new HotkeyManager();

            InitializeComponent();
            
            // Initialize controllers
            _gameWindowController = new GameWindowController(_windowService, _windowPanel);
            _spellButtonController = new SpellButtonController(_spellService, _cooldownService, _classTablePanel);
            _inputMethodController = new InputMethodController(_inputService);
            
            SetupControllers();
            SetupHotkeys();
            SetupHooks();
            
            _hotkeyManager.StartListening();
            
            // Load spells
            _ = LoadSpellsAsync();
        }

        private void InitializeComponent()
        {
            Text = "Game Multi-Window Controller";
            Size = new Size(750, 650);
            StartPosition = FormStartPosition.CenterScreen;

            CreateWindowControls();
            CreateClassTablePanel();
            CreateStatusControls();
            CreateInstructionsLabel();
        }

        private void CreateWindowControls()
        {
            var windowLabel = new Label
            {
                Text = "Registered Windows:",
                Location = new Point(10, 10),
                Size = new Size(200, 20)
            };

            _windowPanel = new Panel
            {
                Location = new Point(10, 35),
                Size = new Size(710, 100),
                BorderStyle = BorderStyle.Fixed3D,
                AutoScroll = true
            };

            Controls.Add(windowLabel);
            Controls.Add(_windowPanel);
        }

        private void CreateClassTablePanel()
        {
            var classTableLabel = new Label
            {
                Text = "Assigned Classes:",
                Location = new Point(10, 330),
                Size = new Size(200, 20)
            };
            
            _classTablePanel = new Panel
            {
                Location = new Point(10, 355),
                Size = new Size(710, 200),
                BorderStyle = BorderStyle.Fixed3D,
                AutoScroll = false,
                BackColor = Color.WhiteSmoke,
                AutoSize = false
            };
            
            // Enable double buffering to reduce flickering
            typeof(Panel).InvokeMember("DoubleBuffered",
                System.Reflection.BindingFlags.SetProperty | 
                System.Reflection.BindingFlags.Instance | 
                System.Reflection.BindingFlags.NonPublic,
                null, _classTablePanel, new object[] { true });

            Controls.Add(classTableLabel);
            Controls.Add(_classTablePanel);
        }

        private void CreateStatusControls()
        {
            _statusLabel = new Label
            {
                Text = "Status: Ready. Use Ctrl+Shift+1-9/0 to register windows.",
                Location = new Point(10, 485),
                Size = new Size(710, 100),
                BorderStyle = BorderStyle.Fixed3D
            };

            Controls.Add(_statusLabel);
        }

        private void CreateInstructionsLabel()
        {
            var instructionsLabel = new Label
            {
                Text = "Note: KeyboardEventOptimized is recommended for most games.\nIt minimizes window flickering and supports proper movement.",
                Location = new Point(10, 595),
                Size = new Size(710, 40),
                ForeColor = Color.DarkGreen
            };

            Controls.Add(instructionsLabel);
        }

        private void SetupControllers()
        {
            // Setup GameWindowController
            _gameWindowController.StatusChanged += UpdateStatus;
            _gameWindowController.WindowListChanged += () => 
            {
                _spellButtonController.UpdateClassTable(_gameWindowController.RegisteredWindows);
            };

            // Setup SpellButtonController
            _spellButtonController.StatusChanged += UpdateStatus;

            // Setup InputMethodController
            _inputMethodController.StatusChanged += UpdateStatus;
            _inputMethodController.GetActiveWindows += () => _gameWindowController.GetActiveWindows();
            _inputMethodController.AddControlsToForm(this);
        }

        private void SetupHotkeys()
        {
            // Ctrl+Shift+1-9/0 for window registration
            _hotkeyManager.RegisterHotkey(Keys.D1, () => _gameWindowController.RegisterWindow(1));
            _hotkeyManager.RegisterHotkey(Keys.D2, () => _gameWindowController.RegisterWindow(2));
            _hotkeyManager.RegisterHotkey(Keys.D3, () => _gameWindowController.RegisterWindow(3));
            _hotkeyManager.RegisterHotkey(Keys.D4, () => _gameWindowController.RegisterWindow(4));
            _hotkeyManager.RegisterHotkey(Keys.D5, () => _gameWindowController.RegisterWindow(5));
            _hotkeyManager.RegisterHotkey(Keys.D6, () => _gameWindowController.RegisterWindow(6));
            _hotkeyManager.RegisterHotkey(Keys.D7, () => _gameWindowController.RegisterWindow(7));
            _hotkeyManager.RegisterHotkey(Keys.D8, () => _gameWindowController.RegisterWindow(8));
            _hotkeyManager.RegisterHotkey(Keys.D9, () => _gameWindowController.RegisterWindow(9));
            _hotkeyManager.RegisterHotkey(Keys.D0, () => _gameWindowController.RegisterWindow(10));
        }

        private void SetupHooks()
        {
            // Initialize keyboard hook
            _keyboardHook = new LowLevelKeyboardHook();
            _keyboardHook.KeyDown += OnGlobalKeyDown;
            
            // Initialize mouse hook and simulator
            _mouseHook = new MouseHook();
            _mouseHook.CtrlLeftClick += OnCtrlLeftClick;
            _mouseHook.CtrlRightClick += OnCtrlRightClick;
            _mouseHook.ShiftLeftClick += OnShiftLeftClick;
            _mouseHook.StartListening();
            _backgroundMouseSimulator = new BackgroundMouseSimulator();
        }

        private async System.Threading.Tasks.Task LoadSpellsAsync()
        {
            try
            {
                await _spellService.ReloadSpellsAsync();
                UpdateStatus("Spells loaded successfully.");
            }
            catch (Exception ex)
            {
                UpdateStatus($"Failed to load spells: {ex.Message}");
            }
        }

        private void OnGlobalKeyDown(object? sender, Keys e)
        {
            if (!_inputMethodController.BroadcastMode) return;

            // Handle broadcast mode for keys 1-9
            var activeWindows = _gameWindowController.GetActiveWindows();
            if (activeWindows.Length == 0) return;

            var key = e;
            if (key >= Keys.D1 && key <= Keys.D9)
            {
                var keyCode = VirtualKeyCode.VK_1 + (key - Keys.D1);
                BroadcastKeyToWindows(activeWindows, keyCode);
            }
        }

        private void OnCtrlLeftClick(object? sender, MouseHook.MouseEventArgs e)
        {
            if (!_inputMethodController.MouseMirroringMode) return;

            var activeWindows = _gameWindowController.GetActiveWindows();
            if (activeWindows.Length == 0) return;

            BroadcastMouseClick(activeWindows, e.X, e.Y, isLeftClick: true);
        }

        private void OnCtrlRightClick(object? sender, MouseHook.MouseEventArgs e)
        {
            if (!_inputMethodController.MouseMirroringMode) return;

            var activeWindows = _gameWindowController.GetActiveWindows();
            if (activeWindows.Length == 0) return;

            BroadcastMouseClick(activeWindows, e.X, e.Y, isLeftClick: false);
        }

        private void OnShiftLeftClick(object? sender, MouseHook.MouseEventArgs e)
        {
            if (!_inputMethodController.ShiftDoubleClickMode) return;

            var activeWindows = _gameWindowController.GetActiveWindows();
            if (activeWindows.Length == 0) return;

            BroadcastDoubleClick(activeWindows, e.X, e.Y);
        }

        private void BroadcastKeyToWindows(GameWindow[] windows, VirtualKeyCode keyCode)
        {
            foreach (var window in windows)
            {
                _ = _inputService.SendKeyPressAsync(window, keyCode);
            }
        }

        private void BroadcastMouseClick(GameWindow[] windows, int x, int y, bool isLeftClick)
        {
            foreach (var window in windows)
            {
                _ = _inputService.SendMouseClickAsync(window, x, y, isLeftClick);
            }
        }

        private void BroadcastDoubleClick(GameWindow[] windows, int x, int y)
        {
            foreach (var window in windows)
            {
                _ = _inputService.SendDoubleClickAsync(window, x, y);
            }
        }

        private void UpdateStatus(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(UpdateStatus), message);
                return;
            }

            _statusLabel.Text = $"Status: {message}";
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _hotkeyManager?.StopListening();
            _keyboardHook?.Dispose();
            _mouseHook?.Dispose();
            _backgroundMouseSimulator?.Dispose();
            _spellButtonController?.Dispose();
            
            base.OnFormClosed(e);
        }
    }
}