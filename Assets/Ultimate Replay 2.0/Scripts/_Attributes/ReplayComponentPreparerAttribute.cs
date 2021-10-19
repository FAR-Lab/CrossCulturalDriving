using System;
using UltimateReplay.Core;

namespace UltimateReplay
{
    /// <summary>
    /// Use this attribute to register a type as a component preparer.
    /// This attribute only works in conjunction with the <see cref="DefaultReplayPreparer"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class ReplayComponentPreparerAttribute : Attribute
    {
        // Public
        public Type componentType;

        // Constructor
        public ReplayComponentPreparerAttribute(Type componentType)
        {
            this.componentType = componentType;
        }
    }
}
