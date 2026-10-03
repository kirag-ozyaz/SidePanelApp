using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SidePanelApp.V6.Controls
{
    /// <summary>
    /// Всплывающая карточка со списком устройств (видеокарты или сетевые адаптеры).
    /// Хвостик указывает на активное кольцо.
    /// </summary>
    public partial class DeviceListPopup : DetailPopupFormBase
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
            if (!IsHandleCreated)
            {
                CreateControl();
            }

            content.TailSide = IndicatorPopupAlignment.ResolveTailSide(indicator);
            Point location = IndicatorPopupAlignment.ComputeScreenLocation(
                indicator,
                content.TailSide,
                content.TailTipX,
                content.TailTipY,
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

            using (GraphicsPath path = content.CreateCardPath())
            {
                PanelColors.ApplyRegion(this, path);
            }
        }
    }
}
