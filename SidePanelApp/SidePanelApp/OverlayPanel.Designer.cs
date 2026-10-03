namespace SidePanelApp
{
    partial class OverlayPanel
    {
        private void InitializeComponent()
        {
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.TopMost = true;
            this.ShowInTaskbar = false;
            this.BackColor = System.Drawing.Color.FromArgb(26, 26, 46);
            this.Opacity = 0.95D;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.ClientSize = new System.Drawing.Size(35, 100);
            this.MaximumSize = new System.Drawing.Size(35, 100);
            this.MinimumSize = new System.Drawing.Size(35, 100);
            this.Name = "OverlayPanel";
            this.Text = "OverlayPanel";
        }
    }
}
