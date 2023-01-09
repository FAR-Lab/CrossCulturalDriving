using System;
using System.Collections.Generic;
using UltimateReplay.Storage;
using UnityEngine;

namespace UltimateReplay.Core.Services
{
    internal class ReplayPlaybackServiceInstance : ReplayServiceInstance
    {
        // Private
        private List<Action> playbackEndListeners = new List<Action>();
        private ReplaySequencer sequencer = new ReplaySequencer();
        private ReplayPlaybackOptions playbackOptions = null;
        private ReplaySnapshot restoreFrame = null;
        private ReplayTimer updateTimer = new ReplayTimer();
        private ReplayTime playbackTime = ReplayTime.startTime;
        private bool fixedFrame = false;
        private float deltaAccumulator = 0;
        private bool restoreReplaySceneOnEnd = true;

        // Public
        public static readonly ReplayInstancePool<ReplayPlaybackServiceInstance> pool = new ReplayInstancePool<ReplayPlaybackServiceInstance>(() => new ReplayPlaybackServiceInstance());

        // Properties
        public ReplaySequencer Sequencer
        {
            get { return sequencer; }
        }

        public ReplayTime PlaybackTime
        {
            get { return playbackTime; }
        }

        public float PlaybackTimesScale
        {
            get { return playbackTime.TimeScale; }
            set { playbackTime = new ReplayTime(playbackTime.Time, target.Duration, value, playbackTime.Delta); }
        }

        public bool RestoreReplaySceneOnEnd
        {
            get { return restoreReplaySceneOnEnd; }
            set { restoreReplaySceneOnEnd = value; }
        }

        public override UltimateReplay.UpdateMethod UpdateMethod
        {
            get { return playbackOptions.PlaybackUpdateMethod; }
        }

        // Constructor
        private ReplayPlaybackServiceInstance() 
        {
            sequencer.OnPlaybackLooped += () =>
            {
                ReplayBehaviour.InvokeReplayStartEvent(scene.ActiveReplayBehaviours);
            };
        }

        // Methods
        public void Initialize(ReplayHandle handle, ReplayServiceState state, ReplayScene scene, ReplayStorageTarget target, ReplayPlaybackOptions playbackOptions, bool fixedFrame)
        {
            base.Initialize(handle, state, scene, target);

            this.playbackOptions = playbackOptions;
            this.fixedFrame = fixedFrame;
            this.updateTimer = new ReplayTimer();
            this.playbackTime = ReplayTime.startTime;
            this.restoreFrame = null;
        }

        public void ReplayRestoreInitialFrame()
        {
            // Get the first frame
            restoreFrame = target.FetchSnapshot(ReplaySnapshot.startSequenceID);

            if(restoreFrame == null)
            {
                Debug.LogWarning("Storage target does not contain any data");
                return;
            }

            // Set playback time
            playbackTime = ReplayTime.startTime;

            // Restore the snapshot
            scene.RestoreSnapshot(restoreFrame, target.InitialStateBuffer);

            // Invoke replay update events
            ReplayBehaviour.InvokeReplayUpdateEvent(scene.ActiveReplayBehaviours, playbackTime);
        }

        public override void ReplayUpdate(float deltaTime)
        {
            // Check for fixed frame
            if (fixedFrame == true)
                return;
            
            // Update the timers
            updateTimer.Tick(deltaTime);

            // Calcualate record frame interval
            float interval = 0;

            // Check for limited frame rate
            if(playbackOptions.IsPlaybackFPSUnlimited == false)
                interval = (1.0f / playbackOptions.PlaybackFPS);

            // Add to accumulator
            deltaAccumulator += deltaTime;

            // Check for elapsed time
            if (playbackOptions.IsPlaybackFPSUnlimited == true || updateTimer.HasElapsed(interval) == true)
            {
                // Reset timer
                updateTimer.Reset();

                // Get the last frame and update time
                ReplaySnapshot lastFrame = restoreFrame;
                ReplayTime lastTime = playbackTime;

                // Update the playback engine
                ReplaySequenceResult result = sequencer.UpdatePlayback(target, out restoreFrame, ref playbackTime, playbackOptions.PlaybackEndBehaviour, deltaAccumulator);

                // Check for sequence advance
                if(result == ReplaySequenceResult.SequenceAdvance)
                {
                    // Set the direction value for the playback target
                    target.PlaybackDirection = playbackTime.TimeScaleDirection;

                    // If the playback fps is low or the game fps is low then we could skip frames between updates.
                    // These frames may contain single frame data such as events so they need to be simulated so that events etc can update even though the dispatch time may be far behind.
                    if(lastFrame != null)
                        ReplaySimulateMissedFrames(lastFrame, lastTime, restoreFrame, playbackTime);

                    // Apply the snapshot and simulate any intermediate snapshots
                    scene.RestoreSnapshot(restoreFrame, target.InitialStateBuffer);
                }
                else if(result == ReplaySequenceResult.SequenceEnd)
                {
                    // Trigger end of playback
                    ReplayManager.StopPlaybackDelayed(handle);

                    // Trigger end event - Clone the listener collection as they may modify the state of the service and cause iterator exceptions
                    foreach(Action action in playbackEndListeners)
                    {
                        // Call at end of replay system update
                        if(action != null)
                            ReplayManager.ReplayLateCallEvent(action);
                    }

                    //foreach(Action listener in playbackEndListeners.ToArray())
                    //{
                    //    try
                    //    {
                    //        // Invoke the callback
                    //        listener();
                    //    }
                    //    catch(Exception e)
                    //    {
                    //        Debug.LogException(e);
                    //    }
                    //}
                }

                // Send replay udpate events
                if(result == ReplaySequenceResult.SequenceIdle ||
                    result == ReplaySequenceResult.SequenceAdvance)
                {
                    // Invoke replay update events
                    ReplayBehaviour.InvokeReplayUpdateEvent(scene.ActiveReplayBehaviours, playbackTime);
                }

                // Reset accumulator
                deltaAccumulator = 0;
            }
        }

        public void ReplaySimulateMissedFrames(ReplaySnapshot lastSnapshot, ReplayTime lastTime, ReplaySnapshot currentSnapshot, ReplayTime currentTime)
        {
            // Get the differnce in frame sequences
            int delta = Mathf.Abs(lastSnapshot.SequenceID - currentSnapshot.SequenceID);

            // The same frame or the next frame in the order is playing so no frames have been missed
            if (delta <= 1)
                return;
            
            if (playbackTime.TimeScaleDirection == ReplayManager.PlaybackDirection.Forward)
            {
                for (int i = lastSnapshot.SequenceID + 1; i < currentSnapshot.SequenceID; i++)
                {
                    // Calcualte the update time for the missed frame
                    ReplayTime missedTime = new ReplayTime(playbackTime.Time, target.Duration, playbackTime.TimeScale, 1f);

                    // Fetch the missed frame
                    ReplaySnapshot missedFrame = target.FetchSnapshot(i);

                    // Restore frame and send update events
                    scene.RestoreSnapshot(missedFrame, target.InitialStateBuffer);

                    ReplayBehaviour.InvokeReplayUpdateEvent(scene.ActiveReplayBehaviours, missedTime);
                }
            }
            else
            {
                for(int i = lastSnapshot.SequenceID - 1; i > currentSnapshot.SequenceID; i--)
                {
                    // Calcualte the update time for the missed frame
                    ReplayTime missedTime = new ReplayTime(playbackTime.Time, target.Duration, playbackTime.TimeScale, 1f);

                    // Fetch the missed frame
                    ReplaySnapshot missedFrame = target.FetchSnapshot(i);

                    // Restore frame and send update events
                    scene.RestoreSnapshot(missedFrame, target.InitialStateBuffer);

                    ReplayBehaviour.InvokeReplayUpdateEvent(scene.ActiveReplayBehaviours, missedTime);
                }
            }
        }

        public void ReplaySeekPlayback(PlaybackOrigin origin, float playbackTimeStamp)
        {
            // Seek to frame
            ReplaySnapshot frame = sequencer.SeekPlayback(target, playbackTimeStamp, origin, ref playbackTime, false);

            // Apply the snapshot and simulate any intermediate snapshots
            scene.RestoreSnapshot(frame, target.InitialStateBuffer);

            // Update playback - This is required to update the sequencer and then we nee to manually invoke update events with full time delta
            ReplayUpdate(0);

            // Set delta to full frame
            playbackTime.Delta = 1f;

            // Invoke replay update events
            ReplayBehaviour.InvokeReplayUpdateEvent(scene.ActiveReplayBehaviours, playbackTime);
        }

        public void ReplaySeekPlaybackNormalized(PlaybackOrigin origin, float normalizedPlaybackValue)
        {
            // Seek to normalized frame
            ReplaySnapshot frame = sequencer.SeekPlayback(target, normalizedPlaybackValue, origin, ref playbackTime, true);

            // Apply the snapshot and simulate any intermediate snapshots
            scene.RestoreSnapshot(frame, target.InitialStateBuffer);

            // Update playback - This is required to update the sequencer and then we nee to manually invoke update events with full time delta
            ReplayUpdate(0);

            // Set delta to full frame
            playbackTime.Delta = 1f;

            // Invoke replay update events
            ReplayBehaviour.InvokeReplayUpdateEvent(scene.ActiveReplayBehaviours, playbackTime);
        }

        public void AddPlaybackEndListener(Action action)
        {
            if(action != null && playbackEndListeners.Contains(action) == false)
                playbackEndListeners.Add(action);
        }

        public void RemovePlaybackEndListener(Action action)
        {
            if (playbackEndListeners.Contains(action) == true)
                playbackEndListeners.Remove(action);
        }

        public override void Dispose()
        {
            base.Dispose();

            this.playbackOptions = null;
            this.restoreFrame = null;
            this.updateTimer = new ReplayTimer();
            this.playbackTime = ReplayTime.startTime;
            this.playbackEndListeners.Clear();

            // Reset the playback sequencer
            this.sequencer.Reset();

            // Pool this instance
            pool.PushReusable(this);
        }
    }
}
