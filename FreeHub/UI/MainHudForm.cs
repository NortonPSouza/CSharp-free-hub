using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using FreeHub.Core;
using FreeHub.Services;

namespace FreeHub.UI
{
    public partial class MainHudForm : Form
    {
        private const int TickIntervalSeconds = 5;
        private const int AutoSyncEveryTicks = 60; // 60 * 5s = 5 minutes

        private readonly TelemetryOrchestrator _orchestrator;
        private readonly ICloudSyncService _cloudSyncService;
        private Icon _customAppIcon;
        private int _tickCounter;
        private bool _isSyncing;
        private bool _allowExit;

        // Parameterless constructor required by Visual Studio WinForms Designer.
        public MainHudForm()
        {
            InitializeComponent();
        }

        public MainHudForm(TelemetryOrchestrator orchestrator, ICloudSyncService cloudSyncService)
            : this()
        {
            if (orchestrator == null)
            {
                throw new ArgumentNullException("orchestrator");
            }

            _orchestrator = orchestrator;
            _cloudSyncService = cloudSyncService;
            _orchestrator.LevelUpOccurred += OnLevelUpOccurred;

            _customAppIcon = HudIconFactory.CreateNeonPulseIcon();
            this.Icon = _customAppIcon;
            this.notifyIconTray.Icon = _customAppIcon;

            this.Load += OnFormLoad;
            this.Resize += OnFormResize;
            this.FormClosing += OnFormClosing;
            this.tmrTelemetry.Tick += OnTelemetryTimerTick;
            this.btnSyncCloud.Click += OnSyncCloudClicked;
            this.btnCloudSettings.Click += OnCloudSettingsClicked;
            this.tsmiSyncNow.Click += OnSyncCloudClicked;
            this.dgvCategorySummary.RowPrePaint += OnSummaryGridRowPrePaint;
            this.notifyIconTray.DoubleClick += OnTrayOpenClicked;
            this.tsmiOpenHud.Click += OnTrayOpenClicked;
            this.tsmiExit.Click += OnTrayExitClicked;
        }

        private void OnFormLoad(object sender, EventArgs e)
        {
            if (_orchestrator == null)
            {
                return;
            }

            _orchestrator.Initialize();
            RefreshHeaderStats();
            RefreshSummaryGrid();
            tmrTelemetry.Start();
        }

        private async void OnTelemetryTimerTick(object sender, EventArgs e)
        {
            if (_orchestrator == null)
            {
                return;
            }

            WindowActivityLog log = _orchestrator.ProcessTick(TickIntervalSeconds);

            string musicTag = _orchestrator.IsMusicBuffActive ? " [MUSIC BUFF +10%]" : string.Empty;

            lblActiveProcessAndCategory.Text = string.Format(
                "[{0}] Process: {1} (+{2} XP){3}",
                log.Category.ToString().ToUpperInvariant(),
                log.ProcessName,
                log.EarnedXp,
                musicTag);

            lblActiveWindowTitle.Text = string.IsNullOrWhiteSpace(log.WindowTitle)
                ? "(No window title)"
                : log.WindowTitle;

            RefreshHeaderStats();
            RefreshSummaryGrid();

            _tickCounter++;
            if (_tickCounter >= AutoSyncEveryTicks && _cloudSyncService != null && _cloudSyncService.IsConfigured() && !_isSyncing)
            {
                _tickCounter = 0;
                await RunCloudSyncAsync(false);
            }
        }

        private async void OnSyncCloudClicked(object sender, EventArgs e)
        {
            if (_cloudSyncService == null)
            {
                return;
            }

            if (!_cloudSyncService.IsConfigured())
            {
                if (!PromptForCloudCredentials())
                {
                    return;
                }
            }

            await RunCloudSyncAsync(true);
        }

        private async void OnCloudSettingsClicked(object sender, EventArgs e)
        {
            if (_cloudSyncService == null)
            {
                return;
            }

            if (PromptForCloudCredentials())
            {
                await RunCloudSyncAsync(true);
            }
        }

        private bool PromptForCloudCredentials()
        {
            using (CloudSyncSetupForm dialog = new CloudSyncSetupForm(_cloudSyncService.GetClientId()))
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    _cloudSyncService.SaveCredentials(dialog.ClientId, dialog.ClientSecret);
                    return true;
                }
            }

            return false;
        }

        private async System.Threading.Tasks.Task RunCloudSyncAsync(bool showNotification)
        {
            if (_isSyncing || _cloudSyncService == null)
            {
                return;
            }

            _isSyncing = true;
            btnSyncCloud.Enabled = false;
            btnSyncCloud.Text = "Syncing...";

            try
            {
                CloudSyncResult result = await _cloudSyncService.SynchronizeAsync();

                _orchestrator.ReloadTotalXp();
                RefreshHeaderStats();
                RefreshSummaryGrid();

                if (result.Success)
                {
                    btnSyncCloud.BackColor = Color.FromArgb(0, 180, 90);
                    btnSyncCloud.Text = "Drive Synced!";
                    if (showNotification)
                    {
                        notifyIconTray.ShowBalloonTip(2500, "Google Drive Sync", result.Message, ToolTipIcon.Info);
                    }
                }
                else
                {
                    btnSyncCloud.BackColor = Color.FromArgb(211, 47, 47);
                    btnSyncCloud.Text = "Sync Failed";
                    if (showNotification)
                    {
                        MessageBox.Show(this, result.Message, "Google Drive Sync Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
            finally
            {
                btnSyncCloud.Enabled = true;
                _isSyncing = false;
            }
        }

        private void RefreshHeaderStats()
        {
            lblLevel.Text = string.Format("LEVEL {0}", _orchestrator.CurrentLevel);
            lblXpStatus.Text = string.Format(
                "{0} / {1} XP ({2}%)",
                _orchestrator.XpInCurrentLevel,
                _orchestrator.XpRequiredPerLevel,
                _orchestrator.LevelProgressPercent);

            if (_orchestrator.IsMusicBuffActive)
            {
                lblStreak.ForeColor = Color.FromArgb(0, 230, 118);
                lblStreak.Text = string.Format("Streak: {0}x | Music Buff +10%", _orchestrator.FocusStreak);
            }
            else
            {
                lblStreak.ForeColor = Color.FromArgb(255, 171, 0);
                lblStreak.Text = string.Format("Focus Streak: {0}x", _orchestrator.FocusStreak);
            }

            pbLevelProgress.Minimum = 0;
            pbLevelProgress.Maximum = _orchestrator.XpRequiredPerLevel > 0 ? _orchestrator.XpRequiredPerLevel : 100;
            pbLevelProgress.IsBoostActive = _orchestrator.IsMusicBuffActive || _orchestrator.FocusStreak >= 3;

            int remainingXp = Math.Max(0, _orchestrator.XpRequiredPerLevel - _orchestrator.XpInCurrentLevel);
            pbLevelProgress.OverlayText = string.Format("{0}% — {1} XP to Level {2}",
                _orchestrator.LevelProgressPercent,
                remainingXp,
                _orchestrator.CurrentLevel + 1);

            pbLevelProgress.HoverOverlayText = string.Format(
                CultureInfo.InvariantCulture,
                "Multiplier: {0:0.0}x  |  Total Career XP: {1}  |  {2} XP needed for Level {3}",
                _orchestrator.CurrentXpMultiplier,
                _orchestrator.TotalXp,
                remainingXp,
                _orchestrator.CurrentLevel + 1);

            pbLevelProgress.SetTargetValue(_orchestrator.XpInCurrentLevel);
        }

        private void RefreshSummaryGrid()
        {
            dgvCategorySummary.DataSource = _orchestrator.GetCategorySummaryTable();
            dgvCategorySummary.ClearSelection();
        }

        private void OnSummaryGridRowPrePaint(object sender, DataGridViewRowPrePaintEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= dgvCategorySummary.Rows.Count)
            {
                return;
            }

            DataGridViewRow row = dgvCategorySummary.Rows[e.RowIndex];
            object statusObj = row.Cells["Status"] != null ? row.Cells["Status"].Value : null;
            string status = statusObj != null ? statusObj.ToString() : string.Empty;

            if (string.Equals(status, "ACTIVE NOW", StringComparison.OrdinalIgnoreCase))
            {
                row.DefaultCellStyle.BackColor = Color.FromArgb(16, 54, 44);
                row.DefaultCellStyle.ForeColor = Color.FromArgb(0, 230, 118);
                row.DefaultCellStyle.SelectionBackColor = Color.FromArgb(20, 68, 56);
                row.DefaultCellStyle.SelectionForeColor = Color.FromArgb(0, 230, 118);
            }
            else
            {
                Color zebraBg = (e.RowIndex % 2 == 0)
                    ? Color.FromArgb(20, 20, 28)
                    : Color.FromArgb(25, 25, 35);
                row.DefaultCellStyle.BackColor = zebraBg;
                row.DefaultCellStyle.ForeColor = Color.FromArgb(205, 210, 222);
                row.DefaultCellStyle.SelectionBackColor = Color.FromArgb(34, 42, 60);
                row.DefaultCellStyle.SelectionForeColor = Color.White;
            }
        }

        private void OnLevelUpOccurred(object sender, int newLevel)
        {
            string message = string.Format(
                "Congratulations! You reached Level {0}! Next level requires {1} XP.",
                newLevel,
                _orchestrator.XpRequiredPerLevel);
            notifyIconTray.ShowBalloonTip(3000, "FREEHUB - LEVEL UP!", message, ToolTipIcon.Info);
        }

        private void OnFormResize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                MinimizeToSystemTray();
            }
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            if (!_allowExit && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                MinimizeToSystemTray();
            }
        }

        private void MinimizeToSystemTray()
        {
            this.Hide();
            this.ShowInTaskbar = false;
            notifyIconTray.ShowBalloonTip(
                2000,
                "FreeHub Running in System Tray",
                "Right-click the tray icon and select 'Exit FreeHub' to close completely.",
                ToolTipIcon.None);
        }

        private void OnTrayOpenClicked(object sender, EventArgs e)
        {
            this.Show();
            this.ShowInTaskbar = true;
            this.WindowState = FormWindowState.Normal;
            this.Activate();
        }

        private void OnTrayExitClicked(object sender, EventArgs e)
        {
            _allowExit = true;
            tmrTelemetry.Stop();
            notifyIconTray.Visible = false;
            Application.Exit();
        }
    }
}
