namespace SidePanelApp.V6.Controls
{
    partial class ThreatDetailPopup
    {
        private void InitializeComponent()
        {
            this.content = new SidePanelApp.V6.Controls.ThreatDetailControl();
            this.SuspendLayout();
            // 
            // content
            // 
            this.content.Dock = System.Windows.Forms.DockStyle.Fill;
            this.content.Location = new System.Drawing.Point(0, 0);
            this.content.Name = "content";
            this.content.Size = new System.Drawing.Size(300, 160);
            this.content.TabIndex = 0;
            // 
            // ThreatDetailPopup
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(300, 160);
            this.Controls.Add(this.content);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "ThreatDetailPopup";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "ThreatDetailPopup";
            this.TopMost = true;
            this.ResumeLayout(false);
        }

        private ThreatDetailControl content;
    }
}
