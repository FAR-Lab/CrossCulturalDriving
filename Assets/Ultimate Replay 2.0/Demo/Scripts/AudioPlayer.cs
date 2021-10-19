using UnityEngine;

namespace UltimateReplay.Example
{
    /// <summary>
    /// Used in the audio example to play an audio source so that it can be replayed.
    /// </summary>
    public class AudioPlayer : MonoBehaviour
    {
        // Public
        /// <summary>
        /// The audio source used to play sounds.
        /// </summary>
        public AudioSource targetAudio;

        // Methods
        /// <summary>
        /// Called by Unity.
        /// </summary>
        public void OnGUI()
        {
            // Only allow play when recording
            if (ReplayManager.IsRecordingAny == false)
                return;

            if(GUILayout.Button("Play Audio") == true)
            {
                if (targetAudio != null)
                    targetAudio.Play();
            }
        }
    }
}