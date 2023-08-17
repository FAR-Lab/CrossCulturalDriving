using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Oculus.Platform;
using Rerun;
using Unity.Netcode;
using UnityEngine;
using Application = UnityEngine.Application;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

public class SimpleServerCameraScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += DisableMe;
        NetworkManager.Singleton.OnServerStarted += StartingAsServer;
        
      
        SceneManager.sceneLoaded += LoadUnityAction;
        SceneManager.sceneUnloaded += UnloadUnityAction;
        

    }
    
    void OnDisable()
    {
      
        SceneManager.sceneLoaded -= LoadUnityAction;
        SceneManager.sceneUnloaded -= UnloadUnityAction;
       
    }

    private bool initFinished = false;

    private void UnloadUnityAction(Scene arg0)
    {
        if (!initFinished)
        { 
            setupCameras();
            initFinished = true;
        }
        if (ConnectionAndSpawning.Singleton.ServerState == ActionState.RERUN)
        {
            DelinkCameras();
        }
       
    }

    private void LoadUnityAction(Scene arg0, LoadSceneMode arg1)
    {
        
        if (ConnectionAndSpawning.Singleton.ServerState == ActionState.RERUN && arg0.name!=ConnectionAndSpawning.WaitingRoomSceneName)
        { 
            if (!initFinished)
            {
                setupCameras();
                initFinished = true;
            }
           
            
            LinkCameras();
        }
    }


    private Dictionary<RerunCameraIdentifier.CameraNumber, RerunCameraIdentifier> m_Cameras;

    private void setupCameras()
    {
        var r = FindObjectOfType<RerunPlaybackCameraManager>();
        Debug.Log(r.enabled);
        if (r != null)
        {

            r.EnableCameras();
        }
        else
        {
            Debug.LogWarning("Could Not enable camera controlls.");
        }
        
        
        m_Cameras = new Dictionary<RerunCameraIdentifier.CameraNumber, RerunCameraIdentifier>();
        
        foreach (RerunCameraIdentifier v in FindObjectsOfType<RerunCameraIdentifier>())
        {
            if (m_Cameras.ContainsKey(v.myNumber))
            {
                Debug.LogError("multiple Cameras with the same identifier please fix in the Inspects dropdown.");
            }
            else
            {
                Debug.Log("Found Camera Number: "+v.myNumber);
                m_Cameras.Add(v.myNumber, v);
            }
        }
    }
    private void StartingAsServer()
    {
        if (NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.OnSceneEvent += SceneEvent;
            setupCameras();
        }
        SceneManager.sceneLoaded -= LoadUnityAction;
        SceneManager.sceneUnloaded -= UnloadUnityAction;
    }

    private void DelinkCameras()
    {
        foreach (RerunCameraIdentifier cam in m_Cameras.Values)
        {
            cam.DelinkFollowTransforms();
        }
    }

    private void LinkCameras()
    {
        
        string scene =ConnectionAndSpawning.Singleton.GetLoadedScene();
        if (scene != ConnectionAndSpawning.WaitingRoomSceneName)
        {
            ScenarioManager tmp = ConnectionAndSpawning.Singleton.GetScenarioManager();
            if (tmp == null) return;
            foreach (CameraSetupXC cameraSetupXc in tmp.CameraSetups)
            {
                Debug.Log("Going through cameras "+cameraSetupXc.CameraMode.ToString());
                if (m_Cameras.ContainsKey(cameraSetupXc.targetNumber))
                {
                    Transform val =
                        ConnectionAndSpawning.Singleton.GetClientMainCameraObject(cameraSetupXc.ParticipantToFollow);
                    ApplyValues(cameraSetupXc, m_Cameras[cameraSetupXc.targetNumber],val);
                }
            }
                 
        }
    }

    private void SceneEvent(SceneEvent sceneEvent)
    {
        switch (sceneEvent.SceneEventType)
        {
            case SceneEventType.Load:
                break;
            case SceneEventType.Unload:
             DelinkCameras();
                break;
            case SceneEventType.Synchronize:
                break;
            case SceneEventType.ReSynchronize:
                break;
            case SceneEventType.LoadEventCompleted:

                LinkCameras();
                break;
            case SceneEventType.UnloadEventCompleted:
                break;
            case SceneEventType.LoadComplete:
                break;
            case SceneEventType.UnloadComplete:
                break;
            case SceneEventType.SynchronizeComplete:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void ApplyValues(CameraSetupXC setup, RerunCameraIdentifier target, Transform followObject = null)
    {
       
        switch (setup.CameraMode)
        {
            case RerunCameraIdentifier.CameraFollowMode.Followone:
                target.SetFollowMode(followObject, setup.PositionOrOffset, setup.RotationOrRot_Offset);
                break;
            case RerunCameraIdentifier.CameraFollowMode.Followmultiple:
                break;
            case RerunCameraIdentifier.CameraFollowMode.Fixed:
                break;
            case RerunCameraIdentifier.CameraFollowMode.Other:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        


    }
    

    private void DisableMe(ulong obj)
    {
        if (NetworkManager.Singleton.IsClient)
        {
            Destroy(gameObject);
        }
        SceneManager.sceneLoaded -= LoadUnityAction;
        SceneManager.sceneUnloaded -= UnloadUnityAction;
    }


// Update is called once per frame
    void Update()
    {
    }
}

[Serializable]
public struct CameraSetupXC
{

    public RerunCameraIdentifier.CameraNumber targetNumber;
        
    public RerunCameraIdentifier.CameraFollowMode CameraMode;
    public ParticipantOrder ParticipantToFollow;
    public Vector3 PositionOrOffset;
    public Vector3 RotationOrRot_Offset;

   
}