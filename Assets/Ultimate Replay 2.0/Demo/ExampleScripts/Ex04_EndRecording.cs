using UnityEngine;
using UltimateReplay;
using UltimateReplay.Storage;
using System.Collections;

namespace UltimateReplay.Example
{
    /// <summary>
    /// An example script which shows how to end recording once you have collected enough replay data.
    /// </summary>
    public class Ex04_EndRecording : MonoBehaviour
    {
        // Methods
        /// <summary>
        /// Called by Unity.
        /// </summary>
        /// <returns></returns>
        public IEnumerator Start()
        {
            // Record as normal
            ReplayHandle handle = ReplayManager.BeginRecording(new ReplayMemoryTarget());

            // Allow recording to run for 1 second
            yield return new WaitForSeconds(1f);

            // Stop recording
            // We need to pass the handle to the recording as a reference which will cause the handle to become invalid
            ReplayManager.StopRecording(ref handle);
        }
    }
}
