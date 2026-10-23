namespace FreeHub.Core
{
    /// <summary>
    /// Abstracts the OS-specific mechanism for inspecting the currently active foreground window
    /// and detecting background media/music players.
    /// </summary>
    public interface IActiveWindowTracker
    {
        /// <summary>
        /// Gets information about the currently focused foreground window, or null if none is active.
        /// </summary>
        ActiveWindowInfo GetActiveWindow();

        /// <summary>
        /// Returns true if a music player (such as Spotify desktop or Spotify/YouTube Music web player) is active.
        /// </summary>
        bool IsBackgroundMusicActive();
    }
}
