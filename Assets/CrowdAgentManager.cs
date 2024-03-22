using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class CrowdAgentManager : NetworkBehaviour
{
    public GameObject[] agentPrefabs;
    public int initialSpawnCount = 10;

    private BoxCollider spawnArea;
    public Transform agentSpawn;

    [SerializeField]
    private List<GameObject> agentInstances = new List<GameObject>();
    public int maxAgentCount = 10;

    public bool spawnOnStart = true;

    public bool spawnOverTime = true;
    public float spawnRate = 1f;

    public static CrowdAgentManager Singleton;

    void Awake()
    {
        if (Singleton)
        {
            Destroy(gameObject);
            return;
        }
        Singleton = this;
    }

    void Start()
    {
        if (!IsServer)
        {
            Destroy(this);
        }

        if (spawnOnStart)
        {
            AgentSetup(null);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            AgentSetup(null);
        }
    }

    public void AgentSetup(Transform parent)
    {
        if (parent)
        {
            transform.parent = parent;
            transform.localPosition = Vector3.zero;
        }

        spawnArea = gameObject.GetComponent<BoxCollider>();
        if (!spawnArea)
        {
            enabled = false;
            return;
        }
        spawnArea.isTrigger = true;

        for (int i = 0; i < initialSpawnCount; i++)
        {
            SpawnAgent();
        }

        if (spawnOverTime)
        {
            InvokeRepeating(nameof(SpawnAgent), spawnRate, spawnRate);
        }
    }

    void SpawnAgent()
    {
        if (agentInstances.Count >= maxAgentCount)
        {
            return;
        }

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

            agentInstance.GetComponent<NetworkObject>().Spawn();
            agentInstance.transform.parent = agentSpawn;
            agentInstances.Add(agentInstance);
        }
        else
        {
            SpawnAgent();
        }
    }

    public void DestroyAgent(CrowdAgent agent)
    {
        agentInstances.Remove(agent.gameObject);
        Destroy(agent.gameObject);
    }
}
