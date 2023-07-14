using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class ZEDInitializationManager : NetworkBehaviour
{
    public GameObject ZEDManager;

    void Start()
    {
        DontDestroyOnLoad(this.gameObject);

        ConnectionAndSpawing.Singleton.ServerStateChange += SetupZED;

        //ConnectionAndSpawing.Singleton.SetupServerFunctionality += SetupZED;

    }
    public void SetupZED(ActionState actionState)
    {
        if (actionState == ActionState.READY)
        {
            GameObject ZEDManagerInstance = Instantiate(ZEDManager, this.transform);
        }


    }
}
