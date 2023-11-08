using UnityEngine;
using UnityEngine.AI;

public class CrowdAgentManager : MonoBehaviour
{
    public GameObject[] agentPrefabs;
    public int initialSpawnCount = 10;
    public float spawnRate = 1f;

    private BoxCollider spawnArea;

    void Start()
    {
        spawnArea = gameObject.GetComponent<BoxCollider>();
        if (!spawnArea)
        {
            enabled = false;
            return;
        }
        // just to make sure spawn is trigger so it doesn't bounce stuff around
        spawnArea.isTrigger = true;

        for (int i = 0; i < initialSpawnCount; i++)
        {
            SpawnAgent();
        }

        InvokeRepeating(nameof(SpawnAgent), spawnRate, spawnRate);
    }

    void SpawnAgent()
    {
        Vector3 randomPoint = new Vector3(
            Random.Range(spawnArea.bounds.min.x, spawnArea.bounds.max.x),
            transform.position.y,
            Random.Range(spawnArea.bounds.min.z, spawnArea.bounds.max.z)
        );

        // try to see if the random point is on the navmesh
        if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, spawnArea.size.magnitude, NavMesh.AllAreas))
        {
            GameObject randomPrefab = agentPrefabs[Random.Range(0, agentPrefabs.Length)];
            GameObject agentInstance = Instantiate(randomPrefab, hit.position, Quaternion.identity);
            agentInstance.transform.parent = transform;
        }
        else
        {
            SpawnAgent();
        }
    }
}
