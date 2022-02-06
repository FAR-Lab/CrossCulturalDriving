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
        
        DontDestroyOnLoad(gameObject);
        
        if(!IsLocalPlayer) {
          

           GetComponent<OVRCameraRig>().disableEyeAnchorCameras = true;
            GetComponent<OVRCameraRig>().enabled = false;
            
            foreach(var c in GetComponentsInChildren<Camera>()){c.enabled = false;}
            GetComponentInChildren<AudioListener>().enabled = false;
            
            foreach (OVRCustomSkeleton tf in GetComponentsInChildren<OVRCustomSkeleton>()) {
                tf.gameObject.SetActive(false);
            }
            foreach (OVRControllerHelper tf in GetComponentsInChildren<OVRControllerHelper>()) {
                tf.gameObject.SetActive(false);
            }
            
           
        }

        else {
            GetComponentInChildren<OVRScreenFade>().enabled = true;
            GetComponent<OVRCameraRig>().enabled = true;
            GetComponent<OVRCameraRig>().disableEyeAnchorCameras = false;
            foreach (OVRHand tf in GetComponentsInChildren<OVRHand>()) {
                tf.enabled = true;
            }
            foreach (OVRCustomSkeleton tf in GetComponentsInChildren<OVRCustomSkeleton>()) {
                tf.enabled = true;
            }
            foreach (OVRControllerHelper tf in GetComponentsInChildren<OVRControllerHelper>()) {
                tf.enabled = true;
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
        base.OnNetworkSpawn();
    }


    // Update is called once per frame
    void Update()
    {
        
    }
}
/*GetComponent<OVRManager>().enabled = false;
            GetComponent<OVRCameraRig>().enabled = true;
            GetComponentInChildren<OVRScreenFade>().enabled = true;
            GetComponentInChildren<Camera>().enabled = true;
            GetComponentInChildren<AudioListener>().enabled = true;
            foreach (OVRHand tf in GetComponentsInChildren<OVRHand>()) {
                tf.enabled = true;
            }
            foreach (OVRCustomSkeleton tf in GetComponentsInChildren<OVRCustomSkeleton>()) {
                tf.enabled = true;
            }
            foreach (OVRControllerHelper tf in GetComponentsInChildren<OVRControllerHelper>()) {
                tf.enabled = true;
            }*/