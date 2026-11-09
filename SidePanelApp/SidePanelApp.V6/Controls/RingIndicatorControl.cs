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
                if (double.IsNaN(value) || double.IsInfinity(value))
                {
                    value = 0;
                }

                _value = Math.Max(0, Math.Min(100, value));
                _displayValue += (_value - _displayValue) * 0.35;
                if (double.IsNaN(_displayValue) || double.IsInfinity(_displayValue))
                {
                    _displayValue = _value;
                }

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
            if (rect.Width <= 0 || rect.Height <= 0)
            {
                return;
            }

            using (Pen backgroundPen = new Pen(PanelColors.RingTrack, RingPenWidth))
            {
                g.DrawEllipse(backgroundPen, rect);
            }

            DrawProgressArc(g, rect);

            using (Font iconFont = new Font("Segoe UI", 10F, FontStyle.Bold))
            using (SolidBrush iconBrush = new SolidBrush(_ringColor))
            using (StringFormat format = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            })
            {
                g.DrawString(IconText, iconFont, iconBrush, centerX, centerY, format);
            }
        }

        /// <summary>
        /// GDI+ отвечает OutOfMemoryException на DrawArc, если угол 0, полный круг
        /// или дуга короче штриха при круглых концах пера.
        /// </summary>
        private void DrawProgressArc(Graphics g, Rectangle rect)
        {
            float sweepAngle = (float)(_displayValue / 100d * 360d);
            if (!(sweepAngle > 0.1f))
            {
                return;
            }

            using (Pen progressPen = new Pen(_ringColor, RingPenWidth))
            {
                if (sweepAngle >= 359.9f)
                {
                    g.DrawEllipse(progressPen, rect);
                    return;
                }

                float radius = rect.Width / 2f;
                float minSweepForCaps = radius > 0f
                    ? (float)(RingPenWidth / radius * (180.0 / Math.PI)) * 1.25f
                    : 360f;
                if (sweepAngle > minSweepForCaps)
                {
                    progressPen.StartCap = LineCap.Round;
                    progressPen.EndCap = LineCap.Round;
                }

                g.DrawArc(progressPen, rect, -90, sweepAngle);
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
