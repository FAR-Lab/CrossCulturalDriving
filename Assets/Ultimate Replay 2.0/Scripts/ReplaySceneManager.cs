using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UltimateReplay.Storage;
using UnityEngine;

namespace UltimateReplay
{
    public class ReplaySceneManager : MonoBehaviour
    {
        // Types
        public enum ReplayStorageMethod
        {
            StoreInMemory,
            StreamToFile,
        }

        // Private
        private static ReplaySceneManager instance = null;

        private ReplayScene currentScene = null;
        private ReplayStorageTarget storageTarget = null;
        private ReplayHandle recordHandle = ReplayHandle.invalid;
        private ReplayHandle playbackHandle = ReplayHandle.invalid;

        // Public
        public bool recordOnStart = true;
        public ReplayStorageMethod recordStorageMethod = ReplayStorageMethod.StoreInMemory;
        public string recordFileFolder = "ReplayFiles";
        public ReplayFileTarget.ReplayFileFormat recordFileFormat = ReplayFileTarget.ReplayFileFormat.Binary;

        // Properties
        public static ReplaySceneManager Instance
        {
            get { return instance; }
        }

        public bool IsRecording
        {
            get { return ReplayManager.IsRecording(recordHandle); }
        }

        public bool IsRecordingPaused
        {
            get { return ReplayManager.IsRecordingPaused(recordHandle); }
        }

        public ReplayTime PlaybackTime
        {
            get { return ReplayManager.GetPlaybackTime(playbackHandle); }
        }

        public bool IsReplaying
        {
            get { return ReplayManager.IsReplaying(playbackHandle); }
        }

        public bool IsPlaybackPaused
        {
            get { return ReplayManager.IsPlaybackPaused(playbackHandle); }
        }

        public ReplayStorageTarget ReplayStorageTarget
        {
            get { return storageTarget; }
        }

        public ReplayScene ReplayScene
        {
            get { return currentScene; }
        }

        // Methods
        public void Awake()
        {
            instance = this;
        }

        public void Start()
        {
            // Create the replay scene
            currentScene = ReplayScene.FromCurrentScene();

            // Start recording
            if (recordOnStart == true)
                BeginRecording();
        }

        public void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        public void BeginRecording(bool cleanRecording = true, bool allowEmptyScene = false, ReplayRecordOptions recordOptions = null)
        {
            // Create the target
            CreateStorageTarget();

            // Start recording
            recordHandle = ReplayManager.BeginRecording(storageTarget, currentScene, cleanRecording, allowEmptyScene, recordOptions);
        }

        public void PauseRecording()
        {
            ReplayManager.PauseRecording(recordHandle);
        }

        public void ResumeRecording()
        {
            ReplayManager.ResumeRecording(recordHandle);
        }

        public void StopRecording()
        {
            ReplayManager.StopPlayback(ref recordHandle);
        }

        public void SetPlaybackTime(float playbackTimeOffset, PlaybackOrigin origin = PlaybackOrigin.Start)
        {
            ReplayManager.SetPlaybackTime(playbackHandle, playbackTimeOffset, origin);
        }

        public void SetPlaybackTimeNormalized(float playbackTimeNormalizedOffset, PlaybackOrigin origin = PlaybackOrigin.Start)
        {
            ReplayManager.SetPlaybackTimeNormalized(playbackHandle, playbackTimeNormalizedOffset, origin);
        }

        public void SetPlaybackDirection(ReplayManager.PlaybackDirection direction)
        {
            ReplayManager.SetPlaybackDirection(playbackHandle, direction);
        }

        public void SetPlaybackTimeScale(float timeScale = 1f)
        {
            ReplayManager.SetPlaybackTimeScale(playbackHandle, timeScale);
        }

        public void BeginPlaybackFrame(bool allowEmptyScene = false, ReplayPlaybackOptions playbackOptions = null)
        {
            playbackHandle = ReplayManager.BeginPlaybackFrame(storageTarget, currentScene, allowEmptyScene, true, playbackOptions);
        }

        public void BeginPlayback(bool allowEmptyScene = false, ReplayPlaybackOptions playbackOptions = null)
        {
            playbackHandle = ReplayManager.BeginPlayback(storageTarget, currentScene, allowEmptyScene, true, playbackOptions);
        }

        public void PausePlayback()
        {
            ReplayManager.PausePlayback(playbackHandle);
        }

        public void ResumePlayback()
        {
            ReplayManager.ResumePlayback(playbackHandle);
        }

        public void AddPlaybackEndListener(Action playbackEndCallback)
        {
            ReplayManager.AddPlaybackEndListener(playbackHandle, playbackEndCallback);
        }

        public void RemovePlaybackEndListener(Action playbackEndCallback)
        {
            ReplayManager.RemovePlaybackEndListener(playbackHandle, playbackEndCallback);
        }

        public void StopPlayback(bool restorePreviousSceneState = true)
        {
            ReplayManager.StopPlayback(ref playbackHandle, restorePreviousSceneState);
        }

        private void CreateStorageTarget()
        {
            if(storageTarget == null)
            {
                if(recordStorageMethod == ReplayStorageMethod.StoreInMemory)
                {
                    storageTarget = new ReplayMemoryTarget();
                }
                else if(recordStorageMethod == ReplayStorageMethod.StreamToFile)
                {
                    storageTarget = ReplayFileTarget.CreateUniqueReplayFile(recordFileFolder, null, recordFileFormat);
                }
            }
        }
    }
}
