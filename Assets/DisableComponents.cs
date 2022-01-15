using System.Collections;
using System.Collections.Generic;
using UltimateReplay;
using UnityEngine;
using Unity.Netcode;


public class DisableComponents : NetworkBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }
    

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        //ToDO Write code that deactivetes stuff that doesnt need to be synced 
        if (IsLocalPlayer) {
            DontDestroyOnLoad(gameObject);
        }
        if (IsClient && !IsLocalPlayer) {
            GetComponent<ParticipantInputCapture>().enabled = false;
            GetComponent<StateManager>().enabled = false;
        }
        if (!IsLocalPlayer || IsServer ) {
            Debug.Log("Trying to destroy things that don't belong to me.");
          
            
            GetComponent<OVRManager>().enabled = false;
            GetComponent<OVRCameraRig>().enabled = false;
           
            GetComponentInChildren<OVRScreenFade>().enabled = false;

            GetComponentInChildren<Camera>().enabled = false;
            GetComponentInChildren<AudioListener>().enabled = false;
            foreach (OVRHand tf in GetComponentsInChildren<OVRHand>()) {
                tf.enabled = false;
            }
             
            foreach (OVRCustomSkeleton tf in GetComponentsInChildren<OVRCustomSkeleton>()) {
                tf.enabled = false;
            }
            foreach (OVRControllerHelper tf in GetComponentsInChildren<OVRControllerHelper>()) {
                tf.enabled = false;
            }
        }
        
        if (!IsServer) {
            GetComponent<ReplayTransform>().enabled = false;
           
            foreach (ReplayTransform tf in GetComponentsInChildren<ReplayTransform>()) {
                tf.enabled = false;
            }
            GetComponent<ReplayObject>().enabled = false;
            
            foreach (ReplayObject tf in GetComponentsInChildren<ReplayObject>()) {
                tf.enabled = false;
            }
        }
        
    }


    // Update is called once per frame
    void Update()
    {
        
    }
}
