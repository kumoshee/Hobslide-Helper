using System;
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
            lblR1.Text = $"Button: {config.R1Button}";
            lblSquare.Text = $"Button: {config.SquareButton}";
            lblCross.Text = $"Button: {config.CrossButton}";
        }

        async Task WaitButtonAsync()
        {
            waitingInput = true;

            lblStatus.Text = "Press controller button...";

            while (waitingInput)
            {
                if (inputManager.IsAnyButtonPressed(out int btn))
                {
                    switch (waitingType)
                    {
                        case WaitingType.R1:
                            config.R1Button = btn;
                            break;

                        case WaitingType.Square:
                            config.SquareButton = btn;
                            break;

                        case WaitingType.Cross:
                            config.CrossButton = btn;
                            break;
                    }

                    RefreshLabels();

                    lblStatus.Text = $"Assigned Button: {btn}";

                    waitingInput = false;

                    return;
                }

                await Task.Delay(10);
            }
        }

        private async void btnR1_Click(object sender, EventArgs e)
        {
            waitingType = WaitingType.R1;
            await WaitButtonAsync();
        }

        private async void btnSquare_Click(object sender, EventArgs e)
        {
            waitingType = WaitingType.Square;
            await WaitButtonAsync();
        }

        private async void btnCross_Click(object sender, EventArgs e)
        {
            waitingType = WaitingType.Cross;
            await WaitButtonAsync();
        }

        private void btnReload_Click(object sender, EventArgs e)
        {
            bool ok = inputManager.Reload();

            lblStatus.Text = ok
                ? "Controller reloaded"
                : "Controller not found";
        }

        private void btnSave_Click(object sender, EventArgs e)
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