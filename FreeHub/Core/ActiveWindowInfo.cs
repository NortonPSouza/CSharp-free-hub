using System;

namespace FreeHub.Core
{
    /// <summary>
    /// Holds raw information about the currently focused OS window.
    /// </summary>
    public class ActiveWindowInfo
    {
        public int ProcessId { get; set; }

        public string ProcessName { get; set; }

        public string WindowTitle { get; set; }

        public DateTime CapturedAtUtc { get; set; }
    }
}
