using System.Collections;
using UnityEngine;

namespace UltimateReplay.Example
{
    /// <summary>
    /// An example that shows how a replay identity can be cloned onto another object.
    /// This allows a different object to be replayed as a previously recorded object.
    /// This is useful for replays like ghost vehicles where you would normally want to record the player car during a lap but replay onto a different ghost vehicle object.
    /// </summary>
    public class Ex_TransferReplayIdentity : MonoBehaviour
    {
        // This example assumes that a replay object has been assigned for recording
        public ReplayObject recordObject;

        // Assign a secondary object that will be replayed using data captured from the recordObject
        public ReplayObject replayObject;

        // Methods
        public IEnumerator Start()
        {
            // Create a record scene for the record object
            ReplayScene recordScene = new ReplayScene(recordObject);

            // Start recording the single object
            ReplayHandle recordHandle = ReplayManager.BeginRecording(null, recordScene);

            // Allow some data to be recorded for 1 second
            yield return new WaitForSeconds(1f);

            // Stop recording
            ReplayManager.StopRecording(ref recordHandle);


            // Clone the identity which allows the replayObject to be replayed as the recordObject
            ReplayObject.CloneReplayObjectIdentity(recordObject, replayObject);


            // Create a playback scene for the replay object
            ReplayScene playbackScene = new ReplayScene(replayObject);

            // Start playback
            ReplayManager.BeginPlayback(null, playbackScene);
        }
    }
}
