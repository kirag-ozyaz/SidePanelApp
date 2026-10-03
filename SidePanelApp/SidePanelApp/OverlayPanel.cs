using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SidePanelApp
{
    public partial class OverlayPanel : Form
    {
        private const int PanelWidth = 35;
        private const int PanelHeight = 100;
        private const int IconSize = 30;
        private const int CornerRadius = 12;
        private const int IconInset = 2;

        public class IndicatorData
        {
            public string Label { get; set; }
            public double Value { get; set; }
            public double MaxValue { get; set; } = 100;
            public Color RingColor { get; set; }
            public string Icon { get; set; } = "●";
        }

        private readonly IndicatorData[] _indicators;
        private readonly Timer _refreshTimer;
        private readonly SystemMetricsProvider _metrics;
        private readonly double[] _displayValues;
        private bool _allowMove;

        public bool AllowMove
        {
            get { return _allowMove; }
            set
            {
                _allowMove = value;
                if (!_allowMove)
                {
                    DockToRightEdge();
                }
            }
        }

        public OverlayPanel()
        {
            _metrics = new SystemMetricsProvider();
            _indicators = new[]
            {
                new IndicatorData
                {
                    Label = "CPU",
                    RingColor = Color.FromArgb(255, 107, 53),
                    Icon = "⚡"
                },
                new IndicatorData
                {
                    Label = "RAM",
                    RingColor = Color.FromArgb(56, 239, 125),
                    Icon = "▣"
                }
            };
            _displayValues = new double[_indicators.Length];

            InitializeComponent();
            Icon = AppIconFactory.GetApplicationIcon(32);
            SetupPanel();

            _refreshTimer = new Timer { Interval = 1000 };
            _refreshTimer.Tick += RefreshTimer_Tick;
            _refreshTimer.Start();
            RefreshTimer_Tick(null, EventArgs.Empty);
        }

        private void SetupPanel()
        {
            MouseDown += OverlayPanel_MouseDown;
            DoubleClick += OverlayPanel_DoubleClick;

            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Скрыть", null, (s, e) => Hide());
            contextMenu.Items.Add("Закрыть", null, (s, e) => Close());
            ContextMenuStrip = contextMenu;
        }

        private void DockToRightEdge()
        {
            Rectangle screen = Screen.FromControl(this).WorkingArea;
            Location = new Point(
                screen.Right - PanelWidth,
                screen.Top + (screen.Height - PanelHeight) / 2);
        }

        private int RingCenterX => IconInset + IconSize / 2;

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (!_allowMove)
            {
                DockToRightEdge();
            }
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Visible && !_allowMove)
            {
                DockToRightEdge();
            }
        }

        private void OverlayPanel_MouseDown(object sender, MouseEventArgs e)
        {
            if (_allowMove && e.Button == MouseButtons.Left)
            {
                DragMove();
            }
        }

        private void OverlayPanel_DoubleClick(object sender, EventArgs e)
        {
            Hide();
        }

        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            _indicators[0].Value = _metrics.GetCpuUsagePercent();
            _indicators[1].Value = _metrics.GetRamUsagePercent();

            for (int i = 0; i < _indicators.Length; i++)
            {
                double target = _indicators[i].Value;
                _displayValues[i] += (target - _displayValues[i]) * 0.35;
            }

            Invalidate();
        }

        private void DragMove()
        {
            const int WM_NCLBUTTONDOWN = 0xA1;
            const int HT_CAPTION = 0x2;
            ReleaseCapture();
            SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
        }

        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (GraphicsPath path = CreateLeftRoundedRect(new Rectangle(0, 0, PanelWidth, PanelHeight), CornerRadius))
            using (SolidBrush background = new SolidBrush(Color.FromArgb(26, 26, 46)))
            {
                g.FillPath(background, path);
            }

            const int startY = 16;
            const int itemSpacing = 48;
            for (int i = 0; i < _indicators.Length; i++)
            {
                int centerY = startY + i * itemSpacing;
                DrawRingIndicator(g, _indicators[i], RingCenterX, centerY, i);
                DrawPercentLabel(g, RingCenterX, centerY + IconSize / 2 + 3, i);
            }
        }

        private void DrawRingIndicator(Graphics g, IndicatorData data, int centerX, int centerY, int index)
        {
            int radius = IconSize / 2;
            const int penWidth = 3;
            Rectangle rect = new Rectangle(centerX - radius, centerY - radius, IconSize, IconSize);

            using (Pen backgroundPen = new Pen(Color.FromArgb(51, 51, 85), penWidth))
            {
                g.DrawArc(backgroundPen, rect, 0, 360);
            }

            double ratio = data.MaxValue > 0 ? _displayValues[index] / data.MaxValue : 0;
            float sweepAngle = (float)(ratio * 360);

            using (Pen progressPen = new Pen(data.RingColor, penWidth))
            {
                progressPen.StartCap = LineCap.Round;
                progressPen.EndCap = LineCap.Round;
                g.DrawArc(progressPen, rect, -90, sweepAngle);
            }

            using (Font iconFont = new Font("Segoe UI", 10, FontStyle.Bold))
            using (SolidBrush iconBrush = new SolidBrush(data.RingColor))
            {
                StringFormat format = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                g.DrawString(data.Icon, iconFont, iconBrush, centerX, centerY, format);
            }
        }

        private void DrawPercentLabel(Graphics g, int centerX, int topY, int index)
        {
            string text = $"{(int)Math.Round(_displayValues[index])}%";
            using (Font font = new Font("Segoe UI", 7F, FontStyle.Bold))
            using (SolidBrush brush = new SolidBrush(Color.White))
            {
                SizeF size = g.MeasureString(text, font);
                g.DrawString(text, font, brush, centerX - size.Width / 2, topY);
            }
        }

        private static GraphicsPath CreateLeftRoundedRect(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = radius * 2;
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddLine(rect.Right, rect.Y, rect.Right, rect.Bottom);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _refreshTimer.Stop();
            _refreshTimer.Dispose();
            _metrics.Dispose();
            base.OnFormClosed(e);
        }
    }
}
