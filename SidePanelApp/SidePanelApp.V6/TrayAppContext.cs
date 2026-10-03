using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using SidePanelApp;
using SidePanelApp.V6.Properties;
using SidePanelApp.V6.Connections;
using SidePanelApp.V6.Infrastructure;
using SidePanelApp.V6.Security;

namespace SidePanelApp.V6
{
    /// <summary>
    /// Приложение в системном трее. ЛКМ — показать/скрыть панель, ПКМ — меню.
    /// </summary>
    public sealed class TrayAppContext : ApplicationContext
    {
        private readonly NotifyIcon _trayIcon;
        private readonly ContextMenuStrip _trayMenu;
        private readonly ToolStripMenuItem _showPanelItem;
        private readonly ToolStripMenuItem _runAtLoginItem;
        private readonly ToolStripMenuItem _autoStartItem;
        private readonly ToolStripMenuItem _allowMoveItem;
        private OverlayPanelV6 _overlayPanel;
        private readonly ThreatBackgroundService _threatService;

        public TrayAppContext()
        {
            _threatService = new ThreatBackgroundService();
            _threatService.Start();
            _showPanelItem = new ToolStripMenuItem("Показать панель сейчас") { CheckOnClick = true };
            _runAtLoginItem = new ToolStripMenuItem("Запускать при входе в Windows") { CheckOnClick = true };
            _autoStartItem = new ToolStripMenuItem("Показывать панель при запуске") { CheckOnClick = true };
            _allowMoveItem = new ToolStripMenuItem("Разрешить перемещение панели") { CheckOnClick = true };

            _trayMenu = new ContextMenuStrip
            {
                RenderMode = ToolStripRenderMode.System,
                // Колонка слева нужна для галочек у пунктов с CheckOnClick.
                ShowCheckMargin = true,
                ShowImageMargin = true
            };
            _trayMenu.Opening += TrayMenu_Opening;
            _trayMenu.Items.Add(_showPanelItem);
            _trayMenu.Items.Add(_runAtLoginItem);
            _trayMenu.Items.Add(_autoStartItem);
            _trayMenu.Items.Add(_allowMoveItem);
            _trayMenu.Items.Add(new ToolStripSeparator());
            _trayMenu.Items.Add("Соединения (TCP/UDP)…", null, OnConnectionsClick);
            _trayMenu.Items.Add(new ToolStripSeparator());
            _trayMenu.Items.Add("Выход", null, OnExit);

            _trayIcon = new NotifyIcon
            {
                Icon = AppIconFactory.GetApplicationIcon(16),
                Text = "SidePanelApp V6",
                Visible = true
            };
            _trayIcon.MouseUp += TrayIcon_MouseUp;

            _showPanelItem.CheckedChanged += (s, e) => OnShowPanelChanged();
            _runAtLoginItem.CheckedChanged += (s, e) => OnRunAtLoginChanged();
            _autoStartItem.CheckedChanged += (s, e) => OnAutoStartChanged();
            _allowMoveItem.CheckedChanged += (s, e) => OnAllowMoveChanged();

            LoadSettings();

            if (_showPanelItem.Checked)
            {
                ScheduleInitialOverlayPanel();
            }
        }

        private void TrayIcon_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                _trayMenu.Show(Cursor.Position);
                return;
            }

            if (e.Button == MouseButtons.Left)
            {
                TogglePanel();
            }
        }

        /// <summary>Перед показом меню — актуальные галочки без тяжёлого COM (только настройки).</summary>
        private void TrayMenu_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _runAtLoginItem.Checked = Settings.Default.RunAtWindowsStartup;
            _autoStartItem.Checked = Settings.Default.AutoStartPanel;
            bool panelVisible = _overlayPanel != null && !_overlayPanel.IsDisposed && _overlayPanel.Visible;
            _showPanelItem.Checked = Settings.Default.ShowPanel || panelVisible;
            _allowMoveItem.Checked = Settings.Default.AllowPanelMove;
        }

        private void ScheduleInitialOverlayPanel()
        {
            EventHandler idleHandler = null;
            idleHandler = (s, e) =>
            {
                Application.Idle -= idleHandler;
                ShowOverlayPanel();
            };
            Application.Idle += idleHandler;
        }

        private void LoadSettings()
        {
            ApplyFirstRunStartupDefaults();

            _runAtLoginItem.Checked = Settings.Default.RunAtWindowsStartup;
            bool runAtStartup = Settings.Default.RunAtWindowsStartup;
            Task.Run(() => UserStartupHelper.SyncFromSettings(runAtStartup));

            _autoStartItem.Checked = Settings.Default.AutoStartPanel;
            _showPanelItem.Checked = Settings.Default.ShowPanel || Settings.Default.AutoStartPanel;
            _allowMoveItem.Checked = Settings.Default.AllowPanelMove;
        }

        /// <summary>
        /// Первый запуск V6: включить автозагрузку Windows и показ панели (отключить можно в меню трея).
        /// </summary>
        private static void ApplyFirstRunStartupDefaults()
        {
            if (Settings.Default.V6StartupConfigured)
            {
                return;
            }

            Settings.Default.V6StartupConfigured = true;
            Settings.Default.RunAtWindowsStartup = true;
            Settings.Default.AutoStartPanel = true;
            Settings.Default.ShowPanel = true;
            Settings.Default.Save();
        }

        private void OnRunAtLoginChanged()
        {
            Settings.Default.RunAtWindowsStartup = _runAtLoginItem.Checked;
            Settings.Default.Save();
            try
            {
                UserStartupHelper.Apply(_runAtLoginItem.Checked);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Не удалось изменить автозагрузку: " + ex.Message,
                    "SidePanelApp V6",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                _runAtLoginItem.Checked = UserStartupHelper.IsRegistered();
                Settings.Default.RunAtWindowsStartup = _runAtLoginItem.Checked;
                Settings.Default.Save();
            }
        }

        private void OnShowPanelChanged()
        {
            if (_showPanelItem.Checked)
            {
                ShowOverlayPanel();
            }
            else
            {
                HideOverlayPanel();
            }

            Settings.Default.ShowPanel = _showPanelItem.Checked;
            Settings.Default.Save();
        }

        private void OnAutoStartChanged()
        {
            Settings.Default.AutoStartPanel = _autoStartItem.Checked;
            Settings.Default.Save();

            if (_autoStartItem.Checked && !_showPanelItem.Checked)
            {
                _showPanelItem.Checked = true;
            }
        }

        private void OnAllowMoveChanged()
        {
            Settings.Default.AllowPanelMove = _allowMoveItem.Checked;
            Settings.Default.Save();
            ApplyAllowMove();
        }

        private void TogglePanel()
        {
            _showPanelItem.Checked = !_showPanelItem.Checked;
        }

        private void OnConnectionsClick(object sender, EventArgs e)
        {
            ConnectionsWindowHost.ShowOrActivate(null);
        }

        private void ApplyAllowMove()
        {
            if (_overlayPanel != null && !_overlayPanel.IsDisposed)
            {
                _overlayPanel.AllowMove = _allowMoveItem.Checked;
            }
        }

        private void ShowOverlayPanel()
        {
            if (_overlayPanel == null || _overlayPanel.IsDisposed)
            {
                _overlayPanel = new OverlayPanelV6(_threatService);
                _overlayPanel.AllowMove = _allowMoveItem.Checked;
                _overlayPanel.FormClosed += (s, e) =>
                {
                    if (_showPanelItem.Checked)
                    {
                        _showPanelItem.Checked = false;
                    }
                };
            }

            if (!_overlayPanel.Visible)
            {
                _overlayPanel.Show();
            }
        }

        private void HideOverlayPanel()
        {
            if (_overlayPanel != null && !_overlayPanel.IsDisposed)
            {
                _overlayPanel.Hide();
            }
        }

        private void OnExit(object sender, EventArgs e)
        {
            HideOverlayPanel();

            if (_overlayPanel != null && !_overlayPanel.IsDisposed)
            {
                _overlayPanel.Close();
            }

            _threatService.Dispose();

            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            ExitThread();
        }
    }
}
