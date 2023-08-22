using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartupManager : MonoBehaviour
{
    public GameObject VRUIStartPrefab;
    public void Start()
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