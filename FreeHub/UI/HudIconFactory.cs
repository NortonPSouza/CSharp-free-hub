using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace FreeHub.UI
{
    /// <summary>
    /// Generates a custom neon-pulse vector icon at runtime for the HUD window and System Tray.
    /// </summary>
    public static class HudIconFactory
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        public static Icon CreateNeonPulseIcon()
        {
            using (Bitmap bitmap = new Bitmap(32, 32))
            {
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.Clear(Color.Transparent);

                    // Dark rounded hexagon/circle badge background
                    using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(20, 22, 32)))
                    {
                        g.FillEllipse(bgBrush, 1, 1, 29, 29);
                    }

                    // Neon green outer border
                    using (Pen borderPen = new Pen(Color.FromArgb(0, 230, 118), 2.2f))
                    {
                        g.DrawEllipse(borderPen, 2, 2, 27, 27);
                    }

                    // Pulse / lightning waveform line inside the badge
                    PointF[] pulsePoints = new PointF[]
                    {
                        new PointF(6f, 16f),
                        new PointF(11f, 16f),
                        new PointF(14f, 8f),
                        new PointF(18f, 24f),
                        new PointF(21f, 16f),
                        new PointF(26f, 16f)
                    };

                    using (Pen pulsePen = new Pen(Color.FromArgb(0, 230, 118), 2.6f))
                    {
                        pulsePen.StartCap = LineCap.Round;
                        pulsePen.EndCap = LineCap.Round;
                        pulsePen.LineJoin = LineJoin.Round;
                        g.DrawLines(pulsePen, pulsePoints);
                    }
                }

                IntPtr hIcon = bitmap.GetHicon();
                try
                {
                    using (Icon tempIcon = Icon.FromHandle(hIcon))
                    {
                        return (Icon)tempIcon.Clone();
                    }
                }
                finally
                {
                    DestroyIcon(hIcon);
                }
            }
        }
    }
}
