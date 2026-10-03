using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using SidePanelApp.V4.Hardware;

namespace SidePanelApp.V4.Controls
{
    /// <summary>
    /// Всплывающая карточка с детальной информацией об оперативной памяти.
    /// </summary>
    public partial class RamDetailPopup : Form
    {
        private const int PanelGap = 8;
        private const int ScreenMargin = 8;

        public RamDetailPopup()
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

        public void UpdateSnapshot(MemorySnapshot snapshot)
        {
            content.UpdateSnapshot(snapshot);
            ClientSize = new Size(RamDetailControl.ControlWidth, Math.Max(110, content.BodyHeight));
            content.Size = ClientSize;
        }

        public void AlignNearIndicator(RingIndicatorControl indicator)
        {
            Point ringCenter = indicator.GetRingCenterScreen();
            Rectangle indicatorBounds = indicator.RectangleToScreen(indicator.ClientRectangle);
            Rectangle area = Screen.FromControl(indicator).WorkingArea;

            int x = indicatorBounds.Left - PanelGap - Width;
            int y = ringCenter.Y - Height / 2;
            x = Math.Max(area.Left + ScreenMargin, x);
            y = Math.Max(area.Top + ScreenMargin, Math.Min(y, area.Bottom - Height - ScreenMargin));

            Location = new Point(x, y);
            content.TailCenterY = Math.Max(18, Math.Min(Height - 18, ringCenter.Y - y));
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
