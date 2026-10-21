namespace FreeHub.Core
{
    /// <summary>
    /// Classifies an active process and window title into an ActivityCategory and calculates base XP.
    /// </summary>
    public interface IProcessClassifier
    {
        ActivityCategory Classify(string processName, string windowTitle);

        int CalculateXp(ActivityCategory category, int durationSeconds);
    }
}
