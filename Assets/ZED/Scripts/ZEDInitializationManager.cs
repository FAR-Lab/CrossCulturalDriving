using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZEDInitializationManager : MonoBehaviour
{
    public GameObject ZEDManager;

    void Start()
    {
        DontDestroyOnLoad(this.gameObject);

        ConnectionAndSpawing.Singleton.SetupServerFunctionality += SetupZED;
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetupZED(string pairName){
        GameObject ZEDManagerInstance = Instantiate(ZEDManager, this.transform);
    }
}
