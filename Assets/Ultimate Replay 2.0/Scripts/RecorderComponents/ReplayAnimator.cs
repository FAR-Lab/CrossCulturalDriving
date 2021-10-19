using System;
using UltimateReplay.Serializers;
using UltimateReplay.Statistics;
using UnityEngine;

namespace UltimateReplay
{
    /// <summary>
    /// A recorder component intended to record and replay the animation state of an Animator controller.
    /// </summary>
    [ReplaySerializer(typeof(ReplayAnimatorSerializer))]
    [DisallowMultipleComponent]
    public class ReplayAnimator : ReplayRecordableBehaviour
    {
        // Types
        /// <summary>
        /// Serialize flags used to specify which elements of the component are recorded and also which features are enabled during playback.
        /// </summary>
        [Flags]
        public enum ReplayAnimatorFlags
        {
            /// <summary>
            /// The main animator state is recorded.
            /// </summary>
            MainState = 1 << 1,
            /// <summary>
            /// Any sub states are recorded.
            /// </summary>
            SubStates = 1 << 2,
            /// <summary>
            /// Animator parameters are recorded.
            /// </summary>
            Parameters = 1 << 3,
            /// <summary>
            /// Supported values are recorded in low precision mode.
            /// </summary>
            LowPrecision = 1 << 4,
            /// <summary>
            /// Animator states are interpolated during playback.
            /// </summary>
            InterpolateStates = 1 << 5,
            /// <summary>
            /// Float parameters will be interpolated during playback.
            /// </summary>
            InterpolateFloatParameters = 1 << 6,
            /// <summary>
            /// Int parameters will be interpolated during playback.
            /// </summary>
            InterpolateIntParameters = 1 << 7,
        }

        // Private
        private ReplayAnimatorSerializer serializer = new ReplayAnimatorSerializer(); // Note - use a non-shared serialize so that we dont need to allocate arrays for each snapshot
        private float initialAnimatorSpeed = 0f;

        private ReplayAnimatorSerializer.ReplayAnimatorState[] targetStates = null;
        private ReplayAnimatorSerializer.ReplayAnimatorState[] lastStates = null;
        private ReplayAnimatorSerializer.ReplayAnimatorParameter[] targetParameters = null;
        private ReplayAnimatorSerializer.ReplayAnimatorParameter[] lastParameters = null;

        private ReplayAnimatorSerializer.ReplayAnimatorSerializeFlags updateFlags = 0;

        // Public
        /// <summary>
        /// The Animator component to record and replay.
        /// </summary>
        public Animator observedAnimator = null;

        /// <summary>
        /// The record flags for the component.
        /// </summary>
        [HideInInspector]
        public ReplayAnimatorFlags recordFlags = ReplayAnimatorFlags.MainState
            | ReplayAnimatorFlags.SubStates
            | ReplayAnimatorFlags.Parameters
            | ReplayAnimatorFlags.InterpolateStates
            | ReplayAnimatorFlags.InterpolateIntParameters
            | ReplayAnimatorFlags.InterpolateFloatParameters;

        // Methods
        /// <summary>
        /// Called by Unity.
        /// </summary>
        public void Start()
        {
            if (observedAnimator == null)
                Debug.LogWarningFormat("Replay animator '{0}' will not record or replay because the observed animator has not been assigned", this);            
        }

        /// <summary>
        /// Called by Unity editor.
        /// </summary>
        public override void Reset()
        {
            // Call base method
            base.Reset();

            // Try to auto-find animator component when adding recorder component
            if (observedAnimator == null)
                observedAnimator = GetComponent<Animator>();
        }

        /// <summary>
        /// Called by the replay system when preserved data should be reset.
        /// </summary>
        public override void OnReplayReset()
        {
            lastStates = targetStates;
            lastParameters = targetParameters;
        }

        /// <summary>
        /// Called by the replay system when playback is about to begin.
        /// </summary>
        public override void OnReplayStart()
        {
            // CHeck for component
            if (observedAnimator == null)
                return;

            // Get the current animator speed
            initialAnimatorSpeed = observedAnimator.speed;
        }

        /// <summary>
        /// Called by the rpelay system when playback will end.
        /// </summary>
        public override void OnReplayEnd()
        {
            // Check for component
            if (observedAnimator == null)
                return;

            // Restore the original animator speed
            observedAnimator.speed = initialAnimatorSpeed;
        }

        /// <summary>
        /// Called by the replay system when playback will be paused or resumed.
        /// </summary>
        /// <param name="paused">True if playback is pausing or false if it is resuming</param>
        public override void OnReplayPlayPause(bool paused)
        {
            // Check for component
            if (observedAnimator == null)
                return;

            if (paused == true)
            {
                // Disable animator causing pause
                observedAnimator.enabled = false;
            }
        }

        /// <summary>
        /// Called by the replay system when the playback will be updated.
        /// Use this method to perform interpolation and smoothing processes.
        /// </summary>
        /// <param name="replayTime">The <see cref="ReplayTime"/> for the playback operation</param>
        public override void OnReplayUpdate(ReplayTime replayTime)
        {
            // Check for component
            if (observedAnimator == null)// || observedAnimator.enabled == false)
                return;

            // Make sure the animator is active and speed is set to 0
            observedAnimator.enabled = true;
            observedAnimator.speed = 0f;


            // Check for interpolated states
            bool interpolateStates = (recordFlags & ReplayAnimatorFlags.InterpolateStates) != 0;

            // Update all states
            UpdateStates(replayTime, updateFlags, interpolateStates);


            // Check for interpolated parameters
            bool interpolateIntParams = (recordFlags & ReplayAnimatorFlags.InterpolateIntParameters) != 0;
            bool interpolateFloatParams = (recordFlags & ReplayAnimatorFlags.InterpolateFloatParameters) != 0;

            // Update parameters
            if((recordFlags & ReplayAnimatorFlags.Parameters) != 0)
                UpdateParameters(replayTime, updateFlags, interpolateIntParams, interpolateFloatParams);
        }
        
        /// <summary>
        /// Called by the replay system when recorded data should be captured and serialized.
        /// </summary>
        /// <param name="state">The <see cref="ReplayState"/> used to store the recorded data</param>
        public override void OnReplaySerialize(ReplayState state)
        {
            // Check for no component
            if (observedAnimator == null || observedAnimator.enabled == false)
                return;

            // Check for playing
            if (Application.isPlaying == false)
            {
                ReplayRecordableStatistics.SupressStatisticsDuringEditMode();
                return;
            }

            // Set storage flags
            serializer.SerializeFlags = (ReplayAnimatorSerializer.ReplayAnimatorSerializeFlags)recordFlags;

            // Update serializer elements
            int requiredLayers = observedAnimator.layerCount;
            int requiredParameters = observedAnimator.parameterCount;

            // Allocate arrays if required - should only occur once
            if (serializer.States.Length != requiredLayers) serializer.States = new ReplayAnimatorSerializer.ReplayAnimatorState[requiredLayers];
            if (serializer.Parameters.Length != requiredParameters) serializer.Parameters = new ReplayAnimatorSerializer.ReplayAnimatorParameter[requiredParameters];

            // Record animator states
            for(int i = 0; i < requiredLayers; i++)
            {
                // Get the observed animator state info
                AnimatorStateInfo animState = observedAnimator.GetCurrentAnimatorStateInfo(i);
                
                // Create the layer state info
                serializer.States[i] = new ReplayAnimatorSerializer.ReplayAnimatorState
                {
                    stateHash = animState.fullPathHash,
                    normalizedTime = animState.normalizedTime,
                    speed = animState.speed,
                    speedMultiplier = animState.speedMultiplier,                    
                };
            }

            // Record animator parameters
            for(int i = 0; i < requiredParameters; i++)
            {
                // Get the observed animator parameter info
                AnimatorControllerParameter animParam = observedAnimator.GetParameter(i);

                switch(animParam.type)
                {
                    case AnimatorControllerParameterType.Bool:
                        {
                            serializer.Parameters[i] = new ReplayAnimatorSerializer.ReplayAnimatorParameter
                            {
                                nameHash = animParam.nameHash,
                                parameterType = AnimatorControllerParameterType.Bool,
                                boolValue = observedAnimator.GetBool(animParam.name),
                            };
                            break;
                        }

                    case AnimatorControllerParameterType.Trigger:
                        {
                            serializer.Parameters[i] = new ReplayAnimatorSerializer.ReplayAnimatorParameter
                            {
                                nameHash = animParam.nameHash,
                                parameterType = AnimatorControllerParameterType.Trigger,
                                boolValue = observedAnimator.GetBool(animParam.name),
                            };
                            break;
                        }

                    case AnimatorControllerParameterType.Int:
                        {
                            serializer.Parameters[i] = new ReplayAnimatorSerializer.ReplayAnimatorParameter
                            {
                                nameHash = animParam.nameHash,
                                parameterType = AnimatorControllerParameterType.Int,
                                intValue = observedAnimator.GetInteger(animParam.name),
                            };
                            break;
                        }

                    case AnimatorControllerParameterType.Float:
                        {
                            serializer.Parameters[i] = new ReplayAnimatorSerializer.ReplayAnimatorParameter
                            {
                                nameHash = animParam.nameHash,
                                parameterType = AnimatorControllerParameterType.Float,
                                floatValue = observedAnimator.GetFloat(animParam.name),
                            };
                            break;
                        }
                }
            }

            // Run the serializer
            serializer.OnReplaySerialize(state);
        }

        /// <summary>
        /// Called by the replay system when replay data should be deserialized and restored.
        /// </summary>
        /// <param name="state">The <see cref="ReplayState"/> containing the previously recorded data</param>
        public override void OnReplayDeserialize(ReplayState state)
        {
            // Check for no component
            if (observedAnimator == null || observedAnimator.enabled == false)
                return;

            // Update last values
            OnReplayReset();

            // Run serializer
            serializer.OnReplayDeserialize(state);

            // Get flags
            updateFlags = serializer.SerializeFlags;

            // Fetch elements
            targetStates = serializer.States;
            targetParameters = serializer.Parameters;
        }

        private void UpdateStates(ReplayTime time, ReplayAnimatorSerializer.ReplayAnimatorSerializeFlags flags, bool interpolate)
        {
            if (targetStates == null)
                return;

            for(int i = 0; i < targetStates.Length; i++)
            {
                // Get the target state
                ReplayAnimatorSerializer.ReplayAnimatorState updateState = targetStates[i];

                // Interpolate values
                if(interpolate == true && lastStates != null)
                {
                    ReplayAnimatorSerializer.ReplayAnimatorState lastState = lastStates[i];

                    // Interpolate supported values
                    updateState.normalizedTime = Mathf.Lerp(lastState.normalizedTime, updateState.normalizedTime, time.Delta);
                }
                

                // Play the animator
                observedAnimator.Play(updateState.stateHash, i, updateState.normalizedTime);
            }
        }

        private void UpdateParameters(ReplayTime time, ReplayAnimatorSerializer.ReplayAnimatorSerializeFlags flags, bool interpolateIntegers, bool interpolateFloats)
        {
            if (targetParameters == null)
                return;

            for(int i = 0; i < targetParameters.Length; i++)
            {
                // Get the target parameter
                ReplayAnimatorSerializer.ReplayAnimatorParameter updateParameter = targetParameters[i];

                // Interpolate int values
                if(interpolateIntegers == true && updateParameter.parameterType == AnimatorControllerParameterType.Int && lastParameters != null)
                {
                    ReplayAnimatorSerializer.ReplayAnimatorParameter lastParameter = lastParameters[i];

                    // Interpolate int parameters
                    updateParameter.intValue = Mathf.RoundToInt(Mathf.Lerp(lastParameter.intValue, updateParameter.intValue, time.Delta));
                }

                // Interpolate float values
                if(interpolateFloats == true && updateParameter.parameterType == AnimatorControllerParameterType.Float && lastParameters != null)
                {
                    ReplayAnimatorSerializer.ReplayAnimatorParameter lastParameter = lastParameters[i];

                    // Inteprolaate float parameters
                    updateParameter.floatValue = Mathf.Lerp(lastParameter.floatValue, updateParameter.floatValue, time.Delta);
                }

                // Update the animator - Note that tirggers are handled in deserialize
                switch(updateParameter.parameterType)
                {
                    case AnimatorControllerParameterType.Bool:
                        {
                            observedAnimator.SetBool(updateParameter.nameHash, updateParameter.boolValue);
                            break;
                        }

                    case AnimatorControllerParameterType.Int:
                        {
                            observedAnimator.SetInteger(updateParameter.nameHash, updateParameter.intValue);
                            break;
                        }

                    case AnimatorControllerParameterType.Float:
                        {
                            observedAnimator.SetFloat(updateParameter.nameHash, updateParameter.floatValue);
                            break;
                        }
                }
            }
        }
    }
}
