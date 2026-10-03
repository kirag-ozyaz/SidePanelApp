using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using SidePanelApp.V5.Security;

namespace SidePanelApp.V5.Controls
{
    public partial class ThreatDetailControl : UserControl
    {
        public const int ControlWidth = 300;
        public const int TailExtension = 12;
        public const int TailHalfBase = 14;

        private const int CornerRadius = 14;
        private const int Pad = 12;
        private const int LineHeight = 16;
        private const int SectionGap = 10;
        private const int MinBodyHeight = 120;

        private ThreatSnapshot _snapshot = new ThreatSnapshot();
        private readonly List<string> _lines = new List<string>();

        public event Action<bool> HoverStateChanged;

        public ThreatDetailControl()
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
            get { return ControlWidth; }
        }

        public int TailTipY
        {
            get { return BodyHeight + TailExtension; }
        }

        internal void SetSnapshot(ThreatSnapshot snapshot)
        {
            _snapshot = snapshot ?? new ThreatSnapshot();
            BuildLines();
            BodyHeight = Math.Max(MinBodyHeight, Pad * 2 + _lines.Count * LineHeight);
            Size = new Size(ControlWidth, BodyHeight + TailExtension);
            Invalidate();
        }

        public GraphicsPath CreateCardPath()
        {
            return PanelColors.CreateCalloutPath(
                Width,
                BodyHeight,
                Height,
                CornerRadius,
                TailHalfBase);
        }

        private void BuildLines()
        {
            _lines.Clear();
            AppendSection("Удалёнка (RDP/VNC/AnyDesk)", _snapshot.Remote);
            AppendSection("SMB", _snapshot.Smb);
            AppendSection("Попытки / шум", _snapshot.Probe);
        }

        private void AppendSection(string title, ThreatChannelSnapshot channel)
        {
            _lines.Add(title + " — " + ThreatLevelLabel(channel.Level));
            if (!string.IsNullOrEmpty(channel.Note))
            {
                _lines.Add("  " + channel.Note);
            }

            int count = 0;
            foreach (ThreatItem item in channel.Items)
            {
                if (count >= 6)
                {
                    _lines.Add("  …");
                    break;
                }

                string time = item.TimeUtc.ToLocalTime().ToString("HH:mm");
                _lines.Add(string.Format("  {0} {1} ← {2}", time, item.Who, item.From));
                _lines.Add(string.Format("    {0} · {1}", item.What, item.Tool));
                count++;
            }

            if (channel.Items.Count == 0 && string.IsNullOrEmpty(channel.Note))
            {
                _lines.Add("  нет записей");
            }

            _lines.Add(string.Empty);
        }

        private static string ThreatLevelLabel(ThreatLevel level)
        {
            switch (level)
            {
                case ThreatLevel.Alert:
                    return "тревога";
                case ThreatLevel.Watch:
                    return "внимание";
                default:
                    return "норма";
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            using (GraphicsPath path = CreateCardPath())
            using (Brush fill = new SolidBrush(PanelColors.SectionFill))
            using (Pen border = new Pen(PanelColors.CalloutBorder))
            {
                g.FillPath(fill, path);
                g.DrawPath(border, path);
            }

            using (Font font = new Font("Segoe UI", 8.25f))
            using (Font bold = new Font("Segoe UI", 8.25f, FontStyle.Bold))
            using (Brush text = new SolidBrush(PanelColors.TextPrimary))
            using (Brush muted = new SolidBrush(PanelColors.TextMuted))
            {
                int y = Pad;
                foreach (string line in _lines)
                {
                    if (string.IsNullOrEmpty(line))
                    {
                        y += SectionGap / 2;
                        continue;
                    }

                    bool section = !line.StartsWith(" ", StringComparison.Ordinal);
                    Font useFont = section ? bold : font;
                    Brush useBrush = section ? text : muted;
                    g.DrawString(line, useFont, useBrush, Pad, y);
                    y += LineHeight;
                }
            }
        }
    }
}
