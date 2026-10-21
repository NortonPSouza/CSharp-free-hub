using System;

namespace FreeHub.Core
{
    /// <summary>
    /// Represents the high-level category of a tracked user activity.
    /// </summary>
    public enum ActivityCategory
    {
        Idle = 0,
        Coding = 1,
        Database = 2,
        Gaming = 3,
        Browsing = 4,
        Other = 5
    }
}
