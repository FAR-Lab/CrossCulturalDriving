using System.IO;
using UltimateReplay.Storage;
using UnityEngine;

namespace UltimateReplay.Example
{
    public class Ex_ReplayToStream : MonoBehaviour
    {
        public void Start()
        {
            // This example assumes that this stream already has valid replay data stored within it.
            // This could be any stream that supports reading, seeking and getting Position.
            MemoryStream stream = new MemoryStream();


            // Create a replay stream target for the specified stream object
            ReplayStreamTarget replayStream = ReplayStreamTarget.CreateReplayStream(stream);

            // Start replaying from the stream
            ReplayHandle playbackHandle = ReplayManager.BeginPlayback(replayStream);
        }
    }
}