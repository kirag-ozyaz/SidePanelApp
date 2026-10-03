using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using SidePanelApp.V6.Hardware;

namespace SidePanelApp.V6.Controls
{
    /// <summary>
    /// Карточка CPU: общая загрузка, частота, время ядра и загрузка по логическим ядрам.
    /// </summary>
    public partial class CpuDetailControl : UserControl
    {
        public const int ControlWidth = 252;
        public const int TailExtension = 12;
        public const int TailHalfBase = 14;

        private const int CornerRadius = 14;
        private const int Pad = 12;
        private const int LineHeight = 18;
        private const int CoreRowHeight = 17;
        private const int BarHeight = 5;
        private const int MinBodyHeight = 120;

        private CpuSnapshot _snapshot = new CpuSnapshot();

        public CalloutTailSide TailSide { get; set; } = CalloutTailSide.Right;

        public event Action<bool> HoverStateChanged;

        public CpuDetailControl()
        {
            InitializeComponent();
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            BackColor = Color.Transparent;
            MouseEnter += (s, e) => HoverStateChanged?.Invoke(true);
            MouseLeave += (s, e) => HoverStateChanged?.Invoke(false);
        }

        public int BodyHeight { get; private set; }

        public int TailTipX
        {
            get { return TailSide == CalloutTailSide.Right ? ControlWidth : 0; }
        }

        public int TailTipY
        {
            get { return BodyHeight + TailExtension; }
        }

        internal void UpdateSnapshot(CpuSnapshot snapshot)
        {
            _snapshot = snapshot ?? new CpuSnapshot();
            BodyHeight = MeasureBodyHeight();
            Size = new Size(ControlWidth, BodyHeight + TailExtension);
            Invalidate();
        }

        public GraphicsPath CreateCardPath()
        {
            int w = Math.Max(Width, ControlWidth);
            int h = Math.Max(Height, BodyHeight + TailExtension);
            return PanelColors.CreateCalloutPath(
                w,
                BodyHeight,
                h,
                CornerRadius,
                TailHalfBase,
                TailSide == CalloutTailSide.Right);
        }

        private int MeasureBodyHeight()
        {
            int height = Pad;
            height += LineHeight + 8; // заголовок + бар
            height += LineHeight; // частота
            height += LineHeight; // время ядра
            height += LineHeight; // подзаголовок "По ядрам"
            height += (_snapshot.Cores != null ? _snapshot.Cores.Length : 0) * CoreRowHeight;
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

            CalloutContentBounds.GetHorizontal(Width, TailSide, out int bodyLeft, out int bodyRight);
            int contentWidth = bodyRight - bodyLeft - Pad * 2;
            int y = Pad;

            using (Font headingFont = new Font("Segoe UI", 8.25F, FontStyle.Bold))
            using (Font labelFont = new Font("Segoe UI", 7.5F))
            using (Font valueFont = new Font("Segoe UI", 7.5F, FontStyle.Bold))
            using (Font coreFont = new Font("Segoe UI", 7F))
            using (SolidBrush headingBrush = new SolidBrush(PanelColors.TextPrimary))
            using (SolidBrush mutedBrush = new SolidBrush(PanelColors.TextMuted))
            using (SolidBrush valueBrush = new SolidBrush(PanelColors.TextPrimary))
            using (SolidBrush accentBrush = new SolidBrush(PanelColors.CpuAccent))
            {
                string title = TruncateProcessorName(_snapshot.Name);
                string totalText = string.Format("{0}%", (int)Math.Round(_snapshot.TotalPercent));
                int textLeft = bodyLeft + Pad;
                g.DrawString("Общая загрузка", headingFont, headingBrush, textLeft, y);
                SizeF totalSize = g.MeasureString(totalText, valueFont);
                g.DrawString(totalText, valueFont, accentBrush, bodyRight - Pad - totalSize.Width, y);
                y += LineHeight;

                DrawBar(g, textLeft, y, contentWidth, _snapshot.TotalPercent, PanelColors.CpuAccent);
                y += 8;

                DrawMetricRow(g, labelFont, valueFont, mutedBrush, valueBrush, textLeft, bodyRight, ref y,
                    "Частота", MetricFormat.FrequencyPair(_snapshot.CurrentFrequencyMhz, _snapshot.MaxFrequencyMhz));

                DrawMetricRow(g, labelFont, valueFont, mutedBrush, valueBrush, textLeft, bodyRight, ref y,
                    "Время ядра", string.Format("{0}%", (int)Math.Round(_snapshot.KernelPercent)));

                g.DrawString("По ядрам", headingFont, headingBrush, textLeft, y);
                y += LineHeight;

                if (_snapshot.Cores == null || _snapshot.Cores.Length == 0)
                {
                    g.DrawString("Нет данных", labelFont, mutedBrush, textLeft, y);
                    return;
                }

                foreach (CoreSnapshot core in _snapshot.Cores)
                {
                    string coreLabel = string.Format("Ядро {0}", core.Index);
                    string coreValue = string.Format(
                        "{0}% · {1}",
                        (int)Math.Round(core.UsagePercent),
                        MetricFormat.Gigahertz(core.FrequencyMhz));

                    g.DrawString(coreLabel, coreFont, mutedBrush, textLeft, y);
                    SizeF coreValueSize = g.MeasureString(coreValue, coreFont);
                    g.DrawString(
                        coreValue,
                        coreFont,
                        valueBrush,
                        bodyRight - Pad - coreValueSize.Width,
                        y);

                    int barTop = y + 11;
                    DrawBar(g, textLeft, barTop, contentWidth, core.UsagePercent, PanelColors.CpuAccent, 4);
                    y += CoreRowHeight;
                }
            }
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

        private static string TruncateProcessorName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return "Процессор";
            }

            const int max = 34;
            string trimmed = name.Trim();
            return trimmed.Length <= max ? trimmed : trimmed.Substring(0, max - 1) + "…";
        }
    }
}
