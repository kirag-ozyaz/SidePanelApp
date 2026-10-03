using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SidePanelApp.V6.Controls
{
    /// <summary>
    /// Индикатор одной метрики: кольцо прогресса, иконка, процент (или свой текст) под кольцом.
    /// </summary>
    public partial class RingIndicatorControl : UserControl
    {
        private const int RingSize = 30;
        private const int RingPenWidth = 4;

        private double _value;
        private double _displayValue;
        private Color _ringColor;
        private string _displayText;

        public RingIndicatorControl()
        {
            InitializeComponent();
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            ApplyTheme();
            Cursor = Cursors.Default;
            lblPercent.Cursor = Cursors.Default;
            MouseEnter += RingIndicatorControl_MouseEnter;
            MouseLeave += RingIndicatorControl_MouseLeave;
            lblPercent.MouseEnter += RingIndicatorControl_MouseEnter;
            lblPercent.MouseLeave += RingIndicatorControl_MouseLeave;
            MouseClick += RingIndicatorControl_MouseClick;
            lblPercent.MouseClick += RingIndicatorControl_MouseClick;
        }

        private void ApplyTheme()
        {
            BackColor = PanelColors.Background;
            lblPercent.ForeColor = PanelColors.TextPrimary;
            lblPercent.BackColor = PanelColors.Background;
            RingColor = PanelColors.CpuAccent;
        }

        [Browsable(true)]
        public string Title { get; set; } = "Метрика";

        [Browsable(true)]
        public string IconText { get; set; } = "●";

        [Browsable(true)]
        public Color RingColor
        {
            get { return _ringColor; }
            set
            {
                _ringColor = value;
                Invalidate();
            }
        }

        /// <summary>Если задан, показывается вместо процента (например скорость сети).</summary>
        [Browsable(true)]
        public string DisplayText
        {
            get { return _displayText; }
            set
            {
                _displayText = value;
                UpdateLabel();
            }
        }

        [Browsable(true)]
        public double Value
        {
            get { return _value; }
            set
            {
                _value = Math.Max(0, Math.Min(100, value));
                _displayValue += (_value - _displayValue) * 0.35;
                UpdateLabel();
                Invalidate();
            }
        }

        public event EventHandler IndicatorMouseEnter;
        public event EventHandler IndicatorMouseLeave;
        public event EventHandler IndicatorClick;

        public Point GetRingCenterScreen()
        {
            int centerX = Width - 2 - RingSize / 2;
            int centerY = 2 + RingSize / 2;
            return PointToScreen(new Point(centerX, centerY));
        }

        private void UpdateLabel()
        {
            if (!string.IsNullOrEmpty(_displayText))
            {
                lblPercent.Text = _displayText;
            }
            else
            {
                lblPercent.Text = string.Format("{0}%", (int)Math.Round(_displayValue));
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int centerX = Width - 2 - RingSize / 2;
            int centerY = 2 + RingSize / 2;
            Rectangle rect = new Rectangle(centerX - RingSize / 2, centerY - RingSize / 2, RingSize, RingSize);

            using (Pen backgroundPen = new Pen(PanelColors.RingTrack, RingPenWidth))
            {
                g.DrawArc(backgroundPen, rect, 0, 360);
            }

            float sweepAngle = (float)(_displayValue / 100d * 360d);
            using (Pen progressPen = new Pen(_ringColor, RingPenWidth))
            {
                progressPen.StartCap = LineCap.Round;
                progressPen.EndCap = LineCap.Round;
                g.DrawArc(progressPen, rect, -90, sweepAngle);
            }

            using (Font iconFont = new Font("Segoe UI", 10F, FontStyle.Bold))
            using (SolidBrush iconBrush = new SolidBrush(_ringColor))
            {
                StringFormat format = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                g.DrawString(IconText, iconFont, iconBrush, centerX, centerY, format);
            }
        }

        private void RingIndicatorControl_MouseEnter(object sender, EventArgs e)
        {
            IndicatorMouseEnter?.Invoke(this, EventArgs.Empty);
        }

        private void RingIndicatorControl_MouseLeave(object sender, EventArgs e)
        {
            IndicatorMouseLeave?.Invoke(this, EventArgs.Empty);
        }

        private void RingIndicatorControl_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                IndicatorClick?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
