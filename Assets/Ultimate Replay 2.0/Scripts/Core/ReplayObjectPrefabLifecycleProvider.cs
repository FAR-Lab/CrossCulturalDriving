using System;
using UnityEngine;

namespace UltimateReplay.Core
{
    [Serializable]
    public class ReplayObjectPrefabLifecycleProvider : ReplayObjectLifecycleProvider
    {
        // Public
        public ReplayObject prefab;

        // Properties
        public override bool IsAssigned
        {
            get { return prefab != null; }
        }

        public override string ItemName
        {
            get
            {
                if (prefab == null)
                    return "None";

                return prefab.gameObject.name;
            }
        }

        public override ReplayIdentity ItemIdentity
        {
            get
            {
                if (prefab == null)
                    return ReplayIdentity.invalid;

                return prefab.PrefabIdentity;
            }
        }

        // Methods
        public override GameObject InstantiateReplayInstance(Vector3 position, Quaternion rotation)
        {
            if (UltimateReplay.OnReplayInstantiate != null)
            {
                try
                {
                    // Try to instantiate
                    GameObject result = UltimateReplay.OnReplayInstantiate(prefab.gameObject, position, rotation);

                    if (result == null)
                        throw new Exception("Result of user instantiate call was null");

                    return result;
                }
                catch (Exception e)
                {
                    Debug.LogErrorFormat("User callback 'OnReplayInstantiate' caused an exception ({0}). Falling back to default instantiate behaviour", e);
                }
            }

            // Use direct instantiate
            return GameObject.Instantiate(prefab.gameObject, position, rotation);
        }

        public override void DestroyReplayInstance(GameObject replayInstance)
        {
            if (UltimateReplay.OnReplayInstantiate != null)
            {
                try
                {
                    // Try to call user method
                    UltimateReplay.OnReplayDestroy(replayInstance);
                    return;
                }
                catch (Exception e)
                {
                    Debug.LogErrorFormat("User callback 'OnReplayDestroy' caused an exception ({0}). Falling back to default destroy behaviour", e);
                }
            }

            if (Application.isPlaying == false)
            {
                GameObject.DestroyImmediate(replayInstance);
                return;
            }

            // Use direct instantiate
            GameObject.Destroy(replayInstance);
        }
    }
}
