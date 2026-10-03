using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SidePanelApp.V4.Controls
{
    /// <summary>
    /// Всплывающее окно-выноска для CPU/RAM.
    /// </summary>
    public partial class MetricDetailPopup : Form
    {
        private const int PanelGap = 8;
        private const int ScreenMargin = 8;

        public MetricDetailPopup()
        {
            InitializeComponent();
            Opacity = PanelColors.PanelOpacity;

            content.HoverStateChanged += entered => HoverStateChanged?.Invoke(entered);
            MouseEnter += (s, e) => HoverStateChanged?.Invoke(true);
            MouseLeave += (s, e) => HoverStateChanged?.Invoke(false);
        }

        public event Action<bool> HoverStateChanged;

        public void PrepareForDisplay()
        {
            if (!IsHandleCreated)
            {
                CreateControl();
            }

            ApplyShape();
        }

        public void UpdateMetric(string title, double value, Color accentColor)
        {
            content.UpdateMetric(title, value, accentColor);
        }

        public void AlignNearIndicator(RingIndicatorControl indicator)
        {
            Point ringCenter = indicator.GetRingCenterScreen();
            Rectangle indicatorBounds = indicator.RectangleToScreen(indicator.ClientRectangle);

            int x = indicatorBounds.Left - PanelGap - MetricDetailControl.TailTipX;
            int y = ringCenter.Y - MetricDetailControl.TailTipY;

            Rectangle area = Screen.FromControl(indicator).WorkingArea;
            x = Math.Max(area.Left + ScreenMargin, x);
            y = Math.Max(area.Top + ScreenMargin, Math.Min(y, area.Bottom - Height - ScreenMargin));

            Location = new Point(x, y);
            ApplyShape();
            content.Invalidate();
        }

        private void ApplyShape()
        {
            if (Width <= 0 || Height <= 0)
            {
                return;
            }

            using (GraphicsPath path = content.CreateBubblePath())
            {
                PanelColors.ApplyRegion(this, path);
            }
        }
    }
}
