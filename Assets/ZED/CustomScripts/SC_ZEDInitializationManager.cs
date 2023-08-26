#if USING_ZED
using UnityEngine;

/// <summary>
///     Is script exists in the scene from the start
///     It would create the actual ZED manager in the scene once the server finishes loading the scene
/// </summary>
public class SC_ZEDInitializationManager : MonoBehaviour {
    public GameObject ZEDManager;
    
    private GameObject ZEDManagerInstance;
    
    private void Start() {
        DontDestroyOnLoad(gameObject);

        ConnectionAndSpawning.Singleton.ServerStateChange += SetupZED;
    }

    public void SetupZED(ActionState actionState) {
        switch (actionState)
        {
            case ActionState.READY:
                if (FindObjectOfType<ZEDBodyTrackingManager>() == null)
                {
                    Debug.Log("Creating ZEDManager");
                    ZEDManagerInstance = Instantiate(ZEDManager, transform);
                }
                break;
            case ActionState.WAITINGROOM:
                // destroy ZedManager
                if (ZEDManagerInstance != null)
                {
                    Destroy(ZEDManagerInstance);
                }

                // destroy all avatars
                ZEDSkeletonAnimator[] avatars = GameObject.FindObjectsOfType<ZEDSkeletonAnimator>();
                foreach (var avatar in avatars)
                {
                    Destroy(avatar.gameObject);
                }
                break;
        }
        
        
    }
}

#endif