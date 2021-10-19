using System;
using UnityEngine;

namespace UltimateReplay
{
    /// <summary>
    /// Use this attribute on a field to mark it for recording.
    /// The type the field is defined in must inheit from <see cref="ReplayBehaviour"/> in order for the field to be recorded automatically.
    /// Interpolation between field values is also possible where low record rates are used. 
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class ReplayVarAttribute : Attribute
    {
        // Public
        /// <summary>
        /// Should the value of the field be interpolated between frames or should the value snap to the exact frame value.
        /// Most built-in types support interpolation such as <see cref="byte"/> and <see cref="float"/>. Basic Unity types such as <see cref="Vector2"/> and <see cref="Color"/> also support interpolation.    
        /// </summary>
        public bool interpolate = true;

        // Constructor
        /// <summary>
        /// Create a new <see cref="ReplayVarAttribute"/> for a field.
        /// </summary>
        /// <param name="interpolated">Should the field value be interpolated between frames</param>
        public ReplayVarAttribute(bool interpolated = true)
        {
            this.interpolate = interpolated;
        }
    }
}
