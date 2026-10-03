using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SidePanelApp.V6.Controls
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
            if (!IsHandleCreated)
            {
                CreateControl();
            }

            content.TailSide = IndicatorPopupAlignment.ResolveTailSide(indicator);
            Point location = IndicatorPopupAlignment.ComputeScreenLocation(
                indicator,
                content.TailSide,
                content.TailTipX,
                MetricDetailControl.TailTipY,
                ClientSize.Width,
                ClientSize.Height);
            ApplyShape();
            content.Invalidate();
            IndicatorPopupAlignment.ApplyPositionAndRaise(this, location);
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
