using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ServerMesh : NetworkBehaviour
{
    public bool behaviorOnServer = false;
    public bool behaviorNotOnServer = false;
    
    public MeshRenderer MyMeshRenderer;
    
    void Start()
    {
        if (IsServer) {
            MyMeshRenderer.enabled = behaviorOnServer;
        }
        else {
            MyMeshRenderer.enabled = behaviorNotOnServer;
        }
        
        
    }

 
}
