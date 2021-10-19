using System;
using System.Collections.Generic;
using UltimateReplay.Core;
using UltimateReplay.Storage;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UltimateReplay
{
    /// <summary>
    /// A <see cref="ReplayScene"/> contains information about all active replay objects. 
    /// </summary>
    public sealed class ReplayScene : ReplayLockable
    {
        /// <summary>
        /// The scene state value used to determine which mode a particular scene instance is in.
        /// </summary>
        public enum ReplaySceneMode
        {
            /// <summary>
            /// The scene and all child objects are in live mode meaning gameplay can continue as normal.
            /// </summary>
            Live,
            /// <summary>
            /// The scene and all child objects are in playback mode. Objects in the scene should not be interfered with and will be updated frequently.
            /// </summary>
            Playback,
            /// <summary>
            /// The scene and all child objects are in record mode. Gameplay can continue but objects will be sampled frequently.
            /// </summary>
            Record,
        }

        // Events
        /// <summary>
        /// Called when a replay object was added to this <see cref="ReplayScene"/>.
        /// </summary>
        public event Action<ReplayObject> OnReplayObjectAdded;

        /// <summary>
        /// Called when a replay object was removed from this <see cref="ReplayScene"/>.
        /// </summary>
        public event Action<ReplayObject> OnReplayObjectRemoved;

        // Private
        private static IReplayPreparer defaultReplayPreparer = null;
        private static ReplayScene currentScene = null;

        private IReplayPreparer replayPreparer = null;
        private List<ReplayObject> replayObjects = new List<ReplayObject>();
        //private Dictionary<ReplayIdentity, ReplayObject> replayObjects = new Dictionary<ReplayIdentity, ReplayObject>();
        private List<ReplayBehaviour> replayBehaviours = new List<ReplayBehaviour>();
        private Queue<ReplayObject> dynamicReplayObjects = new Queue<ReplayObject>();
        private ReplaySnapshot prePlaybackState = null;
        private ReplayHandle activeHandle = ReplayHandle.invalid;
        private bool isPlayback = false;

        // Public        
        /// <summary>
        /// A value indicating whether the replay objects stored in this scene instance should be reverted to their initial state when playback ends.
        /// </summary>
        public bool restorePreviousSceneState = true;

        // Properties
        /// <summary>
        /// Get a <see cref="ReplayScene"/> instance containing all <see cref="ReplayObject"/> instance located within the current active scene.
        /// </summary>
        public static ReplayScene CurrentScene
        {
            get
            {
                // Check if the cached scene has not been created
                if(currentScene == null)
                {
                    // Create a replay scene from the current scene
                    currentScene = FromCurrentScene();
                }

                return currentScene;
            }
        }

        /// <summary>
        /// Enable or disable the replay scene in preparation for playback or live mode.
        /// When true, all replay objects will be prepared for playback causing certain components or scripts to be disabled to prevent interference from game systems.
        /// A prime candidate would be the RigidBody component which could cause a replay object to be affected by gravity and as a result deviate from its intended position.
        /// When false, all replay objects will be returned to their 'Live' state when all game systems will be reactivated.
        /// </summary>
        public bool ReplayEnabled
        {
            get { return isPlayback; }
        }
        
        /// <summary>
        /// Returns a value indicating whether the <see cref="ReplayScene"/> contains any <see cref="ReplayObject"/>.
        /// </summary>
        public bool IsEmpty
        {
            get { return replayObjects.Count == 0; }
        }

        /// <summary>
        /// Get a collection of all game objects that are registered with the replay system.
        /// </summary>
        public IReadOnlyCollection<ReplayObject> ActiveReplayObjects
        {
            get { return replayObjects; }//.Values; }
        }

        /// <summary>
        /// Get a collection of all <see cref="ReplayBehaviour"/> components that are registered in this <see cref="ReplayScene"/>.
        /// </summary>
        public IReadOnlyCollection<ReplayBehaviour> ActiveReplayBehaviours
        {
            get { return replayBehaviours; }
        }

        // Constructor
        /// <summary>
        /// Called when <see cref="ReplayScene"/> is accessed for the first time.
        /// </summary>
        static ReplayScene()
        {
            SceneManager.sceneLoaded += (scene, loadMode) =>
            {
                // Reset scene
                currentScene = null;
            };
            SceneManager.activeSceneChanged += (scene0, scene1) =>
            {
                // Reset scene
                currentScene = null;
            };
        }

        /// <summary>
        /// Create a new replay scene with no <see cref="ReplayObject"/> added.
        /// </summary>
        /// <param name="replayPreparer">A <see cref="IReplayPreparer"/> implementation used to prepare scene objects when switching between playback and live scene modes</param>
        public ReplayScene(IReplayPreparer replayPreparer = null)
        {
            // Create shared default preparer instance
            if(defaultReplayPreparer == null)
            {
                // Create instance
                defaultReplayPreparer = UltimateReplay.Settings.CreateDefaultPreparer();
            }

            if (replayPreparer == null)
                replayPreparer = defaultReplayPreparer;

            this.replayPreparer = replayPreparer;
        }

        /// <summary>
        /// Create a new replay scene and add the specified replay object.
        /// </summary>
        /// <param name="replayObject">The single <see cref="ReplayObject"/> to add to the scene</param>
        /// <param name="replayPreparer">A <see cref="IReplayPreparer"/> implementation used to prepare scene objects when switching between playback and live scene modes</param>
        public ReplayScene(ReplayObject replayObject, IReplayPreparer replayPreparer = null)
        {
            // Paremeter was null
            if (replayObject == null)
                throw new ArgumentNullException("replayObject");

            // Create shared default preparer instance
            if (defaultReplayPreparer == null)
            {
                // Create instance
                defaultReplayPreparer = UltimateReplay.Settings.CreateDefaultPreparer();
            }

            if (replayPreparer == null)
                replayPreparer = defaultReplayPreparer;

            this.replayPreparer = replayPreparer;

            // Add object to scene
            AddReplayObject(replayObject);
        }

        /// <summary>
        /// Create a new replay scene from the specified collection or replay objects.
        /// </summary>
        /// <param name="replayObjects">A collection of <see cref="ReplayObject"/> that will be added to the scene</param>
        /// <param name="replayPreparer">A <see cref="IReplayPreparer"/> implementation used to prepare scene objects when switching between playback and live scene modes</param>
        public ReplayScene(IEnumerable<ReplayObject> replayObjects, IReplayPreparer replayPreparer = null)
        {
            if (replayPreparer == null)
                replayPreparer = defaultReplayPreparer;

            this.replayPreparer = replayPreparer;

            foreach(ReplayObject obj in replayObjects)
            {
                // Only add if not null
                if (obj != null)
                    AddReplayObject(obj);
            }
        }

        // Methods
        /// <summary>
        /// Registers a replay object with the replay system so that it can be recorded for playback.
        /// Typically all <see cref="ReplayObject"/> will auto register when they 'Awake' meaning that you will not need to manually register objects. 
        /// </summary>
        /// <param name="replayObject">The <see cref="ReplayObject"/> to register</param>
        public void AddReplayObject(ReplayObject replayObject)
        {
            // Check for null
            if (replayObject == null)
                throw new ArgumentNullException("replayObject");

            // Check for already added
            if (replayObjects.Contains(replayObject) == true)
            //if(replayObjects.ContainsValue(replayObject) == true)
                return;

            // Add the replay object
            replayObjects.Add(replayObject);
            //if (replayObjects.ContainsKey(replayObject.ReplayIdentity) == true)
                //replayObject.ForceRegenerateIdentity();

            //replayObjects.Add(replayObject.ReplayIdentity, replayObject);

            // Check for disabled object
            if (replayObject.gameObject.activeInHierarchy == false || Application.isPlaying == false)
                replayObject.UpdateRuntimeComponents();

            // Trigger event
            if (OnReplayObjectAdded != null)
                OnReplayObjectAdded(replayObject);

            // Check if we are adding objects during playback
            if(isPlayback == true)
            {
                // We need to prepare the object for playback
                replayPreparer.PrepareForPlayback(replayObject);
            }
            // Check if we are adding objects during recording
            else // if(ReplayManager.IsRecording == true)
            {
                // The object was added during recording
                //if(replayObject.IsPrefab == true)
                    dynamicReplayObjects.Enqueue(replayObject);
            }

            // Find all child behaviours
            ReplayBehaviour[] behaviours = replayObject.GetComponentsInChildren<ReplayBehaviour>();

            // Set handle
            ReplayBehaviour.ApplyReplayHandle(behaviours, activeHandle);

            // Register all behaviours
            replayBehaviours.AddRange(behaviours);
        }

        /// <summary>
        /// Unregisters a replay object from the replay system so that it will no longer be recorded for playback.
        /// Typically all <see cref="ReplayObject"/> will auto un-register when they are destroyed so you will normally not need to un-register a replay object. 
        /// </summary>
        /// <param name="replayObject"></param>
        public void RemoveReplayObject(ReplayObject replayObject)
        {
            // Cannot remove null
            if (replayObject == null)
                return;

            // Remove the replay object
            if (replayObjects.Contains(replayObject) == true)
            //if(replayObjects.ContainsValue(replayObject) == true)
            {
                replayObjects.Remove(replayObject);
                //replayObjects.Remove(replayObject.ReplayIdentity);

                // Trigger event
                if (OnReplayObjectRemoved != null)
                    OnReplayObjectRemoved(replayObject);
            }

            // Find all behvaiour components
            ReplayBehaviour[] behaviours = replayObject.GetComponents<ReplayBehaviour>();

            // Set handle
            ReplayBehaviour.ApplyReplayHandle(behaviours, ReplayHandle.invalid);

            // Unregister behaviours
            foreach (ReplayBehaviour behaviour in behaviours)
            {
                if (replayBehaviours.Contains(behaviour) == true)
                    replayBehaviours.Remove(behaviour);
            }
        }

        /// <summary>
        /// Set the current replay scene mode.
        /// Use this method to switch the scene between playback and live modes.
        /// Playback modes will run the <see cref="replayPreparer"/> on all scene objects to disable or re-enable elements that could affect playback.
        /// </summary>
        /// <param name="mode">The scene mode to switch to</param>
        /// <param name="initialStateBuffer">The initial state buffer</param>
        /// <param name="handle">The <see cref="ReplayHandle"/> of the current record or replay operation</param>
        public void SetReplaySceneMode(ReplaySceneMode mode, ReplayInitialDataBuffer initialStateBuffer, ReplayHandle handle)
        {
            this.activeHandle = handle;

            if (mode == ReplaySceneMode.Playback)
            {
                // Get the scene ready for playback
                PrepareForPlayback(initialStateBuffer);
                isPlayback = true;

                // Apply the behaviour handle
                ReplayBehaviour.ApplyReplayHandle(replayBehaviours, handle);
            }
            else
            {
                // Get the scene ready for gameplay                
                isPlayback = false;

                if (mode == ReplaySceneMode.Record)
                {
                    ReplayBehaviour.ApplyReplayHandle(replayBehaviours, handle);
                }
                else
                {
                    // Return to live mode
                    PrepareForGameplay(initialStateBuffer);

                    // Apply invalid handle
                    ReplayBehaviour.ApplyReplayHandle(replayBehaviours, ReplayHandle.invalid);
                }
            }
        }

        private void PrepareForPlayback(ReplayInitialDataBuffer initialStateBuffer)
        {
            // Sample the current scene
            prePlaybackState = CaptureSnapshot(0, 0, initialStateBuffer);

            for (int i = 0; i < replayObjects.Count; i++)
            {
                if (replayObjects[i] != null)
                {
                    // Prepare the object for playback
                    replayPreparer.PrepareForPlayback(replayObjects[i]);
                }
            }
            //foreach (ReplayObject replayObject in replayObjects.Values)
            //{
            //    if (replayObject != null)
            //    {
            //        replayPreparer.PrepareForPlayback(replayObject);
            //    }
            //}
        }

        private void PrepareForGameplay(ReplayInitialDataBuffer initialStateBuffer)
        {
            // Check if we can restore the previous scene state
            if (prePlaybackState != null)
            {
                // Restore the original game state
                if (restorePreviousSceneState == true)
                    RestoreSnapshot(prePlaybackState, initialStateBuffer);

                // Reset to null so that next states are saved
                prePlaybackState = null;
            }

            for (int i = 0; i < replayObjects.Count; i++)
            {
                if (replayObjects[i] != null)
                {
                    replayPreparer.PrepareForGameplay(replayObjects[i]);
                }
            }
            //foreach (ReplayObject replayObject in replayObjects.Values)
            //{
            //    if(replayObject != null)
            //    {
            //        replayPreparer.PrepareForGameplay(replayObject);
            //    }
            //}
        }

        /// <summary>
        /// Take a snapshot of the current replay scene using the specified timestamp.
        /// </summary>
        /// <param name="timeStamp">The timestamp for the frame indicating its position in the playback sequence</param>
        /// <param name="initialStateBuffer">The <see cref="ReplayInitialDataBuffer"/> to restore dynamic object information from</param>
        /// <returns>A new snapshot of the current replay scene</returns>
        public ReplaySnapshot CaptureSnapshot(float timeStamp, int sequenceID, ReplayInitialDataBuffer initialStateBuffer)
        {
            ReplaySnapshot snapshot = new ReplaySnapshot(timeStamp, sequenceID);

            if (initialStateBuffer != null)
            {
                // Be sure to record any objects initial transform if they were spawned during the snapshot
                while (dynamicReplayObjects.Count > 0)
                {
                    // Get the next object
                    ReplayObject obj = dynamicReplayObjects.Dequeue();

                    // Make sure the object has not been destroyed
                    if (obj != null)
                    {
                        // Record initial values
                        initialStateBuffer.RecordInitialReplayObjectData(obj, timeStamp, obj.transform.position, obj.transform.rotation, obj.transform.localScale);
                    }
                }
            }


            // Record each object in the scene
            foreach (ReplayObject obj in replayObjects)
            //foreach(ReplayObject obj in replayObjects.Values)
            {
                ReplayState state = ReplayState.pool.GetReusable();

                // Serialize the object
                obj.OnReplaySerialize(state);

                // Check if the state contains any information - If not then dont waste valuable memory
                if (state.Size == 0)
                    continue;

                // Record the snapshot
                snapshot.RecordSnapshot(obj.ReplayIdentity, state);
            }

            return snapshot;
        }

        /// <summary>
        /// Restore the scene to the state described by the specified snapshot.
        /// </summary>
        /// <param name="snapshot">The snapshot to restore</param>
        /// <param name="initialStateBuffer">The <see cref="ReplayInitialDataBuffer"/> to restore dynamic object information from</param>
        public void RestoreSnapshot(ReplaySnapshot snapshot, ReplayInitialDataBuffer initialStateBuffer)
        {
            // Restore all events first
            snapshot.RestoreReplayObjects(this, initialStateBuffer);

            // Restore all replay objects
            foreach (ReplayObject obj in replayObjects)
            //foreach(ReplayObject obj in replayObjects.Values)
            {
                // Get the state based on the identity
                ReplayState state = snapshot.RestoreSnapshot(obj.ReplayIdentity);

                // Check if no state information for this object was found
                if (state == null)
                    continue;

                // Deserialize the object
                obj.OnReplayDeserialize(state);
            }
        }

        public bool HasReplayObject(ReplayIdentity replayIdentity)
        {
            return replayObjects.Exists(o => o.ReplayIdentity == replayIdentity);
            //return replayObjects.ContainsKey(replayIdentity);
        }

        public bool CheckIntegrity(bool throwOnError)
        {
            int count = replayObjects.RemoveAll(r => r == null);

            //int count = 0;

            //Stack<ReplayIdentity> deadKeys = new Stack<ReplayIdentity>();

            //foreach (KeyValuePair<ReplayIdentity, ReplayObject> item in replayObjects)
            //{
            //    if (item.Value == null)
            //    {
            //        deadKeys.Push(item.Key);
            //        count++;
            //    }
            //}

            //while (deadKeys.Count > 0)
            //    replayObjects.Remove(deadKeys.Pop());

            count += replayBehaviours.RemoveAll(b => b == null);

            int dynamicCount = dynamicReplayObjects.Count;

            for(int i = 0; i < dynamicCount; i++)
            {
                // Get the item
                ReplayObject dynamic = dynamicReplayObjects.Dequeue();

                if(dynamic != null)
                {
                    dynamicReplayObjects.Enqueue(dynamic);
                }
                else
                {
                    count++;
                }
            }

            // Check for error
            if (throwOnError == true && count > 0)
                throw new Exception("One or more replay objects have been destroyed but are still registered with a replay scene instance. You should remove any dead objects from a replay scene before starting playback or recording");

            return count == 0;
        }

        /// <summary>
        /// Attempt to find a <see cref="ReplayObject"/> with the specified <see cref="ReplayIdentity"/>
        /// </summary>
        /// <param name="replayIdentity">The identity of the object to find</param>
        /// <returns>A <see cref="ReplayObject"/> with the specified ID or null if the object was not found</returns>
        public ReplayObject GetReplayObject(ReplayIdentity replayIdentity)
        {
            return replayObjects.Find(o => o.ReplayIdentity == replayIdentity);
            //ReplayObject result = null;
            //replayObjects.TryGetValue(replayIdentity, out result);
            //return result;
        }

        /// <summary>
        /// Create a new replay scene from the active Unity scene.
        /// All <see cref="ReplayObject"/> in the active scene will be added to the <see cref="ReplayScene"/> result.
        /// The active scene is equivilent of <see cref="SceneManager.GetActiveScene"/>;
        /// </summary>
        /// <returns>A new ReplayScene instance</returns>
        public static ReplayScene FromCurrentScene(IReplayPreparer preparer = null)
        {
            return FromScene(SceneManager.GetActiveScene(), preparer);
        }

        /// <summary>
        /// Create a new replay scene from the specified Unity scene.
        /// All <see cref="ReplayScene"/> in the specified scene will be added to the <see cref="ReplayScene"/> result. 
        /// </summary>
        /// <param name="scene">The Unity scene used to create the <see cref="ReplayScene"/></param>
        /// <returns>A new ReplayScene instance</returns>
        public static ReplayScene FromScene(Scene scene, IReplayPreparer preparer = null)
        {
            // Create an empty scene
            ReplayScene replayScene = new ReplayScene(preparer);

            // Check all registered objects
            foreach (ReplayObject obj in ReplayObject.AllReplayObjects)
            {
                // Check for same scene
                if(obj.gameObject.scene.name != null && 
                    obj.gameObject.scene.name == scene.name)
                {
                    // Add the replay object
                    replayScene.AddReplayObject(obj);
                }
            }

            return replayScene;
        }
    }
}
