using System.IO;

namespace UltimateReplay.Storage
{
    /// <summary>
    /// Represents a replay stream source which can the writen to or read from during recording or playback. 
    /// </summary>
    public interface IReplayStreamProvider
    {
        // Methods
        /// <summary>
        /// Open and return a valid <see cref="ReplayStreamSource"/> with the specified <see cref="ReplayStreamMode"/> as requested by the owning storage target.
        /// </summary>
        /// <param name="mode">The <see cref="ReplayStreamMode"/> used to open the stream</param>
        /// <returns></returns>
        ReplayStreamSource OpenReplayStream(ReplayStreamMode mode);

        /// <summary>
        /// Discard the opened stream.
        /// </summary>
        void DiscardReplayStream();
    }
}
