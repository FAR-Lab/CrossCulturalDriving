using System.Collections;
using System.Collections.Generic;
using UltimateReplay;
using Unity.Netcode;
using UnityEngine;

public class SkeletonNetworkScript : NetworkBehaviour
{
    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();

        if (IsServer) { }
        else {
            GetComponent<ReplayObject>().enabled = false;
            foreach (var t in GetComponentsInChildren<ReplayTransform>()) {
                t.enabled = false;
                
            }

            GetComponentInChildren<BoxCollider>().enabled = false;
            GetComponent<HeightOffsetter>().enabled = false;
        }
    }

    public override void OnNetworkDespawn() {
        base.OnNetworkDespawn();
       
        
    }
}
