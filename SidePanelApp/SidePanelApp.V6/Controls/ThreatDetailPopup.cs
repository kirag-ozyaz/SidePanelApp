using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using SidePanelApp.V6.Security;

namespace SidePanelApp.V6.Controls
{
    public partial class ThreatDetailPopup : DetailPopupFormBase
    {
        private const int PanelGap = 8;
        private const int ScreenMargin = 8;

        public ThreatDetailPopup()
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

        internal void UpdateSnapshot(ThreatSnapshot snapshot)
        {
            content.SetSnapshot(snapshot);
            int height = Math.Max(120, content.BodyHeight + ThreatDetailControl.TailExtension);
            ClientSize = new Size(ThreatDetailControl.ControlWidth, height);
            content.Size = ClientSize;
        }

        public void AlignNearIndicator(ThreatRingIndicator indicator)
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
