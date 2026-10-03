using System;
using System.Windows.Forms;
using SidePanelApp.V2.Properties;

namespace SidePanelApp.V2
{
    /// <summary>Окно настроек: показ панели, автозапуск, разрешение перемещения.</summary>
    public partial class MainForm : Form
    {
        private OverlayPanelV2 _overlayPanel;

        public MainForm()
        {
            InitializeComponent();
            Icon = SidePanelApp.AppIconFactory.GetApplicationIcon(32);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            chkAutoStart.Checked = Settings.Default.AutoStartPanel;
            chkShowPanel.Checked = Settings.Default.ShowPanel || Settings.Default.AutoStartPanel;
            chkAllowMove.Checked = Settings.Default.AllowPanelMove;

            if (chkShowPanel.Checked)
            {
                ShowOverlayPanel();
            }

            ApplyAllowMove();
        }

        private void chkShowPanel_CheckedChanged(object sender, EventArgs e)
        {
            if (chkShowPanel.Checked)
            {
                ShowOverlayPanel();
            }
            else
            {
                HideOverlayPanel();
            }

            Settings.Default.ShowPanel = chkShowPanel.Checked;
            Settings.Default.Save();
        }

        private void chkAutoStart_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Default.AutoStartPanel = chkAutoStart.Checked;
            Settings.Default.Save();

            if (chkAutoStart.Checked && !chkShowPanel.Checked)
            {
                chkShowPanel.Checked = true;
            }
        }

        private void chkAllowMove_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Default.AllowPanelMove = chkAllowMove.Checked;
            Settings.Default.Save();
            ApplyAllowMove();
        }

        private void ApplyAllowMove()
        {
            if (_overlayPanel != null && !_overlayPanel.IsDisposed)
            {
                _overlayPanel.AllowMove = chkAllowMove.Checked;
            }
        }

        private void ShowOverlayPanel()
        {
            if (_overlayPanel == null || _overlayPanel.IsDisposed)
            {
                _overlayPanel = new OverlayPanelV2();
                _overlayPanel.AllowMove = chkAllowMove.Checked;
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

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            HideOverlayPanel();
        }
    }
}
