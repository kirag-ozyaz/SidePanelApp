using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using SidePanelApp.V5.Security;

namespace SidePanelApp.V5.Controls
{
    /// <summary>
    /// Кольцо из трёх секций: удалёнка, SMB, попытки входа.
    /// </summary>
    public partial class ThreatRingIndicator : UserControl
    {
        private const int RingSize = 30;
        private const int RingPenWidth = 4;
        private const float SectorSpan = 120f;
        private const float SectorGap = 6f;

        private ThreatLevel _remoteLevel = ThreatLevel.Ok;
        private ThreatLevel _smbLevel = ThreatLevel.Ok;
        private ThreatLevel _probeLevel = ThreatLevel.Ok;

        public ThreatRingIndicator()
        {
            InitializeComponent();
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            ApplyTheme();

            MouseEnter += ThreatRingIndicator_MouseEnter;
            MouseLeave += ThreatRingIndicator_MouseLeave;
            lblCaption.MouseEnter += ThreatRingIndicator_MouseEnter;
            lblCaption.MouseLeave += ThreatRingIndicator_MouseLeave;
            lblCaption.MouseClick += (s, e) => IndicatorClick?.Invoke(this, EventArgs.Empty);
        }

        [Browsable(true)]
        public string Title { get; set; } = "АРМ";

        [Browsable(true)]
        public string IconText { get; set; } = "◉";

        public event EventHandler IndicatorClick;
        public event EventHandler IndicatorMouseEnter;
        public event EventHandler IndicatorMouseLeave;

        public void SetLevels(ThreatLevel remote, ThreatLevel smb, ThreatLevel probe, string caption)
        {
            _remoteLevel = remote;
            _smbLevel = smb;
            _probeLevel = probe;
            lblCaption.Text = string.IsNullOrEmpty(caption) ? "OK" : caption;
            Invalidate();
        }

        public Point GetRingCenterScreen()
        {
            int centerX = Width - 2 - RingSize / 2;
            int centerY = 2 + RingSize / 2;
            return PointToScreen(new Point(centerX, centerY));
        }

        private void ApplyTheme()
        {
            BackColor = PanelColors.Background;
            lblCaption.ForeColor = PanelColors.TextPrimary;
            lblCaption.BackColor = PanelColors.Background;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int centerX = Width - 2 - RingSize / 2;
            int centerY = 2 + RingSize / 2;
            Rectangle rect = new Rectangle(centerX - RingSize / 2, centerY - RingSize / 2, RingSize, RingSize);

            DrawSector(g, rect, 0, _remoteLevel);
            DrawSector(g, rect, 1, _smbLevel);
            DrawSector(g, rect, 2, _probeLevel);

            ThreatLevel accentLevel = MaxLevel(_remoteLevel, _smbLevel, _probeLevel);
            using (Font iconFont = new Font("Segoe UI", 10F, FontStyle.Bold))
            using (SolidBrush iconBrush = new SolidBrush(PanelColors.ThreatColor(accentLevel)))
            {
                StringFormat format = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                g.DrawString(IconText, iconFont, iconBrush, centerX, centerY, format);
            }
        }

        private static ThreatLevel MaxLevel(ThreatLevel a, ThreatLevel b, ThreatLevel c)
        {
            return (ThreatLevel)Math.Max(Math.Max((int)a, (int)b), (int)c);
        }

        private static void DrawSector(Graphics g, Rectangle rect, int index, ThreatLevel level)
        {
            float start = -90f + index * SectorSpan + SectorGap / 2f;
            float sweep = SectorSpan - SectorGap;

            using (Pen trackPen = new Pen(PanelColors.RingTrack, RingPenWidth))
            {
                trackPen.StartCap = LineCap.Round;
                trackPen.EndCap = LineCap.Round;
                g.DrawArc(trackPen, rect, start, sweep);
            }

            using (Pen levelPen = new Pen(PanelColors.ThreatColor(level), RingPenWidth))
            {
                levelPen.StartCap = LineCap.Round;
                levelPen.EndCap = LineCap.Round;
                g.DrawArc(levelPen, rect, start, sweep);
            }
        }

        private void ThreatRingIndicator_MouseEnter(object sender, EventArgs e)
        {
            IndicatorMouseEnter?.Invoke(this, EventArgs.Empty);
        }

        private void ThreatRingIndicator_MouseLeave(object sender, EventArgs e)
        {
            IndicatorMouseLeave?.Invoke(this, EventArgs.Empty);
        }
    }
}
