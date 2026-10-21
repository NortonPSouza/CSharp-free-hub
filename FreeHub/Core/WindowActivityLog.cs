using System;

namespace FreeHub.Core
{
    /// <summary>
    /// Represents a single recorded snapshot or interval of active window usage on a specific machine.
    /// </summary>
    public class WindowActivityLog
    {
        public long Id { get; set; }

        public string MachineName { get; set; }

        public string ProcessName { get; set; }

        public string WindowTitle { get; set; }

        public ActivityCategory Category { get; set; }

        public int DurationSeconds { get; set; }

        public int EarnedXp { get; set; }

        public DateTime RecordedAtUtc { get; set; }
    }
}
