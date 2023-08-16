#if USING_ZED
using UnityEngine;

/// <summary>
///     Is script exists in the scene from the start
///     It would create the actual ZED manager in the scene once the server finishes loading the scene
/// </summary>
public class SC_ZEDInitializationManager : MonoBehaviour {
    public GameObject ZEDManager;

    private void Start() {
        DontDestroyOnLoad(gameObject);

        ConnectionAndSpawning.Singleton.ServerStateChange += SetupZED;
    }

    public void SetupZED(ActionState actionState) {
        if (actionState == ActionState.READY)
            // if there isn't instance of ZEDManager exist, instantiate it on server
            if (FindObjectsOfType<ZEDBodyTrackingManager>() == null) {
                var ZEDManagerInstance = Instantiate(ZEDManager, transform);
            }
    }
}

#endif