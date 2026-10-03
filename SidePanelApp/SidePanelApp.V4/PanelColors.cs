using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SidePanelApp.V4
{
    /// <summary>
    /// Общая палитра и вспомогательная геометрия для панели и карточек.
    /// </summary>
    internal static class PanelColors
    {
        public static readonly Color Background = Color.FromArgb(26, 26, 46);
        public static readonly Color RingTrack = Color.FromArgb(51, 51, 85);
        public static readonly Color CpuAccent = Color.FromArgb(255, 107, 53);
        public static readonly Color RamAccent = Color.FromArgb(56, 239, 125);
        public static readonly Color GpuAccent = Color.FromArgb(192, 132, 252);
        public static readonly Color NetAccent = Color.FromArgb(56, 189, 248);
        public static readonly Color GoldAccent = Color.FromArgb(255, 215, 0);
        public static readonly Color TextPrimary = Color.White;
        public static readonly Color TextMuted = Color.FromArgb(180, 180, 200);
        public static readonly Color SectionFill = Color.FromArgb(36, 36, 62);
        public static readonly Color CalloutBorder = Color.FromArgb(58, 58, 88);

        public const double PanelOpacity = 0.95;

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

        /// <summary>
        /// Карточка со скруглёнными краями и хвостиком вправо, к кольцу панели.
        /// </summary>
        public static GraphicsPath CreateRightTailCard(
            int width,
            int height,
            int radius,
            int tailCenterY,
            int tailWidth,
            int tailHalfHeight)
        {
            int bodyRight = width - tailWidth;
            int diameter = radius * 2;
            int clampedY = Math.Max(radius + tailHalfHeight, Math.Min(height - radius - tailHalfHeight, tailCenterY));
            int tailTop = clampedY - tailHalfHeight;
            int tailBottom = clampedY + tailHalfHeight;

            GraphicsPath path = new GraphicsPath();
            path.AddArc(0, 0, diameter, diameter, 180, 90);
            path.AddLine(radius, 0, bodyRight - radius, 0);
            path.AddArc(bodyRight - diameter, 0, diameter, diameter, 270, 90);
            path.AddLine(bodyRight, radius, bodyRight, tailTop);
            path.AddLine(bodyRight, tailTop, width, clampedY);
            path.AddLine(width, clampedY, bodyRight, tailBottom);
            path.AddLine(bodyRight, tailBottom, bodyRight, height - radius);
            path.AddArc(bodyRight - diameter, height - diameter, diameter, diameter, 0, 90);
            path.AddLine(bodyRight - radius, height, radius, height);
            path.AddArc(0, height - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static void ApplyRegion(Control control, GraphicsPath path)
        {
            control.Region = new Region(path);
        }
    }
}
