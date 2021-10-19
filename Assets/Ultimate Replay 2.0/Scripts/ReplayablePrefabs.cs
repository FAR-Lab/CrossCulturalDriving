using System;
using System.Collections.Generic;
using UltimateReplay.Core;
using UnityEngine;

namespace UltimateReplay
{
    /// <summary>
    /// A collection of prefabs that the replay system can use to recreate the scene when instantiate or destroy is called while recording.
    /// </summary>
    [Serializable]
    public sealed class ReplayablePrefabs
    {
        // Private
        private Dictionary<ReplayIdentity, ReplayObject> runtimePrefabLookup = null;

        [SerializeField]//, HideInInspector]
        private List<ReplayObject> prefabs = new List<ReplayObject>();

        // Properties
        private Dictionary<ReplayIdentity, ReplayObject> PrefabLookup
        {
            get
            {
                if(runtimePrefabLookup == null)
                {
                    runtimePrefabLookup = new Dictionary<ReplayIdentity, ReplayObject>();

                    // Build runtime lookup for easier and quicker access
                    foreach (ReplayObject replayObject in prefabs)
                        if(replayObject != null)
                            runtimePrefabLookup.Add(replayObject.PrefabIdentity, replayObject);
                }
                return runtimePrefabLookup;
            }
        }

        public ICollection<ReplayIdentity> PrefabIdentites
        {
            get { return PrefabLookup.Keys; }
        }

        public IList<ReplayObject> Prefabs
        {
            get { return prefabs; }
        }

        // Methods
        public void RegisterReplayablePrefab(ReplayObject replayPrefabObject)
        {
            // Check for null
            if (replayPrefabObject == null) throw new ArgumentNullException("replayPrefabObject");

            // Check for prefab
            if (replayPrefabObject.IsPrefab == false)
                throw new InvalidOperationException("The specified replay object is not a prefab asset");

            // Check for already added
            if (PrefabLookup.ContainsKey(replayPrefabObject.PrefabIdentity) == true)
                return;

            // Register object
            prefabs.Add(replayPrefabObject);

            // Runtime register object
            PrefabLookup.Add(replayPrefabObject.PrefabIdentity, replayPrefabObject);
        }

        public bool HasReplayPrefab(ReplayIdentity replayPrefabIdentity)
        {
            // Check for key
            return PrefabLookup.ContainsKey(replayPrefabIdentity);
        }

        public ReplayObject GetReplayPrefab(ReplayIdentity replayPrefabIdentity)
        {
            ReplayObject result = null;

            // Try to find entry
            PrefabLookup.TryGetValue(replayPrefabIdentity, out result);

            return result;
        }

        public ReplayObject GetReplayPrefabWithName(string prefabName)
        {
            // Try to find with name
            return prefabs.Find(p => p.gameObject.name == prefabName);
        }
    }
}
