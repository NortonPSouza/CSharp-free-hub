using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using FreeHub.Core;

namespace FreeHub.Infrastructure
{
    /// <summary>
    /// Tracks the currently active foreground window in Windows using native User32.dll P/Invoke calls
    /// and detects real-time audio playback via the Windows Core Audio API peak meter.
    /// </summary>
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

        public bool IsBackgroundMusicActive()
        {
            return IsSystemAudioPlaying();
        }

        private static bool IsSystemAudioPlaying()
        {
            IMMDeviceEnumerator enumerator = null;
            IMMDevice device = null;
            object meterObject = null;

            try
            {
                Type enumeratorType = Type.GetTypeFromCLSID(new Guid("BCDE0395-E52F-467C-8E3D-C4579291692E"));
                if (enumeratorType == null)
                {
                    return false;
                }

                enumerator = (IMMDeviceEnumerator)Activator.CreateInstance(enumeratorType);
                // eRender = 0 (audio output), eMultimedia = 1
                int hr = enumerator.GetDefaultAudioEndpoint(0, 1, out device);
                if (hr != 0 || device == null)
                {
                    return false;
                }

                Guid iidAudioMeterInformation = new Guid("C02216F6-8C67-4B5B-9D00-D008E73E0064");
                hr = device.Activate(ref iidAudioMeterInformation, 0, IntPtr.Zero, out meterObject);
                if (hr != 0 || meterObject == null)
                {
                    return false;
                }

                IAudioMeterInformation meter = (IAudioMeterInformation)meterObject;
                float peak = 0f;
                meter.GetPeakValue(out peak);

                return peak > 0.0005f;
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                if (meterObject != null && Marshal.IsComObject(meterObject))
                {
                    Marshal.ReleaseComObject(meterObject);
                }

                if (device != null && Marshal.IsComObject(device))
                {
                    Marshal.ReleaseComObject(device);
                }

                if (enumerator != null && Marshal.IsComObject(enumerator))
                {
                    Marshal.ReleaseComObject(enumerator);
                }
            }
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

        [ComImport]
        [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMMDeviceEnumerator
        {
            int NotImpl1();

            [PreserveSig]
            int GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice ppEndpoint);
        }

        [ComImport]
        [Guid("D666063F-1587-4E43-81F1-B948E807363F")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMMDevice
        {
            [PreserveSig]
            int Activate(ref Guid iid, int dwClsCtx, IntPtr pActivationParams, [MarshalAs(UnmanagedType.IUnknown)] out object ppInterface);
        }

        [ComImport]
        [Guid("C02216F6-8C67-4B5B-9D00-D008E73E0064")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IAudioMeterInformation
        {
            [PreserveSig]
            int GetPeakValue(out float pfPeak);
        }
    }
}
