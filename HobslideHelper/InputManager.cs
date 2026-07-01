using SharpDX.DirectInput;
using SharpDX.XInput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;

namespace HobslideHelper
{
    public enum InputBackend
    {
        None,
        DirectInput,
        XInput
    }

    public class InputButton
    {
        public InputBackend Backend;

        public string DeviceGuid;
        public int ButtonIndex;
    }

    class DirectInputDevice
    {
        public Joystick Joystick;
        public Guid Guid;
        public string DeviceGuid;
    }

    class XInputDevice
    {
        public Controller Controller;
        public UserIndex UserIndex;
        public string DeviceGuid;
    }

    public class InputManager
    {
        DirectInput directInput;

        List<DirectInputDevice> directInputDevices =
            new List<DirectInputDevice>();

        List<XInputDevice> xInputDevices =
            new List<XInputDevice>();

        class XInputButtonInfo
        {
            public Func<Gamepad, bool> IsPressed { get; set; }
        }

        static readonly XInputButtonInfo[] XInputButtons =
        {
            new XInputButtonInfo { IsPressed = g => g.Buttons.HasFlag(GamepadButtonFlags.A) },
            new XInputButtonInfo { IsPressed = g => g.Buttons.HasFlag(GamepadButtonFlags.B) },
            new XInputButtonInfo { IsPressed = g => g.Buttons.HasFlag(GamepadButtonFlags.X) },
            new XInputButtonInfo { IsPressed = g => g.Buttons.HasFlag(GamepadButtonFlags.Y) },

            new XInputButtonInfo { IsPressed = g => g.Buttons.HasFlag(GamepadButtonFlags.LeftShoulder) },
            new XInputButtonInfo { IsPressed = g => g.Buttons.HasFlag(GamepadButtonFlags.RightShoulder) },

            new XInputButtonInfo { IsPressed = g => g.Buttons.HasFlag(GamepadButtonFlags.Back) },
            new XInputButtonInfo { IsPressed = g => g.Buttons.HasFlag(GamepadButtonFlags.Start) },

            new XInputButtonInfo { IsPressed = g => g.Buttons.HasFlag(GamepadButtonFlags.LeftThumb) },
            new XInputButtonInfo { IsPressed = g => g.Buttons.HasFlag(GamepadButtonFlags.RightThumb) },

            new XInputButtonInfo { IsPressed = g => g.Buttons.HasFlag(GamepadButtonFlags.DPadUp) },
            new XInputButtonInfo { IsPressed = g => g.Buttons.HasFlag(GamepadButtonFlags.DPadDown) },
            new XInputButtonInfo { IsPressed = g => g.Buttons.HasFlag(GamepadButtonFlags.DPadLeft) },
            new XInputButtonInfo { IsPressed = g => g.Buttons.HasFlag(GamepadButtonFlags.DPadRight) },

            // LT
            new XInputButtonInfo { IsPressed = g => g.LeftTrigger == 255 },

            // RT
            new XInputButtonInfo { IsPressed = g => g.RightTrigger == 255 }
        };

        public bool Connected =>
                    directInputDevices.Count > 0 ||
                    xInputDevices.Count > 0;

        public InputManager()
        {
            Initialize();
        }

        void Initialize()
        {
            InitializeXInput();

            InitializeDirectInput();
        }

        void InitializeXInput()
        {
            xInputDevices.Clear();

            foreach (UserIndex index in Enum.GetValues(typeof(UserIndex)))
            {
                if (index == UserIndex.Any)
                    continue;

                try
                {
                    Controller controller =
                        new Controller(index);

                    if (controller.IsConnected)
                    {
                        xInputDevices.Add(new XInputDevice
                        {
                            Controller = controller,
                            UserIndex = index,

                            // 固定ID化
                            DeviceGuid = $"XINPUT_{index}"
                        });
                    }
                }
                catch
                {
                }
            }
        }

        void InitializeDirectInput()
        {
            directInputDevices.Clear();

            try
            {
                directInput = new DirectInput();

                var devices = directInput.GetDevices(
                    SharpDX.DirectInput.DeviceType.Gamepad,
                    DeviceEnumerationFlags.AttachedOnly);

                foreach (var device in devices)
                {
                    // XInput重複除外
                    if (IsXInputDevice(device.InstanceGuid))
                        continue;

                    try
                    {
                        Joystick joystick =
                            new Joystick(directInput, device.InstanceGuid);

                        joystick.Properties.BufferSize = 128;

                        joystick.Acquire();

                        directInputDevices.Add(new DirectInputDevice
                        {
                            Joystick = joystick,
                            Guid = device.InstanceGuid,
                            DeviceGuid = device.InstanceGuid.ToString()
                        });
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
            }
        }

        bool IsXInputDevice(Guid guid)
        {
            try
            {
                using (var searcher =
                    new ManagementObjectSearcher(
                    "SELECT * FROM Win32_PNPEntity"))
                {
                    foreach (var device in searcher.Get())
                    {
                        string deviceId =
                            device["PNPDeviceID"]?.ToString();

                        if (string.IsNullOrEmpty(deviceId))
                            continue;

                        // XInputデバイス判定
                        if (deviceId.Contains("IG_"))
                        {
                            string guidString =
                                guid.ToString()
                                .Replace("-", "")
                                .ToUpper();

                            if (deviceId.ToUpper().Contains(
                                guidString.Substring(0, 8)))
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            catch
            {
            }

            return false;
        }

        public bool GetButton(InputButton button)
        {
            try
            {
                switch (button.Backend)
                {
                    case InputBackend.DirectInput:

                        return GetDirectInputButton(
                            button.DeviceGuid,
                            button.ButtonIndex);

                    case InputBackend.XInput:

                        return GetXInputButton(
                            button.DeviceGuid,
                            button.ButtonIndex);
                }
            }
            catch
            {
            }

            return false;
        }

        bool GetDirectInputButton(string deviceGuid, int buttonIndex)
        {
            var device =
                directInputDevices.FirstOrDefault(
                    d => d.DeviceGuid == deviceGuid);

            if (device == null)
                return false;

            var joystick = device.Joystick;

            joystick.Poll();

            var state = joystick.GetCurrentState();

            if (state == null)
                return false;

            if (buttonIndex < 0 ||
                buttonIndex >= state.Buttons.Length)
                return false;

            return state.Buttons[buttonIndex];
        }

        bool GetXInputButton(string deviceGuid, int buttonIndex)
        {
            var device =
                xInputDevices.FirstOrDefault(
                    d => d.DeviceGuid == deviceGuid);

            if (device == null)
                return false;

            if (!device.Controller.IsConnected)
                return false;

            if (buttonIndex < 0 ||
                buttonIndex >= XInputButtons.Length)
                return false;

            var gamepad = device.Controller.GetState().Gamepad;

            return XInputButtons[buttonIndex].IsPressed(gamepad);
        }

        public bool IsAnyButtonPressed(
            out InputBackend backend,
            out string deviceGuid,
            out int buttonIndex)
        {
            backend = InputBackend.None;

            deviceGuid = string.Empty;

            buttonIndex = -1;

            // DirectInput
            for (int i = 0; i < directInputDevices.Count; i++)
            {
                try
                {
                    var joystick =
                        directInputDevices[i].Joystick;

                    joystick.Poll();

                    var state =
                        joystick.GetCurrentState();

                    for (int b = 0;
                        b < state.Buttons.Length;
                        b++)
                    {
                        if (state.Buttons[b])
                        {
                            backend =
                                InputBackend.DirectInput;

                            deviceGuid = directInputDevices[i].DeviceGuid;

                            buttonIndex = b;

                            return true;
                        }
                    }
                }
                catch
                {
                }
            }

            // XInput
            for (int i = 0; i < xInputDevices.Count; i++)
            {
                try
                {
                    var controller = xInputDevices[i].Controller;

                    if (!controller.IsConnected)
                        continue;

                    var gamepad = controller.GetState().Gamepad;

                    for (int b = 0; b < XInputButtons.Length; b++)
                    {
                        if (XInputButtons[b].IsPressed(gamepad))
                        {
                            backend = InputBackend.XInput;
                            deviceGuid = xInputDevices[i].DeviceGuid;
                            buttonIndex = b;
                            return true;
                        }
                    }
                }
                catch
                {
                }
            }

            return false;
        }
    }
}