using System;
using System.Drawing;
using System.Windows.Forms;

namespace SidePanelApp.V6.Controls
{
    internal static class IndicatorPopupAlignment
    {
        private const int PanelGap = 8;
        private const int ScreenMargin = 8;

        /// <summary>
        /// Попап слева от панели (хвост справа) или справа от панели (хвост слева).
        /// </summary>
        public static CalloutTailSide ResolveTailSide(Control indicator)
        {
            Form host = indicator.FindForm();
            if (host == null)
            {
                return CalloutTailSide.Right;
            }

            Rectangle screen = Screen.FromControl(host).WorkingArea;
            int distLeft = host.Left - screen.Left;
            int distRight = screen.Right - host.Right;
            return distLeft <= distRight ? CalloutTailSide.Left : CalloutTailSide.Right;
        }

        public static Point ComputeScreenLocation(
            Control indicator,
            CalloutTailSide tailSide,
            int tailTipX,
            int tailTipY,
            int popupWidth,
            int popupHeight)
        {
            Point ringCenter = GetRingCenterScreen(indicator);
            Rectangle indicatorBounds = indicator.RectangleToScreen(indicator.ClientRectangle);
            Rectangle area = Screen.FromPoint(ringCenter).WorkingArea;

            if (popupWidth <= 0 || popupHeight <= 0)
            {
                popupWidth = Math.Max(popupWidth, 120);
                popupHeight = Math.Max(popupHeight, 80);
            }

            int y = ringCenter.Y - tailTipY;
            y = Math.Max(area.Top + ScreenMargin, Math.Min(y, area.Bottom - popupHeight - ScreenMargin));

            int x;
            if (tailSide == CalloutTailSide.Left)
            {
                x = indicatorBounds.Right + PanelGap;
            }
            else
            {
                x = indicatorBounds.Left - PanelGap - tailTipX;
            }

            x = Math.Max(area.Left + ScreenMargin, Math.Min(x, area.Right - popupWidth - ScreenMargin));

            return new Point(x, y);
        }

        public static void ApplyPositionAndRaise(Form popup, Point screenLocation)
        {
            popup.Location = screenLocation;
            popup.TopMost = true;
            popup.BringToFront();
        }

        private static Point GetRingCenterScreen(Control indicator)
        {
            if (indicator is RingIndicatorControl ring)
            {
                return ring.GetRingCenterScreen();
            }

            if (indicator is ThreatRingIndicator threat)
            {
                return threat.GetRingCenterScreen();
            }

            Rectangle bounds = indicator.RectangleToScreen(indicator.ClientRectangle);
            return new Point(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2);
        }
    }
}
