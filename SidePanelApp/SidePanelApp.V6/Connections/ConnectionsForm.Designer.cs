namespace SidePanelApp.V6.Connections
{
    partial class ConnectionsForm
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
            this.panelTop = new System.Windows.Forms.Panel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.statConnections = new SidePanelApp.V6.Connections.StatCardControl();
            this.statListening = new SidePanelApp.V6.Connections.StatCardControl();
            this.statPublic = new SidePanelApp.V6.Connections.StatCardControl();
            this.statProcesses = new SidePanelApp.V6.Connections.StatCardControl();
            this.panelTabs = new System.Windows.Forms.Panel();
            this.btnAll = new System.Windows.Forms.Button();
            this.btnConnected = new System.Windows.Forms.Button();
            this.btnListening = new System.Windows.Forms.Button();
            this.btnUdp = new System.Windows.Forms.Button();
            this.btnLocal = new System.Windows.Forms.Button();
            this.lblRefresh = new System.Windows.Forms.Label();
            this.cmbRefresh = new System.Windows.Forms.ComboBox();
            this.btnRefreshNow = new System.Windows.Forms.Button();
            this.splitMain = new System.Windows.Forms.SplitContainer();
            this.grid = new System.Windows.Forms.DataGridView();
            this.panelDetail = new System.Windows.Forms.Panel();
            this.lblDetailRemote = new System.Windows.Forms.Label();
            this.lblDetailLocal = new System.Windows.Forms.Label();
            this.lblDetailUser = new System.Windows.Forms.Label();
            this.lblDetailPid = new System.Windows.Forms.Label();
            this.lblDetailMeta = new System.Windows.Forms.Label();
            this.lblDetailTitle = new System.Windows.Forms.Label();
            this.panelTop.SuspendLayout();
            this.panelTabs.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).BeginInit();
            this.splitMain.Panel1.SuspendLayout();
            this.splitMain.Panel2.SuspendLayout();
            this.splitMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grid)).BeginInit();
            this.panelDetail.SuspendLayout();
            this.SuspendLayout();
            // 
            // panelTop
            // 
            this.panelTop.Controls.Add(this.statProcesses);
            this.panelTop.Controls.Add(this.statPublic);
            this.panelTop.Controls.Add(this.statListening);
            this.panelTop.Controls.Add(this.statConnections);
            this.panelTop.Controls.Add(this.lblTitle);
            this.panelTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTop.Location = new System.Drawing.Point(0, 0);
            this.panelTop.Name = "panelTop";
            this.panelTop.Padding = new System.Windows.Forms.Padding(12, 10, 12, 8);
            this.panelTop.Size = new System.Drawing.Size(984, 72);
            this.panelTop.TabIndex = 0;
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            this.lblTitle.Location = new System.Drawing.Point(12, 12);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(118, 25);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Connections";
            // 
            // statConnections
            // 
            this.statConnections.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.statConnections.Caption = "Connections";
            this.statConnections.Location = new System.Drawing.Point(380, 10);
            this.statConnections.Name = "statConnections";
            this.statConnections.Size = new System.Drawing.Size(140, 52);
            this.statConnections.TabIndex = 1;
            this.statConnections.Value = "0";
            // 
            // statListening
            // 
            this.statListening.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.statListening.Caption = "Listening";
            this.statListening.Location = new System.Drawing.Point(526, 10);
            this.statListening.Name = "statListening";
            this.statListening.Size = new System.Drawing.Size(140, 52);
            this.statListening.TabIndex = 2;
            this.statListening.Value = "0";
            // 
            // statPublic
            // 
            this.statPublic.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.statPublic.Caption = "Public";
            this.statPublic.Location = new System.Drawing.Point(672, 10);
            this.statPublic.Name = "statPublic";
            this.statPublic.Size = new System.Drawing.Size(140, 52);
            this.statPublic.TabIndex = 3;
            this.statPublic.Value = "0";
            // 
            // statProcesses
            // 
            this.statProcesses.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.statProcesses.Caption = "Processes";
            this.statProcesses.Location = new System.Drawing.Point(818, 10);
            this.statProcesses.Name = "statProcesses";
            this.statProcesses.Size = new System.Drawing.Size(140, 52);
            this.statProcesses.TabIndex = 4;
            this.statProcesses.Value = "0";
            // 
            // panelTabs
            // 
            this.panelTabs.Controls.Add(this.btnRefreshNow);
            this.panelTabs.Controls.Add(this.cmbRefresh);
            this.panelTabs.Controls.Add(this.lblRefresh);
            this.panelTabs.Controls.Add(this.btnLocal);
            this.panelTabs.Controls.Add(this.btnUdp);
            this.panelTabs.Controls.Add(this.btnListening);
            this.panelTabs.Controls.Add(this.btnConnected);
            this.panelTabs.Controls.Add(this.btnAll);
            this.panelTabs.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTabs.Location = new System.Drawing.Point(0, 72);
            this.panelTabs.Name = "panelTabs";
            this.panelTabs.Padding = new System.Windows.Forms.Padding(12, 6, 12, 6);
            this.panelTabs.Size = new System.Drawing.Size(984, 44);
            this.panelTabs.TabIndex = 1;
            // 
            // btnAll
            // 
            this.btnAll.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAll.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnAll.Location = new System.Drawing.Point(12, 8);
            this.btnAll.Name = "btnAll";
            this.btnAll.Size = new System.Drawing.Size(72, 28);
            this.btnAll.TabIndex = 0;
            this.btnAll.Text = "All";
            this.btnAll.UseVisualStyleBackColor = false;
            // 
            // btnConnected
            // 
            this.btnConnected.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnConnected.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnConnected.Location = new System.Drawing.Point(90, 8);
            this.btnConnected.Name = "btnConnected";
            this.btnConnected.Size = new System.Drawing.Size(88, 28);
            this.btnConnected.TabIndex = 1;
            this.btnConnected.Text = "Connected";
            this.btnConnected.UseVisualStyleBackColor = false;
            // 
            // btnListening
            // 
            this.btnListening.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnListening.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnListening.Location = new System.Drawing.Point(184, 8);
            this.btnListening.Name = "btnListening";
            this.btnListening.Size = new System.Drawing.Size(88, 28);
            this.btnListening.TabIndex = 2;
            this.btnListening.Text = "Listening";
            this.btnListening.UseVisualStyleBackColor = false;
            // 
            // btnUdp
            // 
            this.btnUdp.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnUdp.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnUdp.Location = new System.Drawing.Point(278, 8);
            this.btnUdp.Name = "btnUdp";
            this.btnUdp.Size = new System.Drawing.Size(56, 28);
            this.btnUdp.TabIndex = 3;
            this.btnUdp.Text = "UDP";
            this.btnUdp.UseVisualStyleBackColor = false;
            // 
            // btnLocal
            // 
            this.btnLocal.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnLocal.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnLocal.Location = new System.Drawing.Point(340, 8);
            this.btnLocal.Name = "btnLocal";
            this.btnLocal.Size = new System.Drawing.Size(88, 28);
            this.btnLocal.TabIndex = 4;
            this.btnLocal.Text = "Local IPC";
            this.btnLocal.UseVisualStyleBackColor = false;
            // 
            // lblRefresh
            // 
            this.lblRefresh.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.lblRefresh.AutoSize = true;
            this.lblRefresh.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblRefresh.Location = new System.Drawing.Point(700, 14);
            this.lblRefresh.Name = "lblRefresh";
            this.lblRefresh.Size = new System.Drawing.Size(68, 15);
            this.lblRefresh.TabIndex = 5;
            this.lblRefresh.Text = "Обновление";
            // 
            // cmbRefresh
            // 
            this.cmbRefresh.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.cmbRefresh.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbRefresh.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.cmbRefresh.FormattingEnabled = true;
            this.cmbRefresh.Location = new System.Drawing.Point(774, 10);
            this.cmbRefresh.Name = "cmbRefresh";
            this.cmbRefresh.Size = new System.Drawing.Size(120, 23);
            this.cmbRefresh.TabIndex = 6;
            // 
            // btnRefreshNow
            // 
            this.btnRefreshNow.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.btnRefreshNow.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnRefreshNow.Location = new System.Drawing.Point(900, 8);
            this.btnRefreshNow.Name = "btnRefreshNow";
            this.btnRefreshNow.Size = new System.Drawing.Size(72, 28);
            this.btnRefreshNow.TabIndex = 7;
            this.btnRefreshNow.Text = "Сейчас";
            this.btnRefreshNow.UseVisualStyleBackColor = false;
            // 
            // splitMain
            // 
            this.splitMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitMain.Location = new System.Drawing.Point(0, 116);
            this.splitMain.Name = "splitMain";
            // 
            // splitMain.Panel1
            // 
            this.splitMain.Panel1.Controls.Add(this.grid);
            this.splitMain.Panel1.Padding = new System.Windows.Forms.Padding(12, 0, 6, 12);
            // 
            // splitMain.Panel2
            // 
            this.splitMain.Panel2.Controls.Add(this.panelDetail);
            this.splitMain.Panel2.Padding = new System.Windows.Forms.Padding(6, 0, 12, 12);
            this.splitMain.Size = new System.Drawing.Size(1600, 784);
            this.splitMain.SplitterDistance = 720;
            this.splitMain.TabIndex = 2;
            // 
            // grid
            // 
            this.grid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grid.Location = new System.Drawing.Point(12, 0);
            this.grid.Name = "grid";
            this.grid.Size = new System.Drawing.Size(682, 433);
            this.grid.TabIndex = 0;
            // 
            // panelDetail
            // 
            this.panelDetail.Controls.Add(this.lblDetailRemote);
            this.panelDetail.Controls.Add(this.lblDetailLocal);
            this.panelDetail.Controls.Add(this.lblDetailUser);
            this.panelDetail.Controls.Add(this.lblDetailPid);
            this.panelDetail.Controls.Add(this.lblDetailMeta);
            this.panelDetail.Controls.Add(this.lblDetailTitle);
            this.panelDetail.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelDetail.Location = new System.Drawing.Point(6, 0);
            this.panelDetail.Name = "panelDetail";
            this.panelDetail.Padding = new System.Windows.Forms.Padding(14);
            this.panelDetail.Size = new System.Drawing.Size(262, 433);
            this.panelDetail.TabIndex = 0;
            // 
            // lblDetailTitle
            // 
            this.lblDetailTitle.AutoSize = true;
            this.lblDetailTitle.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.lblDetailTitle.Location = new System.Drawing.Point(14, 120);
            this.lblDetailTitle.Name = "lblDetailTitle";
            this.lblDetailTitle.Size = new System.Drawing.Size(17, 20);
            this.lblDetailTitle.TabIndex = 0;
            this.lblDetailTitle.Text = "—";
            // 
            // lblDetailMeta
            // 
            this.lblDetailMeta.AutoSize = true;
            this.lblDetailMeta.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblDetailMeta.Location = new System.Drawing.Point(14, 148);
            this.lblDetailMeta.Name = "lblDetailMeta";
            this.lblDetailMeta.Size = new System.Drawing.Size(0, 15);
            this.lblDetailMeta.TabIndex = 1;
            // 
            // lblDetailPid
            // 
            this.lblDetailPid.AutoSize = true;
            this.lblDetailPid.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblDetailPid.Location = new System.Drawing.Point(14, 178);
            this.lblDetailPid.Name = "lblDetailPid";
            this.lblDetailPid.Size = new System.Drawing.Size(0, 15);
            this.lblDetailPid.TabIndex = 2;
            // 
            // lblDetailUser
            // 
            this.lblDetailUser.AutoSize = true;
            this.lblDetailUser.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblDetailUser.Location = new System.Drawing.Point(14, 200);
            this.lblDetailUser.Name = "lblDetailUser";
            this.lblDetailUser.Size = new System.Drawing.Size(0, 15);
            this.lblDetailUser.TabIndex = 3;
            // 
            // lblDetailLocal
            // 
            this.lblDetailLocal.AutoSize = true;
            this.lblDetailLocal.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblDetailLocal.Location = new System.Drawing.Point(14, 230);
            this.lblDetailLocal.MaximumSize = new System.Drawing.Size(230, 0);
            this.lblDetailLocal.Name = "lblDetailLocal";
            this.lblDetailLocal.Size = new System.Drawing.Size(0, 15);
            this.lblDetailLocal.TabIndex = 4;
            // 
            // lblDetailRemote
            // 
            this.lblDetailRemote.AutoSize = true;
            this.lblDetailRemote.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblDetailRemote.Location = new System.Drawing.Point(14, 260);
            this.lblDetailRemote.MaximumSize = new System.Drawing.Size(230, 0);
            this.lblDetailRemote.Name = "lblDetailRemote";
            this.lblDetailRemote.Size = new System.Drawing.Size(0, 15);
            this.lblDetailRemote.TabIndex = 5;
            // 
            // ConnectionsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1600, 900);
            this.Controls.Add(this.splitMain);
            this.Controls.Add(this.panelTabs);
            this.Controls.Add(this.panelTop);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.MinimumSize = new System.Drawing.Size(960, 520);
            this.Name = "ConnectionsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Connections — SidePanelApp V6";
            this.panelTop.ResumeLayout(false);
            this.panelTop.PerformLayout();
            this.panelTabs.ResumeLayout(false);
            this.splitMain.Panel1.ResumeLayout(false);
            this.splitMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).EndInit();
            this.splitMain.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.grid)).EndInit();
            this.panelDetail.ResumeLayout(false);
            this.panelDetail.PerformLayout();
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.Panel panelTop;
        private System.Windows.Forms.Label lblTitle;
        private StatCardControl statConnections;
        private StatCardControl statListening;
        private StatCardControl statPublic;
        private StatCardControl statProcesses;
        private System.Windows.Forms.Panel panelTabs;
        private System.Windows.Forms.Button btnAll;
        private System.Windows.Forms.Button btnConnected;
        private System.Windows.Forms.Button btnListening;
        private System.Windows.Forms.Button btnUdp;
        private System.Windows.Forms.Button btnLocal;
        private System.Windows.Forms.Label lblRefresh;
        private System.Windows.Forms.ComboBox cmbRefresh;
        private System.Windows.Forms.Button btnRefreshNow;
        private System.Windows.Forms.SplitContainer splitMain;
        private System.Windows.Forms.DataGridView grid;
        private System.Windows.Forms.Panel panelDetail;
        private System.Windows.Forms.Label lblDetailTitle;
        private System.Windows.Forms.Label lblDetailMeta;
        private System.Windows.Forms.Label lblDetailPid;
        private System.Windows.Forms.Label lblDetailUser;
        private System.Windows.Forms.Label lblDetailLocal;
        private System.Windows.Forms.Label lblDetailRemote;
    }
}
