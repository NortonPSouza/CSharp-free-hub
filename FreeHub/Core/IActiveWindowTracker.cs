namespace FreeHub.Core
{
    /// <summary>
    /// Abstracts the OS-specific mechanism for inspecting the currently active foreground window.
    /// </summary>
    public interface IActiveWindowTracker
    {
        /// <summary>
        /// Gets information about the currently focused foreground window, or null if none is active.
        /// </summary>
        ActiveWindowInfo GetActiveWindow();
    }
}
