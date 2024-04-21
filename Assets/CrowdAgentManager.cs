using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

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
    
    
    //NPC change
    private List<BoxCollider> blockAreas;
    public static CrowdAgentManager Singleton;
    public bool dummyTrafficLight = false;

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
        //set up the block areas
        blockAreas = BlocksManager.GetBuildingBlocks();
        if (blockAreas.Count <= 0) {
            Debug.Log("Cannot get building blocks.");
        }

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
            // SpawnAgent();
            
            RandomSpawn();
        }

        if (spawnOverTime)
        {
            // InvokeRepeating(nameof(SpawnAgent), spawnRate, spawnRate);
            InvokeRepeating(nameof(RandomSpawn), spawnRate, spawnRate);
        }
    }
    
    //03-25
    void RandomSpawn() {
        Vector3 startPos = SelectRandomBirthplace();
        if (startPos == -1 * Vector3.one) {
            Debug.Log("No valid start positions, please check the objects in game.");
            return;
        }
        
        GameObject randomPrefab = agentPrefabs[Random.Range(0, agentPrefabs.Length)];
        Vector3 spawnPosition = new Vector3(startPos.x, startPos.y + 1, startPos.z);
        GameObject agentInstance = Instantiate(randomPrefab, spawnPosition, Quaternion.identity);

        agentInstance.GetComponent<NetworkObject>().Spawn();
        agentInstance.transform.parent = agentSpawn;
        agentInstance.GetComponent<NPCStateMachine>().SetUseTraffic(dummyTrafficLight);
        agentInstances.Add(agentInstance);
        
        
        if (agentInstances.Count >= maxAgentCount) {
            CancelInvoke(nameof(RandomSpawn));
        }
        
    }
    
    Vector3 SelectRandomBirthplace() {
        // print("Block number "+blockAreas.Count);
        if (blockAreas != null) {
            int index = Random.Range(0, blockAreas.Count);

            BoxCollider startBox = blockAreas[index];
            Vector3 randomDest = new Vector3(
                Random.Range(startBox.bounds.min.x,startBox.bounds.max.x),
                gameObject.transform.position.y,
                Random.Range(startBox.bounds.min.z,startBox.bounds.max.z)
            );
            // print("Test Random "+randomDest);
            NavMeshHit hit;
            int walkableMask = 1 << NavMesh.GetAreaFromName("Walkable");
            while (! NavMesh.SamplePosition(randomDest, out hit, 5f, walkableMask)) {
                randomDest = new Vector3(
                    Random.Range(startBox.bounds.min.x,startBox.bounds.max.x),
                    gameObject.transform.position.y,
                    Random.Range(startBox.bounds.min.z,startBox.bounds.max.z)
                );
            }

            return hit.position;

        }
        return new Vector3(-1, -1, -1);
    }

    void SpawnAgent() {
        
        Vector3 randomPoint = new Vector3(
            Random.Range(spawnArea.bounds.min.x, spawnArea.bounds.max.x),
            transform.position.y,
            Random.Range(spawnArea.bounds.min.z, spawnArea.bounds.max.z)
        );

        // try to see if the random point is on the navmesh
        int walkableMask = 1 << NavMesh.GetAreaFromName("Walkable");
        if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, spawnArea.size.magnitude, walkableMask))
        {
            GameObject randomPrefab = agentPrefabs[Random.Range(0, agentPrefabs.Length)];
            Vector3 spawnPosition = new Vector3(hit.position.x, hit.position.y + 1, hit.position.z);
            GameObject agentInstance = Instantiate(randomPrefab, spawnPosition, Quaternion.identity);

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
        print("Destroy "+ agent.transform.position);
        agentInstances.Remove(agent.gameObject);
        Destroy(agent.gameObject);
    }
}
