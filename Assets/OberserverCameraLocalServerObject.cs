using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Rerun;
using Unity.Netcode;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(RerunPlaybackCameraManager))]
public class OberserverCameraLocalServerObject : MonoBehaviour {
    private RerunPlaybackCameraManager _RerunCameraManager;

    // Start is called before the first frame update
    void Start() {
        _RerunCameraManager = GetComponent<RerunPlaybackCameraManager>();
        Debug.Log("ObserverCamera Manger ");
        ConnectionAndSpawning.Singleton.ServerStateChange += CameraUpdateStateTracker;
    }

    private void CameraUpdateStateTracker(ActionState state) {
        switch (state) {
            case ActionState.DEFAULT:
                break;
            case ActionState.WAITINGROOM:
                LinkCameras();
               
                break;
            case ActionState.LOADINGSCENARIO:
                 DelinkCameras();
                break;
            case ActionState.LOADINGVISUALS:
                break;
            case ActionState.READY:
                LinkCameras();
                Debug.Log("Attemtpitng to LinkCameras...");
                break;
            case ActionState.DRIVE:
                break;
            case ActionState.QUESTIONS:
                break;
            case ActionState.POSTQUESTIONS:
                DelinkCameras();
                break;
            case ActionState.RERUN:
                SetupForRerun();

                break;
            default: break;
        }
    }

    void OnDisable() {
        if (ConnectionAndSpawning.Singleton != null &&
            ConnectionAndSpawning.Singleton.ServerState == ActionState.RERUN) {
            SceneManager.sceneLoaded -= LoadUnityAction;
            SceneManager.sceneUnloaded -= UnloadUnityAction;
        }
    }

    private void SetupForRerun() {
        SceneManager.sceneLoaded += LoadUnityAction;
        SceneManager.sceneUnloaded += UnloadUnityAction;
    }

    private bool initFinished = false;

    private void UnloadUnityAction(Scene arg0) {
        if (ConnectionAndSpawning.Singleton.ServerState == ActionState.RERUN) {
            DelinkCameras();
        }
    }

    private void LoadUnityAction(Scene arg0, LoadSceneMode arg1) {
        if (ConnectionAndSpawning.Singleton.ServerState == ActionState.RERUN &&
            arg0.name != ConnectionAndSpawning.WaitingRoomSceneName) {
            LinkCameras();
        }
    }

    private void DelinkCameras() {
        _RerunCameraManager.DeLinkCameras();
    }

    private void LinkCameras() {
        _RerunCameraManager.LinkCameras();
    }
}