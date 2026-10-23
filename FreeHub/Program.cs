using System;
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
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Composition Root: Wire up dependencies manually via Constructor Injection.
            StoragePathResolver pathResolver = new StoragePathResolver();
            ITelemetryRepository repository = new SqliteTelemetryRepository(pathResolver);
            IActiveWindowTracker windowTracker = new Win32ActiveWindowTracker();
            IProcessClassifier classifier = new ProcessClassifier();

            TelemetryOrchestrator orchestrator = new TelemetryOrchestrator(
                windowTracker,
                classifier,
                repository);

            Application.Run(new MainHudForm(orchestrator));
        }
    }
}