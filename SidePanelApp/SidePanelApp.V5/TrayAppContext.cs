using System;
using System.Windows.Forms;
using SidePanelApp;
using SidePanelApp.V5.Properties;

namespace SidePanelApp.V5
{
    /// <summary>
    /// Приложение в системном трее. Настройки — ПКМ по иконке в трее.
    /// </summary>
    public sealed class TrayAppContext : ApplicationContext
    {
        private readonly NotifyIcon _trayIcon;
        private readonly ToolStripMenuItem _showPanelItem;
        private readonly ToolStripMenuItem _autoStartItem;
        private readonly ToolStripMenuItem _allowMoveItem;
        private OverlayPanelV5 _overlayPanel;

        public TrayAppContext()
        {
            _showPanelItem = new ToolStripMenuItem("Показать панель сейчас") { CheckOnClick = true };
            _autoStartItem = new ToolStripMenuItem("Запускать панель при старте") { CheckOnClick = true };
            _allowMoveItem = new ToolStripMenuItem("Разрешить перемещение панели") { CheckOnClick = true };

            var menu = new ContextMenuStrip();
            menu.Items.Add(_showPanelItem);
            menu.Items.Add(_autoStartItem);
            menu.Items.Add(_allowMoveItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Выход", null, OnExit);

            _trayIcon = new NotifyIcon
            {
                Icon = AppIconFactory.GetApplicationIcon(16),
                Text = "SidePanelApp V5",
                Visible = true,
                ContextMenuStrip = menu
            };
            _trayIcon.DoubleClick += (s, e) => TogglePanel();

            _showPanelItem.CheckedChanged += (s, e) => OnShowPanelChanged();
            _autoStartItem.CheckedChanged += (s, e) => OnAutoStartChanged();
            _allowMoveItem.CheckedChanged += (s, e) => OnAllowMoveChanged();

            LoadSettings();

            if (_showPanelItem.Checked)
            {
                ShowOverlayPanel();
            }
        }

        private void LoadSettings()
        {
            _autoStartItem.Checked = Settings.Default.AutoStartPanel;
            _showPanelItem.Checked = Settings.Default.ShowPanel || Settings.Default.AutoStartPanel;
            _allowMoveItem.Checked = Settings.Default.AllowPanelMove;
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
                _overlayPanel = new OverlayPanelV5();
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

            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            ExitThread();
        }
    }
}
