using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using SidePanelApp;
using SidePanelApp.V6.Infrastructure;
using SidePanelApp.V6.Network;
using SidePanelApp.V6.Properties;

namespace SidePanelApp.V6.Connections
{
    public partial class ConnectionsForm : Form
    {
        internal enum ConnectionFilter
        {
            All,
            Connected,
            Listening,
            Udp,
            Local
        }

        private readonly Timer _refreshTimer = new Timer();
        private int _autoRefreshIntervalSeconds;
        private List<ConnectionRow> _allRows = new List<ConnectionRow>();
        private ConnectionFilter _filter = ConnectionFilter.All;
        private int _refreshBusy;
        private string _preserveSelectionKey;
        private int _preserveSelectionProcessId;
        private string _preserveSelectionApplication;
        private int _preserveScrollIndex = -1;
        private string _lastSelectionKey;
        private bool _suppressGridSelectionEvents;
        private bool _restorePreserveScroll;
        private int _lastDetailProcessId = -1;
        private string _lastDetailApplication;
        private int _lastScrollIndex = -1;
        private bool _initialGridFocusDone;
        private readonly ImageList _processIcons;
        private readonly Bitmap _blankIcon;
        private int _iconLoadGeneration;
        private int _iconLoadBusy;
        private int _filteredTotalCount;
        private readonly ConnectionRowVanishTracker _vanishTracker = new ConnectionRowVanishTracker();
        private readonly ConnectionToolSessionStore _pingTraceSessions = new ConnectionToolSessionStore();
        private readonly ConnectionPacketRowStore _packetRowStore = new ConnectionPacketRowStore();
        private Panel _panelDetailHeader;
        private Panel _panelDetailTitleRow;
        private FlowLayoutPanel _flowDetailProcess;
        private FlowLayoutPanel _flowDetailConnection;
        private Panel _panelToolOutputs;
        private CollapsibleDetailSection _sectionPingTrace;
        private CollapsibleDetailSection _sectionPackets;
        private TextBox _txtPingTrace;
        private ConnectionPacketWiresharkView _packetWiresharkView;
        private readonly ConnectionPacketStreamMonitor _packetMonitor = new ConnectionPacketStreamMonitor();
        private Button _btnDetailPing;
        private Button _btnDetailTraceroute;
        private Button _btnPacketsStart;
        private Button _btnPacketsStop;
        private Panel _panelNpcapBanner;
        private Label _lblNpcapBanner;
        private LinkLabel _lnkNpcapDownload;
        private Button _btnNpcapLocalInstall;
        private Button _btnNpcapRecheck;
        private ContextMenuStrip _gridContextMenu;
        private ToolStripMenuItem _menuCloseConnection;
        private ToolStripMenuItem _menuPing;
        private ToolStripMenuItem _menuTraceroute;
        private ToolStripMenuItem _menuPackets;
        private int _backgroundEnrichGeneration;
        private int _toolRunBusy;
        private PictureBox _picDetailIcon;
        private Label _lblDetailDescription;
        private Label _lblDetailCompany;
        private Label _lblDetailVersion;
        private Label _lblDetailPath;
        private int _detailMetadataGeneration;

        public ConnectionsForm()
        {
            _blankIcon = CreateBlankIcon();
            _processIcons = new ImageList
            {
                ImageSize = new Size(16, 16),
                ColorDepth = ColorDepth.Depth32Bit
            };
            _processIcons.Images.Add(_blankIcon);

            InitializeComponent();
            Icon = AppIconFactory.GetApplicationIcon(32);
            ApplyTheme();
            SetupGrid();
            SetupDetailPanel();
            SetupGridContextMenu();
            WireFilters();
            SetupRefreshControls();

            _refreshTimer.Tick += (s, e) => RefreshData(manual: false);

            HighlightTab(btnAll);

            Shown += ConnectionsForm_Shown;
            FormClosed += ConnectionsForm_FormClosed;
        }

        private void ConnectionsForm_Shown(object sender, EventArgs e)
        {
            RefreshData(manual: true);
            UpdateNpcapBanner();
            MaybeShowNpcapStartupPrompt();
        }

        private void ConnectionsForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            _refreshTimer.Stop();
            _packetMonitor.Dispose();
            _processIcons.Dispose();
            _blankIcon.Dispose();
        }

        private static Bitmap CreateBlankIcon()
        {
            var bitmap = new Bitmap(16, 16);
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.Transparent);
            }

            return bitmap;
        }

        private void WireFilters()
        {
            btnAll.Click += (s, e) => SetFilter(ConnectionFilter.All, btnAll);
            btnConnected.Click += (s, e) => SetFilter(ConnectionFilter.Connected, btnConnected);
            btnListening.Click += (s, e) => SetFilter(ConnectionFilter.Listening, btnListening);
            btnUdp.Click += (s, e) => SetFilter(ConnectionFilter.Udp, btnUdp);
            btnLocal.Click += (s, e) => SetFilter(ConnectionFilter.Local, btnLocal);
            grid.SelectionChanged += Grid_SelectionChanged;
            grid.MouseEnter += (s, e) => _refreshTimer.Stop();
            grid.MouseLeave += (s, e) => ResumeAutoRefreshAfterGridHover();
        }

        private void SetupRefreshControls()
        {
            cmbRefresh.Items.Clear();
            cmbRefresh.Items.Add(new RefreshOption("Без обновления", 0));
            cmbRefresh.Items.Add(new RefreshOption("1 сек", 1));
            cmbRefresh.Items.Add(new RefreshOption("2 сек", 2));
            cmbRefresh.Items.Add(new RefreshOption("5 сек", 5));
            cmbRefresh.DisplayMember = nameof(RefreshOption.Caption);

            int saved = Settings.Default.ConnectionsRefreshSeconds;
            int index = IndexForSeconds(saved);
            cmbRefresh.SelectedIndex = index;
            ApplyRefreshInterval(GetSelectedSeconds());

            cmbRefresh.SelectedIndexChanged += (s, e) =>
            {
                int seconds = GetSelectedSeconds();
                Settings.Default.ConnectionsRefreshSeconds = seconds;
                Settings.Default.Save();
                ApplyRefreshInterval(seconds);
            };

            btnRefreshNow.Click += (s, e) => RefreshData(manual: true);
        }

        private void ResumeAutoRefreshAfterGridHover()
        {
            if (_autoRefreshIntervalSeconds <= 0)
            {
                _refreshTimer.Stop();
                return;
            }

            ApplyRefreshInterval(_autoRefreshIntervalSeconds);
        }

        private static int IndexForSeconds(int seconds)
        {
            switch (seconds)
            {
                case 0:
                    return 0;
                case 1:
                    return 1;
                case 5:
                    return 3;
                default:
                    return 2;
            }
        }

        private int GetSelectedSeconds()
        {
            if (cmbRefresh.SelectedItem is RefreshOption option)
            {
                return option.Seconds;
            }

            return 2;
        }

        private void ApplyRefreshInterval(int seconds)
        {
            _autoRefreshIntervalSeconds = seconds;
            _refreshTimer.Stop();
            if (seconds <= 0)
            {
                return;
            }

            _refreshTimer.Interval = Math.Max(1000, seconds * 1000);
            _refreshTimer.Start();
        }

        private sealed class RefreshOption
        {
            public RefreshOption(string caption, int seconds)
            {
                Caption = caption;
                Seconds = seconds;
            }

            public string Caption { get; }
            public int Seconds { get; }
        }

        private void SetFilter(ConnectionFilter filter, Button active)
        {
            _filter = filter;
            HighlightTab(active);
            CaptureSelectionPreserveState();
            ApplyFilter();
        }

        private void Grid_SelectionChanged(object sender, EventArgs e)
        {
            if (_suppressGridSelectionEvents)
            {
                return;
            }

            _lastSelectionKey = GetSelectionKey();
            if (grid.FirstDisplayedScrollingRowIndex >= 0)
            {
                _lastScrollIndex = grid.FirstDisplayedScrollingRowIndex;
            }

            UpdateDetailPanel();
        }

        private void HighlightTab(Button active)
        {
            foreach (Control control in panelTabs.Controls)
            {
                if (control is Button button)
                {
                    bool on = button == active;
                    button.BackColor = on ? PanelColors.NetAccent : PanelColors.SectionFill;
                    button.ForeColor = on ? PanelColors.Background : PanelColors.TextPrimary;
                }
            }
        }

        private void SetupGrid()
        {
            grid.AutoGenerateColumns = false;
            grid.Columns.Clear();
            grid.RowTemplate.Height = 22;
            var iconColumn = new DataGridViewImageColumn
            {
                HeaderText = "",
                Width = 24,
                MinimumWidth = 24,
                ImageLayout = DataGridViewImageCellLayout.Zoom,
                ReadOnly = true,
                Resizable = DataGridViewTriState.False
            };
            grid.Columns.Add(iconColumn);
            grid.CellFormatting += Grid_CellFormatting;
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Application", HeaderText = "Process", FillWeight = 16, MinimumWidth = 100 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ProcessId", HeaderText = "PID", FillWeight = 6, MinimumWidth = 48 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Protocol", HeaderText = "Protocol", FillWeight = 7, MinimumWidth = 52 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "LocalAddress", HeaderText = "Local Address", FillWeight = 14, MinimumWidth = 90 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "LocalPort", HeaderText = "Local Port", FillWeight = 8, MinimumWidth = 64 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "RemoteAddress", HeaderText = "Remote Address", FillWeight = 14, MinimumWidth = 90 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "RemotePort", HeaderText = "Remote Port", FillWeight = 8, MinimumWidth = 64 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "State", HeaderText = "State", FillWeight = 10, MinimumWidth = 72 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "SentPacketsDisplay", HeaderText = "Sent Packets", FillWeight = 8, MinimumWidth = 72 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "SentBytesDisplay", HeaderText = "Sent Bytes", FillWeight = 8, MinimumWidth = 72 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "RcvdPacketsDisplay", HeaderText = "Rcvd Packets", FillWeight = 8, MinimumWidth = 72 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "RcvdBytesDisplay", HeaderText = "Rcvd Bytes", FillWeight = 8, MinimumWidth = 72 });
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
            grid.ScrollBars = ScrollBars.Both;
        }

        private void SetupDetailPanel()
        {
            _panelDetailHeader = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(0, 0, 0, 8),
                BackColor = PanelColors.SectionFill
            };
            _panelDetailHeader.Resize += (s, e) => LayoutDetailHeaderTextWidth();

            _panelDetailTitleRow = new Panel
            {
                Dock = DockStyle.Top,
                Height = 36,
                BackColor = Color.Transparent
            };

            _picDetailIcon = new PictureBox
            {
                Location = new Point(0, 0),
                Size = new Size(32, 32),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent
            };

            _flowDetailProcess = CreateDetailFlowPanel();
            _flowDetailConnection = CreateDetailFlowPanel();

            _lblDetailDescription = CreateDetailLabel(muted: false);
            _lblDetailCompany = CreateDetailLabel(muted: true);
            _lblDetailVersion = CreateDetailLabel(muted: true);
            _lblDetailPath = CreateDetailLabel(muted: true, small: true);

            ConfigureDetailLabel(lblDetailTitle, muted: false, bold: true);
            lblDetailTitle.Location = new Point(38, 4);
            lblDetailTitle.MaximumSize = new Size(220, 0);

            ConfigureDetailLabel(lblDetailMeta, muted: true);
            ConfigureDetailLabel(lblDetailPid, muted: true);
            ConfigureDetailLabel(lblDetailUser, muted: true);
            ConfigureDetailLabel(lblDetailLocal, muted: true);
            ConfigureDetailLabel(lblDetailRemote, muted: true);

            panelDetail.Controls.Remove(lblDetailTitle);
            panelDetail.Controls.Remove(lblDetailMeta);
            panelDetail.Controls.Remove(lblDetailPid);
            panelDetail.Controls.Remove(lblDetailUser);
            panelDetail.Controls.Remove(lblDetailLocal);
            panelDetail.Controls.Remove(lblDetailRemote);

            _panelDetailTitleRow.Controls.Add(_picDetailIcon);
            _panelDetailTitleRow.Controls.Add(lblDetailTitle);
            _flowDetailProcess.Controls.Add(_lblDetailDescription);
            _flowDetailProcess.Controls.Add(_lblDetailCompany);
            _flowDetailProcess.Controls.Add(_lblDetailVersion);
            _flowDetailProcess.Controls.Add(_lblDetailPath);
            _flowDetailConnection.Controls.Add(lblDetailMeta);
            _flowDetailConnection.Controls.Add(lblDetailPid);
            _flowDetailConnection.Controls.Add(lblDetailUser);
            _flowDetailConnection.Controls.Add(lblDetailLocal);
            _flowDetailConnection.Controls.Add(lblDetailRemote);

            _panelDetailHeader.Controls.Add(_flowDetailConnection);
            _panelDetailHeader.Controls.Add(_flowDetailProcess);
            _panelDetailHeader.Controls.Add(_panelDetailTitleRow);

            panelDetail.Resize += (s, e) => LayoutDetailHeaderTextWidth();

            _txtPingTrace = CreateToolOutputTextBox();
            _packetWiresharkView = new ConnectionPacketWiresharkView();

            _btnDetailPing = CreateDetailToolbarButton("Ping", BtnDetailPing_Click);
            _btnDetailTraceroute = CreateDetailToolbarButton("Traceroute", BtnDetailTraceroute_Click);
            Panel pingTraceBody = CreateSectionBody(
                CreateToolbarPanel(_btnDetailPing, _btnDetailTraceroute),
                _txtPingTrace);

            _btnPacketsStart = CreateDetailToolbarButton("Старт", BtnPacketsStart_Click);
            _btnPacketsStop = CreateDetailToolbarButton("Стоп", BtnPacketsStop_Click);
            _panelNpcapBanner = CreateNpcapBannerPanel();
            var packetsHost = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = PanelColors.SectionFill
            };
            packetsHost.Controls.Add(_packetWiresharkView);
            packetsHost.Controls.Add(_panelNpcapBanner);
            Panel packetsBody = CreateSectionBody(
                CreateToolbarPanel(_btnPacketsStart, _btnPacketsStop),
                packetsHost);

            _sectionPingTrace = new CollapsibleDetailSection("Ping / Traceroute", pingTraceBody)
            {
                Dock = DockStyle.Top,
                ExpandedBodyHeight = 143,
                StretchVertically = false
            };
            _sectionPackets = new CollapsibleDetailSection("Пакеты", packetsBody)
            {
                Dock = DockStyle.Fill,
                StretchVertically = true
            };

            _panelToolOutputs = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = PanelColors.SectionFill
            };
            _panelToolOutputs.Controls.Add(_sectionPackets);
            _panelToolOutputs.Controls.Add(_sectionPingTrace);

            panelDetail.Controls.Add(_panelToolOutputs);
            panelDetail.Controls.Add(_panelDetailHeader);
            LayoutDetailHeaderTextWidth();
        }

        private static FlowLayoutPanel CreateDetailFlowPanel()
        {
            return new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Margin = Padding.Empty,
                Padding = new Padding(0, 2, 0, 4),
                BackColor = Color.Transparent
            };
        }

        private static Label CreateDetailLabel(bool muted, bool small = false)
        {
            var label = new Label();
            ConfigureDetailLabel(label, muted, small: small);
            return label;
        }

        private static void ConfigureDetailLabel(Label label, bool muted, bool small = false, bool bold = false)
        {
            if (label == null)
            {
                return;
            }

            label.AutoSize = true;
            label.Margin = new Padding(0, 0, 0, 2);
            label.Font = new Font(
                "Segoe UI",
                small ? 8.25F : bold ? 11F : 9F,
                bold ? FontStyle.Bold : FontStyle.Regular);
            label.ForeColor = muted ? PanelColors.TextMuted : PanelColors.TextPrimary;
            label.BackColor = Color.Transparent;
        }

        private void LayoutDetailHeaderTextWidth()
        {
            if (_panelDetailHeader == null)
            {
                return;
            }

            int width = Math.Max(120, _panelDetailHeader.ClientSize.Width);
            if (width <= 0 && panelDetail != null)
            {
                width = Math.Max(120, panelDetail.ClientSize.Width - panelDetail.Padding.Horizontal);
            }

            SetLabelWrapWidth(lblDetailTitle, Math.Max(80, width - 40));
            SetFlowLabelsWidth(_flowDetailProcess, width);
            SetFlowLabelsWidth(_flowDetailConnection, width);
        }

        private static void SetFlowLabelsWidth(FlowLayoutPanel flow, int width)
        {
            if (flow == null)
            {
                return;
            }

            foreach (Control control in flow.Controls)
            {
                SetLabelWrapWidth(control, width);
            }
        }

        private static void SetLabelWrapWidth(Control control, int width)
        {
            if (control is Label label)
            {
                label.MaximumSize = new Size(width, 0);
                label.Width = width;
            }
        }

        private static TextBox CreateToolOutputTextBox()
        {
            return new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                BackColor = PanelColors.SectionFill,
                ForeColor = PanelColors.TextPrimary,
                Font = new Font("Consolas", 9F),
                ScrollBars = ScrollBars.Vertical,
                WordWrap = false,
                HideSelection = false
            };
        }

        private static Panel CreateSectionBody(Control toolbar, Control content)
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = PanelColors.SectionFill
            };
            toolbar.Dock = DockStyle.Top;
            content.Dock = DockStyle.Fill;
            panel.Controls.Add(content);
            panel.Controls.Add(toolbar);
            return panel;
        }

        private static FlowLayoutPanel CreateToolbarPanel(params Control[] controls)
        {
            var bar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 28,
                WrapContents = false,
                AutoSize = false,
                Padding = new Padding(0, 2, 0, 2),
                BackColor = PanelColors.SectionFill
            };
            foreach (Control control in controls)
            {
                bar.Controls.Add(control);
            }

            return bar;
        }

        private Button CreateDetailToolbarButton(string text, EventHandler click)
        {
            var button = new Button
            {
                Text = text,
                AutoSize = true,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(0, 0, 6, 0),
                Padding = new Padding(8, 2, 8, 2),
                BackColor = PanelColors.SectionFill,
                ForeColor = PanelColors.TextPrimary,
                Font = new Font("Segoe UI", 8.25F),
                Cursor = Cursors.Hand
            };
            button.FlatAppearance.BorderColor = PanelColors.CalloutBorder;
            button.Click += click;
            return button;
        }

        private Panel CreateNpcapBannerPanel()
        {
            _lblNpcapBanner = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Top,
                Height = 32,
                Padding = new Padding(0, 0, 0, 4),
                ForeColor = PanelColors.TextMuted,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 8.25F),
                Text = NpcapAvailability.GetBannerText()
            };

            _lnkNpcapDownload = new LinkLabel
            {
                AutoSize = true,
                Text = "Скачать Npcap 1.89",
                LinkBehavior = LinkBehavior.HoverUnderline,
                Margin = new Padding(0, 0, 8, 0),
                Font = new Font("Segoe UI", 8.25F)
            };
            _lnkNpcapDownload.LinkClicked += (s, e) => NpcapInstallHelper.OpenDownloadPage();

            _btnNpcapLocalInstall = CreateDetailToolbarButton("Установить Npcap (локально)", BtnNpcapLocalInstall_Click);
            _btnNpcapRecheck = CreateDetailToolbarButton("Проверить Npcap", BtnNpcapRecheck_Click);

            var actions = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 28,
                WrapContents = false,
                AutoSize = false,
                BackColor = PanelColors.SectionFill
            };
            actions.Controls.Add(_lnkNpcapDownload);
            actions.Controls.Add(_btnNpcapLocalInstall);
            actions.Controls.Add(_btnNpcapRecheck);

            var panel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 62,
                Padding = new Padding(0, 0, 0, 4),
                BackColor = PanelColors.SectionFill,
                Visible = true
            };
            panel.Controls.Add(actions);
            panel.Controls.Add(_lblNpcapBanner);
            return panel;
        }

        private void BtnNpcapLocalInstall_Click(object sender, EventArgs e)
        {
            NpcapInstallHelper.TryLaunchBundledInstaller(this);
        }

        private void BtnNpcapRecheck_Click(object sender, EventArgs e)
        {
            UpdateNpcapBanner();
            if (NpcapAvailability.IsCaptureAvailable())
            {
                string installed = NpcapInstallHelper.FormatInstalledLabelForUi();
                string msg = string.IsNullOrEmpty(installed)
                    ? "Npcap: захват доступен."
                    : installed + ". Захват доступен.";
                MessageBox.Show(
                    this,
                    msg + Environment.NewLine + "Можно нажать «Старт» в панели «Пакеты» для HEX/ASCII.",
                    "Npcap",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            else if (!string.IsNullOrEmpty(NpcapInstallHelper.FormatInstalledLabelForUi()))
            {
                MessageBox.Show(
                    this,
                    NpcapAvailability.GetBannerText(),
                    "Npcap",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private void UpdateNpcapBanner()
        {
            if (_panelNpcapBanner == null)
            {
                return;
            }

            bool ok = NpcapAvailability.IsCaptureAvailable();
            _panelNpcapBanner.Visible = true;
            if (_lblNpcapBanner != null)
            {
                _lblNpcapBanner.Text = NpcapAvailability.GetBannerText();
                _lblNpcapBanner.ForeColor = ok ? PanelColors.ThreatOk : PanelColors.TextMuted;
            }

            if (_lnkNpcapDownload != null)
            {
                _lnkNpcapDownload.Visible = !ok;
            }

            if (_btnNpcapLocalInstall != null)
            {
                _btnNpcapLocalInstall.Visible = NpcapInstallHelper.HasBundledInstaller();
            }

            if (_btnNpcapRecheck != null)
            {
                _btnNpcapRecheck.Visible = true;
            }
        }

        private void MaybeShowNpcapStartupPrompt()
        {
            if (NpcapAvailability.IsCaptureAvailable())
            {
                return;
            }

            if (Settings.Default.ConnectionsNpcapPromptShown)
            {
                return;
            }

            Settings.Default.ConnectionsNpcapPromptShown = true;
            Settings.Default.Save();

            DialogResult answer = MessageBox.Show(
                this,
                "Для просмотра HEX и ASCII в панели «Пакеты» нужен драйвер Npcap (отдельная установка, как для Wireshark)." +
                Environment.NewLine + Environment.NewLine +
                "Скачать и установить Npcap?" + Environment.NewLine + NpcapInstallHelper.DownloadInstallerUrl,
                "Npcap",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);
            if (answer == DialogResult.Yes)
            {
                NpcapInstallHelper.OpenDownloadPage();
            }
        }

        private void SetupGridContextMenu()
        {
            _gridContextMenu = new ContextMenuStrip();
            _menuCloseConnection = new ToolStripMenuItem("Закрыть соединение", null, MenuCloseConnection_Click);
            _menuPing = new ToolStripMenuItem("Пропинговать", null, MenuPing_Click);
            _menuTraceroute = new ToolStripMenuItem("Traceroute", null, MenuTraceroute_Click);
            _menuPackets = new ToolStripMenuItem("Показать пакеты", null, MenuPackets_Click);
            _gridContextMenu.Items.Add(_menuCloseConnection);
            _gridContextMenu.Items.Add(new ToolStripSeparator());
            _gridContextMenu.Items.Add(_menuPing);
            _gridContextMenu.Items.Add(_menuTraceroute);
            _gridContextMenu.Items.Add(_menuPackets);
            _gridContextMenu.Opening += GridContextMenu_Opening;
            grid.ContextMenuStrip = _gridContextMenu;
        }

        private void GridContextMenu_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ConnectionRow row = GetSelectedConnectionRow();
            bool hasRow = row != null;
            bool tcpActive = hasRow && !row.IsUdp && !row.IsListening && row.State != "CLOSED" && !row.IsRecentlyClosed;
            bool hasRemote = hasRow && !row.IsUdp && row.RemoteAddress != "*" && !string.IsNullOrEmpty(row.RemoteAddress);
            _menuCloseConnection.Enabled = tcpActive;
            _menuPing.Enabled = hasRemote;
            _menuTraceroute.Enabled = hasRemote;
            _menuPackets.Enabled = hasRow && !row.IsUdp;
        }

        private ConnectionRow GetSelectedConnectionRow()
        {
            return grid.CurrentRow?.DataBoundItem as ConnectionRow;
        }

        private void MenuCloseConnection_Click(object sender, EventArgs e)
        {
            ConnectionRow row = GetSelectedConnectionRow();
            if (row == null)
            {
                return;
            }

            string error;
            int win32Error;
            if (TcpConnectionCloser.TryClose(row, out error, out win32Error))
            {
                RefreshData(manual: true);
            }
            else if (AdminElevationHelper.IsAccessDenied(win32Error))
            {
                AdminElevationHelper.PromptRestartElevated(
                    this,
                    error ?? "Не удалось закрыть TCP-соединение.");
            }
            else
            {
                MessageBox.Show(this, error ?? "Не удалось закрыть.", "Connections", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnDetailPing_Click(object sender, EventArgs e)
        {
            MenuPing_Click(sender, e);
        }

        private void BtnDetailTraceroute_Click(object sender, EventArgs e)
        {
            MenuTraceroute_Click(sender, e);
        }

        private void BtnPacketsStart_Click(object sender, EventArgs e)
        {
            ConnectionRow row = GetSelectedConnectionRow();
            if (row == null)
            {
                return;
            }

            if (row.IsUdp)
            {
                MessageBox.Show(this, "Мониторинг счётчиков доступен только для TCP.", "Пакеты", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string connectionKey = ConnectionRowKey.Get(row);
            _packetWiresharkView.SetRows(_packetRowStore.Get(connectionKey));
            _sectionPackets.Expanded = true;
            UpdateNpcapBanner();
            int nextNumber = _packetRowStore.GetRowCount(connectionKey);
            _packetMonitor.Start(row, OnPacketCaptured, nextNumber);
            UpdatePacketSectionTitle(row);
        }

        private void BtnPacketsStop_Click(object sender, EventArgs e)
        {
            _packetMonitor.Stop();
            UpdatePacketSectionTitle(GetSelectedConnectionRow());
        }

        private void OnPacketCaptured(PacketListRow packetRow)
        {
            string connectionKey = _packetMonitor.ConnectionKey;
            if (string.IsNullOrEmpty(connectionKey) || packetRow == null)
            {
                return;
            }

            _packetRowStore.Add(connectionKey, packetRow);
            ConnectionRow current = GetSelectedConnectionRow();
            if (current != null
                && string.Equals(ConnectionRowKey.Get(current), connectionKey, StringComparison.Ordinal))
            {
                _packetWiresharkView.AddRow(packetRow);
            }
        }

        private void StopPacketMonitorIfConnectionInactive(HashSet<string> activeConnectionKeys)
        {
            if (!_packetMonitor.IsRunning)
            {
                return;
            }

            string monitoredKey = _packetMonitor.ConnectionKey;
            if (string.IsNullOrEmpty(monitoredKey)
                || activeConnectionKeys == null
                || !activeConnectionKeys.Contains(monitoredKey))
            {
                _packetMonitor.Stop();
                if (IsHandleCreated && !IsDisposed)
                {
                    UpdatePacketSectionTitle(GetSelectedConnectionRow());
                }
            }
        }

        private void MenuPing_Click(object sender, EventArgs e)
        {
            _sectionPingTrace.Expanded = true;
            RunConnectionToolAsync("Ping", ConnectionToolRunner.PingAsync);
        }

        private void MenuTraceroute_Click(object sender, EventArgs e)
        {
            _sectionPingTrace.Expanded = true;
            ConnectionRow row = GetSelectedConnectionRow();
            if (row == null || System.Threading.Interlocked.CompareExchange(ref _toolRunBusy, 1, 0) != 0)
            {
                return;
            }

            string key = ConnectionRowKey.Get(row);
            BeginPingTraceBlock(key, "Traceroute");
            Task.Run(async () =>
            {
                await ConnectionToolRunner.TracerouteAsync(row, line => PostPingTraceLine(key, line));
            }).ContinueWith(t =>
                {
                    System.Threading.Interlocked.Exchange(ref _toolRunBusy, 0);
                    if (!t.IsFaulted || IsDisposed || !IsHandleCreated)
                    {
                        return;
                    }

                    string message = t.Exception.GetBaseException().Message;
                    PostPingTraceLine(key, "Ошибка: " + message);
                });
        }

        private void MenuPackets_Click(object sender, EventArgs e)
        {
            BtnPacketsStart_Click(sender, e);
            grid.Invalidate();
        }

        private void BeginPingTraceBlock(string key, string title)
        {
            string existing = _pingTraceSessions.Get(key);
            string stamp = "[" + DateTime.Now.ToString("HH:mm:ss") + "] " + title;
            string text = string.IsNullOrEmpty(existing)
                ? stamp + Environment.NewLine
                : existing.TrimEnd() + Environment.NewLine + Environment.NewLine + stamp + Environment.NewLine;
            _pingTraceSessions.Set(key, text);
            ShowPingTraceText(text);
        }

        private void PostPingTraceLine(string key, string line)
        {
            if (IsDisposed || !IsHandleCreated || string.IsNullOrEmpty(line))
            {
                return;
            }

            try
            {
                BeginInvoke(new Action(() =>
                {
                    if (IsDisposed)
                    {
                        return;
                    }

                    _pingTraceSessions.AppendPlainLine(key, line);
                    ConnectionRow current = GetSelectedConnectionRow();
                    if (current != null && ConnectionRowKey.Get(current) == key)
                    {
                        ShowPingTraceText(_pingTraceSessions.Get(key));
                    }
                }));
            }
            catch (InvalidOperationException)
            {
            }
        }

        private void ShowPingTraceText(string text)
        {
            if (_txtPingTrace == null)
            {
                return;
            }

            _txtPingTrace.Text = text ?? string.Empty;
            _txtPingTrace.SelectionStart = _txtPingTrace.TextLength;
            _txtPingTrace.ScrollToCaret();
        }

        private void RunConnectionToolAsync(string title, Func<ConnectionRow, Task<string>> work)
        {
            ConnectionRow row = GetSelectedConnectionRow();
            if (row == null || System.Threading.Interlocked.CompareExchange(ref _toolRunBusy, 1, 0) != 0)
            {
                return;
            }

            string key = ConnectionRowKey.Get(row);
            _txtPingTrace.Text = title + "…";
            Task.Run(() => work(row)).ContinueWith(t =>
            {
                System.Threading.Interlocked.Exchange(ref _toolRunBusy, 0);
                if (IsDisposed || !IsHandleCreated)
                {
                    return;
                }

                string body = t.IsFaulted
                    ? "Ошибка: " + (t.Exception?.GetBaseException().Message ?? "?")
                    : t.Result;
                BeginInvoke(new Action(() =>
                {
                    _pingTraceSessions.Append(key, title, body);
                    ConnectionRow current = GetSelectedConnectionRow();
                    if (current != null && ConnectionRowKey.Get(current) == key)
                    {
                        ShowToolOutputForRow(current);
                    }
                }));
            });
        }

        private void ShowToolOutputForRow(ConnectionRow row)
        {
            if (row == null || _txtPingTrace == null || _packetWiresharkView == null)
            {
                return;
            }

            string connectionKey = ConnectionRowKey.Get(row);
            ShowPingTraceText(_pingTraceSessions.Get(connectionKey));
            UpdatePacketSectionTitle(row);
            _packetWiresharkView.SetRows(_packetRowStore.Get(connectionKey));
        }

        private void UpdatePacketSectionTitle(ConnectionRow row)
        {
            if (_sectionPackets == null)
            {
                return;
            }

            if (row == null)
            {
                if (_packetMonitor.IsRunning)
                {
                    _sectionPackets.SetTitle("Пакеты — запись в фоне");
                }
                else
                {
                    _sectionPackets.SetTitle("Пакеты");
                }

                return;
            }

            string connectionKey = ConnectionRowKey.Get(row);
            bool recordingHere = _packetMonitor.IsRunning
                && string.Equals(_packetMonitor.ConnectionKey, connectionKey, StringComparison.Ordinal);
            string label = row.Application;
            if (!row.IsUdp && !string.IsNullOrEmpty(row.RemoteEndpoint) && row.RemoteEndpoint != "*:*")
            {
                label = label + " → " + row.RemoteEndpoint;
            }

            if (recordingHere)
            {
                _sectionPackets.SetTitle("Пакеты — " + label + " (запись)");
            }
            else if (_packetMonitor.IsRunning)
            {
                _sectionPackets.SetTitle("Пакеты — " + label + " (запись в другом соединении)");
            }
            else
            {
                _sectionPackets.SetTitle("Пакеты — " + label);
            }
        }

        private void PruneToolSessions(IList<ConnectionRow> liveRows, IList<ConnectionRow> ghostRows)
        {
            var activeConnections = new HashSet<string>(StringComparer.Ordinal);
            if (liveRows != null)
            {
                foreach (ConnectionRow row in liveRows)
                {
                    string key = ConnectionRowKey.Get(row);
                    if (key != null)
                    {
                        activeConnections.Add(key);
                    }
                }
            }

            if (ghostRows != null)
            {
                foreach (ConnectionRow row in ghostRows)
                {
                    string key = ConnectionRowKey.Get(row);
                    if (key != null)
                    {
                        activeConnections.Add(key);
                    }
                }
            }

            StopPacketMonitorIfConnectionInactive(activeConnections);
            _pingTraceSessions.PruneInactive(activeConnections);
            _packetRowStore.PruneInactive(activeConnections);
        }

        private void QueueBackgroundEnrich(List<ConnectionRow> displayRows)
        {
            if (displayRows == null || displayRows.Count == 0)
            {
                return;
            }

            int generation = System.Threading.Interlocked.Increment(ref _backgroundEnrichGeneration);
            List<ConnectionRow> batch = displayRows;
            Task.Run(() =>
            {
                ConnectionsEnricher.EnrichProcessNames(batch, 150, false, 0);
                TcpConnectionStatsEnricher.Enrich(batch);
                return generation;
            }).ContinueWith(t =>
            {
                if (t.IsFaulted || t.Result != generation || IsDisposed || !IsHandleCreated)
                {
                    return;
                }

                BeginInvoke(new Action(() =>
                {
                    if (generation != _backgroundEnrichGeneration)
                    {
                        return;
                    }

                    grid.InvalidateColumn(1);
                    int statsCol = grid.Columns["SentBytesDisplay"]?.Index ?? -1;
                    if (statsCol >= 0)
                    {
                        grid.InvalidateColumn(statsCol);
                    }

                    QueueIconLoadAsync(batch);
                }));
            });
        }

        private static void ApplyDetailToolbarTheme(Button button)
        {
            if (button == null)
            {
                return;
            }

            button.BackColor = PanelColors.SectionFill;
            button.ForeColor = PanelColors.TextPrimary;
            button.FlatAppearance.BorderColor = PanelColors.CalloutBorder;
        }

        private void ApplyTheme()
        {
            BackColor = PanelColors.Background;
            panelTop.BackColor = PanelColors.SectionFill;
            panelTabs.BackColor = PanelColors.Background;
            panelDetail.BackColor = PanelColors.SectionFill;
            lblTitle.ForeColor = PanelColors.TextPrimary;
            ConfigureDetailLabel(lblDetailTitle, muted: false, bold: true);
            ConfigureDetailLabel(lblDetailMeta, muted: true);
            ConfigureDetailLabel(lblDetailPid, muted: true);
            ConfigureDetailLabel(lblDetailUser, muted: true);
            ConfigureDetailLabel(lblDetailLocal, muted: true);
            ConfigureDetailLabel(lblDetailRemote, muted: true);
            if (_lblDetailDescription != null)
            {
                ConfigureDetailLabel(_lblDetailDescription, muted: false);
            }

            if (_lblDetailCompany != null)
            {
                ConfigureDetailLabel(_lblDetailCompany, muted: true);
            }

            if (_lblDetailVersion != null)
            {
                ConfigureDetailLabel(_lblDetailVersion, muted: true);
            }

            if (_lblDetailPath != null)
            {
                ConfigureDetailLabel(_lblDetailPath, muted: true, small: true);
            }

            if (_panelDetailHeader != null)
            {
                _panelDetailHeader.BackColor = PanelColors.SectionFill;
            }

            if (_txtPingTrace != null)
            {
                _txtPingTrace.BackColor = PanelColors.SectionFill;
                _txtPingTrace.ForeColor = PanelColors.TextPrimary;
            }

            _packetWiresharkView?.ApplyTheme();

            ApplyDetailToolbarTheme(_btnDetailPing);
            ApplyDetailToolbarTheme(_btnDetailTraceroute);
            ApplyDetailToolbarTheme(_btnPacketsStart);
            ApplyDetailToolbarTheme(_btnPacketsStop);
            ApplyDetailToolbarTheme(_btnNpcapLocalInstall);
            ApplyDetailToolbarTheme(_btnNpcapRecheck);
            if (_lblNpcapBanner != null)
            {
                _lblNpcapBanner.ForeColor = PanelColors.TextMuted;
            }

            if (_lnkNpcapDownload != null)
            {
                _lnkNpcapDownload.LinkColor = PanelColors.NetAccent;
                _lnkNpcapDownload.ActiveLinkColor = PanelColors.NetAccent;
                _lnkNpcapDownload.VisitedLinkColor = PanelColors.NetAccent;
                _lnkNpcapDownload.BackColor = PanelColors.SectionFill;
            }

            if (_panelNpcapBanner != null)
            {
                _panelNpcapBanner.BackColor = PanelColors.SectionFill;
            }

            _sectionPingTrace?.ApplyTheme();
            _sectionPackets?.ApplyTheme();

            lblRefresh.ForeColor = PanelColors.TextMuted;
            cmbRefresh.BackColor = PanelColors.SectionFill;
            cmbRefresh.ForeColor = PanelColors.TextPrimary;
            cmbRefresh.FlatStyle = FlatStyle.Flat;
            btnRefreshNow.BackColor = PanelColors.SectionFill;
            btnRefreshNow.ForeColor = PanelColors.TextPrimary;
            btnRefreshNow.FlatStyle = FlatStyle.Flat;

            grid.BackgroundColor = PanelColors.Background;
            grid.GridColor = PanelColors.CalloutBorder;
            grid.BorderStyle = BorderStyle.None;
            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersDefaultCellStyle.BackColor = PanelColors.SectionFill;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = PanelColors.TextPrimary;
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = PanelColors.SectionFill;
            grid.DefaultCellStyle.BackColor = PanelColors.Background;
            grid.DefaultCellStyle.ForeColor = PanelColors.TextPrimary;
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(45, 75, 95);
            grid.DefaultCellStyle.SelectionForeColor = PanelColors.TextPrimary;
            grid.RowHeadersVisible = false;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.ReadOnly = true;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;
            typeof(DataGridView).InvokeMember(
                "DoubleBuffered",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.SetProperty,
                null,
                grid,
                new object[] { true });
        }

        private void RefreshData(bool manual)
        {
            if (IsDisposed || !IsHandleCreated)
            {
                return;
            }

            if (!manual && _autoRefreshIntervalSeconds <= 0)
            {
                _refreshTimer.Stop();
                return;
            }

            if (System.Threading.Interlocked.CompareExchange(ref _refreshBusy, 1, 0) != 0)
            {
                return;
            }

            CaptureSelectionPreserveState();
            btnRefreshNow.Enabled = false;

            ConnectionFilter filter = _filter;
            Task.Run(() => ConnectionsGridHelper.Build(filter)).ContinueWith(t =>
            {
                System.Threading.Interlocked.Exchange(ref _refreshBusy, 0);
                if (IsDisposed || !IsHandleCreated)
                {
                    return;
                }

                BeginInvoke(new Action(() => ApplyRefreshResult(t)));
            });
        }

        private void ApplyRefreshResult(Task<ConnectionsGridHelper.RefreshPayload> task)
        {
            btnRefreshNow.Enabled = true;

            if (task.IsFaulted)
            {
                Exception ex = task.Exception?.GetBaseException();
                lblTitle.Text = "Connections — ошибка: " + (ex != null ? ex.Message : "неизвестно");
                return;
            }

            if (task.IsCanceled || task.Result == null)
            {
                return;
            }

            ConnectionsGridHelper.RefreshPayload payload = task.Result;
            _allRows = payload.AllRows ?? new List<ConnectionRow>();
            List<ConnectionRow> vanished = _vanishTracker.BuildGhosts(_allRows);
            List<ConnectionRow> liveFiltered = ConnectionsGridHelper.FilterDisplayList(
                _allRows,
                _filter,
                out _filteredTotalCount);
            List<ConnectionRow> vanishedForFilter = vanished.FindAll(r => ConnectionsGridHelper.MatchesFilter(r, _filter));
            List<ConnectionRow> display = ConnectionsGridHelper.MergeVanishedOnTop(vanishedForFilter, liveFiltered);
            UpdateStats();
            ApplyIconsToImageList(payload.ExtraIcons);
            PruneToolSessions(_allRows, vanished);
            BindDisplayRows(display);
            QueueBackgroundEnrich(display);
        }

        private void UpdateStats()
        {
            int listening = _allRows.Count(r => r.IsListening && !r.IsUdp);
            int established = _allRows.Count(r => r.State == "ESTABLISHED");
            int processes = _allRows.Select(r => r.ProcessId).Distinct().Count();
            int publicCount = _allRows.Count(r => !r.IsLoopback && r.State == "ESTABLISHED");

            statConnections.Value = established.ToString();
            statListening.Value = listening.ToString();
            statPublic.Value = publicCount.ToString();
            statProcesses.Value = processes.ToString();
            if (_filteredTotalCount > ConnectionsGridHelper.MaxVisibleRows)
            {
                lblTitle.Text = string.Format(
                    "Connections ({0} из {1})",
                    ConnectionsGridHelper.MaxVisibleRows,
                    _filteredTotalCount);
            }
            else
            {
                lblTitle.Text = "Connections";
            }
        }

        private void ApplyFilter()
        {
            List<ConnectionRow> list = ConnectionsGridHelper.FilterDisplayList(_allRows, _filter, out _filteredTotalCount);
            UpdateStats();
            BindDisplayRows(list);
            QueueBackgroundEnrich(list);
        }

        private void CaptureSelectionPreserveState()
        {
            ConnectionRow selected = GetSelectedConnectionRow();
            _preserveSelectionKey = _lastSelectionKey ?? GetSelectionKey();
            _preserveSelectionProcessId = selected != null ? selected.ProcessId : 0;
            _preserveSelectionApplication = selected?.Application;
            _preserveScrollIndex = _lastScrollIndex >= 0 ? _lastScrollIndex : grid.FirstDisplayedScrollingRowIndex;
        }

        private void BindDisplayRows(List<ConnectionRow> list)
        {
            string key = _preserveSelectionKey ?? _lastSelectionKey;
            int scrollIndex = _preserveScrollIndex >= 0 ? _preserveScrollIndex : _lastScrollIndex;
            int preserveProcessId = _preserveSelectionProcessId;
            string preserveApplication = _preserveSelectionApplication;
            _preserveSelectionKey = null;
            _preserveScrollIndex = -1;
            _preserveSelectionProcessId = 0;
            _preserveSelectionApplication = null;

            grid.SuspendLayout();
            _suppressGridSelectionEvents = true;
            _restorePreserveScroll = true;
            try
            {
                grid.DataSource = list;
                bool restored = TryRestoreSelection(key);
                if (!restored)
                {
                    restored = TryRestoreSelectionByEndpoint(key);
                }

                if (!restored && preserveProcessId > 0)
                {
                    restored = TryRestoreSelectionByProcess(preserveProcessId, preserveApplication);
                }

                if (scrollIndex >= 0 && grid.Rows.Count > 0)
                {
                    int idx = Math.Min(scrollIndex, grid.Rows.Count - 1);
                    grid.FirstDisplayedScrollingRowIndex = idx;
                }

                if (!_initialGridFocusDone && grid.Rows.Count > 0 && grid.SelectedRows.Count == 0)
                {
                    grid.Rows[0].Selected = true;
                    grid.CurrentCell = grid.Rows[0].Cells[1];
                    _initialGridFocusDone = true;
                }

                if (grid.CurrentRow != null)
                {
                    _lastSelectionKey = GetSelectionKey();
                }
            }
            finally
            {
                _restorePreserveScroll = false;
                grid.ResumeLayout();
                _suppressGridSelectionEvents = false;
            }

            UpdateDetailPanel();
        }

        private void ApplyIconsToImageList(List<Image> extraIcons)
        {
            _processIcons.Images.Clear();
            _processIcons.Images.Add(_blankIcon);
            if (extraIcons != null)
            {
                foreach (Image image in extraIcons)
                {
                    _processIcons.Images.Add(image);
                }
            }
        }

        private void QueueIconLoadAsync(List<ConnectionRow> rows)
        {
            if (rows == null || rows.Count == 0)
            {
                return;
            }

            if (System.Threading.Interlocked.CompareExchange(ref _iconLoadBusy, 1, 0) != 0)
            {
                return;
            }

            int generation = ++_iconLoadGeneration;
            Task.Run(() =>
            {
                var icons = new List<Image>();
                ConnectionsGridHelper.AssignIconIndices(rows, icons);
                return icons;
            }).ContinueWith(t =>
            {
                System.Threading.Interlocked.Exchange(ref _iconLoadBusy, 0);
                if (IsDisposed || !IsHandleCreated || generation != _iconLoadGeneration)
                {
                    return;
                }

                BeginInvoke(new Action(() =>
                {
                    if (generation != _iconLoadGeneration)
                    {
                        return;
                    }

                    ApplyIconsToImageList(t.Result);
                    grid.InvalidateColumn(0);
                }));
            });
        }

        private void Grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0)
            {
                return;
            }

            ConnectionRow row = grid.Rows[e.RowIndex].DataBoundItem as ConnectionRow;
            if (row == null)
            {
                return;
            }

            TcpConnectionRowStyle.ApplyCellStyle(e, grid, row);

            if (e.ColumnIndex != 0)
            {
                return;
            }

            int index = row.ApplicationIconIndex;
            if (index > 0 && index < _processIcons.Images.Count)
            {
                e.Value = _processIcons.Images[index];
            }
            else
            {
                e.Value = _processIcons.Images[0];
            }

            e.FormattingApplied = true;
        }

        private string GetSelectionKey()
        {
            return ConnectionRowKey.Get(grid.CurrentRow?.DataBoundItem as ConnectionRow);
        }

        private bool TryRestoreSelection(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }

            foreach (DataGridViewRow gridRow in grid.Rows)
            {
                if (ConnectionRowKey.Get(gridRow.DataBoundItem as ConnectionRow) == key)
                {
                    SelectGridRow(gridRow);
                    return true;
                }
            }

            return false;
        }

        private bool TryRestoreSelectionByEndpoint(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }

            string[] parts = key.Split('|');
            if (parts.Length < 3)
            {
                return false;
            }

            string protocol = parts[1];
            string local = parts[2];
            string remote = parts.Length > 3 ? parts[3] : string.Empty;

            foreach (DataGridViewRow gridRow in grid.Rows)
            {
                ConnectionRow row = gridRow.DataBoundItem as ConnectionRow;
                if (row == null)
                {
                    continue;
                }

                if (row.Protocol == protocol && row.LocalEndpoint == local && row.RemoteEndpoint == remote)
                {
                    SelectGridRow(gridRow);
                    return true;
                }
            }

            return false;
        }

        private bool TryRestoreSelectionByProcess(int processId, string application)
        {
            if (processId <= 0)
            {
                return false;
            }

            DataGridViewRow established = null;
            DataGridViewRow sameApplication = null;
            DataGridViewRow anyLive = null;

            foreach (DataGridViewRow gridRow in grid.Rows)
            {
                ConnectionRow row = gridRow.DataBoundItem as ConnectionRow;
                if (row == null || row.ProcessId != processId || row.IsRecentlyClosed)
                {
                    continue;
                }

                anyLive = gridRow;
                bool appMatch = !string.IsNullOrEmpty(application)
                    && string.Equals(row.Application, application, StringComparison.OrdinalIgnoreCase);
                if (appMatch)
                {
                    sameApplication = gridRow;
                    if (row.State == "ESTABLISHED")
                    {
                        established = gridRow;
                        break;
                    }
                }
                else if (row.State == "ESTABLISHED" && established == null)
                {
                    established = gridRow;
                }
            }

            DataGridViewRow pick = established ?? sameApplication ?? anyLive;
            if (pick == null)
            {
                return false;
            }

            SelectGridRow(pick);
            return true;
        }

        private void SelectGridRow(DataGridViewRow gridRow)
        {
            grid.ClearSelection();
            gridRow.Selected = true;
            int textColumn = grid.Columns.Count > 1 ? 1 : 0;
            grid.CurrentCell = gridRow.Cells[textColumn];
            if (_restorePreserveScroll)
            {
                return;
            }

            if (gridRow.Index >= 0)
            {
                int keep = Math.Max(0, gridRow.Index - 2);
                if (grid.FirstDisplayedScrollingRowIndex != keep)
                {
                    grid.FirstDisplayedScrollingRowIndex = keep;
                }
            }
        }

        private void UpdateDetailPanel()
        {
            ConnectionRow row = grid.CurrentRow?.DataBoundItem as ConnectionRow;
            if (row == null)
            {
                _lastDetailProcessId = -1;
                _lastDetailApplication = null;
                lblDetailTitle.Text = "—";
                lblDetailMeta.Text = "";
                lblDetailPid.Text = "";
                lblDetailUser.Text = "";
                lblDetailLocal.Text = "";
                lblDetailRemote.Text = "";
                ClearProcessDetailFields();
                if (_txtPingTrace != null)
                {
                    _txtPingTrace.Text = string.Empty;
                }

                _packetWiresharkView?.Clear();
                UpdatePacketSectionTitle(null);

                return;
            }

            lblDetailTitle.Text = row.Application;
            lblDetailMeta.Text = row.Protocol + " • " + row.State;
            lblDetailPid.Text = "PID  " + row.ProcessId;
            lblDetailUser.Text = "User  " + row.User;
            lblDetailLocal.Text = "Local  " + row.LocalAddress + ":" + row.LocalPort;
            lblDetailRemote.Text = row.IsUdp ? "" : "Remote  " + row.RemoteAddress + ":" + row.RemotePort;
            ShowToolOutputForRow(row);

            bool sameProcess = row.ProcessId > 0
                && row.ProcessId == _lastDetailProcessId
                && string.Equals(row.Application, _lastDetailApplication, StringComparison.OrdinalIgnoreCase);
            if (!sameProcess)
            {
                _lastDetailProcessId = row.ProcessId;
                _lastDetailApplication = row.Application;
                QueueDetailProcessMetadata(row);
            }

            if (row.ProcessId > 0 && row.User == "—")
            {
                Task.Run(() =>
                {
                    ConnectionsEnricher.EnrichSingle(row);
                    return row.User;
                }).ContinueWith(
                    t =>
                    {
                        if (IsDisposed || !IsHandleCreated || t.IsFaulted)
                        {
                            return;
                        }

                        BeginInvoke(new Action(() =>
                        {
                            if (grid.CurrentRow?.DataBoundItem == row)
                            {
                                lblDetailUser.Text = "User  " + row.User;
                            }
                        }));
                    },
                    TaskScheduler.FromCurrentSynchronizationContext());
            }
        }

        private void ClearProcessDetailFields()
        {
            if (_picDetailIcon != null)
            {
                _picDetailIcon.Image = null;
            }

            if (_lblDetailDescription != null)
            {
                _lblDetailDescription.Text = string.Empty;
            }

            if (_lblDetailCompany != null)
            {
                _lblDetailCompany.Text = string.Empty;
            }

            if (_lblDetailVersion != null)
            {
                _lblDetailVersion.Text = string.Empty;
            }

            if (_lblDetailPath != null)
            {
                _lblDetailPath.Text = string.Empty;
            }
        }

        private void QueueDetailProcessMetadata(ConnectionRow row)
        {
            if (row == null || row.ProcessId <= 0)
            {
                ClearProcessDetailFields();
                return;
            }

            int generation = System.Threading.Interlocked.Increment(ref _detailMetadataGeneration);
            string pathHint = row.ExecutablePath;
            int pid = row.ProcessId;
            string app = row.Application;

            _lblDetailDescription.Text = "…";
            _lblDetailCompany.Text = string.Empty;
            _lblDetailVersion.Text = string.Empty;
            _lblDetailPath.Text = string.Empty;
            _picDetailIcon.Image = null;

            Task.Run(() => ProcessFileMetadataProvider.Get(pid, pathHint)).ContinueWith(t =>
            {
                if (t.IsFaulted || IsDisposed || !IsHandleCreated)
                {
                    return;
                }

                ProcessFileMetadata meta = t.Result;
                BeginInvoke(new Action(() =>
                {
                    if (generation != _detailMetadataGeneration)
                    {
                        return;
                    }

                    ConnectionRow current = GetSelectedConnectionRow();
                    if (current == null || current.ProcessId != pid)
                    {
                        return;
                    }

                    ApplyProcessMetadata(meta, app);
                }));
            });
        }

        private void ApplyProcessMetadata(ProcessFileMetadata meta, string applicationName)
        {
            if (meta == null)
            {
                ClearProcessDetailFields();
                return;
            }

            string description = meta.FileDescription;
            if (string.IsNullOrEmpty(description))
            {
                description = applicationName ?? "—";
            }

            _lblDetailDescription.Text = description;
            _lblDetailCompany.Text = string.IsNullOrEmpty(meta.CompanyName) ? string.Empty : meta.CompanyName;
            string fileVersion = meta.FileVersion;
            if (!string.IsNullOrEmpty(fileVersion))
            {
                fileVersion = fileVersion.Replace("\r\n", " ").Replace('\n', ' ').Replace('\r', ' ').Trim();
            }

            _lblDetailVersion.Text = string.IsNullOrEmpty(fileVersion)
                ? string.Empty
                : "Version: " + fileVersion;
            _lblDetailPath.Text = string.IsNullOrEmpty(meta.ExecutablePath)
                ? string.Empty
                : "Path: " + meta.ExecutablePath;

            LayoutDetailHeaderTextWidth();

            Image icon = ProcessIconCache.GetIconForPath(meta.ExecutablePath);
            if (icon == null && !string.IsNullOrEmpty(applicationName))
            {
                string guess = ProcessIconCache.GuessPathFromProcessName(applicationName);
                icon = ProcessIconCache.GetIconForPath(guess);
            }

            _picDetailIcon.Image = icon;
        }
    }
}
