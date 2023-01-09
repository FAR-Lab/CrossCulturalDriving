using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;

namespace UltimateReplay.Core
{
    /// <summary>
    /// Represents a variable that can be recorded using the replay system in order to replay script animations or similar during playback.
    /// </summary>
    public sealed class ReplayVariable : IReplaySerialize
    {
        // Private
        private static readonly Dictionary<Type, Func<object, object, float, object>> interpolators = new Dictionary<Type, Func<object, object, float, object>>
        {
            // Built in types
            { typeof(byte), InterpolateByte },
            { typeof(short), InterpolateShort },
            { typeof(int), InterpolateInt },
            { typeof(long), InterpolateLong },
            { typeof(float), InterpolateFloat },
            { typeof(double), InterpolateDouble },

            // Unity types
            { typeof(Vector2), InterpolateVec2 },
            { typeof(Vector3), InterpolateVec3 },
            { typeof(Vector4), InterpolateVec4 },
            { typeof(Quaternion), InterpolateQuat },
            { typeof(Color), InterpolateColor },
            { typeof(Color32), InterpolateColor32 },
        };

        private ReplayBehaviour owner = null;
        private ReplayVarAttribute attribute = null;
        private FieldInfo field = null;
        private MethodInfo serializeMethod = null;
        private MethodInfo deserializeMethod = null;
        private bool isInterpolationSupported = false;
        private object last = null;
        private object next = null;

        private int cachedOffset = -1;

        // Properties
        /// <summary>
        /// Get the game object that this <see cref="ReplayVariable"/> is attached to. 
        /// </summary>
        public GameObject gameObject
        {
            get { return owner.gameObject; }
        }

        /// <summary>
        /// Get the <see cref="ReplayBehaviour"/> that this variable belongs to.
        /// </summary>
        public ReplayBehaviour Behaviour
        {
            get { return owner; }
        }

        /// <summary>
        /// Get the managed field offset value to uniquely identify the variable.
        /// </summary>
        public int FieldOffset
        {
            get 
            {
                if (cachedOffset == -1)
                {
                    FieldInfo[] fields = field.DeclaringType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);

                    cachedOffset = Array.IndexOf(fields, field);
                }
                return cachedOffset;

                //return Marshal.ReadInt32(field.FieldHandle.Value); 
            }
        }

        /// <summary>
        /// The current value for this <see cref="ReplayVariable"/>. 
        /// </summary>
        public object Value
        {
            get { return field.GetValue(owner); }
            set { field.SetValue(owner, value); }
        }

        /// <summary>
        /// Get the <see cref="ReplayVarAttribute"/> associated with this <see cref="ReplayVariable"/>.  
        /// </summary>
        public ReplayVarAttribute Attribute
        {
            get { return attribute; }
        }

        /// <summary>
        /// Get the name of this <see cref="ReplayVariable"/>. 
        /// </summary>
        public string Name
        {
            get { return field.Name; }
        }

        /// <summary>
        /// Returns true if this <see cref="ReplayVariable"/> should be interpolated between frames. 
        /// </summary>
        public bool IsInterpolated
        {
            get { return attribute.interpolate; }
        }

        /// <summary>
        /// Returns true if this <see cref="ReplayVariable"/> supports interpolation. 
        /// Interpolation can only be supported if the variable type has a registered interpolator.
        /// </summary>
        public bool IsInterpolationSupported
        {
            get { return isInterpolationSupported; }
        }

        // Constructor
        /// <summary>
        /// Create a new <see cref="ReplayVariable"/>. 
        /// </summary>
        /// <param name="owner">The <see cref="ReplayBehaviour"/> that this <see cref="ReplayVariable"/> is defined in</param>
        /// <param name="field">The field info for the variable field</param>
        /// <param name="attribute">The <see cref="ReplayVarAttribute"/> for the field</param>
        public ReplayVariable(ReplayBehaviour owner, FieldInfo field, ReplayVarAttribute attribute)
        {
            this.owner = owner;
            this.field = field;
            this.attribute = attribute;

            // Check if interpolation is supported
            this.isInterpolationSupported = CanInterpolate(field.FieldType);

            this.serializeMethod = ReplayState.GetSerializeMethod(field.FieldType);
            this.deserializeMethod = ReplayState.GetDeserializeMethod(field.FieldType);

            if (serializeMethod == null || deserializeMethod == null)
                throw new NotSupportedException("The replay variable references a field which has a type that cannot be serialized");
        }

        // Methods
        /// <summary>
        /// Called by the replay system when the variable should be serialized.
        /// </summary>
        /// <param name="state">The <see cref="ReplayState"/> to serialize the data into</param>
        public void OnReplaySerialize(ReplayState state)
        {
            try
            {
                // Try to write the field value to the replay state using the cached serialize method
                serializeMethod.Invoke(state, new object[] { Value });
            }
            catch { }
        }

        /// <summary>
        /// Called by the replay system when the variable should be deserialized.
        /// </summary>
        /// <param name="state">The <see cref="ReplayState"/> to deserialize the data from</param>
        public void OnReplayDeserialize(ReplayState state)
        {
            try
            {
                // Update interpolation
                object last = Value;

                // Read the current field value
                object next = deserializeMethod.Invoke(state, null);

                // Make sure we read a valid value
                if (next != null)
                {
                    // Assign the actual value
                    Value = next;

                    // Prepare interpolation values
                    UpdateValueRange(last, next);
                }
            }
            catch { }
        }

        /// <summary>
        /// Sets the current interpolation range for the <see cref="ReplayVariable"/> value. 
        /// </summary>
        /// <param name="last">The value of the variable in the last frame</param>
        /// <param name="next">The value of the variable in the next frame</param>
        public void UpdateValueRange(object last, object next)
        {
            // Get the current value
            this.last = last;
            this.next = next;
        }

        /// <summary>
        /// Attempts to interpolate the <see cref="ReplayVariable"/> value using the values from the last and next frame. 
        /// </summary>
        /// <param name="delta">The normalized delta representing the progression from the last frame to the next frame</param>
        public void Interpolate(float delta)
        {
            // Make sure we can interpolate
            if (IsInterpolationSupported == false || IsInterpolated == false)
                return;

            // Make sure we have a last and next value
            if (last != null && next != null)
                Value = InterpolateValue(last, next, delta);
        }

        /// <summary>
        /// Attempts to interpolate the <see cref="ReplayVariable"/> value using the values from the last and next frame. 
        /// In order for interpolation to succeed, the last and next values must be of the same type.
        /// </summary>
        /// <param name="last">The value of the variable in the last frame</param>
        /// <param name="next">The value of the variable in the next frame</param>
        /// <param name="delta">The normalized delta representing the progression from the last frame to the next frame</param>
        /// <returns>The interpolated value result or null if interpolation is not supported for the type</returns>
        public static object InterpolateValue(object last, object next, float delta)
        {
            Type lastType = last.GetType();
            Type nextType = next.GetType();

            // Require matching types
            if (lastType != nextType)
                return null;

            // Find an interpolator for the type
            if(interpolators.ContainsKey(lastType) == true)
            {
                try
                {
                    // Call the registered interpolator method
                    return interpolators[lastType](last, next, delta);
                }
                catch(Exception e)
                {
                    // Exception in interpolator
                    Debug.LogError(string.Format("An exception occured when invoking the interpolator for type '{0}': {1}", lastType, e));
                }
            }

            // No interpolator found for the type
            return null;
        }

        /// <summary>
        /// Returns true if the specified type can be interpolated by the replay system.
        /// </summary>
        /// <param name="type">The system type to check for interpolation support</param>
        /// <returns>True if interpolation is supported or faluse if it is not</returns>
        public static bool CanInterpolate(Type type)
        {
            // Check for registered interpolator
            return interpolators.ContainsKey(type);
        }

        /// <summary>
        /// Allows a custom interpolation method to be registered so that unsupported variable types can be interpolated automatically.
        /// </summary>
        /// <typeparam name="T">The type of varaible that the custom interpolation should be used for</typeparam>
        /// <param name="interpolatorFunc">The interpolation method to invoke when interpolation of the custom type is required</param>
        public static void RegisterCustomInterpolator<T>(Func<object, object, float, object> interpolatorFunc)
        {
            if (interpolators.ContainsKey(typeof(T)) == false)
            {
                // Register the interpolator
                interpolators.Add(typeof(T), interpolatorFunc);
            }
            else
            {
                // Failed to create interpolator
                Debug.LogWarning(string.Format("Failed to register custom interpolater because there is already an interpolator for '{0}'", typeof(T)));
            }
        }

        #region Interpolators
        /// <summary>
        /// Default interpolator for byte.
        /// </summary>
        /// <param name="last">Last value</param>
        /// <param name="next">Next value</param>
        /// <param name="delta">Interpolation delta</param>
        /// <returns>The interpolated byte value</returns>
        public static object InterpolateByte(object last, object next, float delta)
        {
            return (byte)Mathf.Lerp((byte)last, (byte)next, delta);
        }

        /// <summary>
        /// Default interpolator for short.
        /// </summary>
        /// <param name="last">Last value</param>
        /// <param name="next">Next value</param>
        /// <param name="delta">Interpolation delta</param>
        /// <returns>The interpolated short value</returns>
        public static object InterpolateShort(object last, object next, float delta)
        {
            return (short)Mathf.Lerp((short)last, (short)next, delta);
        }

        /// <summary>
        /// Default interpolator for int.
        /// </summary>
        /// <param name="last">Last value</param>
        /// <param name="next">Next value</param>
        /// <param name="delta">Interpolation delta</param>
        /// <returns>The interpolated int value</returns>
        public static object InterpolateInt(object last, object next, float delta)
        {
            return (int)Mathf.Lerp((int)last, (int)next, delta);
        }

        /// <summary>
        /// Default interpolator for long.
        /// </summary>
        /// <param name="last">Last value</param>
        /// <param name="next">Next value</param>
        /// <param name="delta">Interpolation delta</param>
        /// <returns>The interpolated long value</returns>
        public static object InterpolateLong(object last, object next, float delta)
        {
            return (long)Mathf.Lerp((long)last, (long)next, delta);
        }

        /// <summary>
        /// Default interpolator for float.
        /// </summary>
        /// <param name="last">Last value</param>
        /// <param name="next">Next value</param>
        /// <param name="delta">Interpolation delta</param>
        /// <returns>The interpolated float value</returns>
        public static object InterpolateFloat(object last, object next, float delta)
        {
            return Mathf.Lerp((float)last, (float)next, delta);
        } 

        /// <summary>
        /// Default interpolator for double.
        /// </summary>
        /// <param name="last">Last value</param>
        /// <param name="next">Next value</param>
        /// <param name="delta">Interpolation delta</param>
        /// <returns>The interpolated double value</returns>
        public static object InterpolateDouble(object last, object next, float delta)
        {
            return (double)Mathf.Lerp((float)last, (float)next, delta);
        }

        /// <summary>
        /// Default interpolator for Vector2.
        /// </summary>
        /// <param name="last">Last value</param>
        /// <param name="next">Next value</param>
        /// <param name="delta">Interpolation delta</param>
        /// <returns>The interpolated Vector2 value</returns>
        public static object InterpolateVec2(object last, object next, float delta)
        {
            return Vector2.Lerp((Vector2)last, (Vector2)next, delta);
        }

        /// <summary>
        /// Default interpolator for Vector3.
        /// </summary>
        /// <param name="last">Last value</param>
        /// <param name="next">Next value</param>
        /// <param name="delta">Interpolation delta</param>
        /// <returns>The interpolated Vector3 value</returns>
        public static object InterpolateVec3(object last, object next, float delta)
        {
            return Vector3.Lerp((Vector3)last, (Vector3)next, delta);
        }

        /// <summary>
        /// Default interpolator for Vector4.
        /// </summary>
        /// <param name="last">Last value</param>
        /// <param name="next">Next value</param>
        /// <param name="delta">Interpolation delta</param>
        /// <returns>The interpolated Vector4 value</returns>
        public static object InterpolateVec4(object last, object next, float delta)
        {
            return Vector4.Lerp((Vector4)last, (Vector4)next, delta);
        }

        /// <summary>
        /// Default interpolator for Quaternion.
        /// </summary>
        /// <param name="last">Last value</param>
        /// <param name="next">Next value</param>
        /// <param name="delta">Interpolation delta</param>
        /// <returns>The interpolated Quaternion value</returns>
        public static object InterpolateQuat(object last, object next, float delta)
        {
            Quaternion a = (Quaternion)last;
            Quaternion b = (Quaternion)next;

            if (a == b)
                return a;

            // Cannot lerp to identity
            if (b.x == 0 && b.y == 0 && b.z == 0 && b.w == 0)
                return a;

            return Quaternion.Lerp(a, b, delta);
        }

        /// <summary>
        /// Default interpolator for Color.
        /// </summary>
        /// <param name="last">Last value</param>
        /// <param name="next">Next value</param>
        /// <param name="delta">Interpolation delta</param>
        /// <returns>The interpolated Color value</returns>
        public static object InterpolateColor(object last, object next, float delta)
        {
            return Color.Lerp((Color)last, (Color)next, delta);
        }

        /// <summary>
        /// Default interpolator for Color32.
        /// </summary>
        /// <param name="last">Last value</param>
        /// <param name="next">Next value</param>
        /// <param name="delta">Interpolation delta</param>
        /// <returns>The interpolated Color32 value</returns>
        public static object InterpolateColor32(object last, object next, float delta)
        {
            return Color32.Lerp((Color32)last, (Color32)next, delta);
        }

        #endregion
    }
}
