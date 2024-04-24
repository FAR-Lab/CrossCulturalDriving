using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

public class CrowdNavSurfaceManager : MonoBehaviour
{
    public List<Transform> parentsToSet = new List<Transform>();

    private NavMeshSurface surface;
    private List<MeshCollider> meshes = new List<MeshCollider>();

    [ContextMenu("Prepare Navmesh")]
    void PrepareNavmesh()
    {
        meshes.Clear();
        foreach (var parent in parentsToSet)
        {
            meshes.AddRange(parent.GetComponentsInChildren<MeshCollider>());
        }

        foreach (var mesh in meshes)
        {
            NavMeshObstacle obstacle;
            if (mesh.gameObject.GetComponent<NavMeshObstacle>() == null)
            {
                obstacle = mesh.gameObject.AddComponent<NavMeshObstacle>();
            }
            else
            {
                obstacle = mesh.gameObject.GetComponent<NavMeshObstacle>();
            }
            obstacle.carving = true;
        }
        surface = gameObject.GetComponent<NavMeshSurface>();
        //surface.BuildNavMesh();
    }

    [ContextMenu("Remove All Navmesh Obstacles")]
    void RemoveAllNavObstacles()
    {
        meshes.AddRange(GetComponentsInChildren<MeshCollider>());
        foreach (var mesh in meshes)
        {
            DestroyImmediate(mesh.gameObject.GetComponent<NavMeshObstacle>());
        }
    }



    void Update()
    {
    }
}
