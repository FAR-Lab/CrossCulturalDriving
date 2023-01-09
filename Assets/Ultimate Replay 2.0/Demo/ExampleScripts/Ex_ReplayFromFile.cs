using UnityEngine;
using UltimateReplay;
using UltimateReplay.Storage;

namespace UltimateReplay.Example
{
    /// <summary>
    /// An example script that demonstrates how to load an existing replay file.
    /// </summary>
    public class Ex_ReplayFromFile : MonoBehaviour
    {
        // Methods
        public void Start()
        {
            // Load an existing replay file from the specified file path
            ReplayFileTarget replayFile = ReplayFileTarget.ReadReplayFile("C:/ReplayFiles/Example.replay");

            // Start replaying
            ReplayManager.BeginPlayback(replayFile);
        }
    }
}
