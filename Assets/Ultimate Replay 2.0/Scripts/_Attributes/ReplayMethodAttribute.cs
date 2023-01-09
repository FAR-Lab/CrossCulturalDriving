using System;

namespace UltimateReplay
{
    /// <summary>
    /// Use this attribute to mark a method declared in a <see cref="ReplayBehaviour"/> script as recordable.
    /// The target method must not return a value and must only use primitive parameter types up to a limit of 4 arguments.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class ReplayMethodAttribute : Attribute
    {
        // Empty class
    }
}
