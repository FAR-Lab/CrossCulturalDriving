using UnityEngine;
using UltimateReplay;
using UltimateReplay.Storage;
using System.Collections;

namespace UltimateReplay.Example
{
    // playback handle is assigned but never used
#pragma warning disable 0414

    /// <summary>
    /// An example script which shows how you can start a replay using a replay which was previously recorded to a storage target.
    /// </summary>
    public class Ex05_SimplePlayback : MonoBehaviour
    {
        private ReplayHandle playbackHandle = ReplayHandle.invalid;

        // Methods
        /// <summary>
        /// Called by Unity.
        /// </summary>
        /// <returns></returns>
        public IEnumerator Start()
        {
            // This example assumes that the storage target contains data that was previously recorded.
            ReplayMemoryTarget storage = new ReplayMemoryTarget();

            // Record as normal
            ReplayHandle handle = ReplayManager.BeginRecording(storage);

            // Allow recording to run for 1 second
            yield return new WaitForSeconds(1f);

            // Stop recording
            // We need to pass the handle to the recording as a reference which will cause the handle to become invalid
            ReplayManager.StopRecording(ref handle);


            // Begin playback of the recording that we captured in the storage target
            // Again this method will return a replay handle that is required for many other playback operations
            playbackHandle = ReplayManager.BeginPlayback(storage);
        }
    }

#pragma warning restore 0414
}
