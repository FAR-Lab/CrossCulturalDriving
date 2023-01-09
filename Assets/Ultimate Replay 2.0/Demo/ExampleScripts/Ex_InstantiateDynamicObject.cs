using System.Collections;
using UnityEngine;

namespace UltimateReplay.Example
{
    public class Ex_InstantiateDynamicObject : MonoBehaviour
    {
        // Note that the object must be registered as a replay prefab in the global settings
        public ReplayObject instanitatePrefab;

        // Methods
        public IEnumerator Start()
        {
            // Usually you should register replay prefabs in editor via the global settings but it can also be done via code if required
            UltimateReplay.Settings.prefabs.RegisterReplayablePrefab(instanitatePrefab);


            // Start recording - Note that we need to enable 'allowEmptyScene' if there are no replay objects in the scene initially
            ReplayHandle recordHandle = ReplayManager.BeginRecording(null, null, true, true);

            // Wait for some time to pass
            yield return new WaitForSeconds(1f);

            // Spawn a dynamic replay object
            ReplayObject obj = Instantiate(instanitatePrefab);

            // Add the object to all recording scenes - This is a required step otherwise the object will not be recorded
            ReplayManager.AddReplayObjectToRecordScenes(obj);

            // Wait for some time to pass now that the object has been spawned
            yield return new WaitForSeconds(1);

            // End recording
            // The resulting replay will have 1 second of nothing and then the dynamic object will be re-instantiated by the replay system
            ReplayManager.StopRecording(ref recordHandle);
        }

        public IEnumerator StartAlternative()
        {
            // Create a replay scene from the active Unity scene
            ReplayScene recordScene = ReplayScene.FromCurrentScene();

            // Start recording
            ReplayHandle recordHandle = ReplayManager.BeginRecording(null, recordScene);

            // Wait for some time to pass
            yield return new WaitForSeconds(1f);

            // Spawn a dynamic replay object
            ReplayObject obj = Instantiate(instanitatePrefab);

            // Add the object to the record scene only - This is a required step otherwise the object will not be recorded
            recordScene.AddReplayObject(obj);

            // Wait for some time to pass now that the object has been spawned
            yield return new WaitForSeconds(1);

            // End recording
            // The resulting replay will have 1 second of nothing and then the dynamic object will be re-instantiated by the replay system
            ReplayManager.StopRecording(ref recordHandle);
        }
    }
}
