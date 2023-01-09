using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UltimateReplay.Core;

#if MIRROR
using Mirror;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UltimateReplay
{
    /// <summary>
    /// This interface can be implemented by mono behaviour scripts in order to receive replay start and end events.
    /// It works in a similar way to the 'Start' or 'Update' method however you must explicitly implement the interface as opposed to using magic methods.
    /// This allows for slightly improved performance.
    /// </summary>
#if MIRROR && !ULTIMATEREPLAY_SUPPRESS_MIRROR
    public abstract class ReplayBehaviour : NetworkBehaviour
#else
    public abstract class ReplayBehaviour : MonoBehaviour
#endif
    {
        // Types
        internal enum ReplayBehaviourState
        {
            Idle,
            Recording,
            Replaying,
        }

        // Internal
        internal ReplayHandle behaviourHandle = ReplayHandle.invalid;
#if UNITY_EDITOR
        [HideInInspector]
        public bool noManagingObject = false; // Now marked as public to support asmdef
#endif

        // Private
        private static HashSet<ReplayBehaviour> allBehaviours = new HashSet<ReplayBehaviour>();

        [SerializeField]
        private ReplayIdentity replayIdentity = new ReplayIdentity();

        [SerializeField]
        private ReplayObject.ReplayObjectReference replayObject = new ReplayObject.ReplayObjectReference();
        private List<ReplayVariable> variables = null;
        private int variablesCount = -1;

        // Properties 
        /// <summary>
        /// Get the <see cref="Core.ReplayIdentity"/> associated with this <see cref="ReplayBehaviour"/>.  
        /// </summary>
        public ReplayIdentity ReplayIdentity
        {
            get { return replayIdentity; }
            set { replayIdentity = value; }
        }

        /// <summary>
        /// Get the managing <see cref="ReplayObject"/>.
        /// </summary>
        public ReplayObject ReplayObject
        {
            get { return replayObject.reference; }
        }

        /// <summary>
        /// Get all <see cref="ReplayVariable"/> associated with this <see cref="ReplayBehaviour"/>.
        /// </summary>
        public IList<ReplayVariable> Variables
        {
            get
            {
                if(variablesCount == -1)
                {
                    foreach(FieldInfo field in GetType().GetFields())
                    {
                        // Check for replay var attribute
                        if(field.IsDefined(typeof(ReplayVarAttribute), false) == true)
                        {
                            // Get the attribute
                            ReplayVarAttribute attribute = (ReplayVarAttribute)field.GetCustomAttributes(typeof(ReplayVarAttribute), false)[0];

                            // Create a new variable
                            ReplayVariable variable = new ReplayVariable(this, field, attribute);

                            // Add to collection
                            if (variables == null)
                                variables = new List<ReplayVariable>();

                            // Register the variable
                            variables.Add(variable);
                        }
                    }

                    // Cache item count
                    variablesCount = 0;

                    if(variables != null)
                        variablesCount = variables.Count;
                }

                return variables;
            }
        }

        /// <summary>
        /// Returns a value indicating whether this <see cref="ReplayObject"/> has any <see cref="ReplayVariable"/>.
        /// </summary>
        public bool HasVariables
        {
            get
            {
                // Check fi variables have been initialized
                if (variablesCount != -1)
                    return variablesCount > 0;

                // Initialize by accessing the collection
                return Variables != null;
            }
        }

        /// <summary>
        /// Returns true if the active replay manager is currently recording the scene.
        /// Note: If recording is paused this value will still be true.
        /// </summary>
        public bool IsRecording
        {
            get
            {
                // Check for invalid handle
                if (behaviourHandle.IsDisposed == true)
                    return false;

                // Check handle type
                return behaviourHandle.ReplayType == ReplayHandle.ReplayHandleType.Record;
            }
        }

        /// <summary>
        /// Returns true if the active replay manager is currently replaying a previous recording.
        /// Note: If playback is paused this value will still be true.
        /// </summary>
        public bool IsReplaying
        {
            get
            {
                // Check for invalid handle
                if (behaviourHandle.IsDisposed == true)
                    return false;

                // Check handle type
                return behaviourHandle.ReplayType == ReplayHandle.ReplayHandleType.Replay;
            }
        }

        /// <summary>
        /// Get the current playback time in seconds.
        /// This <see cref="ReplayBehaviour"/> must be attached to an object that is currently being replayed for this value to be valid.
        /// </summary>
        public ReplayTime PlaybackTime
        {
            get
            {
                // Check for invalid handle
                if (behaviourHandle.IsDisposed == true)
                    return new ReplayTime();

                // Check handle type
                return ReplayManager.GetPlaybackTime(behaviourHandle);
            }
        }

        /// <summary>
        /// Gets the current <see cref="PlaybackDirection"/> of replay playback.
        /// </summary>
        public ReplayManager.PlaybackDirection PlaybackDirection
        {
            get
            {
                // Check for invalid handle
                if (behaviourHandle.IsDisposed == true)
                    return ReplayManager.PlaybackDirection.Forward;

                // Check handle type
                ReplayTime time = ReplayManager.GetPlaybackTime(behaviourHandle);

                // Get the time scale direction
                return time.TimeScaleDirection;
            }
        }

        // Methods
        /// <summary>
        /// Called by Unity while in editor mode.
        /// Allows the unique id to be generated when the script is attached to an object.
        /// </summary>
        public virtual void Reset()
        {
            // Try to find a parent replay object or create one if required
#if UNITY_EDITOR
            // Check for ignored components
            if (GetType().IsDefined(typeof(ReplayIgnoreAttribute), true) == true)
                return;

            // Check for no manager
            if (noManagingObject == true)
                return;

            ReplayObject manager = GetManagingObject();

            // Create a manager component
            if (manager == null)
                manager = Undo.AddComponent<ReplayObject>(gameObject);

            // Force rebuild the component list
            if (manager != null)
            {
                this.replayObject = new ReplayObject.ReplayObjectReference(manager);
                manager.RebuildComponentList();
            }
#endif
        }

        /// <summary>
        /// Called by Unity.
        /// </summary>
        public virtual void Awake()
        {
            // Make sure managing object is valid
            if (replayObject.reference == null)
                UpdateManagingObject();

            // Register with managing object
            ReplayObject.RegisterRuntimeBehaviour(this);
        }

        public virtual void OnDestroy()
        {
            // Unregister with managing object
            ReplayObject.UnregisterRuntimeBehaviour(this);
        }

        /// <summary>
        /// Called by Unity.
        /// Be sure to call this base method when overriding otherwise replay events will not be received.
        /// </summary>
        public virtual void OnEnable()
        {
            // Register this behaviour
            if(allBehaviours.Contains(this) == false)
                allBehaviours.Add(this);

            
        }

        /// <summary>
        /// Called by Unity.
        /// Be sure to call this base method when overriding otherwise replay events will not be received.
        /// </summary>
        public virtual void OnDisable()
        {
            // Un-register this behaviour
            if(allBehaviours.Contains(this) == true)
                allBehaviours.Remove(this);

            
        }

        /// <summary>
        /// Called by the replay system when playback is about to start.
        /// You can disable game behaviour that should not run during playback in this method, such as player movement.
        /// </summary>
        public virtual void OnReplayStart() { }

        /// <summary>
        /// Called by the replay system when playback has ended.
        /// You can re-enable game behaviour in this method to allow the gameplay to 'take over'
        /// </summary>
        public virtual void OnReplayEnd() { }

        /// <summary>
        /// Called by the replay system when playback is about to be paused or resumed.
        /// </summary>
        /// <param name="paused">True if playback is about to be paused or false if plyabck is about to be resumed</param>
        public virtual void OnReplayPlayPause(bool paused) { }

        /// <summary>
        /// Called by the replay system during playback when cached values should be reset to safe default to avoid glitches or inaccuracies in the playback.
        /// </summary>
        public virtual void OnReplayReset() { }

        /// <summary>
        /// Called by the replay system when non-recordable components should submit data to be recorded to the managing replay object. This method is ideal for recording variables, events, methods calls and similar.
        /// Update can be used instead however 'OnReplayCapture' is guarenteed to be called during the same frame that replay recordable data is serialized.
        /// </summary>
        public virtual void OnReplayCapture() { }

        /// <summary>
        /// Called by the replay system every frame while playback is active.
        /// </summary>
        public virtual void OnReplayUpdate(ReplayTime replayTime) { }

        /// <summary>
        /// Called by the replay system when an event has been received during playback.
        /// </summary>
        /// <param name="replayEvent">The event that was received</param>
        public virtual void OnReplayEvent(ushort eventID, ReplayState eventData) { }

        /// <summary>
        /// Called by the replay system when the object has been spawned from a prefab instance during playback.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        public virtual void OnReplaySpawned(Vector3 position, Quaternion rotation) { }

        /// <summary>
        /// Force the <see cref="ReplayIdentity"/> of this component to be regenerated.
        /// </summary>
        public void ForceRegenerateIdentity()
        {
            // Regenerate identity
            ReplayIdentity.Generate(ref replayIdentity);
        }

        /// <summary>
        /// Record the value of the specified <see cref="ReplayVariable"/>.
        /// Should only be called when <see cref="IsRecording"/> is true.
        /// The variable data will be recorded for a single frame. 
        /// To order to record a variable over time, simply call this method every frame.
        /// </summary>
        /// <param name="variable"></param>
        public void RecordVariable(ReplayVariable variable)
        {
            ReplayObject.RecordReplayVariable(replayIdentity, variable);
        }

        /// <summary>
        /// Record a replay event on the current record frame.
        /// </summary>
        /// <param name="eventID">A unique event ID value used to identify the event type</param>
        /// <param name="eventData">A replay state containing data associated with the event</param>
        public void RecordEvent(ushort eventID, ReplayState eventData = null)
        {
            ReplayObject.RecordReplayEvent(replayIdentity, eventID, eventData);
        }

        /// <summary>
        /// Record a method call.
        /// Note that this will also cause the target method to be invoked immediatley.
        /// </summary>
        /// <param name="method">The delegate method to record</param>
        public void RecordMethodCall(Action method)
        {
            ReplayObject.Call(replayIdentity, method);
        }

        /// <summary>
        /// Record a method call.
        /// Note that this will also cause the target method to be invoked immediatley.
        /// </summary>
        /// <typeparam name="T">The parameter type of the first method parameter</typeparam>
        /// <param name="method">The delegate method to record</param>
        /// <param name="arg">The first argument for the target method</param>
        public void RecordMethodCall<T>(Action<T> method, T arg)
        {
            ReplayObject.Call(replayIdentity, method, arg);
        }

        /// <summary>
        /// Record a method call.
        /// Note that this will also cause the target method to be invoked immediatley.
        /// </summary>
        /// <typeparam name="T0">The parameter type of the first method parameter</typeparam>
        /// <typeparam name="T1">The parameter type of the second method parameter</typeparam>
        /// <param name="method">The delegate method to record</param>
        /// <param name="arg0">The first argument for the target method</param>
        /// <param name="arg1">The second argument for the target method</param>
        public void RecordMethodCall<T0, T1>(Action<T0, T1> method, T0 arg0, T1 arg1)
        {
            ReplayObject.Call(replayIdentity, method, arg0, arg1);
        }

        /// <summary>
        /// Record a method call.
        /// Note that this will also cause the target method to be invoked immediatley.
        /// </summary>
        /// <typeparam name="T0">The parameter type of the first method parameter</typeparam>
        /// <typeparam name="T1">The parameter type of the second method parameter</typeparam>
        /// <typeparam name="T2">The parameter type of the third method parameter</typeparam>
        /// <param name="method">The delegate method to record</param>
        /// <param name="arg0">The first argument for the target method</param>
        /// <param name="arg1">The second argument for the target method</param>
        /// <param name="arg2">The third argument for the target method</param>
        public void RecordMethodCall<T0, T1, T2>(Action<T0, T1, T2> method, T0 arg0, T1 arg1, T2 arg2)
        {
            ReplayObject.Call(replayIdentity, method, arg0, arg1, arg2);
        }

        /// <summary>
        /// Record a method call.
        /// Note that this will also cause the target method to be invoked immediatley.
        /// </summary>
        /// <typeparam name="T0">The parameter type of the first method parameter</typeparam>
        /// <typeparam name="T1">The parameter type of the second method parameter</typeparam>
        /// <typeparam name="T2">The parameter type of the third method parameter</typeparam>
        /// <typeparam name="T3">The parameter type of the fourth method parameter</typeparam>
        /// <param name="method">The delegate method to record</param>
        /// <param name="arg0">The first argument for the target method</param>
        /// <param name="arg1">The second argument for the target method</param>
        /// <param name="arg2">The third argument for the target method</param>
        /// <param name="arg3">The fourth argument for the target method</param>
        public void RecordMethodCall<T0, T1, T2, T3>(Action<T0, T1, T2, T3> method, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
        {
            ReplayObject.Call(replayIdentity, method, arg0, arg1, arg2, arg3);
        }

        internal void SubmitReplayVariables()
        {
            // Check for no variables
            if (Variables == null)
                return;

            // Process all variables
            foreach (ReplayVariable variable in Variables)
            {
                // Record the variable
                RecordVariable(variable);
            }
        }

        internal void UpdateReplayVariables()
        {
            // Check for no variables
            if (Variables == null || IsReplaying == false)
                return;

            // Get the playback time
            ReplayTime playbackTime = PlaybackTime;

            foreach(ReplayVariable variable in Variables)
            {
                variable.Interpolate(playbackTime.Delta);
            }
        }

        internal ReplayVariable GetReplayVariable(int fieldOffset)
        {
            // Check for any variables
            if (HasVariables == false)
                return null;

            // Try to find variable
            foreach(ReplayVariable variable in Variables)
            {
                if (variable.FieldOffset == fieldOffset)
                    return variable;
            }

            // Error value
            return null;
        }

        internal void UpdateManagingObject()
        {
            if (replayObject.reference == null)
                replayObject = new ReplayObject.ReplayObjectReference(GetManagingObject());
        }

        private ReplayObject GetManagingObject()
        {
            ReplayObject manager = null;

            Transform current = transform;

            do
            {
                // Check for a replay object to manage this component
                if (current.GetComponent<ReplayObject>() != null)
                {
                    // Get the manager object
                    manager = current.GetComponent<ReplayObject>();
                    break;
                }

                // Look higher in the hierarchy
                current = current.parent;
            }
            while (current != null);

            // Dont allow null managing object at runtime
            if (Application.isPlaying == true && manager == null)
                manager = transform.gameObject.AddComponent<ReplayObject>();

            // Get the manager
            return manager;
        }

        internal static void ApplyReplayHandle(IEnumerable<ReplayBehaviour> behaviours, ReplayHandle handle)
        {
            foreach (ReplayBehaviour behaviour in behaviours)
                behaviour.behaviourHandle = handle;
        }

        internal static void InvokeReplayStartEvent(IEnumerable<ReplayBehaviour> behaviours)
        {
            foreach(ReplayBehaviour behaviour in behaviours)
            {
                if (behaviour == null)
                    continue;
                try
                {
                    behaviour.OnReplayStart();
                }
                catch(Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        internal static void InvokeReplayEndEvent(IEnumerable<ReplayBehaviour> behaviours)
        {
            foreach (ReplayBehaviour behaviour in behaviours)
            {
                if (behaviour == null)
                    continue;
                try
                {
                    behaviour.OnReplayEnd();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        internal static void InvokeReplayPlayPauseEvent(IEnumerable<ReplayBehaviour> behaviours, bool paused)
        {
            foreach (ReplayBehaviour behaviour in behaviours)
            {
                if (behaviour == null)
                    continue;
                try
                {
                    behaviour.OnReplayPlayPause(paused);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        internal static void InvokeReplayResetEvent(IEnumerable<ReplayBehaviour> behaviours)
        {
            foreach (ReplayBehaviour behaviour in behaviours)
            {
                if (behaviour == null)
                    continue;
                try
                {
                    behaviour.OnReplayReset();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        internal static void InvokeReplayCaptureEvent(IEnumerable<ReplayBehaviour> behaviours)
        {
            foreach (ReplayBehaviour behaviour in behaviours)
            {
                if (behaviour == null)
                    continue;
                try
                {
                    behaviour.OnReplayCapture();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        internal static void InvokeReplayUpdateEvent(IEnumerable<ReplayBehaviour> behaviours, ReplayTime updateTime)
        {
            foreach (ReplayBehaviour behaviour in behaviours)
            {
                if (behaviour == null)
                    continue;
                try
                {
                    behaviour.OnReplayUpdate(updateTime);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        internal static void InvokeReplaySpawnedEvent(IEnumerable<ReplayBehaviour> behaviours, Vector3 position, Quaternion rotation)
        {
            foreach (ReplayBehaviour behaviour in behaviours)
            {
                if (behaviour == null)
                    continue;
                try
                {
                    behaviour.OnReplaySpawned(position, rotation);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }
    }
}
