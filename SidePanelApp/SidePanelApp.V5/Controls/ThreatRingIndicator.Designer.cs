namespace SidePanelApp.V5.Controls
{
    partial class ThreatRingIndicator
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
            this.lblCaption = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // lblCaption
            // 
            this.lblCaption.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lblCaption.Font = new System.Drawing.Font("Segoe UI", 7F, System.Drawing.FontStyle.Bold);
            this.lblCaption.ForeColor = System.Drawing.Color.White;
            this.lblCaption.Location = new System.Drawing.Point(0, 34);
            this.lblCaption.Name = "lblCaption";
            this.lblCaption.Size = new System.Drawing.Size(35, 14);
            this.lblCaption.TabIndex = 0;
            this.lblCaption.Text = "OK";
            this.lblCaption.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // ThreatRingIndicator
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.FromArgb(26, 26, 46);
            this.Controls.Add(this.lblCaption);
            this.Margin = new System.Windows.Forms.Padding(0);
            this.Name = "ThreatRingIndicator";
            this.Size = new System.Drawing.Size(35, 48);
            this.Click += new System.EventHandler(this.ThreatRingIndicator_Click);
            this.ResumeLayout(false);
        }

        private void ThreatRingIndicator_Click(object sender, System.EventArgs e)
        {
            IndicatorClick?.Invoke(this, System.EventArgs.Empty);
        }

        private System.Windows.Forms.Label lblCaption;
    }
}
