using System.Collections;
using System.Collections.Generic;
using UltimateReplay;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

public class SkeletonNetworkScript : NetworkBehaviour
{
    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();

        Debug.Log("About to destroy client irrelevant objects!");
        if (IsServer) { }
        else {
            GetComponent<ReplayObject>().enabled = false;
            foreach (var t in GetComponentsInChildren<ReplayTransform>()) {
                t.enabled = false;
                
            }

      //      Destroy(GetComponent<ZEDSkeletonAnimator>());
            // Destroy(GetComponent<HeightOffsetter>());
            Destroy(GetComponent<Animator>());

            GetComponentInChildren<BoxCollider>().enabled = false;
            Debug.Log("OkDoneWithThat!");
            //GetComponent<HeightOffsetter>().enabled = false;
        }
    }

    public override void OnNetworkDespawn() {
        base.OnNetworkDespawn();
       
        
    }
}
