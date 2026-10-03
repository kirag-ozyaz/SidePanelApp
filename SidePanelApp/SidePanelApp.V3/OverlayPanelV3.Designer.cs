namespace SidePanelApp.V3
{
    partial class OverlayPanelV3
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
            this.components = new System.ComponentModel.Container();
            this.cpuIndicator = new SidePanelApp.V3.Controls.RingIndicatorControl();
            this.ramIndicator = new SidePanelApp.V3.Controls.RingIndicatorControl();
            this.contextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.menuHide = new System.Windows.Forms.ToolStripMenuItem();
            this.menuClose = new System.Windows.Forms.ToolStripMenuItem();
            this.contextMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // cpuIndicator
            // 
            this.cpuIndicator.BackColor = System.Drawing.Color.FromArgb(26, 26, 46);
            this.cpuIndicator.Location = new System.Drawing.Point(0, 6);
            this.cpuIndicator.Margin = new System.Windows.Forms.Padding(0);
            this.cpuIndicator.Name = "cpuIndicator";
            this.cpuIndicator.Size = new System.Drawing.Size(35, 48);
            this.cpuIndicator.TabIndex = 0;
            // 
            // ramIndicator
            // 
            this.ramIndicator.BackColor = System.Drawing.Color.FromArgb(26, 26, 46);
            this.ramIndicator.Location = new System.Drawing.Point(0, 54);
            this.ramIndicator.Margin = new System.Windows.Forms.Padding(0);
            this.ramIndicator.Name = "ramIndicator";
            this.ramIndicator.Size = new System.Drawing.Size(35, 48);
            this.ramIndicator.TabIndex = 1;
            // 
            // contextMenu
            // 
            this.contextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuHide,
            this.menuClose});
            this.contextMenu.Name = "contextMenu";
            this.contextMenu.Size = new System.Drawing.Size(118, 48);
            // 
            // menuHide
            // 
            this.menuHide.Name = "menuHide";
            this.menuHide.Size = new System.Drawing.Size(117, 22);
            this.menuHide.Text = "Скрыть";
            this.menuHide.Click += new System.EventHandler(this.menuHide_Click);
            // 
            // menuClose
            // 
            this.menuClose.Name = "menuClose";
            this.menuClose.Size = new System.Drawing.Size(117, 22);
            this.menuClose.Text = "Закрыть";
            this.menuClose.Click += new System.EventHandler(this.menuClose_Click);
            // 
            // OverlayPanelV3
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.FromArgb(26, 26, 46);
            this.ClientSize = new System.Drawing.Size(35, 108);
            this.ContextMenuStrip = this.contextMenu;
            this.Controls.Add(this.ramIndicator);
            this.Controls.Add(this.cpuIndicator);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.MaximumSize = new System.Drawing.Size(35, 108);
            this.MinimumSize = new System.Drawing.Size(35, 108);
            this.Name = "OverlayPanelV3";
            this.Opacity = 0.95D;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "OverlayPanelV3";
            this.TopMost = true;
            this.DoubleClick += new System.EventHandler(this.OverlayPanelV3_DoubleClick);
            this.contextMenu.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        private Controls.RingIndicatorControl cpuIndicator;
        private Controls.RingIndicatorControl ramIndicator;
        private System.Windows.Forms.ContextMenuStrip contextMenu;
        private System.Windows.Forms.ToolStripMenuItem menuHide;
        private System.Windows.Forms.ToolStripMenuItem menuClose;
    }
}
