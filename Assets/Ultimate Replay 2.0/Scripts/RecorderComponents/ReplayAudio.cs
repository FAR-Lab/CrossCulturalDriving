using System;
using System.Collections;
using UltimateReplay.Serializers;
using UnityEngine;

namespace UltimateReplay
{
    /// <summary>
    /// Used to record and replay an AudioSource component .
    /// </summary>
    [ReplaySerializer(typeof(ReplayAudioSerializer))]
    public class ReplayAudio : ReplayRecordableBehaviour
    {
        // Types
        /// <summary>
        /// Serialize flags used to specify which data elements should be stored.
        /// </summary>
        [Flags]
        public enum ReplayAudioFlags
        {
            /// <summary>
            /// Only the required audio data will be stored. Ie: Timestamp.
            /// </summary>
            None = 0,
            /// <summary>
            /// The pitch value should be recorded.
            /// </summary>
            Pitch = 1 << 1,
            /// <summary>
            /// The volume value should be recorded.
            /// </summary>
            Volume = 1 << 2,
            /// <summary>
            /// The stero pan value should be recorded.
            /// </summary>
            StereoPan = 1 << 3,
            /// <summary>
            /// The spatial blend value should be recorded.
            /// </summary>
            SpatialBlend = 1 << 4,
            /// <summary>
            /// The reverb zone mix value should be recorded.
            /// </summary>
            ReverbZoneMix = 1 << 5,
            /// <summary>
            /// Supported data elements should be recorded in low precision mode.
            /// </summary>
            LowPrecision = 1 << 6,
            /// <summary>
            /// Supported data elements should be interpolated during playback.
            /// </summary>
            Interpolate = 1 << 7,
        }

        private struct ReplayAudioData
        {
            // Public
            public bool isPlaying;
            public int timeSample;
            public float pitch;
            public float volume;
            public float stereoPan;
            public float spatialBlend;
            public float reverbZoneMix;
        }

        // Private
        private static readonly ushort audioEventIDForward = 14;
        private static readonly ushort audioEventIDBackward = 15;
        private static ReplayAudioSerializer sharedSerializer = new ReplayAudioSerializer();

        private ReplayAudioData lastAudio;
        private ReplayAudioData targetAudio;
        private bool lastPlayState = false;
        private float lastPlayTime = 0;

        // Public
        /// <summary>
        /// The AudioSource component that will be observed during recording and used for playback during replays.
        /// Only a single AudioClip is supported and should be assigned to the AudioSource.
        /// </summary>
        public AudioSource observedAudio = null;
        /// <summary>
        /// The <see cref="ReplayAudioFlags"/> used to specify which elements of the AudioSource will be recorded.
        /// </summary>
        [HideInInspector]
        public ReplayAudioFlags recordFlags = ReplayAudioFlags.Pitch | ReplayAudioFlags.Volume | ReplayAudioFlags.Interpolate;

        // Methods
        /// <summary>
        /// Called by Unity.
        /// </summary>
        public void Start()
        {
            if (observedAudio == null)
                Debug.LogWarningFormat("Replay audio '{0}' will not record or replay because the observed audio has not been assigned", this);
        }

        /// <summary>
        /// Called by Unity.
        /// </summary>
        public void Update()
        {
            // Check for a valid source
            if (observedAudio == null)
                return;

            // Check for recording
            if (IsRecording == true)
            {
                // Check if Play has been called. Note that consideration is required for calling Play again before another sound has finished playing
                if ((observedAudio.isPlaying == true && lastPlayState == false) ||
                    (observedAudio.isPlaying == true && observedAudio.time < lastPlayTime))
                {
                    // Record an audio start event
                    RecordEvent(audioEventIDForward);

                    // Record an end event if 
                    if(observedAudio.clip != null)
                        StartCoroutine(ScheduleEventEnd(observedAudio.clip.length));
                }

                // Update last state
                lastPlayState = observedAudio.isPlaying;
                lastPlayTime = observedAudio.time;
            }
        }

        private IEnumerator ScheduleEventEnd(float delay)
        {
            // Wait for time to pass
            yield return new WaitForSeconds(delay);

            // Record end sound
            if(IsRecording == true)
                RecordEvent(audioEventIDBackward);
        }

        /// <summary>
        /// Called by Unity editor.
        /// </summary>
        public override void Reset()
        {
            // Call base method
            base.Reset();

            // Try to auto-find component
            if (observedAudio == null)
                observedAudio = GetComponent<AudioSource>();
        }

        /// <summary>
        /// Caled by the replay system when the component should reset any persistent data.
        /// </summary>
        public override void OnReplayReset()
        {
            lastAudio = targetAudio;
            lastPlayState = false;
            lastPlayTime = 0;
        }

        /// <summary>
        /// Called by the replay system when an event occurs.
        /// </summary>
        /// <param name="eventID"></param>
        /// <param name="eventData"></param>
        public override void OnReplayEvent(ushort eventID, ReplayState eventData)
        {
            // Check for audio event
            if(eventID == audioEventIDForward && PlaybackDirection == ReplayManager.PlaybackDirection.Forward)
            {
                // Play the audio sound
                observedAudio.Play();
            }
            else if(eventID == audioEventIDBackward && PlaybackDirection == ReplayManager.PlaybackDirection.Backward)
            {
                // Play the audio sound
                observedAudio.timeSamples = observedAudio.clip.samples - 1;
                observedAudio.Play();                
            }
        }

        /// <summary>
        /// Called by the replay system during playback mode.
        /// </summary>
        /// <param name="replayTime">The <see cref="ReplayTime"/> associated with the playback operation for this replay component</param>
        public override void OnReplayUpdate(ReplayTime replayTime)
        {
            // Check for component
            if (observedAudio == null || observedAudio.enabled == false)
                return;

            // Update audio source values
            ReplayAudioData updateAudio = targetAudio;

            if((recordFlags & ReplayAudioFlags.Interpolate) != 0)
            {
                updateAudio.pitch = Mathf.Lerp(lastAudio.pitch, targetAudio.pitch, replayTime.Delta);
                updateAudio.volume = Mathf.Lerp(lastAudio.volume, targetAudio.volume, replayTime.Delta);
                updateAudio.stereoPan = Mathf.Lerp(lastAudio.stereoPan, targetAudio.stereoPan, replayTime.Delta);
                updateAudio.spatialBlend = Mathf.Lerp(lastAudio.spatialBlend, targetAudio.spatialBlend, replayTime.Delta);
                updateAudio.reverbZoneMix = Mathf.Lerp(lastAudio.reverbZoneMix, targetAudio.reverbZoneMix, replayTime.Delta);
            }

            // Apply options
            float pitch = ((replayTime.TimeScaleDirection == ReplayManager.PlaybackDirection.Forward) ? updateAudio.pitch : -updateAudio.pitch) * replayTime.TimeScale;

            // Restore options
            observedAudio.pitch = (PlaybackDirection == ReplayManager.PlaybackDirection.Forward) ? pitch : -pitch;
            observedAudio.volume = updateAudio.volume;
            observedAudio.panStereo = updateAudio.stereoPan;
            observedAudio.spatialBlend = updateAudio.spatialBlend;
            observedAudio.reverbZoneMix = updateAudio.reverbZoneMix;

            //// Check if we should start or stop the audio
            //if (targetAudio.isPlaying == true && observedAudio.isPlaying == false ||// lastAudio.isPlaying != targetAudio.isPlaying ||
            //    (replayTime.TimeScaleDirection == ReplayManager.PlaybackDirection.Forward && targetAudio.timeSample < lastTimeSample - observedAudio.clip.samples * 0.7) ||
            //    (replayTime.TimeScaleDirection == ReplayManager.PlaybackDirection.Backward && targetAudio.timeSample > observedAudio.timeSamples))
            //{
            //    //if(targetAudio.isPlaying == true)
            //    {
            //        ReplayAudioData updateAudio = targetAudio;

            //        // Interpolate
            //        if((recordFlags & ReplayAudioFlags.Interpolate) != 0)
            //        {
            //            updateAudio.timeSample = Mathf.RoundToInt(Mathf.Lerp(lastAudio.timeSample, targetAudio.timeSample, replayTime.Delta));
            //        }
                    
            //        float pitch = ((replayTime.TimeScaleDirection == ReplayManager.PlaybackDirection.Forward) ? updateAudio.pitch : -updateAudio.pitch) * replayTime.TimeScale;

            //        // Restore options
            //        observedAudio.pitch = pitch;
            //        observedAudio.volume = updateAudio.volume;
            //        observedAudio.panStereo = updateAudio.stereoPan;
            //        observedAudio.spatialBlend = updateAudio.spatialBlend;
            //        observedAudio.reverbZoneMix = updateAudio.reverbZoneMix;
                    
            //        // Start playing
            //        if (observedAudio.isPlaying == false || Math.Abs(observedAudio.timeSamples - updateAudio.timeSample) > 0)
            //        {
            //            if (updateAudio.timeSample != lastTimeSample)
            //            {
            //                //if (replayTime.TimeScaleDirection == ReplayManager.PlaybackDirection.Forward)
            //                //{
            //                //    // Check for invalid samples
            //                //    if (observedAudio.clip != null && updateAudio.timeSample >= observedAudio.clip.samples)
            //                //    {
            //                //        // Stop playback
            //                //        observedAudio.Stop();
            //                //        return;
            //                //    }
            //                //}
            //                //else if(replayTime.TimeScaleDirection == ReplayManager.PlaybackDirection.Backward)
            //                //{
            //                //    // Check for invalid samples
            //                //    if(observedAudio.clip != null && updateAudio.timeSample <= 0)
            //                //    {
            //                //        // Stop playback
            //                //        observedAudio.Stop();
            //                //        return;
            //                //    }
            //                //}

            //                // Play the audio from position
            //                //observedAudio.timeSamples = 0;// updateAudio.timeSample;
            //                observedAudio.Play();

            //                lastTimeSample = updateAudio.timeSample;
            //            }
            //        }
            //    }
            //    //else
            //    //{
            //    //    // Stop playing
            //    //    observedAudio.Stop();
            //    //}
            //}

            //lastTimeSample = observedAudio.timeSamples;
        }

        /// <summary>
        /// Called by the replay system when playback is paused or resumed.
        /// </summary>
        /// <param name="paused">True if the replay system is paused or false if it is resuming</param>
        public override void OnReplayPlayPause(bool paused)
        {
            // Check for no component
            if (observedAudio == null || observedAudio.enabled == false)
                return;

            if (paused == true)
            {
                // Pause the clip
                if (observedAudio.isPlaying == true)
                    observedAudio.Pause();
            }
            else
            {
                // Unpause the clip
                if (observedAudio.isPlaying == true)
                    observedAudio.UnPause();
            }
        }

        /// <summary>
        /// Called by the replay system when the replay component should serialize its recorded data.
        /// </summary>
        /// <param name="state">The <see cref="ReplayState"/> to write to</param>
        public override void OnReplaySerialize(ReplayState state)
        {
            // Check for no component
            if (observedAudio == null || observedAudio.enabled == false)
                return;

            // Set serializer flags
            sharedSerializer.SerializeFlags = (ReplayAudioSerializer.ReplayAudioSerializeFlags)recordFlags;

            // Update serializer values
            sharedSerializer.IsPlaying = observedAudio.isPlaying;
            sharedSerializer.TimeSample = observedAudio.timeSamples;
            sharedSerializer.Pitch = observedAudio.pitch;
            sharedSerializer.Volume = observedAudio.volume;
            sharedSerializer.StereoPan = observedAudio.panStereo;
            sharedSerializer.SpatialBlend = observedAudio.spatialBlend;
            sharedSerializer.ReverbZoneMix = observedAudio.reverbZoneMix;

            // Run serializer
            sharedSerializer.OnReplaySerialize(state);
        }

        /// <summary>
        /// Called by the replay system when the replay component should deserialize previously recorded data.
        /// </summary>
        /// <param name="state">The <see cref="ReplayState"/> to read from</param>
        public override void OnReplayDeserialize(ReplayState state)
        {
            // Check for no component - no point wasting time deserializing because the component will not be updated
            if (observedAudio == null || observedAudio.enabled == false)
                return;

            OnReplayReset();

            // Run serializer
            sharedSerializer.OnReplayDeserialize(state);

            // Get all values
            targetAudio = new ReplayAudioData
            {
                isPlaying = sharedSerializer.IsPlaying,
                timeSample = sharedSerializer.TimeSample,
                pitch = sharedSerializer.Pitch,
                volume = sharedSerializer.Volume,
                stereoPan = sharedSerializer.StereoPan,
                spatialBlend = sharedSerializer.SpatialBlend,
                reverbZoneMix = sharedSerializer.ReverbZoneMix,
            };
        }
    }
}
