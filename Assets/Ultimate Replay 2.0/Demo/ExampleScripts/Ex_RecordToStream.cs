using System.Collections;
using System.IO;
using UltimateReplay.Storage;
using UnityEngine;

namespace UltimateReplay.Example
{
    public class Ex_RecordToStream : MonoBehaviour
    {
        public IEnumerator Start()
        {
            // Create a stream object of some form to hold the data. This could be any stream that supports writing, seeking and getting Position.
            MemoryStream stream = new MemoryStream();


            // Create a replay stream target for the specified stream object
            ReplayStreamTarget recordStream = ReplayStreamTarget.CreateReplayStream(stream);

            // Start recording to the stream
            ReplayHandle recordHandle = ReplayManager.BeginRecording(recordStream);

            // Allow some data to be recorded for 1 second
            yield return new WaitForSeconds(1f);

            // Stop recording - This will finalize the replay file, commit any buffered data and dispose of any open file streams.
            ReplayManager.StopRecording(ref recordHandle);
        }
    }
}
