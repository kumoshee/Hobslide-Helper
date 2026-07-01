using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace HobslideHelper
{
    public class AutoResizeManager
    {
        private readonly Form _form;

        private readonly Dictionary<Control, ControlInfo> _controls =
            new Dictionary<Control, ControlInfo>();

        private Size _originalFormSize;
        private float _aspectRatio;
        private bool _resizing = false;

        public Rectangle OriginalGraphRect { get; set; }

        public Rectangle CurrentGraphRect { get; private set; }

        private class ControlInfo
        {
            public Rectangle Bounds;
            public float FontSize;
        }

        public AutoResizeManager(Form form)
        {
            _form = form;
        }

        public void Initialize()
        {
            _originalFormSize = _form.ClientSize;
            _aspectRatio =
                (float)_originalFormSize.Width / _originalFormSize.Height;
            SaveControlInfo(_form);

            _form.Resize += Form_Resize;
        }

        private void SaveControlInfo(Control parent)
        {
            foreach (Control c in parent.Controls)
            {
                _controls[c] = new ControlInfo()
                {
                    Bounds = c.Bounds,
                    FontSize = c.Font.Size
                };

                if (c.Controls.Count > 0)
                    SaveControlInfo(c);
            }
        }

        private void Form_Resize(object sender, EventArgs e)
        {
            if (_resizing)
                return;

            _resizing = true;

            if (_originalFormSize.Width == 0 ||
                _originalFormSize.Height == 0)
                return;

            int newWidth = _form.ClientSize.Width;
            int newHeight = _form.ClientSize.Height;

            float currentRatio =
                (float)newWidth / newHeight;

            if (Math.Abs(currentRatio - _aspectRatio) > 0.001f)
            {
                if (currentRatio > _aspectRatio)
                {
                    // 横が広すぎる
                    newWidth = (int)(newHeight * _aspectRatio);
                }
                else
                {
                    // 縦が長すぎる
                    newHeight = (int)(newWidth / _aspectRatio);
                }

                _form.ClientSize = new Size(newWidth, newHeight);
            }

            float scale =
                Math.Min(
                    (float)_form.ClientSize.Width / _originalFormSize.Width,
                    (float)_form.ClientSize.Height / _originalFormSize.Height);

            _form.SuspendLayout();

            foreach (var pair in _controls)
            {
                Control c = pair.Key;
                ControlInfo info = pair.Value;

                c.Left = (int)(info.Bounds.Left * scale);
                c.Top = (int)(info.Bounds.Top * scale);
                c.Width = (int)(info.Bounds.Width * scale);
                c.Height = (int)(info.Bounds.Height * scale);

                float newFontSize = info.FontSize * scale;

                if (newFontSize < 6)
                    newFontSize = 6;

                if (Math.Abs(c.Font.Size - newFontSize) > 0.1f)
                {
                    c.Font = new Font(
                        c.Font.FontFamily,
                        newFontSize,
                        c.Font.Style);
                }
            }

            _form.ResumeLayout(false);

            if (OriginalGraphRect != Rectangle.Empty)
            {
                CurrentGraphRect = new Rectangle(
                    (int)(OriginalGraphRect.Left * scale),
                    (int)(OriginalGraphRect.Top * scale),
                    (int)(OriginalGraphRect.Width * scale),
                    (int)(OriginalGraphRect.Height * scale));
            }

            _form.Invalidate();
            _resizing = false;
        }
    }
}