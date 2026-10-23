using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using FreeHub.Core;

namespace FreeHub.Services
{
    public class TelemetryOrchestrator
    {
        private const int BaseLevelXp = 100;
        private const double LevelCurveExponent = 1.35;

        private readonly IActiveWindowTracker _windowTracker;
        private readonly IProcessClassifier _classifier;
        private readonly ITelemetryRepository _repository;

        public event EventHandler<int> LevelUpOccurred;

        public int TotalXp { get; private set; }

        public int CurrentLevel { get; private set; }

        public int XpInCurrentLevel { get; private set; }

        public int XpRequiredPerLevel { get; private set; }

        public int LevelProgressPercent
        {
            get
            {
                if (XpRequiredPerLevel <= 0)
                {
                    return 0;
                }

                int percent = (int)Math.Round((XpInCurrentLevel * 100.0) / XpRequiredPerLevel);
                if (percent < 0) return 0;
                if (percent > 100) return 100;
                return percent;
            }
        }

        public int FocusStreak { get; private set; }

        public bool IsMusicBuffActive { get; private set; }

        public double CurrentXpMultiplier
        {
            get
            {
                return CalculateMultiplier(FocusStreak, IsMusicBuffActive);
            }
        }

        public WindowActivityLog LastRecordedLog { get; private set; }

        public TelemetryOrchestrator(
            IActiveWindowTracker windowTracker,
            IProcessClassifier classifier,
            ITelemetryRepository repository)
        {
            if (windowTracker == null) throw new ArgumentNullException("windowTracker");
            if (classifier == null) throw new ArgumentNullException("classifier");
            if (repository == null) throw new ArgumentNullException("repository");

            _windowTracker = windowTracker;
            _classifier = classifier;
            _repository = repository;

            CurrentLevel = 1;
            XpRequiredPerLevel = BaseLevelXp;
        }

        public void Initialize()
        {
            _repository.Initialize();
            ReloadTotalXp();
        }

        public void ReloadTotalXp()
        {
            TotalXp = _repository.GetTotalXpAllMachines();
            RecalculateLevelState();
        }

        public WindowActivityLog ProcessTick(int elapsedSeconds)
        {
            if (elapsedSeconds <= 0)
            {
                elapsedSeconds = 1;
            }

            ActiveWindowInfo windowInfo = _windowTracker.GetActiveWindow();
            IsMusicBuffActive = _windowTracker.IsBackgroundMusicActive();

            string processName = windowInfo != null ? windowInfo.ProcessName : "idle";
            string windowTitle = windowInfo != null ? windowInfo.WindowTitle : string.Empty;

            ActivityCategory category = _classifier.Classify(processName, windowTitle);
            UpdateFocusStreak(category);

            int baseXp = _classifier.CalculateXp(category, elapsedSeconds);
            int totalEarnedXp = ApplyBonuses(baseXp, FocusStreak, IsMusicBuffActive);

            int previousLevel = CurrentLevel;

            WindowActivityLog logEntry = new WindowActivityLog
            {
                MachineName = Environment.MachineName,
                ProcessName = processName,
                WindowTitle = windowTitle,
                Category = category,
                DurationSeconds = elapsedSeconds,
                EarnedXp = totalEarnedXp,
                RecordedAtUtc = DateTime.UtcNow
            };

            _repository.InsertLog(logEntry);

            TotalXp += totalEarnedXp;
            LastRecordedLog = logEntry;
            RecalculateLevelState();

            int newLevel = CurrentLevel;
            if (newLevel > previousLevel)
            {
                OnLevelUpOccurred(newLevel);
            }

            return logEntry;
        }

        public DataTable GetCategorySummaryTable()
        {
            DataTable rawTable = _repository.GetCategorySummaryTable();

            DataTable enrichedTable = new DataTable("CategorySummary");
            enrichedTable.Columns.Add("Status", typeof(string));
            enrichedTable.Columns.Add("MachineName", typeof(string));
            enrichedTable.Columns.Add("Category", typeof(string));
            enrichedTable.Columns.Add("TotalMinutes", typeof(double));
            enrichedTable.Columns.Add("TotalXp", typeof(int));
            enrichedTable.Columns.Add("Share", typeof(string));

            if (rawTable == null || rawTable.Rows.Count == 0)
            {
                return enrichedTable;
            }

            string activeMachine = LastRecordedLog != null ? LastRecordedLog.MachineName : Environment.MachineName;
            string activeCategory = LastRecordedLog != null ? LastRecordedLog.Category.ToString() : string.Empty;

            int grandTotalXp = 0;
            for (int i = 0; i < rawTable.Rows.Count; i++)
            {
                grandTotalXp += Convert.ToInt32(rawTable.Rows[i]["TotalXp"], CultureInfo.InvariantCulture);
            }

            List<DataRow> activeRows = new List<DataRow>();
            List<DataRow> otherRows = new List<DataRow>();

            for (int i = 0; i < rawTable.Rows.Count; i++)
            {
                DataRow row = rawTable.Rows[i];
                string machine = Convert.ToString(row["MachineName"], CultureInfo.InvariantCulture);
                string category = Convert.ToString(row["Category"], CultureInfo.InvariantCulture);

                bool isCurrentActive =
                    !string.IsNullOrEmpty(activeCategory) &&
                    string.Equals(machine, activeMachine, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(category, activeCategory, StringComparison.OrdinalIgnoreCase);

                if (isCurrentActive)
                {
                    activeRows.Add(row);
                }
                else
                {
                    otherRows.Add(row);
                }
            }

            otherRows.Sort(delegate(DataRow a, DataRow b)
            {
                int xpA = Convert.ToInt32(a["TotalXp"], CultureInfo.InvariantCulture);
                int xpB = Convert.ToInt32(b["TotalXp"], CultureInfo.InvariantCulture);
                return xpB.CompareTo(xpA);
            });

            AppendSummaryRows(enrichedTable, activeRows, true, grandTotalXp);
            AppendSummaryRows(enrichedTable, otherRows, false, grandTotalXp);

            return enrichedTable;
        }

        public IList<WindowActivityLog> GetRecentLogs(int limit)
        {
            return _repository.GetRecentLogs(limit);
        }

        public static int GetXpRequiredForLevel(int level)
        {
            if (level <= 1)
            {
                return BaseLevelXp;
            }

            double rawRequirement = BaseLevelXp * Math.Pow(level, LevelCurveExponent);
            int roundedToTen = ((int)Math.Round(rawRequirement / 10.0)) * 10;
            return roundedToTen < BaseLevelXp ? BaseLevelXp : roundedToTen;
        }

        private static void AppendSummaryRows(DataTable target, List<DataRow> sourceRows, bool isActiveRow, int grandTotalXp)
        {
            for (int i = 0; i < sourceRows.Count; i++)
            {
                DataRow src = sourceRows[i];
                string machine = Convert.ToString(src["MachineName"], CultureInfo.InvariantCulture);
                string category = Convert.ToString(src["Category"], CultureInfo.InvariantCulture);
                double minutes = Convert.ToDouble(src["TotalMinutes"], CultureInfo.InvariantCulture);
                int xp = Convert.ToInt32(src["TotalXp"], CultureInfo.InvariantCulture);

                double sharePct = grandTotalXp > 0 ? (xp * 100.0) / grandTotalXp : 0.0;
                string shareText = string.Format(CultureInfo.InvariantCulture, "{0:0.0}%", sharePct);
                string statusText = isActiveRow ? "ACTIVE NOW" : "IDLE";

                target.Rows.Add(statusText, machine, category, minutes, xp, shareText);
            }
        }

        private void RecalculateLevelState()
        {
            int remainingXp = TotalXp < 0 ? 0 : TotalXp;
            int level = 1;

            while (true)
            {
                int requiredForCurrent = GetXpRequiredForLevel(level);
                if (remainingXp < requiredForCurrent)
                {
                    CurrentLevel = level;
                    XpInCurrentLevel = remainingXp;
                    XpRequiredPerLevel = requiredForCurrent;
                    break;
                }

                remainingXp -= requiredForCurrent;
                level++;
            }
        }

        private void UpdateFocusStreak(ActivityCategory category)
        {
            if (category == ActivityCategory.Coding || category == ActivityCategory.Database)
            {
                FocusStreak++;
            }
            else if (category == ActivityCategory.Gaming || category == ActivityCategory.Idle)
            {
                FocusStreak = 0;
            }
        }

        private static double CalculateMultiplier(int streak, bool musicBuffActive)
        {
            double multiplier = 1.0;

            if (streak >= 3)
            {
                int bonusSteps = streak / 3;
                if (bonusSteps > 5)
                {
                    bonusSteps = 5;
                }

                multiplier += (bonusSteps * 0.20);
            }

            if (musicBuffActive)
            {
                multiplier += 0.10;
            }

            return multiplier;
        }

        private static int ApplyBonuses(int baseXp, int streak, bool musicBuffActive)
        {
            if (baseXp <= 0)
            {
                return 0;
            }

            double multiplier = CalculateMultiplier(streak, musicBuffActive);
            int finalXp = (int)Math.Round(baseXp * multiplier);
            if (musicBuffActive && finalXp <= baseXp)
            {
                finalXp = baseXp + 1;
            }

            return finalXp;
        }

        protected virtual void OnLevelUpOccurred(int newLevel)
        {
            EventHandler<int> handler = LevelUpOccurred;
            if (handler != null)
            {
                handler(this, newLevel);
            }
        }
    }
}
