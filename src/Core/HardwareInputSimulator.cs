using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Text;
using System.Linq;
using GameAutomation.Models;

namespace GameAutomation.Core
{
    public class HardwareInputSimulator : IDisposable
    {
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_CHAR = 0x0102;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;
        
        private const int INPUT_KEYBOARD = 1;
        private const int KEYEVENTF_KEYUP = 0x0002;
        private const int KEYEVENTF_SCANCODE = 0x0008;
        private const int KEYEVENTF_UNICODE = 0x0004;
        private const int KEYEVENTF_EXTENDEDKEY = 0x0001;
        
        private const int HC_ACTION = 0;
        private const int WH_KEYBOARD_LL = 13;
        private const int WH_MOUSE_LL = 14;
        
        // Hardware input simulation constants
        private const int MAPVK_VK_TO_VSC = 0;
        private const int MAPVK_VSC_TO_VK = 1;
        private const int MAPVK_VK_TO_CHAR = 2;
        private const int MAPVK_VSC_TO_VK_EX = 3;
        
        // HID constants
        private const int GENERIC_READ = unchecked((int)0x80000000);
        private const int GENERIC_WRITE = 0x40000000;
        private const int FILE_SHARE_READ = 0x00000001;
        private const int FILE_SHARE_WRITE = 0x00000002;
        private const int OPEN_EXISTING = 3;
        private const int INVALID_HANDLE_VALUE = -1;
        
        // Device interface classes
        private static readonly Guid GUID_DEVINTERFACE_KEYBOARD = new Guid("884b96c3-56ef-11d1-bc8c-00a0c91405dd");
        private static readonly Guid GUID_DEVINTERFACE_MOUSE = new Guid("378de44c-56ef-11d1-bc8c-00a0c91405dd");
        private static readonly Guid GUID_DEVINTERFACE_HID = new Guid("4D1E55B2-F16F-11CF-88CB-001111000030");

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint type;
            public InputUnion u;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)]
            public KEYBDINPUT ki;
            [FieldOffset(0)]
            public MOUSEINPUT mi;
            [FieldOffset(0)]
            public HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SP_DEVICE_INTERFACE_DATA
        {
            public int cbSize;
            public Guid InterfaceClassGuid;
            public int Flags;
            public IntPtr Reserved;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SP_DEVICE_INTERFACE_DETAIL_DATA
        {
            public int cbSize;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string DevicePath;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SP_DEVINFO_DATA
        {
            public int cbSize;
            public Guid ClassGuid;
            public int DevInst;
            public IntPtr Reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HIDD_ATTRIBUTES
        {
            public int Size;
            public ushort VendorID;
            public ushort ProductID;
            public ushort VersionNumber;
        }

        // P/Invoke declarations
        [DllImport("user32.dll")]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);

        [DllImport("user32.dll")]
        private static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        [DllImport("user32.dll")]
        private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        // Hardware enumeration
        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern IntPtr SetupDiGetClassDevs(ref Guid classGuid, IntPtr enumerator, IntPtr hwndParent, uint flags);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiEnumDeviceInterfaces(IntPtr hDevInfo, IntPtr devInfo, ref Guid interfaceClassGuid, uint memberIndex, ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr hDevInfo, ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData, ref SP_DEVICE_INTERFACE_DETAIL_DATA deviceInterfaceDetailData, uint deviceInterfaceDetailDataSize, out uint requiredSize, ref SP_DEVINFO_DATA deviceInfoData);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiDestroyDeviceInfoList(IntPtr hDevInfo);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr CreateFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("hid.dll")]
        private static extern bool HidD_GetAttributes(IntPtr hidDeviceObject, ref HIDD_ATTRIBUTES attributes);

        [DllImport("hid.dll")]
        private static extern bool HidD_GetManufacturerString(IntPtr hidDeviceObject, StringBuilder buffer, int bufferLength);

        [DllImport("hid.dll")]
        private static extern bool HidD_GetProductString(IntPtr hidDeviceObject, StringBuilder buffer, int bufferLength);

        [DllImport("hid.dll")]
        private static extern bool HidD_GetSerialNumberString(IntPtr hidDeviceObject, StringBuilder buffer, int bufferLength);

        private readonly Dictionary<IntPtr, HashSet<VirtualKeyCode>> _activeKeys = new();
        private readonly List<HardwareDevice> _keyboardDevices = new();
        private readonly List<HardwareDevice> _mouseDevices = new();
        private readonly System.Threading.Timer _stateTimer;
        private bool _disposed = false;

        public List<GameWindow> RegisteredWindows { get; set; } = new();
        public bool IsRealTimeBroadcastEnabled { get; set; } = false;

        public event Action<string>? OnStatusUpdate;

        private class HardwareDevice
        {
            public string DevicePath { get; set; } = "";
            public string Manufacturer { get; set; } = "";
            public string ProductName { get; set; } = "";
            public string SerialNumber { get; set; } = "";
            public ushort VendorId { get; set; }
            public ushort ProductId { get; set; }
            public ushort VersionNumber { get; set; }
            public DeviceType Type { get; set; }
            public IntPtr Handle { get; set; } = IntPtr.Zero;
        }

        private enum DeviceType
        {
            Keyboard,
            Mouse,
            HID
        }

        public HardwareInputSimulator()
        {
            _stateTimer = new System.Threading.Timer(MaintainKeyStates, null, Timeout.Infinite, Timeout.Infinite);
            EnumerateHardwareDevices();
        }

        private void EnumerateHardwareDevices()
        {
            OnStatusUpdate?.Invoke("Enumerating hardware devices...");
            
            // Enumerate keyboard devices
            EnumerateDevicesByClass(GUID_DEVINTERFACE_KEYBOARD, DeviceType.Keyboard);
            
            // Enumerate mouse devices  
            EnumerateDevicesByClass(GUID_DEVINTERFACE_MOUSE, DeviceType.Mouse);
            
            // Enumerate HID devices
            EnumerateDevicesByClass(GUID_DEVINTERFACE_HID, DeviceType.HID);
            
            OnStatusUpdate?.Invoke($"Found {_keyboardDevices.Count} keyboard devices, {_mouseDevices.Count} mouse devices");
        }

        private void EnumerateDevicesByClass(Guid classGuid, DeviceType deviceType)
        {
            const uint DIGCF_PRESENT = 0x00000002;
            const uint DIGCF_DEVICEINTERFACE = 0x00000010;

            IntPtr hDevInfo = SetupDiGetClassDevs(ref classGuid, IntPtr.Zero, IntPtr.Zero, DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);
            if (hDevInfo == IntPtr.Zero)
                return;

            try
            {
                uint memberIndex = 0;
                while (true)
                {
                    SP_DEVICE_INTERFACE_DATA deviceInterfaceData = new SP_DEVICE_INTERFACE_DATA();
                    deviceInterfaceData.cbSize = Marshal.SizeOf(deviceInterfaceData);

                    if (!SetupDiEnumDeviceInterfaces(hDevInfo, IntPtr.Zero, ref classGuid, memberIndex, ref deviceInterfaceData))
                        break;

                    SP_DEVICE_INTERFACE_DETAIL_DATA deviceInterfaceDetailData = new SP_DEVICE_INTERFACE_DETAIL_DATA();
                    deviceInterfaceDetailData.cbSize = IntPtr.Size == 8 ? 8 : 6; // 64-bit vs 32-bit
                    
                    SP_DEVINFO_DATA deviceInfoData = new SP_DEVINFO_DATA();
                    deviceInfoData.cbSize = Marshal.SizeOf(deviceInfoData);

                    if (SetupDiGetDeviceInterfaceDetail(hDevInfo, ref deviceInterfaceData, ref deviceInterfaceDetailData, 
                        (uint)Marshal.SizeOf(deviceInterfaceDetailData), out uint requiredSize, ref deviceInfoData))
                    {
                        var device = GetDeviceInfo(deviceInterfaceDetailData.DevicePath, deviceType);
                        if (device != null)
                        {
                            switch (deviceType)
                            {
                                case DeviceType.Keyboard:
                                    _keyboardDevices.Add(device);
                                    break;
                                case DeviceType.Mouse:
                                    _mouseDevices.Add(device);
                                    break;
                                case DeviceType.HID:
                                    // Filter HID devices that are keyboards or mice
                                    if (device.ProductName.ToLower().Contains("keyboard") || 
                                        device.ProductName.ToLower().Contains("mouse"))
                                    {
                                        if (device.ProductName.ToLower().Contains("keyboard"))
                                            _keyboardDevices.Add(device);
                                        else
                                            _mouseDevices.Add(device);
                                    }
                                    break;
                            }
                        }
                    }

                    memberIndex++;
                }
            }
            finally
            {
                SetupDiDestroyDeviceInfoList(hDevInfo);
            }
        }

        private HardwareDevice? GetDeviceInfo(string devicePath, DeviceType deviceType)
        {
            IntPtr handle = CreateFile(devicePath, 0, FILE_SHARE_READ | FILE_SHARE_WRITE, 
                IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
            
            if (handle == IntPtr.Zero || handle.ToInt32() == INVALID_HANDLE_VALUE)
                return null;

            try
            {
                var device = new HardwareDevice
                {
                    DevicePath = devicePath,
                    Type = deviceType,
                    Handle = handle
                };

                // Get hardware attributes
                HIDD_ATTRIBUTES attributes = new HIDD_ATTRIBUTES();
                attributes.Size = Marshal.SizeOf(attributes);
                
                if (HidD_GetAttributes(handle, ref attributes))
                {
                    device.VendorId = attributes.VendorID;
                    device.ProductId = attributes.ProductID;
                    device.VersionNumber = attributes.VersionNumber;
                }

                // Get device strings
                StringBuilder manufacturer = new StringBuilder(256);
                StringBuilder product = new StringBuilder(256);
                StringBuilder serial = new StringBuilder(256);

                if (HidD_GetManufacturerString(handle, manufacturer, manufacturer.Capacity))
                    device.Manufacturer = manufacturer.ToString();

                if (HidD_GetProductString(handle, product, product.Capacity))
                    device.ProductName = product.ToString();

                if (HidD_GetSerialNumberString(handle, serial, serial.Capacity))
                    device.SerialNumber = serial.ToString();

                return device;
            }
            catch
            {
                CloseHandle(handle);
                return null;
            }
        }

        public void StartRealTimeBroadcast()
        {
            IsRealTimeBroadcastEnabled = true;
            _stateTimer.Change(0, 50); // Check every 50ms for better responsiveness
            OnStatusUpdate?.Invoke($"Hardware simulation started with {_keyboardDevices.Count} keyboard devices");
        }

        public void StopRealTimeBroadcast()
        {
            IsRealTimeBroadcastEnabled = false;
            _stateTimer.Change(Timeout.Infinite, Timeout.Infinite);
            
            // Release all held keys
            foreach (var windowKeys in _activeKeys.ToArray())
            {
                foreach (var key in windowKeys.Value.ToArray())
                {
                    SendHardwareKeyUpToWindow(windowKeys.Key, key);
                }
            }
            _activeKeys.Clear();
            
            OnStatusUpdate?.Invoke("Hardware simulation stopped");
        }

        public void SendHardwareKeyPress(VirtualKeyCode key)
        {
            foreach (var window in RegisteredWindows)
            {
                if (window.IsActive && IsWindow(window.WindowHandle))
                {
                    SendHardwareKeyToWindow(window.WindowHandle, key);
                }
            }
        }

        public void SendHardwareKeyDown(VirtualKeyCode key)
        {
            foreach (var window in RegisteredWindows)
            {
                if (window.IsActive && IsWindow(window.WindowHandle))
                {
                    if (!_activeKeys.ContainsKey(window.WindowHandle))
                        _activeKeys[window.WindowHandle] = new HashSet<VirtualKeyCode>();
                    
                    _activeKeys[window.WindowHandle].Add(key);
                    SendHardwareKeyDownToWindow(window.WindowHandle, key);
                }
            }
        }

        public void SendHardwareKeyUp(VirtualKeyCode key)
        {
            foreach (var window in RegisteredWindows)
            {
                if (window.IsActive && IsWindow(window.WindowHandle))
                {
                    if (_activeKeys.ContainsKey(window.WindowHandle))
                        _activeKeys[window.WindowHandle].Remove(key);
                    
                    SendHardwareKeyUpToWindow(window.WindowHandle, key);
                }
            }
        }

        private void SendHardwareKeyToWindow(IntPtr windowHandle, VirtualKeyCode key)
        {
            // Try hardware simulation first
            if (TryHardwareSimulation(windowHandle, key))
                return;
            
            // Fallback to enhanced SendInput with hardware-like timing
            SendEnhancedInputSimulation(windowHandle, key);
        }

        private void SendHardwareKeyDownToWindow(IntPtr windowHandle, VirtualKeyCode key)
        {
            if (TryHardwareKeyDown(windowHandle, key))
                return;
            
            SendEnhancedInputKeyDown(windowHandle, key);
        }

        private void SendHardwareKeyUpToWindow(IntPtr windowHandle, VirtualKeyCode key)
        {
            if (TryHardwareKeyUp(windowHandle, key))
                return;
            
            SendEnhancedInputKeyUp(windowHandle, key);
        }

        private bool TryHardwareSimulation(IntPtr windowHandle, VirtualKeyCode key)
        {
            try
            {
                // Use hardware-specific timing and scan codes
                var keyboardDevice = _keyboardDevices.FirstOrDefault();
                if (keyboardDevice == null)
                    return false;

                // Get real hardware scan code
                uint scanCode = MapVirtualKey((uint)key, MAPVK_VK_TO_VSC);
                
                // Create hardware input with real device information
                var inputs = new INPUT[2];
                
                // Key down with hardware scan code
                inputs[0] = new INPUT
                {
                    type = INPUT_KEYBOARD,
                    u = new InputUnion
                    {
                        ki = new KEYBDINPUT
                        {
                            wVk = 0, // Use scan code instead of virtual key
                            wScan = (ushort)scanCode,
                            dwFlags = KEYEVENTF_SCANCODE,
                            time = 0, // Let system generate timestamp
                            dwExtraInfo = new IntPtr(keyboardDevice.VendorId << 16 | keyboardDevice.ProductId) // Hardware ID
                        }
                    }
                };
                
                // Key up with hardware scan code
                inputs[1] = new INPUT
                {
                    type = INPUT_KEYBOARD,
                    u = new InputUnion
                    {
                        ki = new KEYBDINPUT
                        {
                            wVk = 0,
                            wScan = (ushort)scanCode,
                            dwFlags = KEYEVENTF_SCANCODE | KEYEVENTF_KEYUP,
                            time = 0,
                            dwExtraInfo = new IntPtr(keyboardDevice.VendorId << 16 | keyboardDevice.ProductId)
                        }
                    }
                };

                // Send with realistic hardware timing
                uint result1 = SendInput(1, new[] { inputs[0] }, Marshal.SizeOf(typeof(INPUT)));
                Thread.Sleep(10); // Hardware-like key press duration
                uint result2 = SendInput(1, new[] { inputs[1] }, Marshal.SizeOf(typeof(INPUT)));

                return result1 == 1 && result2 == 1;
            }
            catch
            {
                return false;
            }
        }

        private bool TryHardwareKeyDown(IntPtr windowHandle, VirtualKeyCode key)
        {
            try
            {
                var keyboardDevice = _keyboardDevices.FirstOrDefault();
                if (keyboardDevice == null)
                    return false;

                uint scanCode = MapVirtualKey((uint)key, MAPVK_VK_TO_VSC);
                
                var input = new INPUT
                {
                    type = INPUT_KEYBOARD,
                    u = new InputUnion
                    {
                        ki = new KEYBDINPUT
                        {
                            wVk = 0,
                            wScan = (ushort)scanCode,
                            dwFlags = KEYEVENTF_SCANCODE,
                            time = 0,
                            dwExtraInfo = new IntPtr(keyboardDevice.VendorId << 16 | keyboardDevice.ProductId)
                        }
                    }
                };

                uint result = SendInput(1, new[] { input }, Marshal.SizeOf(typeof(INPUT)));
                return result == 1;
            }
            catch
            {
                return false;
            }
        }

        private bool TryHardwareKeyUp(IntPtr windowHandle, VirtualKeyCode key)
        {
            try
            {
                var keyboardDevice = _keyboardDevices.FirstOrDefault();
                if (keyboardDevice == null)
                    return false;

                uint scanCode = MapVirtualKey((uint)key, MAPVK_VK_TO_VSC);
                
                var input = new INPUT
                {
                    type = INPUT_KEYBOARD,
                    u = new InputUnion
                    {
                        ki = new KEYBDINPUT
                        {
                            wVk = 0,
                            wScan = (ushort)scanCode,
                            dwFlags = KEYEVENTF_SCANCODE | KEYEVENTF_KEYUP,
                            time = 0,
                            dwExtraInfo = new IntPtr(keyboardDevice.VendorId << 16 | keyboardDevice.ProductId)
                        }
                    }
                };

                uint result = SendInput(1, new[] { input }, Marshal.SizeOf(typeof(INPUT)));
                return result == 1;
            }
            catch
            {
                return false;
            }
        }

        private void SendEnhancedInputSimulation(IntPtr windowHandle, VirtualKeyCode key)
        {
            // Enhanced fallback with thread attachment and hardware timing
            try
            {
                uint currentThreadId = GetCurrentThreadId();
                GetWindowThreadProcessId(windowHandle, out uint windowThreadId);

                bool attached = false;
                if (currentThreadId != windowThreadId)
                {
                    attached = AttachThreadInput(currentThreadId, windowThreadId, true);
                }

                uint scanCode = MapVirtualKey((uint)key, MAPVK_VK_TO_VSC);
                
                var inputs = new INPUT[2];
                inputs[0] = new INPUT
                {
                    type = INPUT_KEYBOARD,
                    u = new InputUnion
                    {
                        ki = new KEYBDINPUT
                        {
                            wVk = (ushort)key,
                            wScan = (ushort)scanCode,
                            dwFlags = KEYEVENTF_SCANCODE,
                            time = 0,
                            dwExtraInfo = IntPtr.Zero
                        }
                    }
                };
                
                inputs[1] = new INPUT
                {
                    type = INPUT_KEYBOARD,
                    u = new InputUnion
                    {
                        ki = new KEYBDINPUT
                        {
                            wVk = (ushort)key,
                            wScan = (ushort)scanCode,
                            dwFlags = KEYEVENTF_SCANCODE | KEYEVENTF_KEYUP,
                            time = 0,
                            dwExtraInfo = IntPtr.Zero
                        }
                    }
                };

                SendInput(1, new[] { inputs[0] }, Marshal.SizeOf(typeof(INPUT)));
                Thread.Sleep(10);
                SendInput(1, new[] { inputs[1] }, Marshal.SizeOf(typeof(INPUT)));

                if (attached)
                {
                    AttachThreadInput(currentThreadId, windowThreadId, false);
                }
            }
            catch (Exception ex)
            {
                OnStatusUpdate?.Invoke($"Enhanced input simulation failed: {ex.Message}");
            }
        }

        private void SendEnhancedInputKeyDown(IntPtr windowHandle, VirtualKeyCode key)
        {
            try
            {
                uint scanCode = MapVirtualKey((uint)key, MAPVK_VK_TO_VSC);
                
                var input = new INPUT
                {
                    type = INPUT_KEYBOARD,
                    u = new InputUnion
                    {
                        ki = new KEYBDINPUT
                        {
                            wVk = (ushort)key,
                            wScan = (ushort)scanCode,
                            dwFlags = KEYEVENTF_SCANCODE,
                            time = 0,
                            dwExtraInfo = IntPtr.Zero
                        }
                    }
                };

                SendInput(1, new[] { input }, Marshal.SizeOf(typeof(INPUT)));
            }
            catch (Exception ex)
            {
                OnStatusUpdate?.Invoke($"Enhanced input key down failed: {ex.Message}");
            }
        }

        private void SendEnhancedInputKeyUp(IntPtr windowHandle, VirtualKeyCode key)
        {
            try
            {
                uint scanCode = MapVirtualKey((uint)key, MAPVK_VK_TO_VSC);
                
                var input = new INPUT
                {
                    type = INPUT_KEYBOARD,
                    u = new InputUnion
                    {
                        ki = new KEYBDINPUT
                        {
                            wVk = (ushort)key,
                            wScan = (ushort)scanCode,
                            dwFlags = KEYEVENTF_SCANCODE | KEYEVENTF_KEYUP,
                            time = 0,
                            dwExtraInfo = IntPtr.Zero
                        }
                    }
                };

                SendInput(1, new[] { input }, Marshal.SizeOf(typeof(INPUT)));
            }
            catch (Exception ex)
            {
                OnStatusUpdate?.Invoke($"Enhanced input key up failed: {ex.Message}");
            }
        }

        private void MaintainKeyStates(object? state)
        {
            if (!IsRealTimeBroadcastEnabled) return;

            // Re-send key down messages for all active keys
            foreach (var windowKeys in _activeKeys.ToArray())
            {
                var windowHandle = windowKeys.Key;
                var keys = windowKeys.Value;

                if (!IsWindow(windowHandle))
                {
                    _activeKeys.Remove(windowHandle);
                    continue;
                }

                foreach (var key in keys.ToArray())
                {
                    SendHardwareKeyDownToWindow(windowHandle, key);
                }
            }
        }

        public void HandleExternalKeyDown(VirtualKeyCode key)
        {
            if (IsRealTimeBroadcastEnabled)
            {
                SendHardwareKeyDown(key);
            }
        }

        public void HandleExternalKeyUp(VirtualKeyCode key)
        {
            if (IsRealTimeBroadcastEnabled)
            {
                SendHardwareKeyUp(key);
            }
        }

        public string GetHardwareInfo()
        {
            var info = new StringBuilder();
            info.AppendLine("Hardware Devices Found:");
            
            foreach (var device in _keyboardDevices)
            {
                info.AppendLine($"Keyboard: {device.ProductName} (VID:{device.VendorId:X4}, PID:{device.ProductId:X4})");
            }
            
            foreach (var device in _mouseDevices)
            {
                info.AppendLine($"Mouse: {device.ProductName} (VID:{device.VendorId:X4}, PID:{device.ProductId:X4})");
            }
            
            return info.ToString();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                StopRealTimeBroadcast();
                
                // Close all device handles
                foreach (var device in _keyboardDevices)
                {
                    if (device.Handle != IntPtr.Zero)
                        CloseHandle(device.Handle);
                }
                
                foreach (var device in _mouseDevices)
                {
                    if (device.Handle != IntPtr.Zero)
                        CloseHandle(device.Handle);
                }
                
                _stateTimer?.Dispose();
                _disposed = true;
            }
        }
    }
}