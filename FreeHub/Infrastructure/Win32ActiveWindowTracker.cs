using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using FreeHub.Core;

namespace FreeHub.Infrastructure
{
    public class Win32ActiveWindowTracker : IActiveWindowTracker
    {
        private const int MaxWindowTitleLength = 512;
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);


        public ActiveWindowInfo GetActiveWindow()
        {
            IntPtr foregroundWindowHandle = GetForegroundWindow();
            if (foregroundWindowHandle == IntPtr.Zero)
            {
                return null;
            }
            uint processId = 0;
            GetWindowThreadProcessId(foregroundWindowHandle, out processId);
            if (processId == 0)
            {
                return null;
            }
            string windowTitle = ReadWindowTitle(foregroundWindowHandle);
            string processName = ResolveProcessName((int)processId);
            return new ActiveWindowInfo
            {
                ProcessId = (int)processId,
                ProcessName = processName,
                WindowTitle = windowTitle,
                CapturedAtUtc = DateTime.UtcNow
            };

        }

        private static string ReadWindowTitle(IntPtr hWnd)
        {
            StringBuilder buffer = new StringBuilder(MaxWindowTitleLength);
            int length = GetWindowText(hWnd, buffer, MaxWindowTitleLength);
            if (length <= 0)
            {
                return string.Empty;
            }
            return buffer.ToString();
        }

        private static string ResolveProcessName(int processId)
        {
            try
            {
                using (Process process = Process.GetProcessById(processId))
                {
                    return process.ProcessName ?? "unknown";
                }
            }
            catch (Exception)
            {
                // Protected OS processes or elevated processes may throw AccessDenied/ArgumentException.
                return "system";
            }
        }
    }
}
