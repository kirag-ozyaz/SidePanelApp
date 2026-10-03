using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using SidePanelApp.V2;

namespace SidePanelApp.V2.Controls
{
    /// <summary>
    /// Всплывающее окно-выноска. Хостит <see cref="MetricDetailControl"/>,
    /// позиционируется слева от панели, хвостиком к активному кольцу.
    /// </summary>
    public partial class MetricDetailPopup : Form
    {
        /// <summary>Зазор между хвостиком попапа и левым краем боковой панели.</summary>
        private const int PanelGap = 8;

        /// <summary>Отступ от краёв экрана при ограничении позиции.</summary>
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

        /// <summary>Создаёт handle и применяет форму выноски до первого показа.</summary>
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

        /// <summary>
        /// Размещает окно так, чтобы хвостик указывал на центр кольца,
        /// а тело выноски не перекрывало боковую панель.
        /// </summary>
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
