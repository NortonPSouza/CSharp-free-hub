using System;
using System.IO;
using System.Text;

namespace FreeHub.Infrastructure
{
    public class StoragePathResolver
    {
        private const string DataFolderName = "FreeHub_Data";
        private readonly string _dataDirectory;

        //(: this(null)): Permite instanciar new StoragePathResolver() sem parâmetros (ele descobre a pasta sozinho) ou passando um caminho específico
        public StoragePathResolver() : this(null)
        {
        }

        public StoragePathResolver(string customBaseDirectory)
        {
            _dataDirectory = ResolveAndEnsureDirectory(customBaseDirectory);
        }

        public string GetDataDirectory()
        {
            return _dataDirectory;
        }

        public string GetCurrentMachineDatabasePath()
        {
            string safeMachineName = SanitizeMachineName(Environment.MachineName);
            string fileName = string.Format("freehub_{0}.db", safeMachineName);
            return Path.Combine(_dataDirectory, fileName);
        }

        public string[] GetAllMachineDatabasePaths()
        {
            if (!Directory.Exists(_dataDirectory))
            {
                return new string[0];
            }

            return Directory.GetFiles(_dataDirectory, "freehub_*.db", SearchOption.TopDirectoryOnly);
        }

        private static string ResolveAndEnsureDirectory(string customBaseDirectory)
        {
            string targetDirectory = null;
            if (!string.IsNullOrWhiteSpace(customBaseDirectory))
            {
                targetDirectory = customBaseDirectory;
            }
            else
            {
                string envOverride = Environment.GetEnvironmentVariable("FREEHUB_DATA_DIR");
                if (!string.IsNullOrWhiteSpace(envOverride))
                {
                    targetDirectory = envOverride;
                }
                else
                {
                    targetDirectory = TryFindGoogleDriveDataFolder();
                }
            }
            if (string.IsNullOrWhiteSpace(targetDirectory))
            {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                targetDirectory = Path.Combine(localAppData, DataFolderName);
            }
            if (!Directory.Exists(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }
            return targetDirectory;
        }

        private static string TryFindGoogleDriveDataFolder()
        {
            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string[] candidateRoots = new string[]
            {
                @"G:\My Drive",
                @"G:\Meu Drive",
                Path.Combine(userProfile, "Google Drive"),
                Path.Combine(userProfile, "My Drive"),
                Path.Combine(userProfile, "Meu Drive")
            };
            for (int i = 0; i < candidateRoots.Length; i++)
            {
                string root = candidateRoots[i];
                if (Directory.Exists(root))
                {
                    return Path.Combine(root, DataFolderName);
                }
            }
            return null;
        }

        private static string SanitizeMachineName(string rawMachineName)
        {
            if (string.IsNullOrWhiteSpace(rawMachineName))
            {
                return "UNKNOWN-PC";
            }
            char[] invalidChars = Path.GetInvalidFileNameChars();
            StringBuilder builder = new StringBuilder(rawMachineName.Length);
            for (int i = 0; i < rawMachineName.Length; i++)
            {
                char current = rawMachineName[i];
                bool isInvalid = false;
                for (int j = 0; j < invalidChars.Length; j++)
                {
                    if (current == invalidChars[j])
                    {
                        isInvalid = true;
                        break;
                    }
                }
                builder.Append(isInvalid ? '_' : current);
            }
            return builder.ToString().ToUpperInvariant();
        }
    }
}