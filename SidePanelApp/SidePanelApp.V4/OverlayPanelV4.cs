using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SidePanelApp;
using SidePanelApp.V4.Controls;
using SidePanelApp.V4.Hardware;

namespace SidePanelApp.V4
{
    /// <summary>
    /// Боковая панель: CPU, RAM, видеокарты и сеть.
    /// Для GPU и NIC hover показывает карточку по каждому устройству.
    /// </summary>
    public partial class OverlayPanelV4 : Form
    {
        private const int PanelWidth = 35;
        private const int PanelHeight = 204;
        private const int CornerRadius = 12;

        private readonly CpuMetricsCollector _cpus;
        private readonly MemoryMetricsCollector _memory;
        private readonly GpuMetricsCollector _gpus;
        private readonly NetworkMetricsCollector _nics;
        private readonly Timer _refreshTimer;
        private readonly Timer _hideDetailTimer;

        private CpuDetailPopup _cpuPopup;
        private RamDetailPopup _ramPopup;
        private DeviceListPopup _devicePopup;
        private RingIndicatorControl _activeIndicator;
        private CpuSnapshot _cpuSnapshot = new CpuSnapshot();
        private MemorySnapshot _memorySnapshot = new MemorySnapshot();
        private GpuSnapshot[] _gpuSnapshots = new GpuSnapshot[0];
        private NicSnapshot[] _nicSnapshots = new NicSnapshot[0];
        private bool _detailPinned;
        private bool _allowMove;
        private bool _devicePopupIsGpu;

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

        public OverlayPanelV4()
        {
            InitializeComponent();
            Icon = AppIconFactory.GetApplicationIcon(32);
            ApplyTheme();
            SetupIndicators();
            SetupDragHandlers();

            _cpus = new CpuMetricsCollector();
            _memory = new MemoryMetricsCollector();
            _gpus = new GpuMetricsCollector();
            _nics = new NetworkMetricsCollector();

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

            RefreshTimer_Tick(null, EventArgs.Empty);
        }

        private void ApplyTheme()
        {
            BackColor = PanelColors.Background;
            Opacity = PanelColors.PanelOpacity;
            cpuIndicator.BackColor = PanelColors.Background;
            ramIndicator.BackColor = PanelColors.Background;
            gpuIndicator.BackColor = PanelColors.Background;
            netIndicator.BackColor = PanelColors.Background;
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
            _hideDetailTimer.Stop();
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
            _cpuPopup.UpdateSnapshot(_cpuSnapshot);
            _cpuPopup.AlignNearIndicator(cpuIndicator);
            if (!_cpuPopup.Visible)
            {
                _cpuPopup.Show();
            }
            else
            {
                _cpuPopup.BringToFront();
            }
        }

        private void ShowRamPopup()
        {
            EnsureRamPopup();
            _ramPopup.UpdateSnapshot(_memorySnapshot);
            _ramPopup.AlignNearIndicator(ramIndicator);
            if (!_ramPopup.Visible)
            {
                _ramPopup.Show();
            }
            else
            {
                _ramPopup.BringToFront();
            }
        }

        private void ShowDevicePopup(bool gpu)
        {
            EnsureDevicePopup();
            _devicePopupIsGpu = gpu;
            FillDevicePopup();
            _devicePopup.AlignNearIndicator(gpu ? gpuIndicator : netIndicator);
            if (!_devicePopup.Visible)
            {
                _devicePopup.Show();
            }
            else
            {
                _devicePopup.BringToFront();
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
            _activeIndicator = null;
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

        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            _cpuSnapshot = _cpus.GetSnapshot();
            cpuIndicator.Value = _cpuSnapshot.TotalPercent;

            _memorySnapshot = _memory.GetSnapshot();
            ramIndicator.Value = _memorySnapshot.UsagePercent;

            _gpuSnapshots = _gpus.GetGpus();
            double gpuLoad = 0;
            foreach (GpuSnapshot gpu in _gpuSnapshots)
            {
                if (gpu.EnginePercent > gpuLoad)
                {
                    gpuLoad = gpu.EnginePercent;
                }
            }

            gpuIndicator.Value = gpuLoad;

            _nicSnapshots = _nics.GetAdapters();
            double netTotal = 0;
            foreach (NicSnapshot nic in _nicSnapshots)
            {
                netTotal += nic.TotalBytesPerSec;
            }

            netIndicator.Value = NetworkMetricsCollector.ToRingPercent(netTotal);
            netIndicator.DisplayText = MetricFormat.CompactBytesPerSec(netTotal);

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

        private void DockToRightEdge()
        {
            Rectangle screen = Screen.FromControl(this).WorkingArea;
            Location = new Point(
                screen.Right - PanelWidth,
                screen.Top + (screen.Height - PanelHeight) / 2);
        }

        private void SetupDragHandlers()
        {
            MouseDown += OverlayPanelV4_MouseDown;
            cpuIndicator.MouseDown += OverlayPanelV4_MouseDown;
            ramIndicator.MouseDown += OverlayPanelV4_MouseDown;
            gpuIndicator.MouseDown += OverlayPanelV4_MouseDown;
            netIndicator.MouseDown += OverlayPanelV4_MouseDown;
        }

        private void OverlayPanelV4_MouseDown(object sender, MouseEventArgs e)
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
                HideDetailPopups();
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            ApplyRoundedShape();
        }

        private void OverlayPanelV4_DoubleClick(object sender, EventArgs e)
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
            _cpus.Dispose();
            _memory.Dispose();
            _gpus.Dispose();
            _nics.Dispose();

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

            base.OnFormClosed(e);
        }
    }
}
