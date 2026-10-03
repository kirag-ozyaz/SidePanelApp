using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using SidePanelApp.V6.Hardware;

namespace SidePanelApp.V6.Controls
{
    /// <summary>
    /// Всплывающая карточка с детальной информацией об оперативной памяти.
    /// </summary>
    public partial class RamDetailPopup : DetailPopupFormBase
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
            int height = Math.Max(110, content.BodyHeight + RamDetailControl.TailExtension);
            ClientSize = new Size(RamDetailControl.ControlWidth, height);
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
