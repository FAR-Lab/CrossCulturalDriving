using UnityEngine;
using UltimateReplay;
using UltimateReplay.Storage;
using System.Collections;

namespace UltimateReplay.Example
{
    /// <summary>
    /// An example script that demonstrates how to save a recording to file.
    /// </summary>
    public class Ex_RecordToFile : MonoBehaviour
    {
        // Methods
        public IEnumerator Start()
        {
            // Create a replay file target for the specified file path
            ReplayFileTarget recordFile = ReplayFileTarget.CreateReplayFile("C:/ReplayFiles/Example.replay");

            // Start recording to the file
            ReplayHandle recordHandle = ReplayManager.BeginRecording(recordFile);

            // Allow some data to be recorded for 1 second
            yield return new WaitForSeconds(10f);

            // Stop recording - This will finalize the replay file, commit any buffered data and dispose of any open file streams.
            ReplayManager.StopRecording(ref recordHandle);
        }
    }
}
