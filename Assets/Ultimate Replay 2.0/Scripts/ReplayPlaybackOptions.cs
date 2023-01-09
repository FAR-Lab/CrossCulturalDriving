using System;
using UnityEngine;

namespace UltimateReplay
{
    /// <summary>
    /// A number of options used to control the playback behaviour.
    /// </summary>
    [Serializable]
    public class ReplayPlaybackOptions
    {
        // Private
        [SerializeField]
        private PlaybackEndBehaviour playbackEndBehaviour = PlaybackEndBehaviour.EndPlayback;

        [SerializeField]
        private int playbackFPS = -1;

        [SerializeField]
        private UltimateReplay.UpdateMethod playbackUpdateMethod = UltimateReplay.UpdateMethod.Update;

        // Properties
        /// <summary>
        /// When should happen when the replay reaches the end of its playback.
        /// </summary>
        public PlaybackEndBehaviour PlaybackEndBehaviour
        {
            get { return playbackEndBehaviour; }
            set { playbackEndBehaviour = value; }
        }

        /// <summary>
        /// The target playback frame rate.
        /// Use '-1' to set the playback fps to unlimited which will update every game tick.
        /// Playback updates can run more frequently than the record rate but interpolation can blend key frames to create smooth replays.
        /// </summary>
        public int PlaybackFPS
        {
            get { return playbackFPS; }
            set { playbackFPS = value; }
        }

        /// <summary>
        /// Returns a value indicating whether the playback fps is unlimited. Ie: set to '-1'.
        /// </summary>
        public bool IsPlaybackFPSUnlimited
        {
            get { return playbackFPS == -1; }
        }

        /// <summary>
        /// The update method used to update the playback operation.
        /// Used for compatibility with other systems that update objects in other update methods such as LateUpdate.
        /// </summary>
        public UltimateReplay.UpdateMethod PlaybackUpdateMethod
        {
            get { return playbackUpdateMethod; }
            set { playbackUpdateMethod = value; }
        }

        // Constructor
        /// <summary>
        /// Create a new playback options instance with default settings.
        /// </summary>
        public ReplayPlaybackOptions()
        {
        }

        /// <summary>
        /// Create a new playback options instance with the specified end behaviour and frame rate.
        /// </summary>
        /// <param name="endBehaviour">The end behaviour which indicates what should happen when the end of the replay is reached</param>
        /// <param name="playbackFPS">The target playback frate rate or '-1' for unlimited frame rate</param>
        public ReplayPlaybackOptions(PlaybackEndBehaviour endBehaviour, int playbackFPS = -1)
        {
            this.playbackEndBehaviour = endBehaviour;
            this.playbackFPS = playbackFPS;
        }
    }
}
