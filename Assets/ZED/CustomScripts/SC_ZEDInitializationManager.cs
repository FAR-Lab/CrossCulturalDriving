using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Is script exists in the scene from the start
/// It would create the actual ZED manager in the scene once the server finishes loading the scene
/// </summary>

public class SC_ZEDInitializationManager : MonoBehaviour
{
    public GameObject ZEDManager;

    void Start()
    {
        DontDestroyOnLoad(this.gameObject);

        ConnectionAndSpawing.Singleton.ServerStateChange += SetupZED;
    }
    public void SetupZED(ActionState actionState)
    {
        if (actionState == ActionState.READY)
        {
            // if there isn't instance of ZEDManager exist, instantiate it on server
            if(FindObjectsOfType<ZEDBodyTrackingManager>() == null){
                GameObject ZEDManagerInstance = Instantiate(ZEDManager, this.transform);
            }
        }
    }
}
