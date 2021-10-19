using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UltimateReplay
{
    [InitializeOnLoad]
    public class ReplayValidator
    {
        // Constructor
        static ReplayValidator()
        {
            EditorSceneManager.sceneSaving += OnSceneWillSave;
        }

        // Methods
        private static void OnSceneWillSave(Scene scene, string scenePath)
        {
            HashSet<int> usedIds = new HashSet<int>();
            int fixCount = 0;

            foreach (GameObject go in scene.GetRootGameObjects())
            {
                ReplayObject[] replayObjects = go.GetComponentsInChildren<ReplayObject>(true);

                foreach (ReplayObject replayObject in replayObjects)
                {
                    if (usedIds.Contains(replayObject.ReplayIdentity.IDValue) == true)
                    {
                        replayObject.ForceRegenerateIdentity();
                        fixCount++;
                    }
                    usedIds.Add(replayObject.ReplayIdentity.IDValue);
                }

                ReplayBehaviour[] replayBehaviours = go.GetComponentsInChildren<ReplayBehaviour>(true);

                foreach (ReplayBehaviour replayBehaviour in replayBehaviours)
                {
                    if (usedIds.Contains(replayBehaviour.ReplayIdentity.IDValue) == true)
                    {
                        replayBehaviour.ForceRegenerateIdentity();
                        fixCount++;
                    }
                    usedIds.Add(replayBehaviour.ReplayIdentity.IDValue);
                }
            }

            if (fixCount > 0)
            {
                if (fixCount == 1)
                {
                    Debug.LogWarning("Replay Idenity Conflict: '1' replay component has a duplicate replay identity. Fixing!");
                }
                else
                {
                    Debug.LogWarningFormat("Replay Idenity Conflict: '{0}' replay components have duplicate replay identities. Fixing!", fixCount);
                }
            }
        }
    }
}
