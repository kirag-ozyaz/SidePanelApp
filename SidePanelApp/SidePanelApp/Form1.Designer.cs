namespace SidePanelApp
{
    partial class Form1
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

        #region Код, автоматически созданный конструктором форм Windows

        private void InitializeComponent()
        {
            this.lblTitle = new System.Windows.Forms.Label();
            this.chkShowPanel = new System.Windows.Forms.CheckBox();
            this.chkAutoStart = new System.Windows.Forms.CheckBox();
            this.chkAllowMove = new System.Windows.Forms.CheckBox();
            this.lblHint = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.lblTitle.Location = new System.Drawing.Point(24, 20);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(198, 21);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Боковая панель метрик";
            // 
            // chkShowPanel
            // 
            this.chkShowPanel.AutoSize = true;
            this.chkShowPanel.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.chkShowPanel.Location = new System.Drawing.Point(28, 68);
            this.chkShowPanel.Name = "chkShowPanel";
            this.chkShowPanel.Size = new System.Drawing.Size(170, 23);
            this.chkShowPanel.TabIndex = 1;
            this.chkShowPanel.Text = "Показать панель сейчас";
            this.chkShowPanel.UseVisualStyleBackColor = true;
            this.chkShowPanel.CheckedChanged += new System.EventHandler(this.chkShowPanel_CheckedChanged);
            // 
            // chkAutoStart
            // 
            this.chkAutoStart.AutoSize = true;
            this.chkAutoStart.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.chkAutoStart.Location = new System.Drawing.Point(28, 104);
            this.chkAutoStart.Name = "chkAutoStart";
            this.chkAutoStart.Size = new System.Drawing.Size(220, 23);
            this.chkAutoStart.TabIndex = 2;
            this.chkAutoStart.Text = "Запускать панель при старте";
            this.chkAutoStart.UseVisualStyleBackColor = true;
            this.chkAutoStart.CheckedChanged += new System.EventHandler(this.chkAutoStart_CheckedChanged);
            // 
            // chkAllowMove
            // 
            this.chkAllowMove.AutoSize = true;
            this.chkAllowMove.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.chkAllowMove.Location = new System.Drawing.Point(28, 140);
            this.chkAllowMove.Name = "chkAllowMove";
            this.chkAllowMove.Size = new System.Drawing.Size(220, 23);
            this.chkAllowMove.TabIndex = 3;
            this.chkAllowMove.Text = "Разрешить перемещение панели";
            this.chkAllowMove.UseVisualStyleBackColor = true;
            this.chkAllowMove.CheckedChanged += new System.EventHandler(this.chkAllowMove_CheckedChanged);
            // 
            // lblHint
            // 
            this.lblHint.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblHint.ForeColor = System.Drawing.Color.DimGray;
            this.lblHint.Location = new System.Drawing.Point(25, 176);
            this.lblHint.Name = "lblHint";
            this.lblHint.Size = new System.Drawing.Size(340, 72);
            this.lblHint.TabIndex = 4;
            this.lblHint.Text = "Панель отображается справа на экране поверх окон.\r\nПоказывает загрузку CPU и испо" +
    "льзование RAM.\r\nДвойной клик по панели — скрыть. ПКМ — меню.";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(384, 268);
            this.Controls.Add(this.lblHint);
            this.Controls.Add(this.chkAllowMove);
            this.Controls.Add(this.chkAutoStart);
            this.Controls.Add(this.chkShowPanel);
            this.Controls.Add(this.lblTitle);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "SidePanelApp";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.CheckBox chkShowPanel;
        private System.Windows.Forms.CheckBox chkAutoStart;
        private System.Windows.Forms.CheckBox chkAllowMove;
        private System.Windows.Forms.Label lblHint;
    }
}
