using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using SharpDX.DirectInput;
using System.Linq;

namespace HobslideHelper
{
    public partial class Form1 : Form
    {
        DirectInput directInput;
        Joystick joystick;
        Stopwatch stopwatch;

        const double FrameTime = 1000.0 / 60.0; // 16.666ms

        int frameCounter = 0;

        bool r1Pressed = false;
        bool squarePressed = false;
        bool crossPressed = false;

        int r1PressFrame = 0;
        int squarePressFrame = 0;
        int crossPressFrame = 0;

        int r1ReleaseFrame = 0;
        int squareReleaseFrame = 0;
        int crossReleaseFrame = 0;

        int r1HoldFrames = 0;
        int squareHoldFrames = 0;
        int crossHoldFrames = 0;

        int r1ToSquareFrames = 0; // [4] R1 -> □
        int squareToR1Frames = 0; // [5] □ -> R1
        int squareToCrossFrames = 0; // [6] □ -> ✕ 
        int r1ToR1Frames = 0; // [7] R1 -> R1
        int squareToSquareFrames = 0; // [8] □ -> □
        
        int lastR1PressFrame = 0;
        int lastSquarePressFrame = 0;
        int prevR1PressFrame = 0;
        int prevSquarePressFrame = 0;

        int targetFrame = 0;
        
        // Graphing data
        List<int> squareToSquareHistory = new List<int>();

        Rectangle graphRect;

        public Form1()
        {
            InitializeComponent();
            InitializeJoystick();
            StartGameLoop();
            this.GetType()
                .GetProperty("DoubleBuffered",
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic)
                .SetValue(this, true, null);
            cmbMode.SelectedIndex = 0;
            graphRect = new Rectangle(40, 540, 540, 150);
        }

        private void cmbMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            squareToSquareHistory.Clear();
            this.Invalidate(graphRect);

            labelEvalR1ToSquare.Text = "";
            labelEvalSquareToR1.Text = "";
            labelGraphAverage.Text = $"AVG:";
        }

        private void ChkCrossButtonReset_CheckedChanged(object sender, EventArgs e)
        {

        }
        private void ChkOverlay_CheckedChanged(object sender, EventArgs e)
        {
            if (chkOverlay.Checked)
            {
                this.FormBorderStyle = FormBorderStyle.None;
                this.TopMost = true;
                this.TransparencyKey = this.BackColor;
            }
            else
            {
                this.FormBorderStyle = FormBorderStyle.Sizable;
                this.TopMost = false;
                this.TransparencyKey = Color.Empty;
            }
        }

        // Draggable Overlay Window Setup
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [System.Runtime.InteropServices.DllImportAttribute("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [System.Runtime.InteropServices.DllImportAttribute("user32.dll")]
        public static extern bool ReleaseCapture();

        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && chkOverlay.Checked)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // 背景（Overlay時は描かない）
            if (!chkOverlay.Checked)
            {
                using (SolidBrush bg = new SolidBrush(Color.FromArgb(20, 20, 20)))
                {
                    g.FillRectangle(bg, graphRect);
                }
            }

            // グリッド
            using (Pen gridPen = new Pen(Color.FromArgb(50, 50, 50)))
            {
                for (int i = 0; i <= 30; i += 5)
                {
                    float yy = graphRect.Bottom - (i * graphRect.Height / 30f);
                    g.DrawLine(gridPen, graphRect.Left, yy, graphRect.Right, yy);
                }
            }

            if (squareToSquareHistory.Count == 0)
                return;

            int count = squareToSquareHistory.Count;

            // 全幅を必ず使用
            float margin = 20f;
            float stepX = (float)(graphRect.Width - margin * 2) / Math.Max(1, count - 1);

            List<PointF> points = new List<PointF>();

            for (int i = 0; i < count; i++)
            {
                int val = Math.Max(30, Math.Min(60, squareToSquareHistory[i]));
                float x = graphRect.Left + margin + i * stepX;
                float y = graphRect.Bottom - ((val - 30) * graphRect.Height / 30f);
                points.Add(new PointF(x, y));
            }

            // 線描画
            using (Pen linePen = new Pen(Color.Cyan, 2))
            {
                if (points.Count >= 2)
                    g.DrawLines(linePen, points.ToArray());
            }

            // 点描画（評価別色）
            for (int i = 0; i < points.Count; i++)
            {
                int val = squareToSquareHistory[i];
                Brush brush = GetPointBrush(val);
                g.FillEllipse(brush, points[i].X - 4, points[i].Y - 4, 8, 8);
            }

            // 平均表示
            double avg = squareToSquareHistory.Average();
            labelGraphAverage.Text = $"AVG: {avg:F2}F";
        }

        void InitializeJoystick()
        {
            try
            {
                directInput = new DirectInput();

                foreach (var device in directInput.GetDevices(DeviceType.Gamepad, DeviceEnumerationFlags.AllDevices))
                {
                    joystick = new Joystick(directInput, device.InstanceGuid);
                    break;
                }

                if (joystick == null)
                {
                    Console.WriteLine("Controller not found");
                    return;
                }

                joystick.Properties.BufferSize = 128;
                joystick.Acquire();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Joystick initialization failed: " + ex.Message);
            }
        }

        async void StartGameLoop()
        {
            stopwatch = new Stopwatch();
            stopwatch.Start();

            double nextFrameTime = 0;

            while (true)
            {
                double currentTime = stopwatch.Elapsed.TotalMilliseconds;

                if (currentTime >= nextFrameTime)
                {
                    UpdateFrame();
                    nextFrameTime += FrameTime;
                }

                await Task.Delay(1);
            }
        }

        void UpdateFrame()
        {
            frameCounter++;

            bool r1Now = false, squareNow = false, crossNow = false;

            if (joystick != null)
            {
                try
                {
                    joystick.Poll();
                    var state = joystick.GetCurrentState();
                    var buttons = state.Buttons;
                    r1Now = buttons[5];     // R1
                    squareNow = buttons[0]; // □
                    crossNow = buttons[1];  // ✕
                }
                catch { }
            }

            // --- 1. R1 State Management ---
            if (r1Now && !r1Pressed) // On R1 Press
            {
                r1PressFrame = frameCounter;
                prevR1PressFrame = lastR1PressFrame;
                lastR1PressFrame = frameCounter;
                picR1State.Image = Properties.Resources.on;

                // 5. □ -> R1
                if (lastSquarePressFrame > 0 && lastSquarePressFrame > prevR1PressFrame)
                {
                    squareToR1Frames = frameCounter - lastSquarePressFrame;
                    UpdateEvalSquareToR1(); // Trigger Evaluation 6.2 correctly on R1 press
                }

                // 7. R1 -> R1
                if (prevR1PressFrame > 0)
                {
                    r1ToR1Frames = frameCounter - prevR1PressFrame;
                }
            }
            if (r1Now)
            {
                // Dynamic hold display updates without flickering
                r1HoldFrames = frameCounter - r1PressFrame;
            }
            else if (r1Pressed && !r1Now) // On Release
            {
                r1ReleaseFrame = frameCounter;
                r1HoldFrames = r1ReleaseFrame - r1PressFrame;
                labelR1Hold.Text = $"{r1HoldFrames}F";
                picR1State.Image = Properties.Resources.off;
            }

            // --- 2. □ State Management ---
            if (squareNow && !squarePressed) // On □ Press
            {
                squarePressFrame = frameCounter;
                prevSquarePressFrame = lastSquarePressFrame;
                lastSquarePressFrame = frameCounter;
                picSquareState.Image = Properties.Resources.on;

                // 4. R1 -> □
                if (lastR1PressFrame > 0 && lastR1PressFrame > prevSquarePressFrame)
                {
                    r1ToSquareFrames = frameCounter - lastR1PressFrame;
                    UpdateEvalR1ToSquare(); // Trigger Evaluation 6.1 correctly on Square press
                }

                // 8. □ -> □
                if (prevSquarePressFrame > 0)
                {
                    squareToSquareFrames = frameCounter - prevSquarePressFrame;
                    // Add to History if between 30 and 60 frames
                    if (squareToSquareFrames >= 30 && squareToSquareFrames <= 60)
                    {
                        squareToSquareHistory.Add(squareToSquareFrames);
                        this.Invalidate(graphRect);
                    }
                }
            }
            if (squareNow)
            {
                squareHoldFrames = frameCounter - squarePressFrame;
            }
            else if (squarePressed && !squareNow) // On Release
            {
                squareReleaseFrame = frameCounter;
                squareHoldFrames = squareReleaseFrame - squarePressFrame;
                labelSquareHold.Text = $"{squareHoldFrames}F";
                picSquareState.Image = Properties.Resources.off;
            }

            // --- 3. ✕ State Management ---
            if (crossNow && !crossPressed)
            {
                crossPressFrame = frameCounter;
                picCrossState.Image = Properties.Resources.on;

                squareToSquareHistory.Clear();
                this.Invalidate(graphRect);
                labelGraphAverage.Text = $"AVG:";

                // 6. □ -> ✕
                if (lastSquarePressFrame > 0 && lastSquarePressFrame > r1PressFrame)
                {
                    squareToCrossFrames = frameCounter - lastSquarePressFrame;
                }
            }
            if (crossNow)
            {
                crossHoldFrames = frameCounter - crossPressFrame;
            }
            else if (crossPressed && !crossNow)
            {
                crossReleaseFrame = frameCounter;
                crossHoldFrames = crossReleaseFrame - crossPressFrame;
                labelCrossHold.Text = $"{crossHoldFrames}F";
                picCrossState.Image = Properties.Resources.off;
            }

            r1Pressed = r1Now;
            squarePressed = squareNow;
            crossPressed = crossNow;

            // 9. R1 -> □ -> R1
            int r1SquareR1Total = r1ToSquareFrames + squareToR1Frames;

            // Update Constant Frame Displays
            labelR1ToSquare.Text = $"{r1ToSquareFrames}F";
            labelSquareToR1.Text = $"{squareToR1Frames}F";
            labelSquareToCross.Text = $"{squareToCrossFrames}F";
            labelSquareToSquare.Text = $"{squareToSquareFrames}F";
        }

        void UpdateEvalR1ToSquare()
        {
            // 6.1 R1 → □ の評価（項目4）
            // 計算式: [4のフレーム数] - (targetFrame - [5のフレーム数])
            // この評価は□が確定したタイミングで行う
            int modeIdx = cmbMode.SelectedIndex;
            if (cmbMode.InvokeRequired)
                cmbMode.Invoke(new Action(() => { modeIdx = cmbMode.SelectedIndex; }));
            else
                modeIdx = cmbMode.SelectedIndex;

            switch(modeIdx){
                case 0: targetFrame = 45; break; // Average
                case 1: targetFrame = 42; break; // Fast
                case 2: targetFrame = 48; break; // Slow
            }
            
            int diff4 = r1ToSquareFrames - (targetFrame - squareToR1Frames);
            string eval4 = "";
            Color color4 = Color.White;
            labelEvalR1ToSquare.Tag = false;

            switch (modeIdx){
                case 0: // Average 45F
                    if (Math.Abs(diff4) == 0) { eval4 = "Perfect"; labelEvalR1ToSquare.Tag = true; } // Perfect 0
                    else if (Math.Abs(diff4) <= 2) { eval4 = "Great"; color4 = Color.Gold; } // Great ±1~±2
                    else if (Math.Abs(diff4) == 3) { eval4 = "Good"; color4 = Color.LimeGreen; } // Good ±3
                    else if (Math.Abs(diff4) <= 5) { eval4 = "Bad"; color4 = Color.DodgerBlue; } // Bad ±4~±5
                    else if (Math.Abs(diff4) >= 6) { eval4 = "Miss"; color4 = Color.Crimson; } // Miss ±6以上
                    break;
                case 1: // Fast 42F
                    if (diff4 < -2) { eval4 = "Miss"; color4 = Color.Crimson; } // Miss -2未満
                    else if (diff4 <= -1) { eval4 = "Dangerous"; color4 = Color.OrangeRed; } // Dangerous -2~-1
                    else if (diff4 <= 0) { eval4 = "Perfect"; labelEvalR1ToSquare.Tag = true; } // Perfect 0
                    else if (diff4 <= 3) { eval4 = "Great"; color4 = Color.Gold; } // Great 1~3
                    else if (diff4 <= 6) { eval4 = "Good"; color4 = Color.LimeGreen; } // Good 4~6
                    else if (diff4 <= 8) { eval4 = "Bad"; color4 = Color.DodgerBlue; } // Bad 7~8
                    else if (diff4 >= 9) { eval4 = "Miss"; color4 = Color.Crimson; } // Miss 9以上
                    break;
                case 2: // Slow 48F
                    if (diff4 <= -9) { eval4 = "Miss"; color4 = Color.Crimson; } // Miss -9以下
                    else if (diff4 <= -7) { eval4 = "Bad"; color4 = Color.DodgerBlue; } // Bad -8~-7
                    else if (diff4 <= -4) { eval4 = "Good"; color4 = Color.LimeGreen; } // Good -6~-4
                    else if (diff4 <= -1) { eval4 = "Great"; color4 = Color.Gold; } // Great -3~-1
                    else if (diff4 <= 0) { eval4 = "Perfect"; labelEvalR1ToSquare.Tag = true; } // Perfect 0
                    else if (diff4 <= 2) { eval4 = "Dangerous"; color4 = Color.OrangeRed; } // Dangerous 1~2
                    else if (diff4 >= 3) { eval4 = "Miss"; color4 = Color.Crimson; } // Miss 3以上
                    break;
            }

            labelEvalR1ToSquare.Text = $"{diff4:+0;-0;0} {eval4}";
            labelEvalR1ToSquare.ForeColor = color4;
            labelEvalR1ToSquare.Invalidate();
        }

        void UpdateEvalSquareToR1()
        {
            // 6.2 □ → R1 の評価（項目5）
            // 計算式: [5のフレーム数] - 24
            int modeIdx = cmbMode.SelectedIndex;
            if (cmbMode.InvokeRequired)
                cmbMode.Invoke(new Action(() => { modeIdx = cmbMode.SelectedIndex; }));
            else
                modeIdx = cmbMode.SelectedIndex;

            int diff5 = squareToR1Frames - 24;
            string eval5 = "";
            Color color5 = Color.White;
            labelEvalSquareToR1.Tag = false;

            if (diff5 <= -6) { eval5 = "Miss"; color5 = Color.Crimson; } // Miss -6以下
            else if (diff5 <= -4) { eval5 = "Bad"; color5 = Color.DodgerBlue; } // Bad -5~-4
            else if (diff5 <= -2) { eval5 = "Great"; color5 = Color.Gold; } // Great -3~-2
            else if (diff5 <= 1) { eval5 = "Perfect"; labelEvalSquareToR1.Tag = true; } // Perfect -1~1
            else if (diff5 <= 3) { eval5 = "Great"; color5 = Color.Gold; } // Great 2~3
            else if (diff5 <= 5){ eval5 = "Bad"; color5 = Color.DodgerBlue; } // Bad 4~5
            else if (diff5 >= 6){ eval5 = "Miss"; color5 = Color.Crimson; } // Miss 6以上

            labelEvalSquareToR1.Text = $"{diff5:+0;-0;0} {eval5}";
            labelEvalSquareToR1.ForeColor = color5;
            labelEvalSquareToR1.Invalidate();
        }

        Brush GetPointBrush(int value)
        {
            int modeIdx = cmbMode.SelectedIndex;

            switch(modeIdx)
            {
                case 0: // Average 45F
                    if (Math.Abs(value - 45) == 0) {return Brushes.Gold; } // Perfect 0
                    else if (Math.Abs(value - 45) <= 2) {return Brushes.Gold; } // Great ±1
                    else if (Math.Abs(value - 45) <= 3) {return Brushes.LimeGreen; } // Good ±2~±3
                    else if (Math.Abs(value - 45) <= 5) {return Brushes.DodgerBlue; } // Bad ±4~±5
                    else if (Math.Abs(value - 45) >= 6) {return Brushes.Crimson; } // Miss ±6以上
                    break;
                case 1: // Fast 42F
                    if (value - 42 < -2) {return Brushes.Crimson; } // Miss -2未満
                    else if (value - 42 <= -1) {return Brushes.OrangeRed; } // Dangerous -2~-1
                    else if (value - 42 <= 0) {return Brushes.Gold; } // Perfect 0
                    else if (value - 42 <= 3) {return Brushes.Gold; } // Great 1~3
                    else if (value - 42 <= 6) {return Brushes.LimeGreen; } // Good 4~6
                    else if (value - 42 <= 8) {return Brushes.DodgerBlue; } // Bad 7~8
                    else if (value - 42 >= 9) {return Brushes.Crimson; } // Miss 9以上
                    break;
                case 2: // Slow 48F
                    if (value - 48 <= -9) {return Brushes.Crimson; } // Miss -9以下
                    else if (value - 48 <= -7) {return Brushes.DodgerBlue; } // Bad -8~-7
                    else if (value - 48 <= -4) {return Brushes.LimeGreen; } // Good -6~-4
                    else if (value - 48 <= -1) {return Brushes.Gold; } // Great -3~-1
                    else if (value - 48 <= 0) {return Brushes.Gold; } // Perfect 0
                    else if (value - 48 <= 2) {return Brushes.OrangeRed; } // Dangerous 1~2
                    else if (value - 48 >= 3) {return Brushes.Crimson; } // Miss 3以上
                    break;
            }
            return Brushes.White;
        }

        private void LabelEval_Paint(object sender, PaintEventArgs e)
        {
            Label lbl = sender as Label;
            if (lbl == null || lbl.Tag == null || !(bool)lbl.Tag || string.IsNullOrEmpty(lbl.Text))
            {
                return;
            }

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            using (GraphicsPath path = new GraphicsPath())
            {
                float fontSizePixel = lbl.Font.Size * e.Graphics.DpiY / 72f;

                path.AddString(
                    lbl.Text,
                    lbl.Font.FontFamily,
                    (int)lbl.Font.Style,
                    fontSizePixel,
                    new Rectangle(0, 0, lbl.Width, lbl.Height),
                    new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center }
                );

                using (LinearGradientBrush rainbowBrush = new LinearGradientBrush(
                    new Rectangle(0, 0, lbl.Width, lbl.Height),
                    Color.Red, Color.Blue, LinearGradientMode.Horizontal))
                {
                    ColorBlend cb = new ColorBlend();
                    cb.Positions = new[] { 0f, 0.2f, 0.4f, 0.6f, 0.8f, 1f };
                    cb.Colors = new[] { Color.MediumAquamarine, Color.DeepSkyBlue, Color.MediumPurple, Color.LightCoral, Color.Orange, Color.Gold };
                    rainbowBrush.InterpolationColors = cb;

                    using (Pen outlinePen = new Pen(rainbowBrush, 4) { LineJoin = LineJoin.Round })
                    {
                        e.Graphics.DrawPath(outlinePen, path);
                    }
                }

                using (SolidBrush whiteBrush = new SolidBrush(Color.White))
                {
                    e.Graphics.FillPath(whiteBrush, path);
                }
            }
        }

        public class OverlayPanel : Panel
        {
            protected override void OnPaintBackground(PaintEventArgs e)
            {
                Form form = this.FindForm();

                if (form != null &&
                    form.FormBorderStyle == FormBorderStyle.None &&
                    form.TopMost &&
                    form.TransparencyKey == form.BackColor)
                {
                    // Overlayモード中は背景を描画しない
                    return;
                }

                base.OnPaintBackground(e);
            }
        }
    }
}

