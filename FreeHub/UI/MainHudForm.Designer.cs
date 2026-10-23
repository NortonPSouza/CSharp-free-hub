namespace FreeHub.UI
{
    partial class MainHudForm
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Panel pnlHeader;
        private System.Windows.Forms.Label lblLevel;
        private System.Windows.Forms.Label lblXpStatus;
        private System.Windows.Forms.Label lblStreak;
        private System.Windows.Forms.ProgressBar pbLevelProgress;
        private System.Windows.Forms.Panel pnlCurrentActivity;
        private System.Windows.Forms.Label lblActiveWindowTitle;
        private System.Windows.Forms.Label lblActiveProcessAndCategory;
        private System.Windows.Forms.DataGridView dgvCategorySummary;
        private System.Windows.Forms.Timer tmrTelemetry;
        private System.Windows.Forms.NotifyIcon notifyIconTray;
        private System.Windows.Forms.ContextMenuStrip cmsTrayMenu;
        private System.Windows.Forms.ToolStripMenuItem tsmiOpenHud;
        private System.Windows.Forms.ToolStripMenuItem tsmiExit;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.pnlHeader = new System.Windows.Forms.Panel();
            this.lblLevel = new System.Windows.Forms.Label();
            this.lblXpStatus = new System.Windows.Forms.Label();
            this.lblStreak = new System.Windows.Forms.Label();
            this.pbLevelProgress = new System.Windows.Forms.ProgressBar();
            this.pnlCurrentActivity = new System.Windows.Forms.Panel();
            this.lblActiveProcessAndCategory = new System.Windows.Forms.Label();
            this.lblActiveWindowTitle = new System.Windows.Forms.Label();
            this.dgvCategorySummary = new System.Windows.Forms.DataGridView();
            this.tmrTelemetry = new System.Windows.Forms.Timer(this.components);
            this.notifyIconTray = new System.Windows.Forms.NotifyIcon(this.components);
            this.cmsTrayMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.tsmiOpenHud = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiExit = new System.Windows.Forms.ToolStripMenuItem();
            this.pnlHeader.SuspendLayout();
            this.pnlCurrentActivity.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvCategorySummary)).BeginInit();
            this.cmsTrayMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlHeader
            // 
            this.pnlHeader.BackColor = System.Drawing.Color.FromArgb(24, 24, 32);
            this.pnlHeader.Controls.Add(this.pbLevelProgress);
            this.pnlHeader.Controls.Add(this.lblStreak);
            this.pnlHeader.Controls.Add(this.lblXpStatus);
            this.pnlHeader.Controls.Add(this.lblLevel);
            this.pnlHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlHeader.Location = new System.Drawing.Point(0, 0);
            this.pnlHeader.Name = "pnlHeader";
            this.pnlHeader.Padding = new System.Windows.Forms.Padding(16);
            this.pnlHeader.Size = new System.Drawing.Size(760, 105);
            this.pnlHeader.TabIndex = 0;
            // 
            // lblLevel
            // 
            this.lblLevel.AutoSize = true;
            this.lblLevel.Font = new System.Drawing.Font("Segoe UI", 18F, System.Drawing.FontStyle.Bold);
            this.lblLevel.ForeColor = System.Drawing.Color.FromArgb(0, 230, 118);
            this.lblLevel.Location = new System.Drawing.Point(16, 12);
            this.lblLevel.Name = "lblLevel";
            this.lblLevel.Size = new System.Drawing.Size(95, 32);
            this.lblLevel.TabIndex = 0;
            this.lblLevel.Text = "LEVEL 1";
            // 
            // lblXpStatus
            // 
            this.lblXpStatus.AutoSize = true;
            this.lblXpStatus.Font = new System.Drawing.Font("Segoe UI Semibold", 11F, System.Drawing.FontStyle.Bold);
            this.lblXpStatus.ForeColor = System.Drawing.Color.FromArgb(200, 200, 215);
            this.lblXpStatus.Location = new System.Drawing.Point(135, 21);
            this.lblXpStatus.Name = "lblXpStatus";
            this.lblXpStatus.Size = new System.Drawing.Size(120, 20);
            this.lblXpStatus.TabIndex = 1;
            this.lblXpStatus.Text = "0 / 100 XP (0%)";
            // 
            // lblStreak
            // 
            this.lblStreak.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblStreak.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.lblStreak.ForeColor = System.Drawing.Color.FromArgb(255, 171, 0);
            this.lblStreak.Location = new System.Drawing.Point(420, 21);
            this.lblStreak.Name = "lblStreak";
            this.lblStreak.Size = new System.Drawing.Size(320, 22);
            this.lblStreak.TabIndex = 2;
            this.lblStreak.Text = "Focus Streak: 0x";
            this.lblStreak.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // pbLevelProgress
            // 
            this.pbLevelProgress.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pbLevelProgress.Location = new System.Drawing.Point(16, 58);
            this.pbLevelProgress.Maximum = 100;
            this.pbLevelProgress.Name = "pbLevelProgress";
            this.pbLevelProgress.Size = new System.Drawing.Size(728, 26);
            this.pbLevelProgress.TabIndex = 3;
            this.pbLevelProgress.Value = 0;
            // 
            // pnlCurrentActivity
            // 
            this.pnlCurrentActivity.BackColor = System.Drawing.Color.FromArgb(32, 32, 44);
            this.pnlCurrentActivity.Controls.Add(this.lblActiveWindowTitle);
            this.pnlCurrentActivity.Controls.Add(this.lblActiveProcessAndCategory);
            this.pnlCurrentActivity.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlCurrentActivity.Location = new System.Drawing.Point(0, 105);
            this.pnlCurrentActivity.Name = "pnlCurrentActivity";
            this.pnlCurrentActivity.Padding = new System.Windows.Forms.Padding(16, 10, 16, 10);
            this.pnlCurrentActivity.Size = new System.Drawing.Size(760, 75);
            this.pnlCurrentActivity.TabIndex = 1;
            // 
            // lblActiveProcessAndCategory
            // 
            this.lblActiveProcessAndCategory.AutoSize = true;
            this.lblActiveProcessAndCategory.Font = new System.Drawing.Font("Segoe UI", 10.5F, System.Drawing.FontStyle.Bold);
            this.lblActiveProcessAndCategory.ForeColor = System.Drawing.Color.FromArgb(86, 156, 214);
            this.lblActiveProcessAndCategory.Location = new System.Drawing.Point(16, 10);
            this.lblActiveProcessAndCategory.Name = "lblActiveProcessAndCategory";
            this.lblActiveProcessAndCategory.Size = new System.Drawing.Size(210, 19);
            this.lblActiveProcessAndCategory.TabIndex = 0;
            this.lblActiveProcessAndCategory.Text = "Waiting for first telemetry tick...";
            // 
            // lblActiveWindowTitle
            // 
            this.lblActiveWindowTitle.AutoEllipsis = true;
            this.lblActiveWindowTitle.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblActiveWindowTitle.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Regular);
            this.lblActiveWindowTitle.ForeColor = System.Drawing.Color.FromArgb(180, 180, 195);
            this.lblActiveWindowTitle.Location = new System.Drawing.Point(16, 36);
            this.lblActiveWindowTitle.Name = "lblActiveWindowTitle";
            this.lblActiveWindowTitle.Size = new System.Drawing.Size(728, 22);
            this.lblActiveWindowTitle.TabIndex = 1;
            this.lblActiveWindowTitle.Text = "-";
            // 
            // dgvCategorySummary
            // 
            this.dgvCategorySummary.AllowUserToAddRows = false;
            this.dgvCategorySummary.AllowUserToDeleteRows = false;
            this.dgvCategorySummary.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvCategorySummary.BackgroundColor = System.Drawing.Color.FromArgb(18, 18, 24);
            this.dgvCategorySummary.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.dgvCategorySummary.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvCategorySummary.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvCategorySummary.Location = new System.Drawing.Point(0, 180);
            this.dgvCategorySummary.Name = "dgvCategorySummary";
            this.dgvCategorySummary.ReadOnly = true;
            this.dgvCategorySummary.RowHeadersVisible = false;
            this.dgvCategorySummary.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvCategorySummary.Size = new System.Drawing.Size(760, 260);
            this.dgvCategorySummary.TabIndex = 2;
            // 
            // tmrTelemetry
            // 
            this.tmrTelemetry.Interval = 5000;
            // 
            // notifyIconTray
            // 
            this.notifyIconTray.ContextMenuStrip = this.cmsTrayMenu;
            this.notifyIconTray.Text = "FreeHub - Telemetry HUD";
            this.notifyIconTray.Visible = true;
            // 
            // cmsTrayMenu
            // 
            this.cmsTrayMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmiOpenHud,
            this.tsmiExit});
            this.cmsTrayMenu.Name = "cmsTrayMenu";
            this.cmsTrayMenu.Size = new System.Drawing.Size(140, 48);
            // 
            // tsmiOpenHud
            // 
            this.tsmiOpenHud.Name = "tsmiOpenHud";
            this.tsmiOpenHud.Size = new System.Drawing.Size(139, 22);
            this.tsmiOpenHud.Text = "Open HUD";
            // 
            // tsmiExit
            // 
            this.tsmiExit.Name = "tsmiExit";
            this.tsmiExit.Size = new System.Drawing.Size(139, 22);
            this.tsmiExit.Text = "Exit FreeHub";
            // 
            // MainHudForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(18, 18, 24);
            this.ClientSize = new System.Drawing.Size(760, 440);
            this.Controls.Add(this.dgvCategorySummary);
            this.Controls.Add(this.pnlCurrentActivity);
            this.Controls.Add(this.pnlHeader);
            this.Name = "MainHudForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "FreeHub - Gamified Telemetry HUD";
            this.pnlHeader.ResumeLayout(false);
            this.pnlHeader.PerformLayout();
            this.pnlCurrentActivity.ResumeLayout(false);
            this.pnlCurrentActivity.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvCategorySummary)).EndInit();
            this.cmsTrayMenu.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion
    }
}
