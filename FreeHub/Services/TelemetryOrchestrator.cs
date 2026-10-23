using System;
using System.Collections.Generic;
using System.Data;
using FreeHub.Core;

namespace FreeHub.Services
{
    public class TelemetryOrchestrator
    {
        private const int XpPerLevel = 100;

        private readonly IActiveWindowTracker _windowTracker;
        private readonly IProcessClassifier _classifier;
        private readonly ITelemetryRepository _repository;

        public event EventHandler<int> LevelUpOccurred;

        public int TotalXp { get; private set; }

        public int CurrentLevel
        {
            get { return (TotalXp / XpPerLevel) + 1; }
        }

        public int XpInCurrentLevel
        {
            get { return TotalXp % XpPerLevel; }
        }

        public int XpRequiredPerLevel
        {
            get { return XpPerLevel; }
        }

        public int FocusStreak { get; private set; }

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
        }

        public void Initialize()
        {
            _repository.Initialize();
            TotalXp = _repository.GetTotalXpAllMachines();
        }

        public WindowActivityLog ProcessTick(int elapsedSeconds)
        {
            if (elapsedSeconds <= 0)
            {
                elapsedSeconds = 1;
            }
            ActiveWindowInfo windowInfo = _windowTracker.GetActiveWindow();
            string processName = windowInfo != null ? windowInfo.ProcessName : "idle";
            string windowTitle = windowInfo != null ? windowInfo.WindowTitle : string.Empty;
            ActivityCategory category = _classifier.Classify(processName, windowTitle);
            UpdateFocusStreak(category);
            int baseXp = _classifier.CalculateXp(category, elapsedSeconds);
            int totalEarnedXp = ApplyStreakBonus(baseXp, FocusStreak);
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
            int newLevel = CurrentLevel;
            if (newLevel > previousLevel)
            {
                OnLevelUpOccurred(newLevel);
            }
            return logEntry;
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
        private static int ApplyStreakBonus(int baseXp, int streak)
        {
            if (baseXp <= 0 || streak < 3)
            {
                return baseXp;
            }
            // Every 3 consecutive focus ticks grants +20% bonus XP (capped at +100% / 2x multiplier).
            int bonusSteps = streak / 3;
            if (bonusSteps > 5)
            {
                bonusSteps = 5;
            }
            double multiplier = 1.0 + (bonusSteps * 0.20);
            return (int)Math.Round(baseXp * multiplier);
        }
        protected virtual void OnLevelUpOccurred(int newLevel)
        {
            EventHandler<int> handler = LevelUpOccurred;
            if (handler != null)
            {
                handler(this, newLevel);
            }
        }

        public DataTable GetCategorySummaryTable()
        {
            return _repository.GetCategorySummaryTable();
        }

        public IList<WindowActivityLog> GetRecentLogs(int limit)
        {
            return _repository.GetRecentLogs(limit);
        }
    }
}