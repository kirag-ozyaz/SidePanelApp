using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SidePanelApp;
using SidePanelApp.V3.Controls;

namespace SidePanelApp.V3
{
    /// <summary>
    /// Боковая панель у правого края экрана (35×108).
    /// Содержит индикаторы CPU/RAM; при наведении показывает <see cref="Controls.MetricDetailPopup"/>.
    /// </summary>
    public partial class OverlayPanelV3 : Form
    {
        private const int PanelWidth = 35;
        private const int PanelHeight = 108;
        private const int CornerRadius = 12;

        private readonly SystemMetricsProvider _metrics;
        private readonly Timer _refreshTimer;
        private readonly Timer _hideDetailTimer;
        private MetricDetailPopup _detailPopup;
        private RingIndicatorControl _activeIndicator;
        private bool _detailPinned;
        private bool _allowMove;

        /// <summary>Разрешить перетаскивание; при false панель привязана к правому краю.</summary>
        public bool AllowMove
        {
            get { return _allowMove; }
            set
            {
                _allowMove = value;
                if (!_allowMove)
                {
                    DockToRightEdge();
                }
            }
        }

        public OverlayPanelV3()
        {
            InitializeComponent();
            Icon = SidePanelApp.AppIconFactory.GetApplicationIcon(32);
            ApplyTheme();
            SetupIndicators();
            SetupDragHandlers();

            _metrics = new SystemMetricsProvider();
            _refreshTimer = new Timer { Interval = 1000 };
            _refreshTimer.Tick += RefreshTimer_Tick;
            _refreshTimer.Start();

            _hideDetailTimer = new Timer { Interval = 250 };
            _hideDetailTimer.Tick += (s, e) =>
            {
                _hideDetailTimer.Stop();
                if (!_detailPinned)
                {
                    HideDetailPopup();
                }
            };

            RefreshTimer_Tick(null, EventArgs.Empty);
        }

        private void ApplyTheme()
        {
            BackColor = PanelColors.Background;
            Opacity = PanelColors.PanelOpacity;
            cpuIndicator.BackColor = PanelColors.Background;
            ramIndicator.BackColor = PanelColors.Background;
        }

        private void ApplyRoundedShape()
        {
            Rectangle bounds = new Rectangle(0, 0, Width, Height);
            using (GraphicsPath path = PanelColors.CreateLeftRoundedRect(bounds, CornerRadius))
            {
                PanelColors.ApplyRegion(this, path);
            }
        }

        private void SetupIndicators()
        {
            cpuIndicator.Title = "Процессор";
            cpuIndicator.IconText = "⚡";
            cpuIndicator.RingColor = PanelColors.CpuAccent;

            ramIndicator.Title = "Память";
            ramIndicator.IconText = "▣";
            ramIndicator.RingColor = PanelColors.RamAccent;

            cpuIndicator.IndicatorMouseEnter += Indicator_IndicatorMouseEnter;
            cpuIndicator.IndicatorMouseLeave += Indicator_IndicatorMouseLeave;
            ramIndicator.IndicatorMouseEnter += Indicator_IndicatorMouseEnter;
            ramIndicator.IndicatorMouseLeave += Indicator_IndicatorMouseLeave;
        }

        private void Indicator_IndicatorMouseEnter(object sender, EventArgs e)
        {
            ShowDetailPopup((RingIndicatorControl)sender);
        }

        private void Indicator_IndicatorMouseLeave(object sender, EventArgs e)
        {
            ScheduleHideDetailPopup();
        }

        private void EnsureDetailPopup()
        {
            if (_detailPopup == null || _detailPopup.IsDisposed)
            {
                _detailPopup = new MetricDetailPopup();
                _detailPopup.HoverStateChanged += DetailPopup_HoverStateChanged;
                _detailPopup.PrepareForDisplay();
            }
        }

        private void ShowDetailPopup(RingIndicatorControl indicator)
        {
            _hideDetailTimer.Stop();
            _activeIndicator = indicator;
            EnsureDetailPopup();

            _detailPopup.UpdateMetric(indicator.Title, indicator.Value, indicator.RingColor);
            _detailPopup.AlignNearIndicator(indicator);

            if (!_detailPopup.Visible)
            {
                _detailPopup.Show();
            }
            else
            {
                _detailPopup.BringToFront();
            }
        }

        private void DetailPopup_HoverStateChanged(bool entered)
        {
            _detailPinned = entered;
            if (entered)
            {
                _hideDetailTimer.Stop();
            }
            else
            {
                ScheduleHideDetailPopup();
            }
        }

        private void ScheduleHideDetailPopup()
        {
            _hideDetailTimer.Stop();
            _hideDetailTimer.Start();
        }

        private void HideDetailPopup()
        {
            if (_detailPopup != null && !_detailPopup.IsDisposed)
            {
                _detailPopup.Hide();
            }

            _activeIndicator = null;
        }

        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            cpuIndicator.Value = _metrics.GetCpuUsagePercent();
            ramIndicator.Value = _metrics.GetRamUsagePercent();

            if (_detailPopup != null && !_detailPopup.IsDisposed && _detailPopup.Visible && _activeIndicator != null)
            {
                _detailPopup.UpdateMetric(
                    _activeIndicator.Title,
                    _activeIndicator.Value,
                    _activeIndicator.RingColor);
            }
        }

        private void DockToRightEdge()
        {
            Rectangle screen = Screen.FromControl(this).WorkingArea;
            Location = new Point(
                screen.Right - PanelWidth,
                screen.Top + (screen.Height - PanelHeight) / 2);
        }

        private void SetupDragHandlers()
        {
            MouseDown += OverlayPanelV3_MouseDown;
            cpuIndicator.MouseDown += OverlayPanelV3_MouseDown;
            ramIndicator.MouseDown += OverlayPanelV3_MouseDown;
        }

        private void OverlayPanelV3_MouseDown(object sender, MouseEventArgs e)
        {
            if (_allowMove && e.Button == MouseButtons.Left)
            {
                DragMove();
            }
        }

        private void DragMove()
        {
            const int WM_NCLBUTTONDOWN = 0xA1;
            const int HT_CAPTION = 0x2;
            ReleaseCapture();
            SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
        }

        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            ApplyRoundedShape();
            if (!_allowMove)
            {
                DockToRightEdge();
            }
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Visible)
            {
                if (!_allowMove)
                {
                    DockToRightEdge();
                }
            }
            else
            {
                HideDetailPopup();
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            ApplyRoundedShape();
        }

        private void OverlayPanelV3_DoubleClick(object sender, EventArgs e)
        {
            Hide();
        }

        private void menuHide_Click(object sender, EventArgs e)
        {
            Hide();
        }

        private void menuClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _refreshTimer.Stop();
            _refreshTimer.Dispose();
            _hideDetailTimer.Dispose();
            _metrics.Dispose();

            if (_detailPopup != null && !_detailPopup.IsDisposed)
            {
                _detailPopup.Close();
            }

            base.OnFormClosed(e);
        }
    }
}
