using SharpDX.DirectInput;
using SharpDX.XInput;
using System.Linq;

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
        public int ButtonIndex;
    }

    public class InputManager
    {
        DirectInput directInput;
        Joystick joystick;

        Controller xController;

        public bool Connected { get; private set; }

        public InputBackend CurrentBackend { get; private set; }

        public InputManager()
        {
            Initialize();
        }

        void Initialize()
        {
            // ----- XInput優先 -----
            xController = new Controller(UserIndex.One);

            if (xController.IsConnected)
            {
                CurrentBackend = InputBackend.XInput;
                Connected = true;
                return;
            }

            // ----- DirectInput fallback -----
            try
            {
                directInput = new DirectInput();

                var device = directInput
                    .GetDevices(SharpDX.DirectInput.DeviceType.Gamepad, DeviceEnumerationFlags.AllDevices)
                    .FirstOrDefault();

                if (device != null)
                {
                    joystick = new Joystick(directInput, device.InstanceGuid);

                    joystick.Properties.BufferSize = 128;
                    joystick.Acquire();

                    CurrentBackend = InputBackend.DirectInput;
                    Connected = true;
                    return;
                }
            }
            catch
            {
            }

            CurrentBackend = InputBackend.None;
            Connected = false;
        }

        public bool GetButton(InputButton button)
        {
            if (!Connected)
                return false;

            try
            {
                switch (button.Backend)
                {
                    case InputBackend.XInput:
                        return GetXInputButton(button.ButtonIndex);

                    case InputBackend.DirectInput:
                        return GetDirectInputButton(button.ButtonIndex);
                }
            }
            catch
            {
            }

            return false;
        }

        bool GetDirectInputButton(int index)
        {
            if (joystick == null)
                return false;

            joystick.Poll();

            var state = joystick.GetCurrentState();

            if (state == null)
                return false;

            if (index < 0 || index >= state.Buttons.Length)
                return false;

            return state.Buttons[index];
        }

        bool GetXInputButton(int index)
        {
            if (xController == null || !xController.IsConnected)
                return false;

            var state = xController.GetState();

            GamepadButtonFlags flags = state.Gamepad.Buttons;

            switch (index)
            {
                case 0: return flags.HasFlag(GamepadButtonFlags.X);              // □
                case 1: return flags.HasFlag(GamepadButtonFlags.A);              // ✕
                case 2: return flags.HasFlag(GamepadButtonFlags.RightShoulder);  // R1
            }

            return false;
        }

        public bool Reload()
        {
            try
            {
                joystick?.Unacquire();
                joystick?.Dispose();

                joystick = null;

                Initialize();

                return Connected;
            }
            catch
            {
                return false;
            }
        }

        public bool IsAnyButtonPressed(out int pressedButton)
        {
            pressedButton = -1;

            try
            {
                switch (CurrentBackend)
                {
                    case InputBackend.DirectInput:

                        joystick.Poll();

                        var state = joystick.GetCurrentState();

                        for (int i = 0; i < state.Buttons.Length; i++)
                        {
                            if (state.Buttons[i])
                            {
                                pressedButton = i;
                                return true;
                            }
                        }

                        break;

                    case InputBackend.XInput:

                        var xState = xController.GetState();

                        var flags = xState.Gamepad.Buttons;

                        if (flags.HasFlag(GamepadButtonFlags.X))
                        {
                            pressedButton = 0;
                            return true;
                        }

                        if (flags.HasFlag(GamepadButtonFlags.A))
                        {
                            pressedButton = 1;
                            return true;
                        }

                        if (flags.HasFlag(GamepadButtonFlags.RightShoulder))
                        {
                            pressedButton = 2;
                            return true;
                        }

                        break;
                }
            }
            catch
            {
            }

            return false;
        }
    }
}