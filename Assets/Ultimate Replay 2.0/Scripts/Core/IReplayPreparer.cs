
namespace UltimateReplay.Core
{
    /// <summary>
    /// A preparer is used by Ultimate Replay to prepare any replay objects for either gameplay mode or playback mode.
    /// In order for game systems such as physics and scritps to not affect playback, replay objects must be prepared in some way to disable these systems while playback is enabled.
    /// The appropriate prepare method will be called by the replay system when objects need to either enter playback mode or return to gameplay mode.
    /// </summary>
    public interface IReplayPreparer
    {
        // Methods
        /// <summary>
        /// Prepares the specified replay object for playback.
        /// The implementing type should ensure that all game systems likley to affect the replay object during playback are suitable disabled in order to avoid glitching or unpredicted behaviour.
        /// This method will be called for each replay object that must be prepared.
        /// </summary>
        /// <param name="replayObject">The replay object that should be prepared</param>
        void PrepareForPlayback(ReplayObject replayObject);

        /// <summary>
        /// Prepares the specified replay object for gameplay.
        /// The implementing type should restore all game systems that affect the replay object so that the object is in its original state.
        /// This method will be called for each replay object that must be prepared.
        /// </summary>
        /// <param name="replayObject">The replay object to prepare</param>
        void PrepareForGameplay(ReplayObject replayObject);
    }
}
