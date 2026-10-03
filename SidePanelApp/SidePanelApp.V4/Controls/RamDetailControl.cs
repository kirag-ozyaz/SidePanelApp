using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using SidePanelApp.V4.Hardware;

namespace SidePanelApp.V4.Controls
{
    /// <summary>
    /// Карточка основной оперативной памяти: загрузка, объёмы и планки.
    /// </summary>
    public partial class RamDetailControl : UserControl
    {
        public const int ControlWidth = 252;
        public const int TailWidth = 10;

        private const int CornerRadius = 14;
        private const int TailHalfHeight = 8;
        private const int Pad = 12;
        private const int LineHeight = 18;
        private const int ModuleRowHeight = 17;
        private const int BarHeight = 5;
        private const int MinBodyHeight = 110;

        private MemorySnapshot _snapshot = new MemorySnapshot();
        private int _tailCenterY = 40;

        public event Action<bool> HoverStateChanged;

        public RamDetailControl()
        {
            InitializeComponent();
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            BackColor = Color.Transparent;
            MouseEnter += (s, e) => HoverStateChanged?.Invoke(true);
            MouseLeave += (s, e) => HoverStateChanged?.Invoke(false);
        }

        public int TailCenterY
        {
            get { return _tailCenterY; }
            set
            {
                _tailCenterY = value;
                Invalidate();
            }
        }

        public int BodyHeight { get; private set; }

        internal void UpdateSnapshot(MemorySnapshot snapshot)
        {
            _snapshot = snapshot ?? new MemorySnapshot();
            BodyHeight = MeasureBodyHeight();
            Size = new Size(ControlWidth, BodyHeight);
            Invalidate();
        }

        public GraphicsPath CreateCardPath()
        {
            return PanelColors.CreateRightTailCard(
                Width,
                Height,
                CornerRadius,
                _tailCenterY,
                TailWidth,
                TailHalfHeight);
        }

        private int MeasureBodyHeight()
        {
            int height = Pad;
            height += LineHeight + 8;
            height += LineHeight * 3; // используется, свободно, всего
            if (_snapshot.CommitLimitBytes > 0)
            {
                height += LineHeight;
            }

            if (_snapshot.Modules != null && _snapshot.Modules.Length > 0)
            {
                height += LineHeight;
                height += _snapshot.Modules.Length * ModuleRowHeight;
            }

            height += Pad;
            return Math.Max(MinBodyHeight, height);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            using (GraphicsPath path = CreateCardPath())
            using (SolidBrush background = new SolidBrush(PanelColors.Background))
            {
                g.FillPath(background, path);
            }

            int bodyRight = Width - TailWidth;
            int y = Pad;

            using (Font headingFont = new Font("Segoe UI", 8.25F, FontStyle.Bold))
            using (Font labelFont = new Font("Segoe UI", 7.5F))
            using (Font valueFont = new Font("Segoe UI", 7.5F, FontStyle.Bold))
            using (Font moduleFont = new Font("Segoe UI", 7F))
            using (SolidBrush headingBrush = new SolidBrush(PanelColors.TextPrimary))
            using (SolidBrush mutedBrush = new SolidBrush(PanelColors.TextMuted))
            using (SolidBrush valueBrush = new SolidBrush(PanelColors.TextPrimary))
            using (SolidBrush accentBrush = new SolidBrush(PanelColors.RamAccent))
            {
                string totalText = string.Format("{0}%", (int)Math.Round(_snapshot.UsagePercent));
                g.DrawString("Оперативная память", headingFont, headingBrush, Pad, y);
                SizeF totalSize = g.MeasureString(totalText, valueFont);
                g.DrawString(totalText, valueFont, accentBrush, bodyRight - Pad - totalSize.Width, y);
                y += LineHeight;

                DrawBar(g, Pad, y, bodyRight - Pad * 2, _snapshot.UsagePercent, PanelColors.RamAccent);
                y += 8;

                string usedText = string.Format(
                    "{0} / {1}",
                    MetricFormat.Gigabytes(_snapshot.UsedBytes),
                    MetricFormat.Gigabytes(_snapshot.TotalBytes));
                DrawMetricRow(g, labelFont, valueFont, mutedBrush, valueBrush, Pad, bodyRight, ref y,
                    "Используется", usedText);
                DrawMetricRow(g, labelFont, valueFont, mutedBrush, valueBrush, Pad, bodyRight, ref y,
                    "Свободно", MetricFormat.Gigabytes(_snapshot.AvailableBytes));
                DrawMetricRow(g, labelFont, valueFont, mutedBrush, valueBrush, Pad, bodyRight, ref y,
                    "Всего", MetricFormat.Gigabytes(_snapshot.TotalBytes));

                if (_snapshot.CommitLimitBytes > 0)
                {
                    string commitText = string.Format(
                        "{0} / {1}",
                        MetricFormat.Gigabytes(_snapshot.CommittedBytes),
                        MetricFormat.Gigabytes(_snapshot.CommitLimitBytes));
                    DrawMetricRow(g, labelFont, valueFont, mutedBrush, valueBrush, Pad, bodyRight, ref y,
                        "Коммит", commitText);
                }

                if (_snapshot.Modules != null && _snapshot.Modules.Length > 0)
                {
                    g.DrawString("Планки памяти", headingFont, headingBrush, Pad, y);
                    y += LineHeight;

                    int contentWidth = bodyRight - Pad * 2;
                    foreach (MemoryModuleSnapshot module in _snapshot.Modules)
                    {
                        string moduleLabel = module.Slot;
                        string moduleValue = FormatModule(module);

                        g.DrawString(moduleLabel, moduleFont, mutedBrush, Pad, y);
                        SizeF moduleValueSize = g.MeasureString(moduleValue, moduleFont);
                        g.DrawString(
                            moduleValue,
                            moduleFont,
                            valueBrush,
                            bodyRight - Pad - moduleValueSize.Width,
                            y);

                        int barTop = y + 11;
                        double modulePercent = _snapshot.TotalBytes == 0
                            ? 0
                            : module.CapacityBytes * 100d / _snapshot.TotalBytes;
                        DrawBar(g, Pad, barTop, contentWidth, modulePercent, PanelColors.RamAccent, 4);
                        y += ModuleRowHeight;
                    }
                }
            }
        }

        private static string FormatModule(MemoryModuleSnapshot module)
        {
            string capacity = MetricFormat.Gigabytes(module.CapacityBytes);
            if (module.SpeedMhz > 0 && !string.IsNullOrEmpty(module.TypeName))
            {
                return string.Format("{0} · {1} {2} МГц", capacity, module.TypeName, module.SpeedMhz);
            }

            if (module.SpeedMhz > 0)
            {
                return string.Format("{0} · {1} МГц", capacity, module.SpeedMhz);
            }

            if (!string.IsNullOrEmpty(module.TypeName))
            {
                return string.Format("{0} · {1}", capacity, module.TypeName);
            }

            return capacity;
        }

        private static void DrawMetricRow(
            Graphics g,
            Font labelFont,
            Font valueFont,
            Brush labelBrush,
            Brush valueBrush,
            int pad,
            int bodyRight,
            ref int y,
            string label,
            string value)
        {
            g.DrawString(label, labelFont, labelBrush, pad, y);
            SizeF valueSize = g.MeasureString(value, valueFont);
            g.DrawString(value, valueFont, valueBrush, bodyRight - pad - valueSize.Width, y);
            y += LineHeight;
        }

        private static void DrawBar(Graphics g, int x, int y, int width, double percent, Color color, int height = BarHeight)
        {
            Rectangle track = new Rectangle(x, y, width, height);
            using (GraphicsPath trackPath = PanelColors.CreateRoundedRect(track, 2))
            using (SolidBrush trackBrush = new SolidBrush(PanelColors.RingTrack))
            {
                g.FillPath(trackBrush, trackPath);
            }

            int fillWidth = (int)Math.Round(width * Math.Max(0, Math.Min(100, percent)) / 100d);
            if (fillWidth <= 0)
            {
                return;
            }

            Rectangle fill = new Rectangle(track.X, track.Y, Math.Max(height, fillWidth), track.Height);
            using (GraphicsPath fillPath = PanelColors.CreateRoundedRect(fill, 2))
            using (SolidBrush fillBrush = new SolidBrush(color))
            {
                g.FillPath(fillBrush, fillPath);
            }
        }
    }
}
