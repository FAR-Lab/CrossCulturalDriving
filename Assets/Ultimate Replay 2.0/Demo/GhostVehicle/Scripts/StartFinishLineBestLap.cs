using System;
using System.Collections.Generic;
using System.IO;
using UltimateReplay.Storage;
using UnityEngine;
using UnityEngine.UI;

namespace UltimateReplay.Example
{
    public class StartFinishLineBestLap : MonoBehaviour
    {
        // Private
        private ReplayScene recordScene = null;
        private ReplayScene playbackScene = null;
        private ReplayFileTarget recordStorage = null;
        private ReplayFileTarget playbackStorage = null;
        private ReplayHandle recordHandle;
        private ReplayHandle playbackHandle;
        private bool lapStarted = false;
        private float lapStartTime = 0;
        private float lapBestTime = -1f;

        // Public
        public Text timer;
        public Text bestTime;
        public ReplayObject playerCar;
        public ReplayObject ghostCar;

        // Methods
        public void Start()
        {
            // Reset prefs
            //PlayerPrefs.SetFloat("race.bestlap", -1f);

            // Create a recordable scene containg the player car only
            recordScene = new ReplayScene(playerCar);
            playbackScene = new ReplayScene(ghostCar);

            // Load best time
            lapBestTime = PlayerPrefs.GetFloat("race.bestlap", -1f);
        }

        public void OnDestroy()
        {
            if(playbackStorage != null)
                playbackStorage.Dispose();

            if(recordStorage != null)
                recordStorage.Dispose();
        }

        public void Update()
        {
            if (lapStarted == true)
            {
                TimeSpan raceTime = TimeSpan.FromSeconds(Time.time - lapStartTime);

                timer.text = string.Format("{0:00}:{1:00}:{2:00}", raceTime.Minutes, raceTime.Seconds, raceTime.Milliseconds);
            }

            // Update best time
            if(lapBestTime >= 0f)
            {
                TimeSpan bestRaceTime = TimeSpan.FromSeconds(lapBestTime);

                bestTime.text = string.Format("Best: {0:00}:{1:00}:{2:00}", bestRaceTime.Minutes, bestRaceTime.Seconds, bestRaceTime.Milliseconds);
            }
            else
            {
                bestTime.text = "Best: --:--:---";
            }
        }

        public void OnTriggerEnter(Collider other)
        {            
            bool betterLap = false;
            float currentLapTime = Time.time - lapStartTime;

            // Check for improved lap time
            if(lapStarted == true && (lapBestTime < 0f || currentLapTime < lapBestTime))
            {
                betterLap = true;
                lapBestTime = currentLapTime;

                // Save best lap
                PlayerPrefs.SetFloat("race.bestlap", lapBestTime);
            }

            lapStartTime = Time.time;

            Debug.LogWarning("Trigger");

            // Stop replaying
            if (ReplayManager.IsReplaying(playbackHandle) == true)
            {
                ReplayManager.StopPlayback(ref playbackHandle);
                playbackStorage.Dispose();
                playbackStorage = null;
            }

            // Stop recording
            if (ReplayManager.IsRecording(recordHandle) == true)
            {
                // Stop recording
                ReplayManager.StopRecording(ref recordHandle);
                Debug.Log("Recording Length: " + recordStorage.Duration);

                recordStorage.Dispose();
                recordStorage = null;

                if (betterLap == true)
                {
                    if (File.Exists("best.replay") == true)
                        File.Delete("best.replay");

                    File.Move("current.replay", "best.replay");
                }

                             
            }

            if (lapBestTime >= 0f && File.Exists("best.replay") == true)
            { 
                // Enable the ghost car
                ghostCar.gameObject.SetActive(true);

                // Clone identities - This allows the ghost car to be replayed as the player car
                ReplayObject.CloneReplayObjectIdentity(playerCar, ghostCar);

                playbackStorage = ReplayFileTarget.ReadReplayFile("best.replay");

                // Start replaying
                playbackHandle = ReplayManager.BeginPlayback(playbackStorage, playbackScene);

                // Add end playback listener
                ReplayManager.AddPlaybackEndListener(playbackHandle, OnGhostVehiclePlaybackComplete);
            }

            // Check for first time passed the start line
            if(lapStarted == false && File.Exists("best.replay") == true && playbackStorage == null)
            {
                // Load the replay
                playbackStorage = ReplayFileTarget.ReadReplayFile("best.replay");

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
            recordStorage = ReplayFileTarget.CreateReplayFile("current.replay");

            recordHandle = ReplayManager.BeginRecording(recordStorage, recordScene);

            lapStarted = true;
        }

        private void OnGhostVehiclePlaybackComplete()
        {
            // Hide ghost car
            ghostCar.gameObject.SetActive(false);

            Debug.Log("Playback end");
        }
    }
}
