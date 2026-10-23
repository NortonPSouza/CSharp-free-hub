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

            this.notifyIconTray.Icon = SystemIcons.Application;
            this.Icon = SystemIcons.Application;

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

            lblActiveProcessAndCategory.Text = string.Format(
                "[{0}] Process: {1} (+{2} XP)",
                log.Category.ToString().ToUpperInvariant(),
                log.ProcessName,
                log.EarnedXp);

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
                "{0} / {1} XP (Total: {2})",
                _orchestrator.XpInCurrentLevel,
                _orchestrator.XpRequiredPerLevel,
                _orchestrator.TotalXp);

            lblStreak.Text = string.Format("Focus Streak: {0}x", _orchestrator.FocusStreak);

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
            string message = string.Format("Congratulations! You reached Level {0}!", newLevel);
            notifyIconTray.ShowBalloonTip(3000, "FREEHUB — LEVEL UP!", message, ToolTipIcon.Info);
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