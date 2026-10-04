using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using SidePanelApp;
using SidePanelApp.V6.Controls;
using SidePanelApp.V6.Hardware;
using SidePanelApp.V6.Connections;
using SidePanelApp.V6.Security;

namespace SidePanelApp.V6
{
    /// <summary>
    /// Боковая панель: CPU, RAM, видеокарты и сеть.
    /// Для GPU и NIC hover показывает карточку по каждому устройству.
    /// </summary>
    public partial class OverlayPanelV6 : Form
    {
        private const int PanelWidth = 35;
        private const int PanelHeight = 260;
        private const int CornerRadius = 12;
        private const int EdgeSnapDistance = 32;
        private const int WM_MOVING = 0x0216;
        private const int WM_EXITSIZEMOVE = 0x0232;

        private readonly object _collectorLock = new object();
        private CpuMetricsCollector _cpus;
        private MemoryMetricsCollector _memory;
        private GpuMetricsCollector _gpus;
        private NetworkMetricsCollector _nics;
        private readonly ThreatBackgroundService _threatService;
        private int _metricsCollectBusy;
        private readonly Timer _refreshTimer;
        private readonly Timer _hideDetailTimer;

        private CpuDetailPopup _cpuPopup;
        private RamDetailPopup _ramPopup;
        private DeviceListPopup _devicePopup;
        private ThreatDetailPopup _threatPopup;
        private RingIndicatorControl _activeIndicator;
        private bool _threatPopupPinned;
        private CpuSnapshot _cpuSnapshot = new CpuSnapshot();
        private MemorySnapshot _memorySnapshot = new MemorySnapshot();
        private GpuSnapshot[] _gpuSnapshots = new GpuSnapshot[0];
        private NicSnapshot[] _nicSnapshots = new NicSnapshot[0];
        private ThreatSnapshot _threatSnapshot = new ThreatSnapshot();
        private bool _detailPinned;
        private bool _allowMove;
        private bool _devicePopupIsGpu;
        /// <summary>Скруглённые углы слева (панель у правого края экрана).</summary>
        private bool _roundPanelLeft = true;

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

        public OverlayPanelV6(ThreatBackgroundService threatService)
        {
            if (threatService == null)
            {
                throw new ArgumentNullException(nameof(threatService));
            }

            _threatService = threatService;
            InitializeComponent();
            UseWaitCursor = false;
            Icon = AppIconFactory.GetApplicationIcon(32);
            ApplyTheme();
            SetupIndicators();
            SetupDragHandlers();

            _refreshTimer = new Timer { Interval = 1000 };
            _refreshTimer.Tick += RefreshTimer_Tick;
            _refreshTimer.Start();

            _hideDetailTimer = new Timer { Interval = 250 };
            _hideDetailTimer.Tick += (s, e) =>
            {
                _hideDetailTimer.Stop();
                if (!_detailPinned)
                {
                    HideDetailPopups();
                }
            };
        }

        private void ApplyTheme()
        {
            BackColor = PanelColors.Background;
            Opacity = PanelColors.PanelOpacity;
            cpuIndicator.BackColor = PanelColors.Background;
            ramIndicator.BackColor = PanelColors.Background;
            gpuIndicator.BackColor = PanelColors.Background;
            netIndicator.BackColor = PanelColors.Background;
            threatIndicator.BackColor = PanelColors.Background;
        }

        private void ApplyRoundedShape()
        {
            Rectangle bounds = new Rectangle(0, 0, Width, Height);
            using (GraphicsPath path = _roundPanelLeft
                ? PanelColors.CreateLeftRoundedRect(bounds, CornerRadius)
                : PanelColors.CreateRightRoundedRect(bounds, CornerRadius))
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

            gpuIndicator.Title = "Видеокарта";
            gpuIndicator.IconText = "◆";
            gpuIndicator.RingColor = PanelColors.GpuAccent;

            netIndicator.Title = "Сеть";
            netIndicator.IconText = "⇅";
            netIndicator.RingColor = PanelColors.NetAccent;

            cpuIndicator.IndicatorMouseEnter += Indicator_IndicatorMouseEnter;
            cpuIndicator.IndicatorMouseLeave += Indicator_IndicatorMouseLeave;
            ramIndicator.IndicatorMouseEnter += Indicator_IndicatorMouseEnter;
            ramIndicator.IndicatorMouseLeave += Indicator_IndicatorMouseLeave;
            gpuIndicator.IndicatorMouseEnter += Indicator_IndicatorMouseEnter;
            gpuIndicator.IndicatorMouseLeave += Indicator_IndicatorMouseLeave;
            netIndicator.IndicatorMouseEnter += Indicator_IndicatorMouseEnter;
            netIndicator.IndicatorMouseLeave += Indicator_IndicatorMouseLeave;
            netIndicator.MouseUp += NetIndicator_MouseUp;

            threatIndicator.Title = "АРМ";
            threatIndicator.IconText = "◉";
            threatIndicator.IndicatorMouseEnter += ThreatIndicator_IndicatorMouseEnter;
            threatIndicator.IndicatorMouseLeave += ThreatIndicator_IndicatorMouseLeave;
            threatIndicator.IndicatorClick += ThreatIndicator_IndicatorClick;
        }

        private void ThreatIndicator_IndicatorMouseEnter(object sender, EventArgs e)
        {
            _hideDetailTimer.Stop();
            _threatPopupPinned = false;
            _activeIndicator = null;
            HideCpuPopup();
            HideRamPopup();
            HideDevicePopup();
            ShowThreatPopup();
        }

        private void ThreatIndicator_IndicatorMouseLeave(object sender, EventArgs e)
        {
            ScheduleHideDetailPopup();
        }

        private void NetIndicator_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                ConnectionsWindowHost.ShowOrActivate(this);
            }
        }

        private void ThreatIndicator_IndicatorClick(object sender, EventArgs e)
        {
            _hideDetailTimer.Stop();
            _detailPinned = true;
            _threatPopupPinned = true;
            _activeIndicator = null;
            HideCpuPopup();
            HideRamPopup();
            HideDevicePopup();
            ShowThreatPopup();
        }

        private void Indicator_IndicatorMouseEnter(object sender, EventArgs e)
        {
            ShowDetailPopup((RingIndicatorControl)sender);
        }

        private void Indicator_IndicatorMouseLeave(object sender, EventArgs e)
        {
            ScheduleHideDetailPopup();
        }

        private void ShowDetailPopup(RingIndicatorControl indicator)
        {
            Cursor = Cursors.Default;
            _hideDetailTimer.Stop();
            _threatPopupPinned = false;
            HideThreatPopup();
            _activeIndicator = indicator;

            if (indicator == cpuIndicator)
            {
                HideRamPopup();
                HideDevicePopup();
                ShowCpuPopup();
            }
            else if (indicator == ramIndicator)
            {
                HideCpuPopup();
                HideDevicePopup();
                ShowRamPopup();
            }
            else if (indicator == gpuIndicator || indicator == netIndicator)
            {
                HideCpuPopup();
                HideRamPopup();
                ShowDevicePopup(indicator == gpuIndicator);
            }
        }

        private void ShowCpuPopup()
        {
            EnsureCpuPopup();
            _cpuPopup.Owner = null;
            _cpuPopup.UpdateSnapshot(_cpuSnapshot);
            ShowDetailPopupForm(_cpuPopup);
            _cpuPopup.AlignNearIndicator(cpuIndicator);
        }

        private void ShowRamPopup()
        {
            EnsureRamPopup();
            _ramPopup.Owner = null;
            _ramPopup.UpdateSnapshot(_memorySnapshot);
            ShowDetailPopupForm(_ramPopup);
            _ramPopup.AlignNearIndicator(ramIndicator);
        }

        private void ShowDevicePopup(bool gpu)
        {
            EnsureDevicePopup();
            _devicePopup.Owner = null;
            _devicePopupIsGpu = gpu;
            FillDevicePopup();
            ShowDetailPopupForm(_devicePopup);
            _devicePopup.AlignNearIndicator(gpu ? gpuIndicator : netIndicator);
        }

        private static void ShowDetailPopupForm(Form popup)
        {
            if (!popup.Visible)
            {
                popup.Show();
            }
            else
            {
                popup.BringToFront();
            }
        }

        private void FillDevicePopup()
        {
            if (_devicePopupIsGpu)
            {
                _devicePopup.UpdateDevices(
                    _gpuSnapshots.Length > 1 ? "Видеокарты" : "Видеокарта",
                    "Видеокарта не найдена",
                    BuildGpuCards());
            }
            else
            {
                _devicePopup.UpdateDevices(
                    _nicSnapshots.Length > 1 ? "Сетевые адаптеры" : "Сеть",
                    "Сетевые адаптеры не найдены",
                    BuildNicCards());
            }
        }

        private List<DeviceCardModel> BuildGpuCards()
        {
            var cards = new List<DeviceCardModel>();
            foreach (GpuSnapshot gpu in _gpuSnapshots)
            {
                cards.Add(new DeviceCardModel
                {
                    Title = gpu.Name,
                    Metrics =
                    {
                        new DeviceMetricRow
                        {
                            Label = "Процессор",
                            Percent = gpu.EnginePercent,
                            ValueText = string.Format("{0}%", (int)Math.Round(gpu.EnginePercent)),
                            Accent = PanelColors.GpuAccent
                        },
                        new DeviceMetricRow
                        {
                            Label = "Выделенная",
                            Percent = gpu.DedicatedPercent,
                            ValueText = FormatGpuMemory(gpu.DedicatedUsed, gpu.DedicatedTotal),
                            Accent = Color.FromArgb(232, 168, 255)
                        },
                        new DeviceMetricRow
                        {
                            Label = "Общая",
                            Percent = gpu.SharedPercent,
                            ValueText = FormatGpuMemory(gpu.SharedUsed, gpu.SharedTotal),
                            Accent = Color.FromArgb(196, 181, 253)
                        },
                        new DeviceMetricRow
                        {
                            Label = "Всего",
                            Percent = gpu.MemoryPercent,
                            ValueText = FormatGpuMemory(gpu.MemoryUsed, gpu.MemoryTotal),
                            Accent = Color.FromArgb(167, 139, 250)
                        }
                    }
                });
            }

            return cards;
        }

        private static string FormatGpuMemory(ulong used, ulong total)
        {
            if (total > 0)
            {
                return string.Format(
                    "{0} / {1}",
                    MetricFormat.Gigabytes(used),
                    MetricFormat.Gigabytes(total));
            }

            return MetricFormat.Gigabytes(used);
        }

        private List<DeviceCardModel> BuildNicCards()
        {
            var cards = new List<DeviceCardModel>();
            foreach (NicSnapshot nic in _nicSnapshots)
            {
                cards.Add(new DeviceCardModel
                {
                    Title = nic.Name,
                    Metrics =
                    {
                        new DeviceMetricRow
                        {
                            Label = "Приём",
                            Percent = NetworkMetricsCollector.ToRingPercent(nic.ReceivedBytesPerSec),
                            ValueText = "↓ " + MetricFormat.BytesPerSec(nic.ReceivedBytesPerSec),
                            Accent = PanelColors.NetAccent
                        },
                        new DeviceMetricRow
                        {
                            Label = "Отдача",
                            Percent = NetworkMetricsCollector.ToRingPercent(nic.SentBytesPerSec),
                            ValueText = "↑ " + MetricFormat.BytesPerSec(nic.SentBytesPerSec),
                            Accent = Color.FromArgb(125, 211, 252)
                        }
                    }
                });
            }

            return cards;
        }

        private void EnsureCpuPopup()
        {
            if (_cpuPopup == null || _cpuPopup.IsDisposed)
            {
                _cpuPopup = new CpuDetailPopup();
                _cpuPopup.HoverStateChanged += DetailPopup_HoverStateChanged;
                _cpuPopup.PrepareForDisplay();
            }
        }

        private void EnsureRamPopup()
        {
            if (_ramPopup == null || _ramPopup.IsDisposed)
            {
                _ramPopup = new RamDetailPopup();
                _ramPopup.HoverStateChanged += DetailPopup_HoverStateChanged;
                _ramPopup.PrepareForDisplay();
            }
        }

        private void EnsureDevicePopup()
        {
            if (_devicePopup == null || _devicePopup.IsDisposed)
            {
                _devicePopup = new DeviceListPopup();
                _devicePopup.HoverStateChanged += DetailPopup_HoverStateChanged;
                _devicePopup.PrepareForDisplay();
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

        private void HideDetailPopups()
        {
            HideCpuPopup();
            HideRamPopup();
            HideDevicePopup();
            HideThreatPopup();
            _activeIndicator = null;
            _threatPopupPinned = false;
        }

        private void HideCpuPopup()
        {
            if (_cpuPopup != null && !_cpuPopup.IsDisposed)
            {
                _cpuPopup.Hide();
            }
        }

        private void HideRamPopup()
        {
            if (_ramPopup != null && !_ramPopup.IsDisposed)
            {
                _ramPopup.Hide();
            }
        }

        private void HideDevicePopup()
        {
            if (_devicePopup != null && !_devicePopup.IsDisposed)
            {
                _devicePopup.Hide();
            }
        }

        private void EnsureThreatPopup()
        {
            if (_threatPopup == null || _threatPopup.IsDisposed)
            {
                _threatPopup = new ThreatDetailPopup();
                _threatPopup.HoverStateChanged += ThreatPopup_HoverStateChanged;
                _threatPopup.PrepareForDisplay();
            }
        }

        private void ThreatPopup_HoverStateChanged(bool entered)
        {
            _threatPopupPinned = entered;
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

        private void ShowThreatPopup()
        {
            EnsureThreatPopup();
            _threatPopup.Owner = null;
            _threatPopup.UpdateSnapshot(_threatSnapshot);
            ShowDetailPopupForm(_threatPopup);
            _threatPopup.AlignNearIndicator(threatIndicator);
        }

        private void HideThreatPopup()
        {
            if (_threatPopup != null && !_threatPopup.IsDisposed)
            {
                _threatPopup.Hide();
            }
        }

        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            if (IsDisposed || !IsHandleCreated)
            {
                return;
            }

            if (System.Threading.Interlocked.CompareExchange(ref _metricsCollectBusy, 1, 0) != 0)
            {
                return;
            }

            Task.Run(() => CollectMetricsBundle()).ContinueWith(
                t =>
                {
                    System.Threading.Interlocked.Exchange(ref _metricsCollectBusy, 0);
                    if (IsDisposed || !IsHandleCreated || t.IsFaulted || t.IsCanceled)
                    {
                        return;
                    }

                    ApplyMetricsBundle(t.Result);
                },
                TaskScheduler.FromCurrentSynchronizationContext());
        }

        private PanelMetricsBundle CollectMetricsBundle()
        {
            EnsureHardwareCollectors();
            return new PanelMetricsBundle
            {
                Cpu = _cpus.GetSnapshot(),
                Memory = _memory.GetSnapshot(),
                Gpus = _gpus.GetGpus(),
                Nics = _nics.GetAdapters(),
                Threat = _threatService.GetLatestSnapshot()
            };
        }

        private void EnsureHardwareCollectors()
        {
            if (_cpus != null)
            {
                return;
            }

            lock (_collectorLock)
            {
                if (_cpus != null)
                {
                    return;
                }

                _cpus = new CpuMetricsCollector();
                _memory = new MemoryMetricsCollector();
                _gpus = new GpuMetricsCollector();
                _nics = new NetworkMetricsCollector();
            }
        }

        private void ApplyMetricsBundle(PanelMetricsBundle bundle)
        {
            if (bundle == null)
            {
                return;
            }

            _cpuSnapshot = bundle.Cpu ?? new CpuSnapshot();
            cpuIndicator.Value = _cpuSnapshot.TotalPercent;

            _memorySnapshot = bundle.Memory ?? new MemorySnapshot();
            ramIndicator.Value = _memorySnapshot.UsagePercent;

            _gpuSnapshots = bundle.Gpus ?? new GpuSnapshot[0];
            double gpuLoad = 0;
            foreach (GpuSnapshot gpu in _gpuSnapshots)
            {
                if (gpu.EnginePercent > gpuLoad)
                {
                    gpuLoad = gpu.EnginePercent;
                }
            }

            gpuIndicator.Value = gpuLoad;

            _nicSnapshots = bundle.Nics ?? new NicSnapshot[0];
            double netTotal = 0;
            foreach (NicSnapshot nic in _nicSnapshots)
            {
                netTotal += nic.TotalBytesPerSec;
            }

            netIndicator.Value = NetworkMetricsCollector.ToRingPercent(netTotal);
            netIndicator.DisplayText = MetricFormat.CompactBytesPerSec(netTotal);

            _threatSnapshot = bundle.Threat ?? new ThreatSnapshot();
            string threatCaption = _threatSnapshot.OverallLevel == ThreatLevel.Alert
                ? "!"
                : _threatSnapshot.OverallLevel == ThreatLevel.Watch ? "…" : "OK";
            threatIndicator.SetLevels(
                _threatSnapshot.Remote.Level,
                _threatSnapshot.Smb.Level,
                _threatSnapshot.Probe.Level,
                threatCaption);

            RefreshOpenDetailPopups();
        }

        private void RefreshOpenDetailPopups()
        {
            if (_threatPopup != null && !_threatPopup.IsDisposed && _threatPopup.Visible)
            {
                _threatPopup.UpdateSnapshot(_threatSnapshot);
                _threatPopup.AlignNearIndicator(threatIndicator);
            }

            if (_activeIndicator == null && !_threatPopupPinned)
            {
                return;
            }

            if (_activeIndicator == null)
            {
                return;
            }

            if (_cpuPopup != null && !_cpuPopup.IsDisposed && _cpuPopup.Visible &&
                _activeIndicator == cpuIndicator)
            {
                _cpuPopup.UpdateSnapshot(_cpuSnapshot);
                _cpuPopup.AlignNearIndicator(cpuIndicator);
            }

            if (_ramPopup != null && !_ramPopup.IsDisposed && _ramPopup.Visible &&
                _activeIndicator == ramIndicator)
            {
                _ramPopup.UpdateSnapshot(_memorySnapshot);
                _ramPopup.AlignNearIndicator(ramIndicator);
            }

            if (_devicePopup != null && !_devicePopup.IsDisposed && _devicePopup.Visible &&
                (_activeIndicator == gpuIndicator || _activeIndicator == netIndicator))
            {
                FillDevicePopup();
                _devicePopup.AlignNearIndicator(_activeIndicator);
            }
        }

        private void PreWarmDetailPopups()
        {
            EnsureCpuPopup();
            EnsureRamPopup();
            EnsureDevicePopup();
            EnsureThreatPopup();
            PrewarmHiddenShow(_cpuPopup);
            PrewarmHiddenShow(_ramPopup);
            PrewarmHiddenShow(_devicePopup);
            PrewarmHiddenShow(_threatPopup);
        }

        private void PrewarmHiddenShow(Form popup)
        {
            if (popup == null || popup.IsDisposed || popup.Visible)
            {
                return;
            }

            double opacity = popup.Opacity;
            try
            {
                popup.Opacity = 0;
                popup.Owner = null;
                popup.Show();
                popup.Hide();
            }
            finally
            {
                popup.Opacity = opacity;
            }
        }

        private void DockToRightEdge()
        {
            Rectangle screen = Screen.FromControl(this).WorkingArea;
            _roundPanelLeft = true;
            Location = new Point(
                screen.Right - PanelWidth,
                screen.Top + (screen.Height - PanelHeight) / 2);
            ApplyRoundedShape();
        }

        /// <summary>
        /// После перетаскивания — к ближайшему вертикальному краю рабочей области (левый или правый).
        /// </summary>
        private void SnapPanelToEdge()
        {
            if (!_allowMove)
            {
                return;
            }

            Rectangle bounds = Bounds;
            Rectangle screen = Screen.FromRectangle(bounds).WorkingArea;
            int width = bounds.Width;
            int height = bounds.Height;

            int distLeft = bounds.Left - screen.Left;
            int distRight = screen.Right - bounds.Right;
            int x;
            if (distLeft <= distRight)
            {
                x = screen.Left;
                _roundPanelLeft = false;
            }
            else
            {
                x = screen.Right - width;
                _roundPanelLeft = true;
            }

            int y = Math.Max(screen.Top, Math.Min(screen.Bottom - height, bounds.Top));
            Location = new Point(x, y);
            ApplyRoundedShape();
            RepositionVisibleDetailPopups();
        }

        private void RepositionVisibleDetailPopups()
        {
            if (_cpuPopup != null && !_cpuPopup.IsDisposed && _cpuPopup.Visible)
            {
                _cpuPopup.AlignNearIndicator(cpuIndicator);
            }

            if (_ramPopup != null && !_ramPopup.IsDisposed && _ramPopup.Visible)
            {
                _ramPopup.AlignNearIndicator(ramIndicator);
            }

            if (_devicePopup != null && !_devicePopup.IsDisposed && _devicePopup.Visible && _activeIndicator != null)
            {
                _devicePopup.AlignNearIndicator(_activeIndicator);
            }

            if (_threatPopup != null && !_threatPopup.IsDisposed && _threatPopup.Visible)
            {
                _threatPopup.AlignNearIndicator(threatIndicator);
            }
        }

        private void AdjustMovingRectangle(ref RECT rc)
        {
            Rectangle proposed = new Rectangle(rc.Left, rc.Top, rc.Right - rc.Left, rc.Bottom - rc.Top);
            Rectangle screen = Screen.FromRectangle(proposed).WorkingArea;
            int width = proposed.Width;
            int height = proposed.Height;

            int distLeft = rc.Left - screen.Left;
            int distRight = screen.Right - rc.Right;

            if (distRight <= EdgeSnapDistance && distRight <= distLeft)
            {
                rc.Left = screen.Right - width;
                rc.Right = screen.Right;
            }
            else if (distLeft <= EdgeSnapDistance)
            {
                rc.Left = screen.Left;
                rc.Right = screen.Left + width;
            }

            if (rc.Top < screen.Top)
            {
                rc.Top = screen.Top;
                rc.Bottom = screen.Top + height;
            }
            else if (rc.Bottom > screen.Bottom)
            {
                rc.Bottom = screen.Bottom;
                rc.Top = screen.Bottom - height;
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (_allowMove && m.Msg == WM_MOVING)
            {
                RECT rc = (RECT)Marshal.PtrToStructure(m.LParam, typeof(RECT));
                AdjustMovingRectangle(ref rc);
                Marshal.StructureToPtr(rc, m.LParam, false);
            }

            base.WndProc(ref m);

            if (_allowMove && m.Msg == WM_EXITSIZEMOVE)
            {
                BeginInvoke(new Action(SnapPanelToEdge));
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private void SetupDragHandlers()
        {
            MouseDown += OverlayPanelV6_MouseDown;
            MouseUp += OverlayPanelV6_MouseUp;
            cpuIndicator.MouseDown += OverlayPanelV6_MouseDown;
            cpuIndicator.MouseUp += OverlayPanelV6_MouseUp;
            ramIndicator.MouseDown += OverlayPanelV6_MouseDown;
            ramIndicator.MouseUp += OverlayPanelV6_MouseUp;
            gpuIndicator.MouseDown += OverlayPanelV6_MouseDown;
            gpuIndicator.MouseUp += OverlayPanelV6_MouseUp;
            netIndicator.MouseDown += OverlayPanelV6_MouseDown;
            netIndicator.MouseUp += OverlayPanelV6_MouseUp;
            threatIndicator.MouseDown += OverlayPanelV6_MouseDown;
            threatIndicator.MouseUp += OverlayPanelV6_MouseUp;
        }

        private void OverlayPanelV6_MouseDown(object sender, MouseEventArgs e)
        {
            if (_allowMove && e.Button == MouseButtons.Left)
            {
                DragMove();
            }
        }

        private void OverlayPanelV6_MouseUp(object sender, MouseEventArgs e)
        {
            if (_allowMove && e.Button == MouseButtons.Left)
            {
                SnapPanelToEdge();
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
                HideDetailPopups();
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            ApplyRoundedShape();
            BeginInvoke(new Action(() => RefreshTimer_Tick(null, EventArgs.Empty)));

            var prewarmTimer = new Timer { Interval = 500 };
            prewarmTimer.Tick += (s, ev) =>
            {
                prewarmTimer.Stop();
                prewarmTimer.Dispose();
                if (!IsDisposed)
                {
                    PreWarmDetailPopups();
                }
            };
            prewarmTimer.Start();
        }

        private void OverlayPanelV6_DoubleClick(object sender, EventArgs e)
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
            lock (_collectorLock)
            {
                _cpus?.Dispose();
                _memory?.Dispose();
                _gpus?.Dispose();
                _nics?.Dispose();
            }

            if (_cpuPopup != null && !_cpuPopup.IsDisposed)
            {
                _cpuPopup.Close();
            }

            if (_ramPopup != null && !_ramPopup.IsDisposed)
            {
                _ramPopup.Close();
            }

            if (_devicePopup != null && !_devicePopup.IsDisposed)
            {
                _devicePopup.Close();
            }

            if (_threatPopup != null && !_threatPopup.IsDisposed)
            {
                _threatPopup.Close();
            }

            base.OnFormClosed(e);
        }
    }
}
