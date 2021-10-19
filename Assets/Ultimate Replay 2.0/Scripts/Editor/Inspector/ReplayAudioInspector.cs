using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;

namespace UltimateReplay
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ReplayAudio))]
    public class ReplayAudioInspector : ReplayRecordableBehaviourInspector
    {
        // Private
#pragma warning disable 0414
        private ReplayAudio targetAudio = null;
#pragma warning restore 0414
        private ReplayAudio[] targetAudios = null;

        // Methods
        public override void OnEnable()
        {
            base.OnEnable();

            // Get correct type instances
            GetTargetInstances(out targetAudio, out targetAudios);
        }

        public override void OnInspectorGUI()
        {
            DisplayDefaultInspectorProperties();


            // Get values
            bool[] pitch = GetFlagValuesForTargets(ReplayAudio.ReplayAudioFlags.Pitch);
            bool[] volume = GetFlagValuesForTargets(ReplayAudio.ReplayAudioFlags.Volume);
            bool[] stereoPan = GetFlagValuesForTargets(ReplayAudio.ReplayAudioFlags.StereoPan);
            bool[] spatialBlend = GetFlagValuesForTargets(ReplayAudio.ReplayAudioFlags.SpatialBlend);
            bool[] reverbZoneMix = GetFlagValuesForTargets(ReplayAudio.ReplayAudioFlags.ReverbZoneMix);
            bool[] interpolate = GetFlagValuesForTargets(ReplayAudio.ReplayAudioFlags.Interpolate);
            bool[] lowPrecision = GetFlagValuesForTargets(ReplayAudio.ReplayAudioFlags.LowPrecision);

            // Display fields
            DisplayMultiEditableToggleField("Replay Pitch", "Record pitch value", ref pitch);
            DisplayMultiEditableToggleField("Replay Volume", "Record volume value", ref volume);
            DisplayMultiEditableToggleField("Replay Stereo Pan", "Record stereo pan value", ref stereoPan);
            DisplayMultiEditableToggleField("Replay Spatial Blend", "Record spatial blend value", ref spatialBlend);
            DisplayMultiEditableToggleField("Replay Reverb Zone Mix", "Record reverb zone mix value", ref reverbZoneMix);
            DisplayMultiEditableToggleField("Interpolate", "Interpolate the timesample value of the audio source", ref interpolate);
            DisplayMultiEditableToggleField("Low Precision", "Record supported data in low precision to reduce storage usage. Not recommended for main objects such as player", ref lowPrecision);

            // Set flags values
            SetFlagValuesForTargets(ReplayAudio.ReplayAudioFlags.Pitch, pitch);
            SetFlagValuesForTargets(ReplayAudio.ReplayAudioFlags.Volume, volume);
            SetFlagValuesForTargets(ReplayAudio.ReplayAudioFlags.StereoPan, stereoPan);
            SetFlagValuesForTargets(ReplayAudio.ReplayAudioFlags.SpatialBlend, spatialBlend);
            SetFlagValuesForTargets(ReplayAudio.ReplayAudioFlags.ReverbZoneMix, reverbZoneMix);
            SetFlagValuesForTargets(ReplayAudio.ReplayAudioFlags.Interpolate, interpolate);
            SetFlagValuesForTargets(ReplayAudio.ReplayAudioFlags.LowPrecision, lowPrecision);


            // Display replay stats
            DisplayReplayStorageStatistics();
        }

        private bool[] GetFlagValuesForTargets(ReplayAudio.ReplayAudioFlags flag)
        {
            bool[] values = new bool[targetAudios.Length];

            for(int i = 0; i < targetAudios.Length; i++)
            {
                values[i] = (targetAudios[i].recordFlags & flag) != 0;
            }

            return values;
        }

        private void SetFlagValuesForTargets(ReplayAudio.ReplayAudioFlags flag, bool[] toggleValues)
        {
            for(int i = 0; i < targetAudios.Length; i++)
            {
                if (toggleValues[i] == true) targetAudios[i].recordFlags |= flag;
                else targetAudios[i].recordFlags &= ~flag;
            }
        }
    }
}
