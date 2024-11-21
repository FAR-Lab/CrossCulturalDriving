using System;
using System.Collections.Generic;
using Rerun;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(RerunPlaybackCameraManager))]
public class ObserverCamera_ClientObject : Client_Object {
    private RerunPlaybackCameraManager _RerunCameraManager;

    private bool initFinished = false;
    private ParticipantOrder m_ParticipantOrder;
    private Interactable_Object MyInteractableObject;
    private SpawnType spawnType;
    private ulong targetClient;

    // Start is called before the first frame update
    private void Start() {
        _RerunCameraManager = GetComponent<RerunPlaybackCameraManager>();
        Debug.Log("ObserverCamera Manger ");
        ConnectionAndSpawning.Singleton.ServerStateChange += CameraUpdateStateTracker;
    }

    private void OnDisable() {
        if (ConnectionAndSpawning.Singleton != null &&
            ConnectionAndSpawning.Singleton.ServerState == ActionState.RERUN) {
            SceneManager.sceneLoaded -= LoadUnityAction;
            SceneManager.sceneUnloaded -= UnloadUnityAction;
        }
    }

    private void CameraUpdateStateTracker(ActionState state) {
        switch (state) {
            case ActionState.DEFAULT:
                break;
            case ActionState.WAITINGROOM:
                DelinkCameras();
                break;
            case ActionState.LOADINGSCENARIO:
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
                break;
            case ActionState.RERUN:
                SetupForRerun();

                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }
    }

    private void SetupForRerun() {
        SceneManager.sceneLoaded += LoadUnityAction;
        SceneManager.sceneUnloaded += UnloadUnityAction;
    }

    private void UnloadUnityAction(Scene arg0) {
        if (ConnectionAndSpawning.Singleton.ServerState == ActionState.RERUN) DelinkCameras();
    }

    private void LoadUnityAction(Scene arg0, LoadSceneMode arg1) {
        if (ConnectionAndSpawning.Singleton.ServerState == ActionState.RERUN &&
            arg0.name != ConnectionAndSpawning.WaitingRoomSceneName)
            LinkCameras();
    }

    private void DelinkCameras() {
        _RerunCameraManager.DeLinkCameras();
    }

    private void LinkCameras() {
        _RerunCameraManager.LinkCameras();
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        if (!IsServer) gameObject.SetActive(false);
        if (IsServer) SetupButtons();
    }

    private void SetupButtons() {
        var researcher_UI = FindObjectOfType<Researcher_UI>();
        researcher_UI.CreateButton("Calibrate", Calibrate, OwnerClientId);
    }

    public override void SetParticipantOrder(ParticipantOrder _ParticipantOrder) {
        m_ParticipantOrder = _ParticipantOrder;
    }

    public override ParticipantOrder GetParticipantOrder() {
        return m_ParticipantOrder;
    }

    public override void SetSpawnType(SpawnType _spawnType) {
        spawnType = _spawnType;
    }

    public override void AssignFollowTransform(Interactable_Object _MyInteractableObject, ulong _targetClient) {
        MyInteractableObject = _MyInteractableObject;
        targetClient = _targetClient;
    }

    public override Interactable_Object GetFollowTransform() {
        return MyInteractableObject;
    }

    public override void De_AssignFollowTransform(ulong clientID, NetworkObject netobj) {
        MyInteractableObject = null;
    }

    public override Transform GetMainCamera() {
        return _RerunCameraManager.GetFollowCamera();
    }

    public void Calibrate(Action<bool> finishedCalibration) {
        Debug.Log("Here we could try to find all relevant cameras again..");
        finishedCalibration.Invoke(true);
    }

    public override void StartQuestionair(QNDataStorageServer m_QNDataStorageServer) { }

    public override void GoForPostQuestion() {
        if (!IsLocalPlayer) return;
        PostQuestionServerRPC(OwnerClientId);
    }

    public override void SetNewNavigationInstruction(
        Dictionary<ParticipantOrder, NavigationScreen.Direction> Directions) {
        throw new NotImplementedException();
    }

    [ServerRpc]
    public void PostQuestionServerRPC(ulong clientID) {
        ConnectionAndSpawning.Singleton.FinishedQuestionair(clientID);
    }
}