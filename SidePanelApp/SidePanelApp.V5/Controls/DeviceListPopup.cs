using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SidePanelApp.V5.Controls
{
    /// <summary>
    /// Всплывающая карточка со списком устройств (видеокарты или сетевые адаптеры).
    /// Хвостик указывает на активное кольцо.
    /// </summary>
    public partial class DeviceListPopup : Form
    {
        private const int PanelGap = 8;
        private const int ScreenMargin = 8;

        public DeviceListPopup()
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

        internal void UpdateDevices(string heading, string emptyText, IList<DeviceCardModel> devices)
        {
            content.SetContent(heading, emptyText, devices);
            int height = Math.Max(56, content.BodyHeight + DeviceListDetailControl.TailExtension);
            ClientSize = new Size(DeviceListDetailControl.ControlWidth, height);
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
