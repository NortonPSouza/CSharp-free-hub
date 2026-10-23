using System;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using FreeHub.Core;
using FreeHub.Infrastructure;
using FreeHub.Services;
using FreeHub.UI;

namespace FreeHub
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Bootstrap embedded SQLite assemblies before JIT compiles StartHudApplication()
            NativeDependencyBootstrapper.Initialize();
            StartHudApplication();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void StartHudApplication()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Composition Root: Wire up dependencies manually via Constructor Injection.
            StoragePathResolver pathResolver = new StoragePathResolver();
            ITelemetryRepository repository = new SqliteTelemetryRepository(pathResolver);
            IActiveWindowTracker windowTracker = new Win32ActiveWindowTracker();
            IProcessClassifier classifier = new ProcessClassifier();
            ICloudSyncService cloudSyncService = new GoogleDriveHttpSyncService(pathResolver);

            TelemetryOrchestrator orchestrator = new TelemetryOrchestrator(
                windowTracker,
                classifier,
                repository);

            Application.Run(new MainHudForm(orchestrator, cloudSyncService));
        }
    }
}
