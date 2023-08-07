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
            GameObject ZEDManagerInstance = Instantiate(ZEDManager, this.transform);
        }
    }
}
