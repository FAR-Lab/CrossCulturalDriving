using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class StartupManager : MonoBehaviour
{
    [FormerlySerializedAs("ZEDManager")] public GameObject ZEDInitializationManager;
    public GameObject ServerUICanvas;
    
    public GameObject VRUIStartPrefab;

    private void Awake()
    {
        DontDestroyOnLoad(this);
    }

    private void Start()
    {
        DetectPlatform();
    }
    
    
    void DetectPlatform()
    {
        switch (Application.platform)
        {
            case RuntimePlatform.WindowsEditor:
            case RuntimePlatform.WindowsPlayer:
                // Do stuff for Windows
                StartServerClientGUI.Singleton.enabled = true;
                Instantiate(ServerUICanvas);
                #if USING_ZED
                Debug.Log("ZED is enabled");
                Instantiate(ZEDInitializationManager);
                # endif
                break;
            case RuntimePlatform.Android:
                // Do stuff for Oculus
                Instantiate(VRUIStartPrefab);
                break;
            case RuntimePlatform.LinuxPlayer:
                // Do stuff for Linux (ROS?)
                break;
            default:
                Debug.LogError("Platform not supported!");
                break;
        }
    }
}