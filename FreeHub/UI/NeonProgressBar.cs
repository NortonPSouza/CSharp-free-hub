using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace FreeHub.UI
{
    /// <summary>
    /// Custom owner-drawn ProgressBar with smooth animation, dynamic neon gradients, and interactive hover details.
    /// </summary>
    public class NeonProgressBar : ProgressBar
    {
        private readonly Timer _animationTimer;
        private float _animatedValue;
        private bool _isHovered;

        public bool IsBoostActive { get; set; }

        public string OverlayText { get; set; }

        public string HoverOverlayText { get; set; }

        public NeonProgressBar()
        {
            this.SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);

            _animationTimer = new Timer();
            _animationTimer.Interval = 25;
            _animationTimer.Tick += OnAnimationTick;
        }

        public void SetTargetValue(int targetValue)
        {
            if (targetValue < Minimum) targetValue = Minimum;
            if (targetValue > Maximum) targetValue = Maximum;

            if (this.Value != targetValue)
            {
                this.Value = targetValue;
            }

            if (!_animationTimer.Enabled)
            {
                _animationTimer.Start();
            }

            this.Invalidate();
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _isHovered = true;
            this.Cursor = Cursors.Hand;
            this.Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _isHovered = false;
            this.Cursor = Cursors.Default;
            this.Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            Rectangle rect = this.ClientRectangle;
            if (rect.Width <= 0 || rect.Height <= 0)
            {
                return;
            }

            // 1. Background track
            Color trackColor = _isHovered
                ? Color.FromArgb(40, 42, 58)
                : Color.FromArgb(30, 32, 44);

            using (SolidBrush trackBrush = new SolidBrush(trackColor))
            {
                g.FillRectangle(trackBrush, rect);
            }

            // 2. Calculate fill width using smoothed animated value
            int range = Maximum - Minimum;
            float ratio = range > 0 ? (_animatedValue - Minimum) / range : 0f;
            if (ratio < 0f) ratio = 0f;
            if (ratio > 1f) ratio = 1f;

            int fillWidth = (int)Math.Round((rect.Width - 2) * ratio);
            if (fillWidth > 1)
            {
                Rectangle fillRect = new Rectangle(1, 1, fillWidth, rect.Height - 2);

                Color startColor = IsBoostActive
                    ? Color.FromArgb(138, 43, 226)  // Cyberpunk Violet when Streak/Music Buff active
                    : Color.FromArgb(0, 176, 255);  // Electric Cyan normal start

                Color endColor = IsBoostActive
                    ? Color.FromArgb(0, 230, 118)   // Neon Emerald end
                    : Color.FromArgb(0, 210, 110);

                if (_isHovered)
                {
                    startColor = ControlPaint.Light(startColor, 0.15f);
                    endColor = ControlPaint.Light(endColor, 0.15f);
                }

                using (LinearGradientBrush gradientBrush = new LinearGradientBrush(
                    fillRect,
                    startColor,
                    endColor,
                    LinearGradientMode.Horizontal))
                {
                    g.FillRectangle(gradientBrush, fillRect);
                }

                // Subtle top glossy highlight
                Rectangle glossRect = new Rectangle(1, 1, fillWidth, Math.Max(2, (rect.Height - 2) / 3));
                using (SolidBrush glossBrush = new SolidBrush(Color.FromArgb(45, 255, 255, 255)))
                {
                    g.FillRectangle(glossBrush, glossRect);
                }
            }

            // 3. Outer border (glows brighter on hover or boost)
            Color borderColor = _isHovered
                ? Color.FromArgb(0, 230, 118)
                : (IsBoostActive ? Color.FromArgb(0, 180, 110) : Color.FromArgb(55, 60, 80));

            using (Pen borderPen = new Pen(borderColor, 1f))
            {
                g.DrawRectangle(borderPen, 0, 0, rect.Width - 1, rect.Height - 1);
            }

            // 4. Interactive overlay text inside the bar
            string textToDraw = _isHovered && !string.IsNullOrWhiteSpace(HoverOverlayText)
                ? HoverOverlayText
                : OverlayText;

            if (!string.IsNullOrWhiteSpace(textToDraw))
            {
                using (Font font = new Font("Segoe UI Semibold", 8.75f, FontStyle.Bold))
                using (StringFormat sf = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                })
                {
                    // Drop shadow for readability over both dark track and bright neon gradient
                    Rectangle shadowRect = new Rectangle(rect.X + 1, rect.Y + 1, rect.Width, rect.Height);
                    using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(190, 10, 10, 16)))
                    {
                        g.DrawString(textToDraw, font, shadowBrush, shadowRect, sf);
                    }

                    using (SolidBrush textBrush = new SolidBrush(Color.White))
                    {
                        g.DrawString(textToDraw, font, textBrush, rect, sf);
                    }
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _animationTimer != null)
            {
                _animationTimer.Stop();
                _animationTimer.Dispose();
            }

            base.Dispose(disposing);
        }

        private void OnAnimationTick(object sender, EventArgs e)
        {
            float target = this.Value;
            float diff = target - _animatedValue;

            if (Math.Abs(diff) < 0.4f)
            {
                _animatedValue = target;
                _animationTimer.Stop();
            }
            else
            {
                _animatedValue += diff * 0.25f;
            }

            this.Invalidate();
        }
    }
}
