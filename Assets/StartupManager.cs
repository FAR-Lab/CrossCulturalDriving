using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class StartupManager : MonoBehaviour
{
    public GameObject ZEDInitializationManager;
    public GameObject ServerUICanvas;
    
    public GameObject VRUIStartPrefab;

  
    public GameObject ServerEventSystem;
    
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
            case RuntimePlatform.OSXPlayer:
            case RuntimePlatform.OSXEditor:
                // Do stuff for Windows
                StartServerClientGUI.Singleton.enabled = true;
                Instantiate(ServerUICanvas);
                #if USING_ZED && (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN )
                Debug.Log("ZED is enabled");
                Instantiate(ZEDInitializationManager);
                # endif
                break;
            case RuntimePlatform.Android:
                // Do stuff for Oculus
                
                Destroy(ServerEventSystem);
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