using UnityEngine;
using UnityEngine.AI;

public class CrowdAgentManager : MonoBehaviour
{
    public GameObject agentPrefab;
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
            GameObject agentInstance = Instantiate(agentPrefab, hit.position, Quaternion.identity);
            agentInstance.transform.parent = transform;
        }
        else
        {
            SpawnAgent();
        }
    }
}
