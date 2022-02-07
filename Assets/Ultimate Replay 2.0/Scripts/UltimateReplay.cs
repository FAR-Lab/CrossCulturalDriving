using System;
using UltimateReplay.Core;
using UnityEngine;

namespace UltimateReplay
{
    /// <summary>
    /// Settings class used to hold the global project settings for Ultimate Replay.
    /// Use <see cref="UltimateReplay.Settings"/> to get or load the current settings instance.
    /// </summary>
#if ULTIMATEREPLAY_DEV
    [CreateAssetMenu(fileName = "UltimateReplaySettings", menuName = "UltimateReplay/Create Setting Asset")]
#endif
    public class UltimateReplay : ScriptableObject
    {
        /// <summary>
        /// The update method used by the replay manager for all recording and replaying samples.
        /// </summary>
        public enum UpdateMethod
        {
            /// <summary>
            /// Use the Update method.
            /// </summary>
            Update,
            /// <summary>
            /// Use the late update method.
            /// </summary>
            LateUpdate,
            /// <summary>
            /// Use the fixed update method.
            /// </summary>
            FixedUpdate,
        }

        // Events
        /// <summary>
        /// Called by the replay system whenever it needs to instantiate a prefab for use during playback.
        /// You can add a listener to override the default behaviour which can be useful if you want to handle the instantiation manually for purposes such as object pooling.
        /// </summary>
        public static Func<GameObject, Vector3, Quaternion, GameObject> OnReplayInstantiate;

        /// <summary>
        /// Called by the replay system whenever it needs to destroy a game object in order to restore a previous scene state.
        /// You can add a listener to override the default behaviour which can be useful if you want to handle the destruction manually for purposes such as object pooling.
        /// </summary>
        public static Action<GameObject> OnReplayDestroy;

        // Private
        private static UltimateReplay settingsInstance = null;

        // Public
        [HideInInspector]
        public DefaultReplayPreparer defaultReplayPreparer = new DefaultReplayPreparer();
        /// <summary>
        /// The default record options used when recording a scene.
        /// </summary>
        public ReplayRecordOptions recordOptions = new ReplayRecordOptions();
        /// <summary>
        /// The default replay options used when replaying a scene.
        /// </summary>
        public ReplayPlaybackOptions playbackOptions = new ReplayPlaybackOptions();
        /// <summary>
        /// A collection of prefabs which can be instantiated or destroyed during recording.
        /// </summary>
        public ReplayablePrefabs prefabs = new ReplayablePrefabs();

        // Properties
        /// <summary>
        /// Get the current Ultimate Replay settings.
        /// </summary>
        public static UltimateReplay Settings
        {
            get
            {
                // Load settings if required
                if (settingsInstance == null)
                    settingsInstance = Resources.Load<UltimateReplay>("UltimateReplaySettings");

                // Check for error - create default instance
                if (settingsInstance == null)
                    settingsInstance = CreateInstance<UltimateReplay>();

                return settingsInstance;
            }
        }

        // Methods
        /// <summary>
        /// Request a prefab to be instantiated at the specified position and rotation.
        /// This will cause the <see cref="OnReplayInstantiate"/> event to be invoked so that pooling support can easily be added.
        /// </summary>
        /// <param name="prefab">The prefab to instantiate</param>
        /// <param name="position">The instantiate position</param>
        /// <param name="rotation">The instantiate rotation</param>
        /// <returns>An instance of the specified prefab</returns>
        public static GameObject ReplayInstantiate(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if(OnReplayInstantiate != null)
            {
                try
                {
                    // Try to instantiate
                    GameObject result =  OnReplayInstantiate(prefab, position, rotation);

                    if (result == null)
                        throw new Exception("Result of user instantiate call was null");

                    return result;
                }
                catch(Exception e)
                {
                    Debug.LogErrorFormat("User callback 'OnReplayInstantiate' caused an exception ({0}). Falling back to default instantiate behaviour", e);
                }
            }

            // Use direct instantiate
            return GameObject.Instantiate(prefab, position, rotation);
        }

        /// <summary>
        /// Request a scene object to be destroyed.
        /// This will cause the <see cref="OnReplayDestroy"/> event to be invoked so that pooling support can easily be added.
        /// </summary>
        /// <param name="go">The game object to destroy</param>
        public static void ReplayDestroy(GameObject go)
        {
            if (OnReplayDestroy != null)
            {
                try
                {
                    // Try to call user method
                    OnReplayDestroy(go);
                    return;
                }
                catch (Exception e)
                {
                    Debug.LogErrorFormat("User callback 'OnReplayDestroy' caused an exception ({0}). Falling back to default destroy behaviour", e);
                }
            }

            if(Application.isPlaying == false)
            {
                GameObject.DestroyImmediate(go);
                return;
            }

            // Use direct instantiate
            GameObject.Destroy(go);
        }

        public DefaultReplayPreparer CreateDefaultPreparer()
        {
            // Create instance of settings
            return defaultReplayPreparer.CreateInstance();
        }
    }
}
