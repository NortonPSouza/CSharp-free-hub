using System;
using System.Drawing;
using System.Windows.Forms;
using FreeHub.Core;
using FreeHub.Services;

namespace FreeHub.UI
{
    public partial class MainHudForm : Form
    {
        private const int TickIntervalSeconds = 5;
        private readonly TelemetryOrchestrator _orchestrator;
        private Icon _customAppIcon;

        // Parameterless constructor required by Visual Studio WinForms Designer.
        public MainHudForm()
        {
            InitializeComponent();
        }

        public MainHudForm(TelemetryOrchestrator orchestrator)
            : this()
        {
            if (orchestrator == null)
            {
                throw new ArgumentNullException("orchestrator");
            }

            _orchestrator = orchestrator;
            _orchestrator.LevelUpOccurred += OnLevelUpOccurred;

            _customAppIcon = HudIconFactory.CreateNeonPulseIcon();
            this.Icon = _customAppIcon;
            this.notifyIconTray.Icon = _customAppIcon;

            this.Load += OnFormLoad;
            this.Resize += OnFormResize;
            this.tmrTelemetry.Tick += OnTelemetryTimerTick;
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

        private void OnTelemetryTimerTick(object sender, EventArgs e)
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

            int progressValue = _orchestrator.XpInCurrentLevel;
            if (progressValue < pbLevelProgress.Minimum) progressValue = pbLevelProgress.Minimum;
            if (progressValue > pbLevelProgress.Maximum) progressValue = pbLevelProgress.Maximum;
            pbLevelProgress.Value = progressValue;
        }

        private void RefreshSummaryGrid()
        {
            dgvCategorySummary.DataSource = _orchestrator.GetCategorySummaryTable();
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
                this.Hide();
                this.ShowInTaskbar = false;
                notifyIconTray.ShowBalloonTip(
                    2000,
                    "FreeHub Running in Background",
                    "Double-click the tray icon anytime to open the HUD.",
                    ToolTipIcon.None);
            }
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
            tmrTelemetry.Stop();
            notifyIconTray.Visible = false;
            Application.Exit();
        }
    }
}
