using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace GameAutomation.Core
{
    public class HotkeyManager : IDisposable
    {
        private const int WM_HOTKEY = 0x0312;
        private const int MOD_CONTROL = 0x0002;
        private const int MOD_SHIFT = 0x0004;

        private readonly Dictionary<int, Action> _hotkeyCallbacks = new();
        private readonly Form _hiddenForm;
        private int _currentHotkeyId = 1;
        private bool _disposed = false;

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        public HotkeyManager()
        {
            _hiddenForm = new HiddenForm();
        }

        public bool RegisterHotkey(Keys key, Action callback)
        {
            var hotkeyId = _currentHotkeyId++;
            var modifiers = MOD_CONTROL | MOD_SHIFT;

            var success = RegisterHotKey(_hiddenForm.Handle, hotkeyId, (uint)modifiers, (uint)key);
            if (success)
            {
                _hotkeyCallbacks[hotkeyId] = callback;
            }

            return success;
        }

        public bool RegisterHotkeyWithModifiers(Keys key, uint modifiers, Action callback)
        {
            var hotkeyId = _currentHotkeyId++;

            var success = RegisterHotKey(_hiddenForm.Handle, hotkeyId, modifiers, (uint)key);
            if (success)
            {
                _hotkeyCallbacks[hotkeyId] = callback;
            }

            return success;
        }

        public void StartListening()
        {
            Application.AddMessageFilter(new HotkeyMessageFilter(this));
        }

        public void StopListening()
        {
            foreach (var hotkeyId in _hotkeyCallbacks.Keys)
            {
                UnregisterHotKey(_hiddenForm.Handle, hotkeyId);
            }
            _hotkeyCallbacks.Clear();
        }

        internal void HandleHotkeyMessage(int hotkeyId)
        {
            if (_hotkeyCallbacks.TryGetValue(hotkeyId, out var callback))
            {
                callback?.Invoke();
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                StopListening();
                _hiddenForm?.Dispose();
                _disposed = true;
            }
        }

        private class HiddenForm : Form
        {
            public HiddenForm()
            {
                WindowState = FormWindowState.Minimized;
                ShowInTaskbar = false;
                Visible = false;
                SetStyle(ControlStyles.UserPaint, false);
            }

            protected override CreateParams CreateParams
            {
                get
                {
                    var cp = base.CreateParams;
                    cp.ExStyle |= 0x80;
                    return cp;
                }
            }
        }

        private class HotkeyMessageFilter : IMessageFilter
        {
            private readonly HotkeyManager _manager;

            public HotkeyMessageFilter(HotkeyManager manager)
            {
                _manager = manager;
            }

            public bool PreFilterMessage(ref Message m)
            {
                if (m.Msg == WM_HOTKEY)
                {
                    var hotkeyId = m.WParam.ToInt32();
                    _manager.HandleHotkeyMessage(hotkeyId);
                    return true;
                }
                return false;
            }
        }
    }
}