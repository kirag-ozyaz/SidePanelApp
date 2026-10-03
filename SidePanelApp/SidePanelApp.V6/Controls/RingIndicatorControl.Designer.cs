namespace SidePanelApp.V6.Controls
{
    partial class RingIndicatorControl
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
            this.lblPercent = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // lblPercent
            // 
            this.lblPercent.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lblPercent.Font = new System.Drawing.Font("Segoe UI", 7F, System.Drawing.FontStyle.Bold);
            this.lblPercent.ForeColor = System.Drawing.Color.White;
            this.lblPercent.Location = new System.Drawing.Point(0, 34);
            this.lblPercent.Name = "lblPercent";
            this.lblPercent.Size = new System.Drawing.Size(35, 14);
            this.lblPercent.TabIndex = 0;
            this.lblPercent.Text = "0%";
            this.lblPercent.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // RingIndicatorControl
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.FromArgb(26, 26, 46);
            this.Controls.Add(this.lblPercent);
            this.Margin = new System.Windows.Forms.Padding(0);
            this.Name = "RingIndicatorControl";
            this.Size = new System.Drawing.Size(35, 48);
            this.MouseEnter += new System.EventHandler(this.RingIndicatorControl_MouseEnter);
            this.MouseLeave += new System.EventHandler(this.RingIndicatorControl_MouseLeave);
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.Label lblPercent;
    }
}
