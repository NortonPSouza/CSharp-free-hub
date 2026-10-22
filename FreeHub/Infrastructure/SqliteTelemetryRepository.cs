using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Globalization;
using System.Linq;
using FreeHub.Core;

namespace FreeHub.Infrastructure
{
    public class SqliteTelemetryRepository : ITelemetryRepository
    {
        private readonly StoragePathResolver _pathResolver;

        public SqliteTelemetryRepository(StoragePathResolver pathResolver)
        {
            if (pathResolver == null)
            {
                throw new ArgumentNullException("pathResolver");
            }

            _pathResolver = pathResolver;
        }

        public void Initialize()
        {
            string dbPath = _pathResolver.GetCurrentMachineDatabasePath();
            string connectionString = BuildConnectionString(dbPath, false);

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                const string createTableSql = @"
                    CREATE TABLE IF NOT EXISTS ActivityLogs (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        MachineName TEXT NOT NULL,
                        ProcessName TEXT NOT NULL,
                        WindowTitle TEXT NOT NULL,
                        Category INTEGER NOT NULL,
                        DurationSeconds INTEGER NOT NULL,
                        EarnedXp INTEGER NOT NULL,
                        RecordedAtUtc TEXT NOT NULL
                    );
                    CREATE INDEX IF NOT EXISTS IX_ActivityLogs_RecordedAtUtc ON ActivityLogs(RecordedAtUtc DESC);
                    CREATE INDEX IF NOT EXISTS IX_ActivityLogs_Category ON ActivityLogs(Category);";

                using (SQLiteCommand command = new SQLiteCommand(createTableSql, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        public void InsertLog(WindowActivityLog log)
        {
            if (log == null)
            {
                throw new ArgumentNullException("log");
            }
            string dbPath = _pathResolver.GetCurrentMachineDatabasePath();
            string connectionString = BuildConnectionString(dbPath, false);
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                const string insertSql = @"
                    INSERT INTO ActivityLogs (
                        MachineName,
                        ProcessName,
                        WindowTitle,
                        Category,
                        DurationSeconds,
                        EarnedXp,
                        RecordedAtUtc
                    ) VALUES (
                        @MachineName,
                        @ProcessName,
                        @WindowTitle,
                        @Category,
                        @DurationSeconds,
                        @EarnedXp,
                        @RecordedAtUtc
                    );";
                using (SQLiteCommand command = new SQLiteCommand(insertSql, connection))
                {
                    command.Parameters.AddWithValue("@MachineName", log.MachineName ?? Environment.MachineName);
                    command.Parameters.AddWithValue("@ProcessName", log.ProcessName ?? string.Empty);
                    command.Parameters.AddWithValue("@WindowTitle", log.WindowTitle ?? string.Empty);
                    command.Parameters.AddWithValue("@Category", (int)log.Category);
                    command.Parameters.AddWithValue("@DurationSeconds", log.DurationSeconds);
                    command.Parameters.AddWithValue("@EarnedXp", log.EarnedXp);
                    command.Parameters.AddWithValue("@RecordedAtUtc", log.RecordedAtUtc.ToString("o", CultureInfo.InvariantCulture));
                    command.ExecuteNonQuery();
                }
            }
        }

        public int GetTotalXpAllMachines()
        {
            int totalXp = 0;
            string[] dbFiles = _pathResolver.GetAllMachineDatabasePaths();
            for (int i = 0; i < dbFiles.Length; i++)
            {
                string dbPath = dbFiles[i];
                try
                {
                    string connectionString = BuildConnectionString(dbPath, true);
                    using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                    {
                        connection.Open();
                        const string sumSql = "SELECT COALESCE(SUM(EarnedXp), 0) FROM ActivityLogs;";
                        using (SQLiteCommand command = new SQLiteCommand(sumSql, connection))
                        {
                            object result = command.ExecuteScalar();
                            if (result != null && result != DBNull.Value)
                            {
                                totalXp += Convert.ToInt32(result, CultureInfo.InvariantCulture);
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // Ignore transient lock or incomplete sync on secondary machine DB files.
                }
            }
            return totalXp;
        }
        public IList<WindowActivityLog> GetRecentLogs(int limit)
        {
            if (limit <= 0)
            {
                limit = 50;
            }
            List<WindowActivityLog> mergedLogs = new List<WindowActivityLog>();
            string[] dbFiles = _pathResolver.GetAllMachineDatabasePaths();
            for (int i = 0; i < dbFiles.Length; i++)
            {
                string dbPath = dbFiles[i];
                try
                {
                    string connectionString = BuildConnectionString(dbPath, true);
                    using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                    {
                        connection.Open();
                        const string selectSql = @"
                            SELECT Id, MachineName, ProcessName, WindowTitle, Category, DurationSeconds, EarnedXp, RecordedAtUtc
                            FROM ActivityLogs
                            ORDER BY RecordedAtUtc DESC
                            LIMIT @Limit;";
                        using (SQLiteCommand command = new SQLiteCommand(selectSql, connection))
                        {
                            command.Parameters.AddWithValue("@Limit", limit);
                            using (SQLiteDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    WindowActivityLog item = new WindowActivityLog
                                    {
                                        Id = reader.GetInt64(0),
                                        MachineName = reader.GetString(1),
                                        ProcessName = reader.GetString(2),
                                        WindowTitle = reader.GetString(3),
                                        Category = (ActivityCategory)reader.GetInt32(4),
                                        DurationSeconds = reader.GetInt32(5),
                                        EarnedXp = reader.GetInt32(6),
                                        RecordedAtUtc = ParseUtcDate(reader.GetString(7))
                                    };
                                    mergedLogs.Add(item);
                                }
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // Ignore transient lock on secondary DB files.
                }
            }
            return mergedLogs
                .OrderByDescending(x => x.RecordedAtUtc)
                .Take(limit)
                .ToList();
        }
        public DataTable GetCategorySummaryTable()
        {
            DataTable table = new DataTable("CategorySummary");
            table.Columns.Add("MachineName", typeof(string));
            table.Columns.Add("Category", typeof(string));
            table.Columns.Add("TotalMinutes", typeof(double));
            table.Columns.Add("TotalXp", typeof(int));
            string[] dbFiles = _pathResolver.GetAllMachineDatabasePaths();
            for (int i = 0; i < dbFiles.Length; i++)
            {
                string dbPath = dbFiles[i];
                try
                {
                    string connectionString = BuildConnectionString(dbPath, true);
                    using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                    {
                        connection.Open();
                        const string groupSql = @"
                            SELECT
                                MachineName,
                                Category,
                                COALESCE(SUM(DurationSeconds), 0) AS TotalSeconds,
                                COALESCE(SUM(EarnedXp), 0) AS TotalXp
                            FROM ActivityLogs
                            GROUP BY MachineName, Category
                            ORDER BY TotalXp DESC;";
                        using (SQLiteCommand command = new SQLiteCommand(groupSql, connection))
                        {
                            using (SQLiteDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    string machineName = reader.GetString(0);
                                    ActivityCategory category = (ActivityCategory)reader.GetInt32(1);
                                    int totalSeconds = reader.GetInt32(2);
                                    int totalXp = reader.GetInt32(3);
                                    double totalMinutes = Math.Round(totalSeconds / 60.0, 1);
                                    table.Rows.Add(machineName, category.ToString(), totalMinutes, totalXp);
                                }
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // Ignore transient lock on secondary DB files.
                }
            }
            return table;
        }
        private static DateTime ParseUtcDate(string isoDate)
        {
            DateTime parsed;
            if (DateTime.TryParse(isoDate, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out parsed))
            {
                return parsed;
            }
            return DateTime.UtcNow;
        }



        private static string BuildConnectionString(string databaseFilePath, bool readOnly)
        {
            SQLiteConnectionStringBuilder builder = new SQLiteConnectionStringBuilder
            {
                DataSource = databaseFilePath,
                Version = 3,
                JournalMode = SQLiteJournalModeEnum.Truncate,
                Pooling = false,
                BusyTimeout = 3000,
                ReadOnly = readOnly
            };

            return builder.ToString();
        }
    }
}