using UltimateReplay;
using Unity.Netcode;
using UnityEngine;

public class DisableComponents : NetworkBehaviour {
    // Start is called before the first frame update
    private void Start() {
    }


    // Update is called once per frame
    private void Update() {
    }


    public override void OnNetworkSpawn() {
        DontDestroyOnLoad(gameObject);

        if (!IsLocalPlayer) {
            GetComponent<OVRCameraRig>().disableEyeAnchorCameras = true;
            GetComponent<OVRCameraRig>().enabled = false;

            foreach (var c in GetComponentsInChildren<Camera>()) c.enabled = false;
            GetComponentInChildren<AudioListener>().enabled = false;

            foreach (var tf in GetComponentsInChildren<OVRCustomSkeleton>()) tf.gameObject.SetActive(false);
            foreach (var tf in GetComponentsInChildren<OVRControllerHelper>()) tf.gameObject.SetActive(false);
        }

        else {
            GetComponentInChildren<OVRScreenFade>().enabled = true;
            GetComponent<OVRCameraRig>().enabled = true;
            GetComponent<OVRCameraRig>().disableEyeAnchorCameras = false;
            foreach (var tf in GetComponentsInChildren<OVRHand>()) tf.enabled = true;
            foreach (var tf in GetComponentsInChildren<OVRCustomSkeleton>()) tf.enabled = true;
            foreach (var tf in GetComponentsInChildren<OVRControllerHelper>()) tf.enabled = true;
        }

        if (!IsServer) {
            GetComponent<ReplayTransform>().enabled = false;

            foreach (var tf in GetComponentsInChildren<ReplayTransform>()) tf.enabled = false;
            GetComponent<ReplayObject>().enabled = false;

            foreach (var tf in GetComponentsInChildren<ReplayObject>()) tf.enabled = false;
        }

        base.OnNetworkSpawn();
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