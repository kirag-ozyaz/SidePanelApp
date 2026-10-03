using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SidePanelApp.V6.Controls
{
    internal sealed class DeviceMetricRow
    {
        public string Label { get; set; }
        public double Percent { get; set; }
        public string ValueText { get; set; }
        public Color Accent { get; set; }
    }

    internal sealed class DeviceCardModel
    {
        public string Title { get; set; }
        public List<DeviceMetricRow> Metrics { get; set; }

        public DeviceCardModel()
        {
            Metrics = new List<DeviceMetricRow>();
        }
    }

    /// <summary>
    /// Карточка со списком устройств: несколько видеокарт или несколько сетевых адаптеров.
    /// </summary>
    public partial class DeviceListDetailControl : UserControl
    {
        public const int ControlWidth = 252;
        public const int TailExtension = 12;
        public const int TailHalfBase = 14;

        private const int CornerRadius = 14;
        private const int Pad = 12;
        private const int TitleHeight = 18;
        private const int MetricHeight = 20;
        private const int DeviceGap = 10;
        private const int BarHeight = 5;
        private const int MinBodyHeight = 56;

        private readonly List<DeviceCardModel> _devices = new List<DeviceCardModel>();
        private string _heading = "";
        private string _emptyText = "Нет данных";

        public CalloutTailSide TailSide { get; set; } = CalloutTailSide.Right;

        public event Action<bool> HoverStateChanged;

        public DeviceListDetailControl()
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

        internal void SetContent(string heading, string emptyText, IList<DeviceCardModel> devices)
        {
            _heading = heading ?? "";
            _emptyText = emptyText ?? "Нет данных";
            _devices.Clear();
            if (devices != null)
            {
                _devices.AddRange(devices);
            }

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
            if (!string.IsNullOrEmpty(_heading))
            {
                height += TitleHeight;
            }

            if (_devices.Count == 0)
            {
                height += TitleHeight + Pad;
                return Math.Max(MinBodyHeight, height);
            }

            for (int i = 0; i < _devices.Count; i++)
            {
                height += TitleHeight;
                height += _devices[i].Metrics.Count * MetricHeight;
                if (i < _devices.Count - 1)
                {
                    height += DeviceGap;
                }
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

            CalloutContentBounds.GetHorizontal(Width, TailSide, out int bodyLeft, out int bodyRight);
            int textLeft = bodyLeft + Pad;
            int contentWidth = bodyRight - bodyLeft - Pad * 2;
            int y = Pad;

            using (Font headingFont = new Font("Segoe UI", 8.25F, FontStyle.Bold))
            using (Font titleFont = new Font("Segoe UI", 8.25F, FontStyle.Bold))
            using (Font labelFont = new Font("Segoe UI", 7.5F))
            using (Font valueFont = new Font("Segoe UI", 7.5F, FontStyle.Bold))
            using (SolidBrush headingBrush = new SolidBrush(PanelColors.TextPrimary))
            using (SolidBrush mutedBrush = new SolidBrush(PanelColors.TextMuted))
            using (SolidBrush valueBrush = new SolidBrush(PanelColors.TextPrimary))
            {
                if (!string.IsNullOrEmpty(_heading))
                {
                    g.DrawString(_heading, headingFont, headingBrush, textLeft, y);
                    y += TitleHeight;
                }

                if (_devices.Count == 0)
                {
                    g.DrawString(_emptyText, labelFont, mutedBrush, textLeft, y);
                    return;
                }

                for (int i = 0; i < _devices.Count; i++)
                {
                    DeviceCardModel device = _devices[i];
                    int blockHeight = TitleHeight + device.Metrics.Count * MetricHeight;
                    Rectangle section = new Rectangle(textLeft - 4, y - 2, contentWidth + 8, blockHeight + 4);
                    using (GraphicsPath sectionPath = PanelColors.CreateRoundedRect(section, 8))
                    using (SolidBrush sectionBrush = new SolidBrush(PanelColors.SectionFill))
                    {
                        g.FillPath(sectionBrush, sectionPath);
                    }

                    TextRenderer.DrawText(
                        g,
                        device.Title,
                        titleFont,
                        new Rectangle(textLeft, y, contentWidth, TitleHeight),
                        PanelColors.TextPrimary,
                        TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                    y += TitleHeight;

                    foreach (DeviceMetricRow metric in device.Metrics)
                    {
                        g.DrawString(metric.Label, labelFont, mutedBrush, textLeft, y);
                        SizeF valueSize = g.MeasureString(metric.ValueText, valueFont);
                        g.DrawString(
                            metric.ValueText,
                            valueFont,
                            valueBrush,
                            bodyRight - Pad - valueSize.Width,
                            y);

                        int barTop = y + 13;
                        int barWidth = contentWidth;
                        Rectangle track = new Rectangle(textLeft, barTop, barWidth, BarHeight);
                        using (GraphicsPath trackPath = PanelColors.CreateRoundedRect(track, 2))
                        using (SolidBrush trackBrush = new SolidBrush(PanelColors.RingTrack))
                        {
                            g.FillPath(trackBrush, trackPath);
                        }

                        int fillWidth = (int)Math.Round(barWidth * Math.Max(0, Math.Min(100, metric.Percent)) / 100d);
                        if (fillWidth > 0)
                        {
                            Rectangle fill = new Rectangle(track.X, track.Y, Math.Max(BarHeight, fillWidth), track.Height);
                            using (GraphicsPath fillPath = PanelColors.CreateRoundedRect(fill, 2))
                            using (SolidBrush fillBrush = new SolidBrush(metric.Accent))
                            {
                                g.FillPath(fillBrush, fillPath);
                            }
                        }

                        y += MetricHeight;
                    }

                    if (i < _devices.Count - 1)
                    {
                        y += DeviceGap;
                    }
                }
            }
        }
    }
}
