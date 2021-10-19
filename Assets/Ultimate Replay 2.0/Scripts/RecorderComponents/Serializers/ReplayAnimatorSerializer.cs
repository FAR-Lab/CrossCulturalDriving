using System;
using UltimateReplay.Core;
using UnityEngine;

namespace UltimateReplay.Serializers
{
    /// <summary>
    /// Used to serialize data related to the <see cref="ReplayAnimator"/> component.
    /// </summary>
    public sealed class ReplayAnimatorSerializer : IReplaySerialize
    {
        // Types
        /// <summary>
        /// Serialize flags used to indicate which data elements are stored.
        /// </summary>
        [Flags]
        public enum ReplayAnimatorSerializeFlags : ushort
        {
            /// <summary>
            /// The main state layer data will be serialized.
            /// </summary>
            MainState = 1 << 1,
            /// <summary>
            /// Sub state layers will be serialized.
            /// </summary>
            SubStates = 1 << 2,
            /// <summary>
            /// Parameter values will be serialized.
            /// </summary>
            Parameters = 1 << 3,
            /// <summary>
            /// Supported data elements will be serialized using low precision mode.
            /// </summary>
            LowPrecision = 1 << 4,
        }

        /// <summary>
        /// Contains data about a specific animator state.
        /// </summary>
        public struct ReplayAnimatorState
        {
            // Public
            /// <summary>
            /// The hash of the current animator state.
            /// </summary>
            public int stateHash;
            /// <summary>
            /// The normalized playback time of the current animation.
            /// </summary>
            public float normalizedTime;
            /// <summary>
            /// The current speed of the animation.
            /// </summary>
            public float speed;
            /// <summary>
            /// The current speed multipier value.
            /// </summary>
            public float speedMultiplier;
        }

        /// <summary>
        /// Contains data about a specific animtor parameter.
        /// </summary>
        public struct ReplayAnimatorParameter
        {
            // Public
            /// <summary>
            /// The name hash of the parameter.
            /// </summary>
            public int nameHash;
            /// <summary>
            /// The <see cref="AnimatorControllerParameterType"/> which describes the type of parameter.
            /// </summary>
            public AnimatorControllerParameterType parameterType;
            /// <summary>
            /// The integer value of the parameter.
            /// </summary>
            public int intValue;
            /// <summary>
            /// The float value of the parameter.
            /// </summary>
            public float floatValue;
            /// <summary>
            /// The bool value of the parameter.
            /// </summary>
            public bool boolValue;
        }

        // Private
        private ReplayAnimatorSerializeFlags serializeFlags = 0;
        private ReplayAnimatorState[] states = new ReplayAnimatorState[0];
        private ReplayAnimatorParameter[] parameters = new ReplayAnimatorParameter[0];

        // Properties
        /// <summary>
        /// The current <see cref="ReplayAnimatorSerializeFlags"/> used by this serializer. 
        /// You should set this value manually prior to serializing data. 
        /// This value will be automatically filled after deserialization.
        /// </summary>
        public ReplayAnimatorSerializeFlags SerializeFlags
        {
            get { return serializeFlags; }
            set { serializeFlags = value; }
        }

        /// <summary>
        /// Get the <see cref="ReplayAnimatorState"/> information for the main state.
        /// </summary>
        public ReplayAnimatorState MainState
        {
            get
            {
                if (states.Length == 0)
                    return new ReplayAnimatorState();

                return states[0];
            }
            set
            {
                if (states.Length == 0)
                    states = new ReplayAnimatorState[1];

                states[0] = value;
            }
        }

        /// <summary>
        /// Get all <see cref="ReplayAnimatorState"/> information for all sub states.
        /// </summary>
        public ReplayAnimatorState[] States
        {
            get { return states; }
            set
            {
                states = value;

                if (states == null)
                    states = new ReplayAnimatorState[0];
            }
        }

        /// <summary>
        /// Get all <see cref="ReplayAnimatorParameter"/> that will be serialized.
        /// </summary>
        public ReplayAnimatorParameter[] Parameters
        {
            get { return parameters; }
            set
            {
                parameters = value;

                if (parameters == null)
                    parameters = new ReplayAnimatorParameter[0];
            }
        }

        // Methods
        /// <summary>
        /// Invoke this method to serialize the animator data to the specified <see cref="ReplayState"/>.
        /// </summary>
        /// <param name="state">The state object used to store the data</param>
        public void OnReplaySerialize(ReplayState state)
        {
            // Write flags
            state.Write((ushort)serializeFlags);

            // Check if main state or sub states are recorded
            if ((serializeFlags & ReplayAnimatorSerializeFlags.MainState) != 0 || (serializeFlags & ReplayAnimatorSerializeFlags.SubStates) != 0)
            {
                int writeStateCount = 0;

                if ((serializeFlags & ReplayAnimatorSerializeFlags.MainState) != 0) writeStateCount++;
                if ((serializeFlags & ReplayAnimatorSerializeFlags.SubStates) != 0) writeStateCount += states.Length - 1;

                // Write the states
                state.Write((ushort)writeStateCount);

                // Write all states
                for (int i = 0; i < writeStateCount; i++)
                {
                    // Get the state data
                    ReplayAnimatorState animState = states[i];

                    // Has cannot be low precision because it could invalidate it
                    state.Write(animState.stateHash);

                    // Check for low precision time values
                    if ((serializeFlags & ReplayAnimatorSerializeFlags.LowPrecision) != 0)
                    {
                        state.WriteLowPrecision(animState.normalizedTime);
                    }
                    else
                    {
                        state.Write(animState.normalizedTime);
                    }
                }
            }


            // Write parameters
            if ((serializeFlags & ReplayAnimatorSerializeFlags.Parameters) != 0)
            {
                state.Write((ushort)parameters.Length);

                // Write all parameters
                for (int i = 0; i < parameters.Length; i++)
                {
                    // Get the parameter data
                    ReplayAnimatorParameter animParam = parameters[i];

                    // Write the name hash
                    state.Write(animParam.nameHash);

                    // Write the type
                    state.Write((byte)animParam.parameterType);

                    switch(animParam.parameterType)
                    {
                        case AnimatorControllerParameterType.Bool:
                        case AnimatorControllerParameterType.Trigger:
                            {
                                state.Write(animParam.boolValue);
                                break;
                            }

                        case AnimatorControllerParameterType.Float:
                            {
                                if((serializeFlags & ReplayAnimatorSerializeFlags.LowPrecision) != 0)
                                {
                                    state.WriteLowPrecision(animParam.floatValue);
                                }
                                else
                                {
                                    state.Write(animParam.floatValue);
                                }
                                break;
                            }

                        case AnimatorControllerParameterType.Int:
                            {
                                state.Write(animParam.intValue);
                                break;
                            }
                    }
                }
            }
        }

        /// <summary>
        /// Invoke this method to deserialize the animator data from the specified <see cref="ReplayState"/>.
        /// </summary>
        /// <param name="state">The state object which should contain valid animator data</param>
        public void OnReplayDeserialize(ReplayState state)
        {
            // Read flags
            serializeFlags = (ReplayAnimatorSerializeFlags)state.ReadUInt16();
            
            // Check if main state or sub states are recorded
            if ((serializeFlags & ReplayAnimatorSerializeFlags.MainState) != 0 || (serializeFlags & ReplayAnimatorSerializeFlags.SubStates) != 0)
            {
                // Read the number of states
                int readStateCount = state.ReadUInt16();

                // Allocate array
                states = new ReplayAnimatorState[readStateCount];

                // Read all states
                for (int i = 0; i < readStateCount; i++)
                {
                    // Create the state data
                    ReplayAnimatorState animState = new ReplayAnimatorState();

                    // Has cannot be low precision because it could invalidate it
                    animState.stateHash = state.ReadInt32();

                    // Check for low precision time values
                    if ((serializeFlags & ReplayAnimatorSerializeFlags.LowPrecision) != 0)
                    {
                        animState.normalizedTime = state.ReadFloatLowPrecision();
                    }
                    else
                    {
                        animState.normalizedTime = state.ReadFloat();
                    }

                    // Fill element
                    states[i] = animState;
                }
            }


            // Read parameters
            if ((serializeFlags & ReplayAnimatorSerializeFlags.Parameters) != 0)
            {
                // Read the number of parameters
                int readParamterCount = state.ReadUInt16();

                // Check if we need to allocate the array
                if (parameters.Length != readParamterCount)
                {
                    // Allocate array
                    parameters = new ReplayAnimatorParameter[readParamterCount];
                }
                else
                {
                    // Clear the array for reuse
                    Array.Clear(parameters, 0, parameters.Length);
                }

                // Read all parameters
                for (int i = 0; i < readParamterCount; i++)
                {
                    // Create the parameter data
                    ReplayAnimatorParameter animParam = new ReplayAnimatorParameter();

                    // Read the name hash
                    animParam.nameHash = state.ReadInt32();

                    // Read the type
                    animParam.parameterType = (AnimatorControllerParameterType)state.ReadByte();
                    
                    switch (animParam.parameterType)
                    {
                        case AnimatorControllerParameterType.Bool:
                        case AnimatorControllerParameterType.Trigger:
                            {
                                animParam.boolValue = state.ReadBool();
                                break;
                            }

                        case AnimatorControllerParameterType.Float:
                            {
                                if ((serializeFlags & ReplayAnimatorSerializeFlags.LowPrecision) != 0)
                                {
                                    animParam.floatValue = state.ReadFloatLowPrecision();
                                }
                                else
                                {
                                    animParam.floatValue = state.ReadFloat();
                                }
                                break;
                            }

                        case AnimatorControllerParameterType.Int:
                            {
                                animParam.intValue = state.ReadInt32();
                                break;
                            }
                    }

                    parameters[i] = animParam;
                }
            }
        }
    }
}
