using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SidePanelApp.V2
{
    /// <summary>
    /// Общая палитра и вспомогательная геометрия для панели и выносок.
    /// </summary>
    internal static class PanelColors
    {
        public static readonly Color Background = Color.FromArgb(26, 26, 46);
        public static readonly Color RingTrack = Color.FromArgb(51, 51, 85);
        public static readonly Color CpuAccent = Color.FromArgb(255, 107, 53);
        public static readonly Color RamAccent = Color.FromArgb(56, 239, 125);
        public static readonly Color GoldAccent = Color.FromArgb(255, 215, 0);
        public static readonly Color TextPrimary = Color.White;
        public static readonly Color TextMuted = Color.FromArgb(180, 180, 200);
        public static readonly Color GradientStart = Color.FromArgb(45, 27, 105);
        public static readonly Color GradientMiddle = Color.FromArgb(17, 153, 142);
        public static readonly Color GradientEnd = Color.FromArgb(56, 239, 125);

        public const double PanelOpacity = 0.95;

        public static LinearGradientBrush CreateDetailGradient(Rectangle bounds)
        {
            var brush = new LinearGradientBrush(bounds, GradientStart, GradientEnd, 45f);
            brush.InterpolationColors = new ColorBlend(3)
            {
                Colors = new[] { GradientStart, GradientMiddle, GradientEnd },
                Positions = new[] { 0f, 0.5f, 1f }
            };
            return brush;
        }

        public static GraphicsPath CreateLeftRoundedRect(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = radius * 2;
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddLine(rect.Right, rect.Y, rect.Right, rect.Bottom);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static GraphicsPath CreateRoundedRect(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = radius * 2;
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static readonly Color CalloutBorder = Color.FromArgb(58, 58, 88);

        /// <summary>
        /// Форма выноски-примечания: скруглённое тело и хвостик вниз-вправо.
        /// </summary>
        public static GraphicsPath CreateCalloutPath(
            int width,
            int bodyHeight,
            int totalHeight,
            int radius,
            int tailHalfBase)
        {
            int bodyRight = width - 6;
            int tipX = width;
            int tipY = totalHeight;
            int attachY = bodyHeight - radius - 1;
            int baseX = bodyRight - tailHalfBase;
            int diameter = radius * 2;

            GraphicsPath path = new GraphicsPath();
            path.AddArc(0, 0, diameter, diameter, 180, 90);
            path.AddLine(radius, 0, bodyRight - radius, 0);
            path.AddArc(bodyRight - diameter, 0, diameter, diameter, 270, 90);
            path.AddLine(bodyRight, radius, bodyRight, attachY);
            path.AddLine(bodyRight, attachY, tipX, tipY);
            path.AddLine(tipX, tipY, baseX, bodyHeight);
            path.AddLine(baseX, bodyHeight, radius, bodyHeight);
            path.AddArc(0, bodyHeight - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static void ApplyRegion(Control control, GraphicsPath path)
        {
            control.Region = new Region(path);
        }
    }
}
