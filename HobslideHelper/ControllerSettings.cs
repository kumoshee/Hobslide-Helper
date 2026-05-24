using System;
using System.Reflection.Emit;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HobslideHelper
{
    public partial class ControllerSettings : Form
    {
        InputManager inputManager;
        ControllerConfig config;
        bool waitingInput = false;

        enum WaitingType
        {
            None,
            R1,
            Square,
            Cross
        }

        WaitingType waitingType = WaitingType.None;

        public ControllerSettings(InputManager manager)
        {
            InitializeComponent();
            inputManager = manager;
            config = ControllerConfig.Load();
            RefreshLabels();
        }

        void RefreshLabels()
        {
            lblR1.Text =
                $"Button {config.R1Button}";
            lblSquare.Text =
                $"Button {config.SquareButton}";
            lblCross.Text =
                $"Button {config.CrossButton}";
        }

        async Task WaitButtonAsync()
        {
            waitingInput = true;
            lblStatus.Text = "Press controller button...";

            while (waitingInput)
            {
                if (inputManager.IsAnyButtonPressed(
                    out InputBackend backend,
                    out string deviceGuid,
                    out int buttonIndex))
                {
                    switch (waitingType)
                    {
                        case WaitingType.R1:

                            config.R1Backend = backend;
                            config.R1DeviceGuid = deviceGuid;
                            config.R1Button = buttonIndex;

                            break;

                        case WaitingType.Square:

                            config.SquareBackend = backend;
                            config.SquareDeviceGuid = deviceGuid;
                            config.SquareButton = buttonIndex;

                            break;

                        case WaitingType.Cross:

                            config.CrossBackend = backend;
                            config.CrossDeviceGuid = deviceGuid;
                            config.CrossButton = buttonIndex;

                            break;
                    }

                    RefreshLabels();

                    lblStatus.Text =
                        $"Assigned Button {buttonIndex}";

                    waitingInput = false;

                    return;
                }

                await Task.Delay(10);
            }
        }

        private async void BtnR1_Click(object sender, EventArgs e)
        {
            waitingType = WaitingType.R1;
            await WaitButtonAsync();
        }

        private async void BtnSquare_Click(object sender, EventArgs e)
        {
            waitingType = WaitingType.Square;
            await WaitButtonAsync();
        }

        private async void BtnCross_Click(object sender, EventArgs e)
        {
            waitingType = WaitingType.Cross;
            await WaitButtonAsync();
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            config.Save();

            lblStatus.Text = "Saved";
        }

        public ControllerConfig GetConfig()
        {
            return config;
        }
    }
}