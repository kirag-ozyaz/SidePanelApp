using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SidePanelApp.V5.Controls
{
    /// <summary>
    /// Содержимое выноски CPU/RAM: заголовок, процент, горизонтальный бар.
    /// </summary>
    public partial class MetricDetailControl : UserControl
    {
        public const int ControlWidth = 148;
        public const int BodyHeight = 40;
        public const int TailExtension = 12;
        public const int ControlHeight = BodyHeight + TailExtension;
        public const int TailTipX = ControlWidth;
        public const int TailTipY = ControlHeight;

        private const int CornerRadius = 12;
        private const int TailHalfBase = 14;
        private const int ContentPadding = 14;
        private const int BarHeight = 5;
        private const int BarTop = 26;

        private string _title = "Метрика";
        private string _valueText = "0%";
        private double _value;
        private Color _accentColor = PanelColors.CpuAccent;

        public event Action<bool> HoverStateChanged;

        public MetricDetailControl()
        {
            InitializeComponent();
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            BackColor = Color.Transparent;

            MouseEnter += (s, e) => HoverStateChanged?.Invoke(true);
            MouseLeave += (s, e) => HoverStateChanged?.Invoke(false);
        }

        public void UpdateMetric(string title, double value, Color accentColor)
        {
            _title = title;
            _value = value;
            _valueText = string.Format("{0}%", (int)Math.Round(value));
            _accentColor = accentColor;
            Invalidate();
        }

        public GraphicsPath CreateBubblePath()
        {
            return PanelColors.CreateCalloutPath(
                Width,
                BodyHeight,
                Height,
                CornerRadius,
                TailHalfBase);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            using (GraphicsPath bubblePath = CreateBubblePath())
            using (SolidBrush backgroundBrush = new SolidBrush(PanelColors.Background))
            {
                g.FillPath(backgroundBrush, bubblePath);
            }

            int barWidth = Width - ContentPadding * 2 - 6;
            Rectangle barRect = new Rectangle(ContentPadding, BarTop, barWidth, BarHeight);

            using (GraphicsPath trackPath = PanelColors.CreateRoundedRect(barRect, 2))
            using (SolidBrush trackBrush = new SolidBrush(PanelColors.RingTrack))
            {
                g.FillPath(trackBrush, trackPath);
            }

            int fillWidth = Math.Max(BarHeight, (int)Math.Round(barWidth * _value / 100d));
            if (fillWidth > 0)
            {
                Rectangle fillRect = new Rectangle(barRect.X, barRect.Y, fillWidth, barRect.Height);
                using (GraphicsPath fillPath = PanelColors.CreateRoundedRect(fillRect, 2))
                using (SolidBrush fillBrush = new SolidBrush(_accentColor))
                {
                    g.FillPath(fillBrush, fillPath);
                }
            }

            using (Font titleFont = new Font("Segoe UI", 8.25F))
            using (Font valueFont = new Font("Segoe UI", 8.25F, FontStyle.Bold))
            using (SolidBrush titleBrush = new SolidBrush(PanelColors.TextMuted))
            using (SolidBrush valueBrush = new SolidBrush(PanelColors.TextPrimary))
            {
                g.DrawString(_title, titleFont, titleBrush, ContentPadding, 8);
                SizeF valueSize = g.MeasureString(_valueText, valueFont);
                g.DrawString(_valueText, valueFont, valueBrush, Width - ContentPadding - 6 - valueSize.Width, 8);
            }
        }
    }
}
