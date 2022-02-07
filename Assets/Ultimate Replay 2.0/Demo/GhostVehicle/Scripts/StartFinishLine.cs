using System.Collections.Generic;
using UltimateReplay.Storage;
using UnityEngine;

namespace UltimateReplay.Example
{
    public class StartFinishLine : MonoBehaviour
    {
        // Private
        private ReplayScene recordScene = null;
        private ReplayScene playbackScene = null;
        private ReplayStorageTarget recordStorage = new ReplayMemoryTarget();
        private ReplayStorageTarget playbackStorage = new ReplayMemoryTarget();
        private ReplayHandle recordHandle;
        private ReplayHandle playbackHandle;

        // Public
        public ReplayObject playerCar;
        public ReplayObject ghostCar;

        // Methods
        public void Start()
        {
            // Create a recordable scene containg the player car only
            recordScene = new ReplayScene(playerCar);
            playbackScene = new ReplayScene(ghostCar);
        }

        public void OnTriggerEnter(Collider other)
        {
            Debug.LogWarning("Trigger");

            // Stop replaying
            if (ReplayManager.IsReplaying(playbackHandle) == true)
                ReplayManager.StopPlayback(ref playbackHandle);

            // Stop recording
            if(ReplayManager.IsRecording(recordHandle) == true)
            {
                // Stop recording
                ReplayManager.StopRecording(ref recordHandle);

                playbackStorage = recordStorage;
                recordStorage = new ReplayMemoryTarget();

                Debug.Log("Recording Length: " + playbackStorage.Duration);

                // Enable the ghost car
                ghostCar.gameObject.SetActive(true);

                // Clone identities - This allows the ghost car to be replayed as the player car
                ReplayObject.CloneReplayObjectIdentity(playerCar, ghostCar);

                // Start replaying
                playbackHandle = ReplayManager.BeginPlayback(playbackStorage, playbackScene);

                // Add end playback listener
                ReplayManager.AddPlaybackEndListener(playbackHandle, OnGhostVehiclePlaybackComplete);
            }

            // Start recording
            recordHandle = ReplayManager.BeginRecording(recordStorage, recordScene);
        }

        private void OnGhostVehiclePlaybackComplete()
        {
            // Hide ghost car
            ghostCar.gameObject.SetActive(false);

            Debug.Log("Playback end");
        }
    }
}
