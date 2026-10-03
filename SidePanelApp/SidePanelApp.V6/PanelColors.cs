using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SidePanelApp.V6
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
        public static readonly Color ThreatOk = Color.FromArgb(56, 239, 125);
        public static readonly Color ThreatWatch = Color.FromArgb(250, 204, 21);
        public static readonly Color ThreatAlert = Color.FromArgb(248, 113, 113);

        public static Color ThreatColor(Security.ThreatLevel level)
        {
            switch (level)
            {
                case Security.ThreatLevel.Alert:
                    return ThreatAlert;
                case Security.ThreatLevel.Watch:
                    return ThreatWatch;
                default:
                    return ThreatOk;
            }
        }
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

        /// <summary>Скругление справа — панель у левого края экрана.</summary>
        public static GraphicsPath CreateRightRoundedRect(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = radius * 2;
            path.AddLine(rect.Left, rect.Top, rect.Right - radius, rect.Top);
            path.AddArc(rect.Right - diameter, rect.Top, diameter, diameter, 270, 90);
            path.AddLine(rect.Right, rect.Top + radius, rect.Right, rect.Bottom - radius);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddLine(rect.Right - radius, rect.Bottom, rect.Left, rect.Bottom);
            path.AddLine(rect.Left, rect.Bottom, rect.Left, rect.Top);
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
            int tailHalfBase,
            bool tailOnRight = true)
        {
            if (tailOnRight)
            {
                return CreateCalloutPathTailRight(width, bodyHeight, totalHeight, radius, tailHalfBase);
            }

            return CreateCalloutPathTailLeft(width, bodyHeight, totalHeight, radius, tailHalfBase);
        }

        /// <summary>Хвост вниз-вправо — попап слева от панели у правого края экрана.</summary>
        private static GraphicsPath CreateCalloutPathTailRight(
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

        /// <summary>Хвост вниз-влево — попап справа от панели у левого края экрана.</summary>
        private static GraphicsPath CreateCalloutPathTailLeft(
            int width,
            int bodyHeight,
            int totalHeight,
            int radius,
            int tailHalfBase)
        {
            GraphicsPath path = CreateCalloutPathTailRight(width, bodyHeight, totalHeight, radius, tailHalfBase);
            // x' = width - x: тот же клюв, что справа, без ухода контура в отрицательные X.
            using (Matrix matrix = new Matrix(-1, 0, 0, 1, width, 0))
            {
                path.Transform(matrix);
            }

            path.FillMode = FillMode.Winding;
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
            if (control.Width <= 0 || control.Height <= 0)
            {
                return;
            }

            path.FillMode = FillMode.Winding;
            control.Region = new Region(path);
        }
    }
}
