using UnityEngine;
using UltimateReplay.Storage;
using System;

namespace UltimateReplay.Core.Services
{
    internal enum ReplaySequenceResult
    {
        SequenceIdle = 0,
        SequenceAdvance,
        SequenceEnd,
    }

    internal sealed class ReplaySequencer
    {
        // Events
        public event Action OnPlaybackLooped;

        // Private
        private ReplaySnapshot current = null;
        private ReplaySnapshot last = null;
        private float playbackTimeStamp = 0;

        // Methods
        public ReplaySnapshot SeekPlayback(ReplayStorageTarget target, float offset, PlaybackOrigin origin, ref ReplayTime playbackTime, bool normalized)
        {
            // Check for normalized
            if (normalized == false)
            {
                // Check for seek mode
                switch (origin)
                {
                    case PlaybackOrigin.Start:
                        playbackTimeStamp = offset;
                        break;

                    case PlaybackOrigin.End:
                        playbackTimeStamp = target.Duration - offset;
                        break;

                    case PlaybackOrigin.Current:
                        playbackTimeStamp += offset;
                        break;
                }
            }
            else
            {
                // Clamp the input valid
                offset = Mathf.Clamp01(offset);

                // Check for seek mode
                switch (origin)
                {
                    case PlaybackOrigin.Start:
                        playbackTimeStamp = MapScale(offset, 0, 1, 0, target.Duration);
                        break;

                    case PlaybackOrigin.End:
                        playbackTimeStamp = MapScale(offset, 1, 0, 0, target.Duration);
                        break;

                    case PlaybackOrigin.Current:
                        playbackTimeStamp = MapScale(offset, 0, 1, playbackTimeStamp, target.Duration);
                        break;
                }
            }

            // Clamp to valid range
            playbackTimeStamp = Mathf.Clamp(playbackTimeStamp, 0, target.Duration);

            // Restore the scene state
            current = target.FetchSnapshot(playbackTimeStamp);

            // Check for change
            if (current != last)
            {
                // Update the replay time
                playbackTime.Time = playbackTimeStamp;
                playbackTime.NormalizedTime = Mathf.InverseLerp(0, target.Duration, playbackTimeStamp);

                if (last != null && current != null)
                {
                    // Check for backwards
                    if (last.TimeStamp <= current.TimeStamp)
                    {
                        // Forward
                        playbackTime.Delta = MapScale(playbackTimeStamp, last.TimeStamp, current.TimeStamp, 0, 1);
                    }
                    else
                    {
                        // Backward
                        playbackTime.Delta = -MapScale(playbackTimeStamp, last.TimeStamp, current.TimeStamp, 1, 0);
                    }
                }
            }
            //else
            //{
            //    ReplayTime.Delta = 0;
            //}

            // Store current frame
            last = current;

            return current;
        }

        public ReplaySequenceResult UpdatePlayback(ReplayStorageTarget target, out ReplaySnapshot frame, ref ReplayTime playbackTime, PlaybackEndBehaviour endBehaviour, float deltaTime)
        {
            ReplayManager.PlaybackDirection direction = playbackTime.TimeScaleDirection;

            // Default to idle
            ReplaySequenceResult result = ReplaySequenceResult.SequenceIdle;

            if (last != null)
            {
                if (direction == ReplayManager.PlaybackDirection.Forward)
                {
                    // Calculatet the delta time
                    playbackTime.Delta = MapScale(playbackTimeStamp, last.TimeStamp, current.TimeStamp, 0, 1);
                }
                else
                {
                    // Calculate the delta for reverse playback
                    playbackTime.Delta = MapScale(playbackTimeStamp, current.TimeStamp, last.TimeStamp, 1, 0);
                }
            }
            else
            {
                if (current == null)
                {
                    playbackTime.Delta = 0;
                }
                else
                {
                    playbackTime.Delta = MapScale(playbackTimeStamp, 0, current.TimeStamp, 0, 1);
                }
            }

            // Clamp delta
            playbackTime.Delta = Mathf.Clamp01(playbackTime.Delta);

            float delta = (deltaTime * playbackTime.TimeScale);


            // Advance the sequence timer - delta shold be negative for reverse playback
            playbackTimeStamp += delta;

            // Advance our frame
            //switch (direction)
            //{
            //    case ReplayManager.PlaybackDirection.Forward:
            //        {
            //            playbackTimeStamp += delta;
            //        }
            //        break;

            //    case ReplayManager.PlaybackDirection.Backward:
            //        {
            //            playbackTimeStamp -= delta;
            //        }
            //        break;
            //}

            switch (endBehaviour)
            {
                default:
                case PlaybackEndBehaviour.EndPlayback:
                    {
                        // Check for end of playback
                        if (playbackTimeStamp >= target.Duration || playbackTimeStamp < 0)
                        {
                            frame = null;
                            return ReplaySequenceResult.SequenceEnd;
                        }
                        break;
                    }

                case PlaybackEndBehaviour.LoopPlayback:
                    {
                        if (playbackTimeStamp >= target.Duration || playbackTimeStamp < 0)
                        {
                            playbackTimeStamp = (direction == ReplayManager.PlaybackDirection.Forward) ? 0 : target.Duration;

                            // Trigger event
                            OnPlaybackLooped?.Invoke();
                        }
                        break;
                    }
                    
                case PlaybackEndBehaviour.StopPlayback:
                    {
                        if (playbackTimeStamp >= target.Duration)
                        {
                            playbackTimeStamp = target.Duration;
                        }
                        else if (playbackTimeStamp < 0)
                        {
                            playbackTimeStamp = 0;
                        }
                        break;
                    }
            }


            // Try to get the current frame
            ReplaySnapshot temp = target.FetchSnapshot(playbackTimeStamp);

            // Check for valid frame
            if (temp != null)
            {
                playbackTime.Time = playbackTimeStamp;
                playbackTime.NormalizedTime = Mathf.InverseLerp(0, target.Duration, playbackTimeStamp);

                // Check for sequence advancement
                if (current != temp)
                {
                    // Snap to next frame
                    playbackTime.Delta = 0;

                    // Set the result
                    result = ReplaySequenceResult.SequenceAdvance;

                    // Update last frame
                    last = current;
                }

                // Update the current frame
                current = temp;
            }
            else
            {
                // Do nothing - We may be inbetween replay frames

                // Trigger sequence end
                //frame = null;
                //return ReplaySequenceResult.SequenceEnd;
            }


            //if (last != null)
            //{
            //    if (direction == ReplayManager.PlaybackDirection.Backward)
            //    {
            //        // Calculate the delta for reverse playback
            //        playbackTime.Delta = MapScale(playbackTimeStamp, current.TimeStamp, last.TimeStamp, 1, 0);
            //    }
            //}
            //else
            //{
            //    if (current == null)
            //    {
            //        playbackTime.Delta = 0;
            //    }
            //    else
            //    {
            //        playbackTime.Delta = MapScale(playbackTimeStamp, 0, current.TimeStamp, 0, 1);
            //    }
            //}

            //// Clamp delta
            //playbackTime.Delta = Mathf.Clamp01(playbackTime.Delta);


            // The sequencer only updated its timing values and there was no state change
            frame = current;
            return result;
        }

        public void Reset()
        {
            current = null;
            last = null;
            playbackTimeStamp = 0;
        }
        
        private float MapScale(float value, float min, float max, float newMin, float newMax)
        {
            float result = newMin + (value - min) * (newMax - newMin) / (max - min);

            if (float.IsNaN(result) == true)
                return 0;

            return result;
        }
    }
}
