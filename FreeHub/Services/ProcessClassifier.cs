using System;
using System.Collections.Generic;
using FreeHub.Core;

namespace FreeHub.Services
{
    public class ProcessClassifier : IProcessClassifier
    {
        private static readonly HashSet<string> CodingProcesses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "devenv",
            "code",
            "antigravity",
            "cursor",
            "windsurf",
            "rider64",
            "notepad++",
            "gitExtensions",
            "git",
            "git-bash",
            "postman",
            "insomnia",
            "powershell",
            "cmd",
            "WindowsTerminal"
        };

        private static readonly HashSet<string> DatabaseProcesses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ssms",
            "sqldeveloper",
            "dbeaver",
            "pgadmin4",
            "heidisql",
            "sqlitebrowser"
        };

        private static readonly HashSet<string> BrowsingProcesses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "chrome",
            "msedge",
            "firefox",
            "brave",
            "opera"
        };

        private static readonly HashSet<string> GamingProcesses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "steam",
            "cs2",
            "dota2",
            "valorant",
            "leagueclient",
            "eldenring",
            "witcher3"
        };

        private static readonly HashSet<string> IdleProcesses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "LockApp",
            "LogonUI",
            "ScreenSaver"
        };

        public ActivityCategory Classify(string processName, string windowTitle)
        {
            if (string.IsNullOrWhiteSpace(processName))
            {
                return ActivityCategory.Idle;
            }

            string normalizedProcess = processName.Trim();

            if (IdleProcesses.Contains(normalizedProcess))
            {
                return ActivityCategory.Idle;
            }

            if (CodingProcesses.Contains(normalizedProcess) || IsCodingWindowTitle(windowTitle))
            {
                return ActivityCategory.Coding;
            }

            if (DatabaseProcesses.Contains(normalizedProcess))
            {
                return ActivityCategory.Database;
            }

            if (GamingProcesses.Contains(normalizedProcess))
            {
                return ActivityCategory.Gaming;
            }

            if (BrowsingProcesses.Contains(normalizedProcess))
            {
                return ClassifyBrowserActivity(windowTitle);
            }

            return ActivityCategory.Other;
        }

        public int CalculateXp(ActivityCategory category, int durationSeconds)
        {
            if (durationSeconds <= 0)
            {
                return 0;
            }

            int baseXpPerMinute;
            switch (category)
            {
                case ActivityCategory.Coding:
                    baseXpPerMinute = 15;
                    break;
                case ActivityCategory.Database:
                    baseXpPerMinute = 15;
                    break;
                case ActivityCategory.Gaming:
                    baseXpPerMinute = 8;
                    break;
                case ActivityCategory.Browsing:
                    baseXpPerMinute = 5;
                    break;
                case ActivityCategory.Other:
                    baseXpPerMinute = 3;
                    break;
                case ActivityCategory.Idle:
                default:
                    return 0;
            }

            int calculatedXp = (int)Math.Round((durationSeconds / 60.0) * baseXpPerMinute);
            if (calculatedXp < 1)
            {
                calculatedXp = 1;
            }

            return calculatedXp;
        }

        private static bool IsCodingWindowTitle(string windowTitle)
        {
            if (string.IsNullOrWhiteSpace(windowTitle))
            {
                return false;
            }

            return windowTitle.IndexOf("Antigravity", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   windowTitle.IndexOf("Visual Studio", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static ActivityCategory ClassifyBrowserActivity(string windowTitle)
        {
            if (string.IsNullOrWhiteSpace(windowTitle))
            {
                return ActivityCategory.Browsing;
            }

            string[] codingKeywords = new string[]
            {
                "GitHub",
                "Stack Overflow",
                "DevExpress",
                "Microsoft Learn",
                "Documentation",
                "Pull Request",
                "localhost",
                "Antigravity"
            };

            for (int i = 0; i < codingKeywords.Length; i++)
            {
                if (windowTitle.IndexOf(codingKeywords[i], StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return ActivityCategory.Coding;
                }
            }

            return ActivityCategory.Browsing;
        }
    }
}
