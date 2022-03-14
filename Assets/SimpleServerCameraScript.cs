using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Oculus.Platform;
using Rerun;
using Unity.Netcode;
using UnityEngine;
using Application = UnityEngine.Application;

public class SimpleServerCameraScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += DisableMe;
        NetworkManager.Singleton.OnServerStarted += StartingAsServer;
        //ConnectionAndSpawing.Singleton.OnLevelChange += 
    }


    private Dictionary<RerunCameraIdentifier.CameraNumber, RerunCameraIdentifier> m_Cameras;

    private void StartingAsServer()
    {
        FindObjectOfType<RerunPlaybackCameraManager>()?.EnableCameras();
        NetworkManager.Singleton.SceneManager.OnSceneEvent += SceneEvent;
        m_Cameras = new Dictionary<RerunCameraIdentifier.CameraNumber, RerunCameraIdentifier>();
        foreach (RerunCameraIdentifier v in FindObjectsOfType<RerunCameraIdentifier>())
        {
            if (m_Cameras.ContainsKey(v.myNumber))
            {
                Debug.LogError("multiple Camreas with the same idetefier please fix in the Inspects dropdown.");
            }
            else
            {
                m_Cameras.Add(v.myNumber, v);
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
                foreach (RerunCameraIdentifier cam in m_Cameras.Values)
                {
                    cam.DelinkFollowTransforms();
                }
                break;
            case SceneEventType.Synchronize:
                break;
            case SceneEventType.ReSynchronize:
                break;
            case SceneEventType.LoadEventCompleted:
                string scene =ConnectionAndSpawing.Singleton.GetLoadedScene();
                if (scene != ConnectionAndSpawing.WaitingRoomSceneName)
                {
                    ScenarioManager tmp = ConnectionAndSpawing.Singleton.GetScenarioManager();
                    if (tmp == null) return;
                    foreach (CameraSetupXC cameraSetupXc in tmp.CameraSetups)
                    {
                        if (m_Cameras.ContainsKey(cameraSetupXc.targetNumber))
                        {
                            Transform val =
                                ConnectionAndSpawing.Singleton.GetMainClientCameraObject(cameraSetupXc.ParticipantToFollow);
                            ApplyValues(cameraSetupXc, m_Cameras[cameraSetupXc.targetNumber],val);
                        }
                    }
                 
                }
                
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