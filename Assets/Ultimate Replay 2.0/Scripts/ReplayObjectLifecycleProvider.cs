using System;
using UltimateReplay.Core;
using UnityEngine;

namespace UltimateReplay
{
    [Serializable]
    public abstract class ReplayObjectLifecycleProvider : ScriptableObject
    {
        // Properties
        public abstract bool IsAssigned { get; }

        public abstract string ItemName { get; }

        public abstract ReplayIdentity ItemIdentity { get; }

        // Methods
        public abstract GameObject InstantiateReplayInstance(Vector3 position, Quaternion rotation);

        public abstract void DestroyReplayInstance(GameObject replayInstance);
    }
}
