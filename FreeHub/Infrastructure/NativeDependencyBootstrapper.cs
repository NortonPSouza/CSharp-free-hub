using System;
using System.IO;
using System.Reflection;

namespace FreeHub.Infrastructure
{
    /// <summary>
    /// Enables single-file portable execution of FreeHub.exe by resolving System.Data.SQLite.dll
    /// and extracting the native x86 SQLite.Interop.dll from embedded resources at startup.
    /// </summary>
    public static class NativeDependencyBootstrapper
    {
        private const string ManagedSqliteResourceName = "FreeHub.EmbeddedDlls.System.Data.SQLite.dll";
        private const string NativeInteropResourceName = "FreeHub.EmbeddedDlls.SQLite.Interop.dll";

        private static bool _initialized;

        public static void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            ExtractNativeSqliteInterop();
            AppDomain.CurrentDomain.AssemblyResolve += OnResolveAssembly;
        }

        private static void ExtractNativeSqliteInterop()
        {
            try
            {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string nativeBaseDir = Path.Combine(localAppData, "FreeHub_Data", "native_runtime");
                string x86Dir = Path.Combine(nativeBaseDir, "x86");

                if (!Directory.Exists(x86Dir))
                {
                    Directory.CreateDirectory(x86Dir);
                }

                string interopTargetFile = Path.Combine(x86Dir, "SQLite.Interop.dll");
                if (!File.Exists(interopTargetFile))
                {
                    Assembly executingAssembly = Assembly.GetExecutingAssembly();
                    using (Stream resourceStream = executingAssembly.GetManifestResourceStream(NativeInteropResourceName))
                    {
                        if (resourceStream != null)
                        {
                            using (FileStream fileStream = new FileStream(interopTargetFile, FileMode.Create, FileAccess.Write, FileShare.Read))
                            {
                                resourceStream.CopyTo(fileStream);
                            }
                        }
                    }
                }

                // Official System.Data.SQLite environment variable to locate native SQLite.Interop.dll
                Environment.SetEnvironmentVariable("PreLoadSQLite_BaseDirectory", nativeBaseDir);
            }
            catch (Exception)
            {
                // If running inside Visual Studio bin\Debug, the local x86\SQLite.Interop.dll is already present.
            }
        }

        private static Assembly OnResolveAssembly(object sender, ResolveEventArgs args)
        {
            AssemblyName requestedName = new AssemblyName(args.Name);
            if (!string.Equals(requestedName.Name, "System.Data.SQLite", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            using (Stream stream = executingAssembly.GetManifestResourceStream(ManagedSqliteResourceName))
            {
                if (stream == null)
                {
                    return null;
                }

                byte[] assemblyBytes = new byte[stream.Length];
                stream.Read(assemblyBytes, 0, assemblyBytes.Length);
                return Assembly.Load(assemblyBytes);
            }
        }
    }
}
