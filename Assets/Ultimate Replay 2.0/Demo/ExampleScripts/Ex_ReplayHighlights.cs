using System;
using System.Collections;
using System.Collections.Generic;
using UltimateReplay.Storage;
using UnityEngine;

namespace UltimateReplay.Example
{
    /// <summary>
    /// This example demonstrates how multiple recorded segments can be replayed in a sequece of highlits such as goals in a football game or similar.
    /// A popular use case would be to record certain highlights in a game and replay them in a highlght reel when the game is over.
    /// </summary>
    public class Ex_ReplayHighlights : MonoBehaviour
    {
        // Private
        private List<ReplayStorageTarget> highlightsStorage = new List<ReplayStorageTarget>();
        private bool isReplayingHighlights = false;
        private int replayHighlightIndex = 0;
        private ReplayHandle replayHighlightHandle = ReplayHandle.invalid;

        // Methods
        public IEnumerator Start()
        {
            for(int i = 0; i < 5; i++)
            {
                ReplayMemoryTarget storage = new ReplayMemoryTarget();

                // Record some gameplay
                ReplayHandle handle = ReplayManager.BeginRecording(storage);

                // Wait for some data to be recorded
                yield return new WaitForSeconds(3);

                // End the recording
                ReplayManager.StopRecording(ref handle);

                // Add storage
                highlightsStorage.Add(storage);
            }
        }

        public void ReplayHighlights()
        {
            if (replayHighlightIndex >= 0 && replayHighlightIndex < highlightsStorage.Count)
            {
                isReplayingHighlights = true;
                replayHighlightIndex = 0;

                // Start replaying highlights
                replayHighlightHandle = ReplayManager.BeginPlayback(highlightsStorage[replayHighlightIndex]);

                // Add listener for playback finished
                ReplayManager.AddPlaybackEndListener(replayHighlightHandle, OnHighlightPlaybackFinished);
            }
        }

        public float GetReplayHighlightsTimeNormalized()
        {
            if(isReplayingHighlights == true)
            {
                float currentTime = 0;
                float totalDuration = 0;

                for(int i = 0; i < highlightsStorage.Count; i++)
                {
                    if(i < replayHighlightIndex)
                        currentTime += highlightsStorage[i].Duration;

                    totalDuration += highlightsStorage[i].Duration;
                }

                currentTime += ReplayManager.GetPlaybackTime(replayHighlightHandle).Time;

                return Mathf.InverseLerp(0, totalDuration, currentTime);
            }
            return 0;
        }

        public void SeekReplayHighlightsNormalized(float normalizedOffset)
        {
            if(isReplayingHighlights == true)
            {
                float totalDuration = 0;

                for (int i = 0; i < highlightsStorage.Count; i++)
                    totalDuration += highlightsStorage[i].Duration;

                float targetTime = Mathf.Lerp(0, totalDuration, Mathf.Clamp01(normalizedOffset));

                for(int i = 0; i < highlightsStorage.Count; i++)
                {
                    if(targetTime < highlightsStorage[i].Duration)
                    {
                        ReplayManager.StopPlayback(ref replayHighlightHandle);
                        replayHighlightHandle = ReplayManager.BeginPlayback(highlightsStorage[i]);
                        ReplayManager.SetPlaybackTime(replayHighlightHandle, targetTime);
                        break;
                    }

                    targetTime -= highlightsStorage[i].Duration;
                }
            }
        }

        private void OnHighlightPlaybackFinished()
        {
            // Increase storage index
            replayHighlightIndex++;

            if (replayHighlightIndex >= 0 && replayHighlightIndex < highlightsStorage.Count)
            {
                // Start replaying highlights
                replayHighlightHandle = ReplayManager.BeginPlayback(highlightsStorage[replayHighlightIndex]);

                // Add listener for playback finished
                ReplayManager.AddPlaybackEndListener(replayHighlightHandle, OnHighlightPlaybackFinished);
            }
            else
            {
                isReplayingHighlights = false;
                replayHighlightIndex = 0;
                replayHighlightHandle = ReplayHandle.invalid;
            }
        }
    }
}
