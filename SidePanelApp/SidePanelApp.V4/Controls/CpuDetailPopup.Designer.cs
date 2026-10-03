namespace SidePanelApp.V4.Controls
{
    partial class CpuDetailPopup
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.content = new CpuDetailControl();
            this.SuspendLayout();
            this.content.Dock = System.Windows.Forms.DockStyle.Fill;
            this.content.Location = new System.Drawing.Point(0, 0);
            this.content.Name = "content";
            this.content.Size = new System.Drawing.Size(252, 160);
            this.content.TabIndex = 0;
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.FromArgb(26, 26, 46);
            this.ClientSize = new System.Drawing.Size(252, 160);
            this.Controls.Add(this.content);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "CpuDetailPopup";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "CpuDetailPopup";
            this.TopMost = true;
            this.ResumeLayout(false);
        }

        private CpuDetailControl content;
    }
}
