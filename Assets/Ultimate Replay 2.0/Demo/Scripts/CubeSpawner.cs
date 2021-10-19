using System.Collections;
using System.Collections.Generic;
using System.IO;
using UltimateReplay;
using UltimateReplay.Storage;
using UnityEngine;

namespace UltimateReplay.Example
{
    /// <summary>
    /// A demo script used in the stress test scene which spawns a large number of cubes.
    /// </summary>
    public class CubeSpawner : MonoBehaviour
    {
        // Private
        private List<GameObject> spawnedCubes = new List<GameObject>();

        // Public
        /// <summary>
        /// The maximum distance that an object can be spawned from the center.
        /// </summary>
        public float spawnRange = 4;
        /// <summary>
        /// The maximum height that an object can be spawned from the center. 
        /// </summary>
        public float spawnHeight = 6;
        /// <summary>
        /// The amount of objects to spawn into the scene.
        /// </summary>
        public int spawnAmount = 300;
        /// <summary>
        /// The amount of force that is initially given to the spawning cubes.
        /// </summary>
        public float explosiveForce = 5;
        /// <summary>
        /// The transform that all spawned cubes will be parented to.
        /// </summary>
        public Transform parent;
        /// <summary>
        /// An array of prefabs used to randomly spawn objects.
        /// </summary>
        public GameObject[] spawnCubes;

        // Methods
        /// <summary>
        /// Called by Unity.
        /// </summary>
        public IEnumerator Start()
        {
            //ReplayFileTarget file = ReplayFileTarget.CreateReplayFile("StressTest.replay");

            //ReplayHandle handle = ReplayManager.BeginRecording(file, null, true, true);

            // Start delay
            yield return new WaitForSeconds(1);

            Transform setParent = parent;

            if (setParent == null)
            {
                // Keep the hierarchy clean with a filter object
                GameObject root = new GameObject("DemoCubes");

                // Store the transform
                setParent = root.transform;
            }

            for (int i = 0; i < spawnAmount; i++)
            {
                // Select a random location
                Vector3 pos = randomPosition();
                Quaternion rot = randomRotation();

                // Select a random prefab
                int index = Random.Range(0, spawnCubes.Length);

                // Spawn a cube
                GameObject result = Instantiate(spawnCubes[index], pos, rot) as GameObject;

                // Add as a child object
                result.transform.SetParent(setParent);

                spawnedCubes.Add(result);

                // Add to replay
                ReplayManager.AddReplayObjectToRecordScenes(result.GetComponent<ReplayObject>());

                // Give upwards velocity
                Rigidbody body = result.GetComponent<Rigidbody>();

                if (body != null)
                {
                    // Find a random direction
                    Vector3 xzForce = new Vector3(Random.Range(-1, 1), 0, Random.Range(-1, 1));
                    xzForce *= 2;

                    // Upwards force
                    Vector3 upForce = Vector3.up * explosiveForce;

                    // Apply force
                    body.AddForce(xzForce + upForce, ForceMode.Impulse);
                }

                // Wait for next frame
                yield return null;
            }

            float time = Time.time;

            //while(Time.time < (time + 3600))
            //{
            //    // Wait 6 seconds
            //    yield return new WaitForSeconds(6f);

            //    // Add impulse to all objects
            //    foreach(GameObject go in spawnedCubes)
            //    {
            //        // Give upwards velocity
            //        Rigidbody body = go.GetComponent<Rigidbody>();

            //        if (body != null)
            //        {
            //            // Find a random direction
            //            Vector3 xzForce = new Vector3(Random.Range(-1, 1), 0, Random.Range(-1, 1));
            //            xzForce *= 2;

            //            // Upwards force
            //            Vector3 upForce = Vector3.up * explosiveForce;

            //            // Apply force
            //            body.AddForce(xzForce + upForce, ForceMode.Impulse);
            //        }

            //        yield return null;
            //    }
            //}

            //ReplayManager.StopRecording(ref handle);

            //Debug.Log("Replay stress test has finished");
        }

        private Vector3 randomPosition()
        {
            float x = Random.Range(-spawnRange, spawnRange);
            float z = Random.Range(-spawnRange, spawnRange);

            return new Vector3(x, spawnHeight, z);
        }

        private Quaternion randomRotation()
        {
            float x = Random.Range(0, 360);
            float y = Random.Range(0, 360);
            float z = Random.Range(0, 360);

            return Quaternion.Euler(x, y, z);
        }

    }
}
