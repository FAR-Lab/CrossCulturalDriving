using System;

namespace UltimateReplay
{
    /// <summary>
    /// Attach this attribute to a class that derives from <see cref="ReplayBehaviour"/> and the replay system will ignore it.
    /// This is useful when you want to receive replay events but dont need to record any data.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ReplayIgnoreAttribute : Attribute
    {
        // Empty class
    }
}
