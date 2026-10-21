using System;
using System.Collections.Generic;
using System.Data;

namespace FreeHub.Core
{
    /// <summary>
    /// Defines persistence operations for telemetry data across single-machine writes and multi-machine reads.
    /// </summary>
    public interface ITelemetryRepository
    {
        /// <summary>
        /// Ensures the local machine's SQLite database and schema exist.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Saves an activity log entry to the current machine's isolated database file.
        /// </summary>
        void InsertLog(WindowActivityLog log);

        /// <summary>
        /// Aggregates total XP across all discovered machine databases in the data folder.
        /// </summary>
        int GetTotalXpAllMachines();

        /// <summary>
        /// Retrieves recent activity logs across all discovered machine databases.
        /// </summary>
        IList<WindowActivityLog> GetRecentLogs(int limit);

        /// <summary>
        /// Retrieves a summarized DataTable grouped by category and machine for UI grid and chart binding.
        /// </summary>
        DataTable GetCategorySummaryTable();
    }
}
