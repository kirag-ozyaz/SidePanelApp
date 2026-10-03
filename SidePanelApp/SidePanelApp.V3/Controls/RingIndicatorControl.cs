using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using SidePanelApp.V3;

namespace SidePanelApp.V3.Controls
{
    /// <summary>
    /// Индикатор одной метрики: кольцо прогресса, иконка, процент под кольцом.
    /// Наследуйте для добавления новых типов метрик (сеть, диск и т.д.).
    /// </summary>
    public partial class RingIndicatorControl : UserControl
    {
        private const int RingSize = 30;
        private const int RingPenWidth = 4;

        private double _value;
        private double _displayValue;
        private Color _ringColor;

        public RingIndicatorControl()
        {
            InitializeComponent();
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            ApplyTheme();
            lblPercent.MouseEnter += RingIndicatorControl_MouseEnter;
            lblPercent.MouseLeave += RingIndicatorControl_MouseLeave;
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

        [Browsable(true)]
        public double Value
        {
            get { return _value; }
            set
            {
                _value = Math.Max(0, Math.Min(100, value));
                _displayValue += (_value - _displayValue) * 0.35;
                lblPercent.Text = $"{(int)Math.Round(_displayValue)}%";
                Invalidate();
            }
        }

        public event EventHandler IndicatorMouseEnter;
        public event EventHandler IndicatorMouseLeave;

        /// <summary>Центр кольца в экранных координатах (для привязки выноски).</summary>
        public Point GetRingCenterScreen()
        {
            int centerX = Width - 2 - RingSize / 2;
            int centerY = 2 + RingSize / 2;
            return PointToScreen(new Point(centerX, centerY));
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
    }
}
