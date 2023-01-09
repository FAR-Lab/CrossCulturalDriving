
using UnityEngine;

namespace UltimateReplay
{
    /// <summary>
    /// This class emulates the behaviour of the Time class in Unity and can be used to modify the playback speed of a replay.
    /// There are also delta values that can be used to interpolate between frames where a low record frame rate is used. See <see cref="ReplayTransform"/> for an example. 
    /// </summary>
    public struct ReplayTime
    {
        // Private
        private float time;
        private float normalizedTime;
        private float timeScale;
        private float delta;

        // Public
        /// <summary>
        /// Returns a <see cref="ReplayTime"/> representing the 0 positioon or start position of a replay. 
        /// </summary>
        public static readonly ReplayTime startTime = new ReplayTime(0, 0, 1, 0);

        // Properties
        /// <summary>
        /// Get the current replay playback time.
        /// </summary>
        public float Time
        {
            get { return time; }
            internal set { time = value; }
        }

        /// <summary>
        /// Get the current replay playback time represented as a value between 0-1.
        /// A value of 0 indicates the start of the recording where a value of 1 represent the end.
        /// </summary>
        public float NormalizedTime
        {
            get { return normalizedTime; }
            internal set { normalizedTime = value; }
        }

        /// <summary>
        /// The time scale value used during replay playback. 
        /// You can set this value to negative values to control the direction of playback.
        /// This value is ignored during replay recording.
        /// </summary>
        public float TimeScale
        {
            get { return timeScale; }
        }

        /// <summary>
        /// Represents a delta between current replay frames.
        /// This normalized value can be used to interpolate smoothly between replay states where a low record rate is used.
        /// Note: this value is not the actual delta time but a value representing the transition progress between replay frames.
        /// </summary>
        public float Delta
        {
            get { return delta; }
            internal set { delta = value; }
        }

        /// <summary>
        /// Get the playback direction based on the current <see cref="TimeScale"/> value. 
        /// </summary>
        public ReplayManager.PlaybackDirection TimeScaleDirection
        {
            get
            {
                if (timeScale < 0)
                    return ReplayManager.PlaybackDirection.Backward;

                return ReplayManager.PlaybackDirection.Forward;
            }
        }

        // Constructor
        internal ReplayTime(float time, float duration, float timeScale, float delta)
        {
            this.time = time;
            this.normalizedTime = Mathf.InverseLerp(0, duration, time);
            this.timeScale = timeScale;
            this.delta = delta;
        }

        // Methods
        /// <summary>
        /// Gets the current time as a float and converts it to minutes and seconds formatted as a string.
        /// </summary>
        /// <param name="timeValue">The time value input, for example: Time.time</param>
        /// <returns>A formatted time string</returns>
        public static string GetCorrectedTimeValueString(float timeValue)
        {
            int minutes = (int)(timeValue / 60);
            int seconds = (int)(timeValue % 60);

            return string.Format("{0}:{1}", minutes, seconds.ToString("00"));
        }
    }
}
