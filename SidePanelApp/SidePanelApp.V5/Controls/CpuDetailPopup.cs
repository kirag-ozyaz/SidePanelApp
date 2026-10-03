using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using SidePanelApp.V5.Hardware;

namespace SidePanelApp.V5.Controls
{
    /// <summary>
    /// Всплывающая карточка с детальной информацией о процессоре.
    /// </summary>
    public partial class CpuDetailPopup : Form
    {
        private const int PanelGap = 8;
        private const int ScreenMargin = 8;

        public CpuDetailPopup()
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

        public void UpdateSnapshot(CpuSnapshot snapshot)
        {
            content.UpdateSnapshot(snapshot);
            int height = Math.Max(120, content.BodyHeight + CpuDetailControl.TailExtension);
            ClientSize = new Size(CpuDetailControl.ControlWidth, height);
            content.Size = ClientSize;
        }

        public void AlignNearIndicator(RingIndicatorControl indicator)
        {
            Point ringCenter = indicator.GetRingCenterScreen();
            Rectangle indicatorBounds = indicator.RectangleToScreen(indicator.ClientRectangle);
            Rectangle area = Screen.FromControl(indicator).WorkingArea;

            int x = indicatorBounds.Left - PanelGap - content.TailTipX;
            int y = ringCenter.Y - content.TailTipY;
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

            using (GraphicsPath path = content.CreateCardPath())
            {
                PanelColors.ApplyRegion(this, path);
            }
        }
    }
}
