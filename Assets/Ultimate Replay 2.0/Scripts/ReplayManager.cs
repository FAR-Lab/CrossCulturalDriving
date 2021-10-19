using System;
using System.Collections.Generic;
using UnityEngine;
using UltimateReplay.Core;
using UltimateReplay.Core.Services;
using UltimateReplay.Storage;

namespace UltimateReplay
{
    /// <summary>
    /// Represents a playback node that can be used to calcualte playback offsets.
    /// </summary>
    public enum PlaybackOrigin
    {
        /// <summary>
        /// The start of the playback sequence.
        /// </summary>
        Start,
        /// <summary>
        /// The current frame in the playback sequence.
        /// </summary>
        Current,
        /// <summary>
        /// The end of the playback sequence.
        /// </summary>
        End,
    }

    /// <summary>
    /// Used to indicate what should happen when the end of a replay is reached.
    /// </summary>
    public enum PlaybackEndBehaviour
    {
        /// <summary>
        /// The playback service should automatically end the replay and trigger and playback end events listeners.
        /// The active replay scene will also be reverted to live mode causing physics objects and scripts to be re-activated.
        /// </summary>
        EndPlayback,
        /// <summary>
        /// The playback service should stop the playback and return to the start of the replay.
        /// The active replay scene will remain in playback mode and you will need to call <see cref="ReplayManager.StopPlayback(ref ReplayHandle, bool)"/> manually to end playback.
        /// </summary>
        StopPlayback,
        /// <summary>
        /// The playback service should loop back around to the start of the replay and continue playing.
        /// The replay will play indfinitley until <see cref="ReplayManager.StopPlayback(ref ReplayHandle, bool)"/> is called.
        /// </summary>
        LoopPlayback
    }

    /// <summary>
    /// The main interface for Ultimate Replay and allows full control over object recording and playback.
    /// </summary>
    public sealed class ReplayManager : MonoBehaviour
    {
        // Types
        /// <summary>
        /// The playback direction used during replay plaback.
        /// </summary>
        public enum PlaybackDirection
        {
            /// <summary>
            /// The replay should be played back in normal mode.
            /// </summary>
            Forward,
            /// <summary>
            /// The replay should be played back in reverse mode.
            /// </summary>
            Backward,
        }

        // Private
        private static readonly ArgumentException disposedHandleException = new ArgumentException("The specified replay handle is not valid");
        private static readonly InvalidOperationException invalidHandleException = new InvalidOperationException("Invalid replay handle. The handle is not valid for this operation handle");
        private static readonly InvalidOperationException emptySceneException = new InvalidOperationException("The specified replay scene does not conatain any replay objects");

        private static ReplayManager managerInstance = null;
        private static ReplayStorageTarget defaultStorageTarget = null;
        private static Dictionary<ReplayHandle, ReplayServiceInstance> replayInstances = new Dictionary<ReplayHandle, ReplayServiceInstance>();
        private static Queue<Action> replayLateCall = new Queue<Action>();
        private static Queue<ReplayHandle> stopPlaybackQueue = new Queue<ReplayHandle>();

        // Public
        /// <summary>
        /// Should manual state update be enabled?
        /// If you set this value to true, you will then be responsible for update all replay and record operations by manually calling <see cref="UpdateState(float)"/>.
        /// </summary>
        public static bool manualStateUpdate = false;

        // Properties
        /// <summary>
        /// The default replay storage target used when no storage target is passed to <see cref="BeginRecording(ReplayStorageTarget, ReplayScene, bool, bool, ReplayRecordOptions)"/>.
        /// The default target will always be a <see cref="ReplayMemoryTarget"/>.
        /// </summary>
        public static ReplayStorageTarget DefaultStorageTarget
        {
            get
            {
                if (defaultStorageTarget == null)
                    defaultStorageTarget = new ReplayMemoryTarget();

                return defaultStorageTarget;
            }
            set { defaultStorageTarget = value; }
        }

        /// <summary>
        /// Returns a value indicating if one or more recording operations are running.
        /// </summary>
        public static bool IsRecordingAny
        {
            get
            {
                foreach(ReplayHandle handle in replayInstances.Keys)
                {
                    // Check if any handle is recording
                    if (handle.ReplayType == ReplayHandle.ReplayHandleType.Record)
                        return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Returns a value indicating if one or more replay operations are running.
        /// </summary>
        public static bool IsReplayingAny
        {
            get
            {
                foreach (ReplayHandle handle in replayInstances.Keys)
                {
                    // Check if any handle is recording
                    if (handle.ReplayType == ReplayHandle.ReplayHandleType.Replay)
                        return true;
                }
                return false;
            }
        }

        // Methods
        /// <summary>
        /// Called by Unity.
        /// </summary>
        public void Awake()
        {
        }

        /// <summary>
        /// Called by Unity.
        /// Allows the active replay manager to initialize.
        /// </summary>
        public void Start()
        {
#if ULTIMATEREPLAY_TRIAL
            // Required to keep the editor reference
            UnityEditor.AssetDatabase.Refresh();
#endif
        }


        /// <summary>
        /// Called by Unity.
        /// Allows the singleton to prevent recreation of the instance when the game is about to quit.
        /// </summary>
        public void OnApplicationQuit()
        {
        }
        
        /// <summary>
        /// Called by Unity.
        /// Allows the active replay manager to update recoring or playback.
        /// </summary>
        public void Update()
        {
            if (manualStateUpdate == false)
            {
                // Update all replay services
                UpdateState(UltimateReplay.UpdateMethod.Update, Time.deltaTime);
            }
        }

        /// <summary>
        /// Called by Unity.
        /// </summary>
        public void LateUpdate()
        {
            if (manualStateUpdate == false)
            {
                // Update all replay services
                UpdateState(UltimateReplay.UpdateMethod.LateUpdate, Time.deltaTime);
            }
        }

        /// <summary>
        /// Called by Unity.
        /// </summary>
        public void FixedUpdate()
        {
            if (manualStateUpdate == false)
            {
                // Update all replay services
                UpdateState(UltimateReplay.UpdateMethod.FixedUpdate, Time.fixedDeltaTime);
            }
        }

        /// <summary>
        /// Called by Unity.
        /// </summary>
        public void OnValidate()
        {
            
        }

        /// <summary>
        /// Called by Unity.
        /// </summary>
        public void OnDestroy()
        {
            // Remove static registered instance
            if (managerInstance == this)
                managerInstance = null;

            // Relase any undisposed resources
            ReplayCleanupUtility.CleanupUnreleasedResources();
        }

        internal static void ReplayLateCallEvent(Action action)
        {
            replayLateCall.Enqueue(action);
        }

        /// <summary>
        /// Update all running replay services using the specified delta time.
        /// </summary>
        /// <param name="updateMethod">The update method used to update all services. Some services will require a specific update method to run</param>
        /// <param name="deltaTime">The amout of time in seconds that has passed since the last update. This value must be greater than 0</param>
        public static void UpdateState(UltimateReplay.UpdateMethod updateMethod, float deltaTime)
        {
            // Only update if time has increased
            if (deltaTime <= 0)
                return;

            foreach(ReplayServiceInstance service in replayInstances.Values)
            {
                // Check for updateable
                if (service.IsUpdateable(updateMethod) == false)
                    continue;

                // Udpate the service
                service.ReplayUpdate(deltaTime);
            }

            // Check for playback stopped services
            if (updateMethod == UltimateReplay.UpdateMethod.Update)
            {
                while (stopPlaybackQueue.Count > 0)
                {
                    // Get the target handle
                    ReplayHandle stopHandle = stopPlaybackQueue.Dequeue();

                    // Stop playback
                    StopPlayback(ref stopHandle);
                }

                while(replayLateCall.Count > 0)
                {
                    try
                    {
                        // Invoke the delegate
                        replayLateCall.Dequeue()();
                    }
                    catch(Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
            }
        }

        /// <summary>
        /// Update all running replay services using the specified delta time.
        /// </summary>
        /// <param name="deltaTime">The amout of time in seconds that has passed since the last update. This value must be greater than 0</param>
        public static void UpdateState(float deltaTime)
        {
            // Only update if time has increased
            if (deltaTime <= 0)
                return;

            foreach (ReplayServiceInstance service in replayInstances.Values)
            {
                // Check for updateable
                if (service.IsUpdateable() == false)
                    continue;

                // Udpate the service
                service.ReplayUpdate(deltaTime);
            }

            while (stopPlaybackQueue.Count > 0)
            {
                // Get the target handle
                ReplayHandle stopHandle = stopPlaybackQueue.Dequeue();

                // Stop playback
                StopPlayback(ref stopHandle);
            }

            while (replayLateCall.Count > 0)
            {
                try
                {
                    // Invoke the delegate
                    replayLateCall.Dequeue()();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        /// <summary>
        /// Update the replay service associated with the specified <see cref="ReplayHandle"/>.
        /// </summary>
        /// <param name="handle">The <see cref="ReplayHandle"/> to update</param>
        /// <param name="deltaTime">The amout of time in seconds that has passed since the last update. This value must be greater than 0</param>
        public static void UpdateState(ReplayHandle handle, float deltaTime)
        {
            // Only update if time has increased
            if (deltaTime <= 0)
                return;

            ReplayServiceInstance service;

            // Try to get managing service instance
            if(replayInstances.TryGetValue(handle, out service) == true)
            {
                // Make sure the service state can be update
                if (service.IsUpdateable() == false)
                    return;

                // Update the service
                service.ReplayUpdate(deltaTime);
            }

            while (stopPlaybackQueue.Count > 0)
            {
                // Get the target handle
                ReplayHandle stopHandle = stopPlaybackQueue.Dequeue();

                // Stop playback
                StopPlayback(ref stopHandle);
            }

            while (replayLateCall.Count > 0)
            {
                try
                {
                    // Invoke the delegate
                    replayLateCall.Dequeue()();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        /// <summary>
        /// Start a new recording operation with the specified parameters.
        /// </summary>
        /// <param name="recordTarget">The <see cref="ReplayStorageTarget"/> that replay data should be saved to. Pass null to cause the <see cref="DefaultStorageTarget"/> to be used</param>
        /// <param name="recordScene">The <see cref="ReplayScene"/> that should be sampled during recording. Pass null to use all <see cref="ReplayObject"/> in the active unity scene</param>
        /// <param name="cleanRecording">Should the recording start from scratch</param>
        /// <param name="allowEmptyScene">Are empty scenes allowed to be provided. it may be useful to enable this option if <see cref="ReplayObject"/> will be dynamically spawned after recording is started</param>
        /// <param name="recordOptions">The <see cref="ReplayRecordOptions"/> used to control the record behaviour. Pass null if the global record options should be used</param>
        /// <returns>A <see cref="ReplayHandle"/> instance used to identify this record operation</returns>
        /// <exception cref="InvalidOperationException">The specified <see cref="ReplayScene"/> is null and <see cref="allowEmptyScene"/> is not enabled</exception>
        /// <exception cref="AccessViolationException">The specified storage target is in use by another replay operation</exception>
        /// <exception cref="NotSupportedException">The specified storage target is not writable</exception>
        public static ReplayHandle BeginRecording(ReplayStorageTarget recordTarget = null, ReplayScene recordScene = null, bool cleanRecording = true, bool allowEmptyScene = false, ReplayRecordOptions recordOptions = null)
        {
            // Check for default target
            if (recordTarget == null)
                recordTarget = DefaultStorageTarget;

            // Check for default scene
            if (recordScene == null)
                recordScene = ReplayScene.CurrentScene;

            // Check for empty scene
            if (recordScene.IsEmpty == true && allowEmptyScene == false) throw emptySceneException;

            // Check scene integrity
            try
            {
                recordScene.CheckIntegrity(true);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            // Check for not supported or locked
            if (recordTarget.CanWrite == false) throw new NotSupportedException("The specified storage target is not writable and cannot be used for recording");

            // Check for valid record options
            if (recordOptions == null)
                recordOptions = UltimateReplay.Settings.recordOptions;


            // Create the replay handle
            ReplayHandle handle = new ReplayHandle(ReplayHandle.ReplayHandleType.Record);

            // Check for locked
            try
            {
                recordTarget.Lock(typeof(ReplayManager), handle);
            }
            catch(AccessViolationException)
            {
                throw new AccessViolationException("The specified storage target is in use by another replay operation");
            }


            // Clear replay target if required
            if (cleanRecording == true)
                recordTarget.PrepareTarget(ReplayTargetTask.Discard);

            // Prepare the target for writing operations
            recordTarget.PrepareTarget(ReplayTargetTask.PrepareWrite);

            // Get a new or shared replay service instance
            ReplayRecordServiceInstance serviceInstance = ReplayRecordServiceInstance.GetPooledServiceInstance();

            // Initialize from parameters
            serviceInstance.Initialize(handle, ReplayServiceInstance.ReplayServiceState.Active, recordScene, recordTarget, recordOptions);

            // Prepare scene and apply the replay handle to all behaviours
            recordScene.SetReplaySceneMode(ReplayScene.ReplaySceneMode.Record, recordTarget.InitialStateBuffer, handle);

            // Every recording should start with frame: 0, timestamp: 0
            serviceInstance.ReplayRecordInitialFrame();


            // Push the replay task to the update collection
            replayInstances.Add(handle, serviceInstance);

            // Make sure a manager component is running
            ForceAwake();

            return handle;
        }

        /// <summary>
        /// Pause the record operation associated with the specified <see cref="ReplayHandle"/>.
        /// If the associated record operation is not in a recording state, this method will do nothing.
        /// </summary>
        /// <param name="handle">The <see cref="ReplayHandle"/> of the record operation that was returned by the <see cref="BeginRecording(ReplayStorageTarget, ReplayScene, bool, bool, ReplayRecordOptions)"/> call</param>
        /// <exception cref="ArgumentException">The specified <see cref="ReplayHandle"/> has already been disposed</exception>
        /// <exception cref="InvalidOperationException">The specified <see cref="ReplayHandle"/> is not a valid record operation handle</exception>
        public static void PauseRecording(ReplayHandle handle)
        {
            // Check for invalid handle
            if (handle.IsDisposed == true) throw disposedHandleException;

            // Get record service
            ReplayServiceInstance serviceInstance = GetServiceInstance<ReplayRecordServiceInstance>(handle);

            // Update the service instance
            if(serviceInstance != null && serviceInstance.State != ReplayServiceInstance.ReplayServiceState.Paused)
            {
                // Check for invalid handle state
                if (serviceInstance.Handle.ReplayType != ReplayHandle.ReplayHandleType.Record)
                    throw invalidHandleException;

                // Set paused state
                serviceInstance.State = ReplayServiceInstance.ReplayServiceState.Paused;
            }
        }

        /// <summary>
        /// Resume the record operation associated with the specified <see cref="ReplayHandle"/>.
        /// If the associated record operation is not is a paused state, this method wiil do nothing.
        /// </summary>
        /// <param name="handle">The <see cref="ReplayHandle"/> of the record operation that was returned by the <see cref="BeginRecording(ReplayStorageTarget, ReplayScene, bool, bool, ReplayRecordOptions)"/> call</param>
        /// <exception cref="ArgumentException">The specified <see cref="ReplayHandle"/> has already been disposed</exception>
        /// <exception cref="InvalidOperationException">The specified <see cref="ReplayHandle"/> is not a valid record operation handle</exception>
        public static void ResumeRecording(ReplayHandle handle)
        {
            // Check for invalid handle
            if (handle.IsDisposed == true) throw disposedHandleException;

            // Get record service
            ReplayServiceInstance serviceInstance = GetServiceInstance<ReplayRecordServiceInstance>(handle);

            // Update the service instance
            if (serviceInstance != null && serviceInstance.State != ReplayServiceInstance.ReplayServiceState.Active)
            {
                // Check for invalid handle state
                if (serviceInstance.Handle.ReplayType != ReplayHandle.ReplayHandleType.Record)
                    throw invalidHandleException;

                // Set paused state
                serviceInstance.State = ReplayServiceInstance.ReplayServiceState.Active;
            }
        }

        /// <summary>
        /// Returns a value indicating whether there is a running record operation associated with the specified <see cref="ReplayHandle"/>.
        /// </summary>
        /// <param name="handle">The <see cref="ReplayHandle"/> of the record operation that was returned by the <see cref="BeginRecording(ReplayStorageTarget, ReplayScene, bool, bool, ReplayRecordOptions)"/> call</param>
        /// <exception cref="ArgumentException">The specified <see cref="ReplayHandle"/> has already been disposed</exception>
        /// <exception cref="InvalidOperationException">The specified <see cref="ReplayHandle"/> is not a valid record operation handle</exception>
        /// <returns>True if the specified <see cref="ReplayHandle"/> has a running record operation associated with it or false if not</returns>
        public static bool IsRecording(ReplayHandle handle)
        {
            // Check for invalid handle
            if (handle.IsDisposed == true)
                return false;

            // Get the registered service
            ReplayServiceInstance serviceInstance = GetServiceInstance<ReplayRecordServiceInstance>(handle);

            // Update the service instance
            if (serviceInstance != null)
            {
                // Check for invalid handle state
                if (serviceInstance.Handle.ReplayType != ReplayHandle.ReplayHandleType.Record)
                    return false;

                // The handle is a valid replay handle and the recording service is running
                return true;
            }

            // Not recording
            return false;
        }

        public static bool IsRecordingPaused(ReplayHandle handle)
        {
            // Check for invalid handle
            if (handle.IsDisposed == true) throw disposedHandleException;

            // Get record service
            ReplayServiceInstance serviceInstance = GetServiceInstance<ReplayRecordServiceInstance>(handle);

            // Update the service instance
            if (serviceInstance != null)
            {
                // Check for invalid handle state
                if (serviceInstance.Handle.ReplayType != ReplayHandle.ReplayHandleType.Record)
                    throw invalidHandleException;

                // CHeck for paused
                return serviceInstance.State == ReplayServiceInstance.ReplayServiceState.Paused;
            }
            return false;
        }

        public static void StopRecording(ref ReplayHandle handle)
        {
            // Check for invalid handle
            if (handle.IsDisposed == true) throw disposedHandleException;

            // Get the record service
            ReplayServiceInstance serviceInstance = GetServiceInstance<ReplayRecordServiceInstance>(handle);

            // Update the service instance
            if (serviceInstance != null)
            {
                // Check for invalid handle state
                if (serviceInstance.Handle.ReplayType != ReplayHandle.ReplayHandleType.Record)
                    throw invalidHandleException;

                // Remove the task for the update list
                replayInstances.Remove(handle);

                // Return to live mode
                serviceInstance.Scene.SetReplaySceneMode(ReplayScene.ReplaySceneMode.Live, serviceInstance.Target.InitialStateBuffer, handle);

                // Finalize the storage target
                serviceInstance.Target.PrepareTarget(ReplayTargetTask.Commit);

                // Unload the storage target
                serviceInstance.Target.Unlock(typeof(ReplayManager), handle);

                // Release resources
                serviceInstance.Dispose();
                handle.Dispose();
            }
        }

        public static void SetPlaybackTime(ReplayHandle handle, float playbackTimeOffset, PlaybackOrigin origin = PlaybackOrigin.Start)
        {
            // Check for invalid handle
            if (handle.IsDisposed == true) throw disposedHandleException;

            // Get playback service
            ReplayPlaybackServiceInstance playbackService = GetServiceInstance<ReplayPlaybackServiceInstance>(handle);

            // Update the service instance
            if (playbackService != null)
            {
                // Make sure the handle is currently replaying
                if (playbackService.Handle.ReplayType != ReplayHandle.ReplayHandleType.Replay)
                    return;

                    // Seek to time stamp
                playbackService.ReplaySeekPlayback(origin, playbackTimeOffset);
            }
        }

        public static void SetPlaybackTimeNormalized(ReplayHandle handle, float playbackNormalizedOffset, PlaybackOrigin origin = PlaybackOrigin.Start)
        {
            // Check for invalid handle
            if (handle.IsDisposed == true) throw disposedHandleException;

            // Get playback service
            ReplayPlaybackServiceInstance playbackService = GetServiceInstance<ReplayPlaybackServiceInstance>(handle);
            
            // Update the service instance
            if (playbackService != null)
            {
                // Make sure the handle is currently replaying
                if (playbackService.Handle.ReplayType != ReplayHandle.ReplayHandleType.Replay)
                    return;

                // Seek to frame
                playbackService.ReplaySeekPlaybackNormalized(origin, playbackNormalizedOffset);
            }
        }

        public static void SetPlaybackDirection(ReplayHandle handle, PlaybackDirection direction = PlaybackDirection.Forward)
        {
            // Check for invalid handle
            if (handle.IsDisposed == true) throw disposedHandleException;

            // Get playback service
            ReplayPlaybackServiceInstance playbackService = GetServiceInstance<ReplayPlaybackServiceInstance>(handle);

            // Update the service instance
            if (playbackService != null)
            {
                // Make sure the handle is currently replaying
                if (playbackService.Handle.ReplayType != ReplayHandle.ReplayHandleType.Replay)
                    return;

                if (direction == PlaybackDirection.Forward)
                {
                    playbackService.PlaybackTimesScale = Mathf.Abs(playbackService.PlaybackTimesScale);
                }
                else
                {
                    playbackService.PlaybackTimesScale = -Mathf.Abs(playbackService.PlaybackTimesScale);
                }
            }
        }

        public static void SetPlaybackTimeScale(ReplayHandle handle, float timeScale = 1f)
        {
            // Check for invalid handle
            if (handle.IsDisposed == true) throw disposedHandleException;

            // Get playback service
            ReplayPlaybackServiceInstance playbackService = GetServiceInstance<ReplayPlaybackServiceInstance>(handle);

            // Update the service instance
            if (playbackService != null)
            {
                playbackService.PlaybackTimesScale = timeScale;
            }
        }

        public static ReplayTime GetPlaybackTime(ReplayHandle handle)
        {
            // Check for invalid handle
            if (handle.IsDisposed == true) throw disposedHandleException;

            // Get playback service
            ReplayPlaybackServiceInstance playbackService = GetServiceInstance<ReplayPlaybackServiceInstance>(handle);

            // Update the service instance
            if (playbackService != null)
            {
                return playbackService.PlaybackTime;
            }

            // Error value
            return ReplayTime.startTime;
        }

        public static float GetPlaybackTimeNormalized(ReplayHandle handle)
        {
            return GetPlaybackTime(handle).NormalizedTime;
        }

        public static float GetPlaybackTimeScale(ReplayHandle handle)
        {
            return GetPlaybackTime(handle).TimeScale;
        }

        public static PlaybackDirection GetPlaybackDirection(ReplayHandle handle)
        {
            return GetPlaybackTime(handle).TimeScaleDirection;
        }

        public static void SetPlaybackEndRestoreSceneMode(ReplayHandle handle, bool restoreReplaySceneOnEnd)
        {
            // Check for invalid handle
            if (handle.IsDisposed == true) throw disposedHandleException;

            // Get playback service
            ReplayPlaybackServiceInstance playbackService = GetServiceInstance<ReplayPlaybackServiceInstance>(handle);

            // Update the service instance
            if (playbackService != null)
            {
                playbackService.RestoreReplaySceneOnEnd = restoreReplaySceneOnEnd;
            }
        }

        public static ReplayHandle BeginPlaybackFrame(ReplayStorageTarget replaySource = null, ReplayScene playbackScene = null, bool allowEmptyScene = false, bool restoreReplayScene = true, ReplayPlaybackOptions playbackOptions = null)
        {
            return BeginPlayback(replaySource, playbackScene, playbackOptions, allowEmptyScene, restoreReplayScene, true);
        }

        public static ReplayHandle BeginPlayback(ReplayStorageTarget replaySource = null, ReplayScene playbackScene = null, bool allowEmptyScene = false, bool restoreReplayScene = true, ReplayPlaybackOptions playbackOptions = null)
        {
            return BeginPlayback(replaySource, playbackScene, playbackOptions, allowEmptyScene, restoreReplayScene, false);
        }

        private static ReplayHandle BeginPlayback(ReplayStorageTarget replaySource, ReplayScene playbackScene, ReplayPlaybackOptions playbackOptions, bool allowEmptyScene, bool restoreReplayScene, bool fixedFrame)
        {
            // Check for default storage device required
            if (replaySource == null)
                replaySource = DefaultStorageTarget;

            // Check for default scene required
            if (playbackScene == null)
                playbackScene = ReplayScene.CurrentScene;

            // Check for empty scene
            if (playbackScene.IsEmpty == true && allowEmptyScene == false) throw emptySceneException;

            // Check scene integrity
            try
            {
                playbackScene.CheckIntegrity(true);
            }
            catch(Exception e)
            {
                Debug.LogException(e);
            }

            // Check for not supported or locked
            if (replaySource.CanRead == false) throw new NotSupportedException("The specified replay storage target is not readable and cannot be used for replaying");

            // Make sure options are valid
            if (playbackOptions == null)
                playbackOptions = UltimateReplay.Settings.playbackOptions;

            // Create the replay handle
            ReplayHandle handle = new ReplayHandle(ReplayHandle.ReplayHandleType.Replay);

            // Lock the target
            try
            {
                // Lock the storage target because we will bre reading from it and cannot allow concurrent access
                replaySource.Lock(typeof(ReplayManager), handle);
            }
            catch(AccessViolationException)
            {
                throw new AccessViolationException("The specified replay storage target is in use by another replay operation");
            }

            // Lock playback scene
            try
            {
                // Lock the playback scene because we will be restoring objects and do not want other playback calls to manipulate the same scene
                playbackScene.Lock(typeof(ReplayManager), handle);
            }
            catch(AccessViolationException)
            {
                throw new AccessViolationException("The specified replay playback scene is in use by another replay playback operation");
            }

            // Prepare for playback
            playbackScene.SetReplaySceneMode(ReplayScene.ReplaySceneMode.Playback, replaySource.InitialStateBuffer, handle);

            // Trigger start events
            ReplayBehaviour.InvokeReplayStartEvent(playbackScene.ActiveReplayBehaviours);
            

            // Prepare the target for writing operations
            replaySource.PrepareTarget(ReplayTargetTask.PrepareRead);

            // Get a new or shared replay service instance
            ReplayPlaybackServiceInstance serviceInstance = ReplayPlaybackServiceInstance.pool.GetReusable();

            // Initialize from parameters
            serviceInstance.Initialize(handle, ReplayServiceInstance.ReplayServiceState.Active, playbackScene, replaySource, playbackOptions, fixedFrame);


            // Replay the initial frame
            serviceInstance.RestoreReplaySceneOnEnd = restoreReplayScene;
            serviceInstance.ReplayRestoreInitialFrame();

            // Push the replay task to the update collection
            replayInstances.Add(handle, serviceInstance);

            // Make sure a manager component is running
            ForceAwake();

            return handle;
        }

        public static void PausePlayback(ReplayHandle handle)
        {
            // Check for invalid handle
            if (handle.IsDisposed == true) throw disposedHandleException;

            // Get playback service
            ReplayPlaybackServiceInstance playbackService = GetServiceInstance<ReplayPlaybackServiceInstance>(handle);

            // Update the service instance
            if (playbackService != null && playbackService.State != ReplayServiceInstance.ReplayServiceState.Paused)
            {
                // Check for invalid handle state
                if (playbackService.Handle.ReplayType != ReplayHandle.ReplayHandleType.Replay)
                    throw invalidHandleException;

                // Set paused state
                playbackService.State = ReplayServiceInstance.ReplayServiceState.Paused;

                // Trigger paused events
                ReplayBehaviour.InvokeReplayPlayPauseEvent(playbackService.Scene.ActiveReplayBehaviours, true);
            }
        }

        public static void ResumePlayback(ReplayHandle handle)
        {
            // Check for invalid handle
            if (handle.IsDisposed == true) throw disposedHandleException;

            // Get playback service
            ReplayPlaybackServiceInstance playbackService = GetServiceInstance<ReplayPlaybackServiceInstance>(handle);

            // Update the service instance
            if (playbackService != null && playbackService.State != ReplayServiceInstance.ReplayServiceState.Active)
            {
                // Check for invalid handle state
                if (playbackService.Handle.ReplayType != ReplayHandle.ReplayHandleType.Replay)
                    throw invalidHandleException;

                // Set paused state
                playbackService.State = ReplayServiceInstance.ReplayServiceState.Active;

                // Trigger resume events
                ReplayBehaviour.InvokeReplayPlayPauseEvent(playbackService.Scene.ActiveReplayBehaviours, false);
            }
        }

        public static bool IsReplaying(ReplayHandle handle)
        {
            // Check for invalid handle
            if (handle.IsDisposed == true)
                return false;

            // Get playback service
            ReplayPlaybackServiceInstance playbackService = GetServiceInstance<ReplayPlaybackServiceInstance>(handle);

            // Update the service instance
            if (playbackService != null)
            {
                // Check for invalid handle state
                if (playbackService.Handle.ReplayType != ReplayHandle.ReplayHandleType.Replay)
                    return false;

                // The handle is a valid replay handle and the playback service is running
                return true;
            }

            // Not replaying
            return false;
        }

        public static bool IsPlaybackPaused(ReplayHandle handle)
        {
            // Check for invalid handle
            if (handle.IsDisposed == true) throw disposedHandleException;

            // Get playback service
            ReplayPlaybackServiceInstance playbackService = GetServiceInstance<ReplayPlaybackServiceInstance>(handle);

            // Update the service instance
            if (playbackService != null)
            {
                // Check for invalid handle state
                if (playbackService.Handle.ReplayType != ReplayHandle.ReplayHandleType.Replay)
                    throw invalidHandleException;

                // CHeck for paused
                return playbackService.State == ReplayServiceInstance.ReplayServiceState.Paused;
            }
            return false;
        }

        public static void AddPlaybackEndListener(ReplayHandle handle, Action playbackEndCallback)
        {
            // Check for invalid handle
            if (handle.IsDisposed == true) throw disposedHandleException;

            // Get playback service
            ReplayPlaybackServiceInstance playbackService = GetServiceInstance<ReplayPlaybackServiceInstance>(handle);

            // Update the service instance
            if (playbackService != null)
            {
                // Register listener
                playbackService.AddPlaybackEndListener(playbackEndCallback);
            }
        }

        public static void RemovePlaybackEndListener(ReplayHandle handle, Action playbackEndCallback)
        {
            // Check for invalid handle
            if (handle.IsDisposed == true) throw disposedHandleException;

            // Get playback service
            ReplayPlaybackServiceInstance playbackService = GetServiceInstance<ReplayPlaybackServiceInstance>(handle);

            // Update the service instance
            if (playbackService != null)
            {
                // Register listener
                playbackService.RemovePlaybackEndListener(playbackEndCallback);
            }
        }

        [Obsolete("[Pending removal in 2.2.x]: Use 'StopPlayback(ref ReplayHandle)'. If you need to change scene restore behaviour, this can now be done when calling 'BeginPlayback' or via 'SetPlaybackEndRestoreMode'")]
        public static void StopPlayback(ref ReplayHandle handle, bool restorePreviousSceneState = true)
        {
            StopPlayback(ref handle);
        }

        public static void StopPlayback(ref ReplayHandle handle)
        {
            // Check for invalid handle
            if (handle.IsDisposed == true) throw disposedHandleException;

            // Get playback service
            ReplayPlaybackServiceInstance playbackService = GetServiceInstance<ReplayPlaybackServiceInstance>(handle);

            // Update the service instance
            if (playbackService != null)
            {
                // Check for invalid handle state
                if (playbackService.Handle.ReplayType != ReplayHandle.ReplayHandleType.Replay)
                    throw invalidHandleException;

                // Remove the task for the update list
                replayInstances.Remove(handle);
                
                // Unlock access to resources
                playbackService.Target.Unlock(typeof(ReplayManager), handle);
                playbackService.Scene.Unlock(typeof(ReplayManager), handle);

                // Switch to game mode
                playbackService.Scene.restorePreviousSceneState = playbackService.RestoreReplaySceneOnEnd;
                playbackService.Scene.SetReplaySceneMode(ReplayScene.ReplaySceneMode.Live, playbackService.Target.InitialStateBuffer, handle);

                // Call update and reset events
                ReplayTime endTime = new ReplayTime(playbackService.Target.Duration, playbackService.Target.Duration, 1f, 1f);

                ReplayBehaviour.InvokeReplayUpdateEvent(playbackService.Scene.ActiveReplayBehaviours, endTime);
                ReplayBehaviour.InvokeReplayResetEvent(playbackService.Scene.ActiveReplayBehaviours);

                // Trigger end events
                ReplayBehaviour.InvokeReplayEndEvent(playbackService.Scene.ActiveReplayBehaviours);

                // Release resources
                playbackService.Dispose();
                handle.Dispose();
            }
        }

        internal static void StopPlaybackDelayed(ReplayHandle handle)
        {
            // Check for disposed handle
            if (handle.IsDisposed == true)
                return;

            // Push to queue
            stopPlaybackQueue.Enqueue(handle);
        }

        public static ReplayStorageTarget GetReplayStorageTarget(ReplayHandle handle)
        {
            // Check for invalid handle
            if (handle.IsDisposed == true) throw disposedHandleException;

            // Get playback service
            ReplayServiceInstance serviceInstance = GetServiceInstance<ReplayServiceInstance>(handle);

            // Update the service instance
            if (serviceInstance != null)
            {
                // Get the replay target
                return serviceInstance.Target;
            }
            return null;
        }

        public static ReplayScene GetReplayScene(ReplayHandle handle)
        {
            // Check for invalid handle
            if (handle.IsDisposed == true) throw disposedHandleException;

            // Get replay service
            ReplayServiceInstance serviceInstance = GetServiceInstance<ReplayServiceInstance>(handle);

            // Check for vlaid
            if (serviceInstance != null)
            {
                // Get the replay scene
                return serviceInstance.Scene;
            }
            return null;
        }

        public static void AddReplayObjectToRecordScenes(ReplayObject replayObject)
        {
            // Check for null
            if (replayObject == null) throw new ArgumentNullException("replayObject");

            // Process all running services
            foreach(ReplayServiceInstance serviceInstance in replayInstances.Values)
            {
                // Check for record service
                if(serviceInstance is ReplayRecordServiceInstance)
                {
                    // Add object to scene
                    serviceInstance.Scene.AddReplayObject(replayObject);
                }
            }
        }

        public static void AddReplayObjectToRecordScene(ReplayHandle recordHandle, ReplayObject replayObject)
        {
            // Check for disposed and null
            if (recordHandle.IsDisposed == true) throw disposedHandleException;
            if (replayObject == null) throw new ArgumentNullException("replayObject");

            // Get service instance
            ReplayRecordServiceInstance serviceInstance = GetServiceInstance<ReplayRecordServiceInstance>(recordHandle);

            // Check for error
            if(serviceInstance != null)
            {
                // Add object to scene
                serviceInstance.Scene.AddReplayObject(replayObject);
            }
        }

        public static void AddReplayObjectToPlaybackScenes(ReplayObject playbackObject)
        {
            // Check for null
            if (playbackObject == null) throw new ArgumentNullException("playbackObject");

            // Process all running services
            foreach (ReplayServiceInstance serviceInstance in replayInstances.Values)
            {
                // Check for playback service
                if (serviceInstance is ReplayPlaybackServiceInstance)
                {
                    // Add object to scene
                    serviceInstance.Scene.AddReplayObject(playbackObject);
                }
            }
        }

        public static void AddReplayObjectToPlaybackScene(ReplayHandle playbackHandle, ReplayObject replayObject)
        {
            // Check for disposed and null
            if (playbackHandle.IsDisposed == true) throw disposedHandleException;
            if (replayObject == null) throw new ArgumentNullException("replayObject");

            // Get service instance
            ReplayPlaybackServiceInstance serviceInstance = GetServiceInstance<ReplayPlaybackServiceInstance>(playbackHandle);

            // Check for error
            if (serviceInstance != null)
            {
                // Add object to scene
                serviceInstance.Scene.AddReplayObject(replayObject);
            }
        }

        public static void RemoveReplayObjectFromRecordScenes(ReplayObject replayObject)
        {
            // Check for null
            if (replayObject == null) throw new ArgumentNullException("replayObject", "You should remove the specified object before it is destroyed");

            // Process all running services
            foreach (ReplayServiceInstance serviceInstance in replayInstances.Values)
            {
                // Check for record service
                if (serviceInstance is ReplayRecordServiceInstance)
                {
                    // Add object to scene
                    serviceInstance.Scene.RemoveReplayObject(replayObject);
                }
            }
        }

        public static void RemoveReplayObjectFromRecordScene(ReplayHandle recordHandle, ReplayObject replayObject)
        {
            // Check for disposed and null
            if (recordHandle.IsDisposed == true) throw disposedHandleException;
            if (replayObject == null) throw new ArgumentNullException("replayObject", "You should remove the specified object before it is destroyed");

            // Get service instance
            ReplayRecordServiceInstance serviceInstance = GetServiceInstance<ReplayRecordServiceInstance>(recordHandle);

            // Check for error
            if (serviceInstance != null)
            {
                // Add object to scene
                serviceInstance.Scene.RemoveReplayObject(replayObject);
            }
        }

        public static void RemoveReplayObjectFromPlaybackScenes(ReplayObject playbackObject)
        {
            // Check for null
            if (playbackObject == null) throw new ArgumentNullException("playbackObject", "You should remove the specified object before it is destroyed");

            // Process all running services
            foreach (ReplayServiceInstance serviceInstance in replayInstances.Values)
            {
                // Check for playback service
                if (serviceInstance is ReplayPlaybackServiceInstance)
                {
                    // Add object to scene
                    serviceInstance.Scene.RemoveReplayObject(playbackObject);
                }
            }
        }

        public static void RemoveReplayObjectFromPlaybackScene(ReplayHandle playbackHandle, ReplayObject replayObject)
        {
            // Check for disposed and null
            if (playbackHandle.IsDisposed == true) throw disposedHandleException;
            if (replayObject == null) throw new ArgumentNullException("replayObject", "You should remove the specified object before it is destroyed");

            // Get service instance
            ReplayPlaybackServiceInstance serviceInstance = GetServiceInstance<ReplayPlaybackServiceInstance>(playbackHandle);

            // Check for error
            if (serviceInstance != null)
            {
                // Add object to scene
                serviceInstance.Scene.RemoveReplayObject(replayObject);
            }
        }

        public static void ForceAwake()
        {
            if(managerInstance == null && Application.isPlaying == true)
            {
                GameObject go = new GameObject("ReplayManager");
                managerInstance = go.AddComponent<ReplayManager>();

                DontDestroyOnLoad(go);
            }
        }

        private static T GetServiceInstance<T>(ReplayHandle handle) where T : ReplayServiceInstance
        {
            ReplayServiceInstance serviceInstance = null;

            // Try to get registered instance
            if(replayInstances.TryGetValue(handle, out serviceInstance) == true)
            {
                // Get as type
                return serviceInstance as T;
            }
            return null;
        }
        
        public static void RegisterReplayPrefab(GameObject prefab)
        {
            // Check for null
            if (prefab == null)
                throw new ArgumentNullException(nameof(prefab));

            // Try to get replay object
            ReplayObject replayObject = prefab.GetComponent<ReplayObject>();

            if (replayObject != null)
            {
                // Register the prefab
                UltimateReplay.Settings.prefabs.RegisterReplayablePrefab(replayObject);
            }
        }

        public static GameObject FindReplayPrefab(string prefabName)
        {
            // Check for null
            if (prefabName == null)
                throw new ArgumentNullException(nameof(prefabName));

            // Try to get replay object
            ReplayObject replayObject = UltimateReplay.Settings.prefabs.GetReplayPrefabWithName(prefabName);

            // Get game object
            if (replayObject != null)
                return replayObject.gameObject;

            return null;
        }
    }
}
