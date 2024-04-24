using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UltimateReplay;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Interaction.Toolkit;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

public class VR_Participant : Client_Object {
    private const string OffsetFileName = "offset";

    //public NetworkVariable<NavigationScreen.Direction> CurrentDirection = new(); //ToDo shopuld be moved to the Navigation Screen
    public bool FinishedImageSending { get; private set; }

    //public Transform FollowTransform;
    private Transform MyCamera;

    public NetworkVariable<bool> ButtonPushed; // This is only active during QN time

    public Interactable_Object NetworkedInteractableObject;
    private bool init;
    private Quaternion LastRot = Quaternion.identity;
    private bool lastValue;
    public NetworkVariable<ParticipantOrder> m_participantOrder = new NetworkVariable<ParticipantOrder>();

    public NetworkVariable<SpawnType> mySpawnType = new NetworkVariable<SpawnType>();

    private Vector3 offsetPositon = Vector3.zero;
    private Quaternion offsetRotation = Quaternion.identity;


    public GameObject QuestionairPrefab;

    private PedestrianNavigationAudioCues AudioCuePlayer;

    public CalibrationTimerDisplay m_callibDisplay;

    private void Update() {
        if (IsServer) {
            ButtonPushed.Value = SteeringWheelManager.Singleton.GetHornButtonInput(m_participantOrder.Value);
        }
    }


    public override void SetNewNavigationInstruction(
        Dictionary<ParticipantOrder, NavigationScreen.Direction> Directions) {
        switch (mySpawnType.Value) {
            case SpawnType.NONE:
                break;
            case SpawnType.CAR:
                var nvc = NetworkedInteractableObject.GetComponent<NetworkVehicleController>();
                if (nvc != null) {
                    nvc.SetNewNavigationInstructions(Directions);
                }

                break;
            case SpawnType.PEDESTRIAN:
                NavigationScreen.Direction oneDirection = Directions[m_participantOrder.Value];
                SetPedestrianNavigationInstructionsClientRPC(oneDirection);
                break;
            case SpawnType.PASSENGER:
                var nvc2 = NetworkedInteractableObject.GetComponent<NetworkVehicleController>();
                if (nvc2 != null) {
                    nvc2.SetNewNavigationInstructions(Directions);
                }

                break;
            case SpawnType.ROBOT:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    [ClientRpc]
    private void SetPedestrianNavigationInstructionsClientRPC(NavigationScreen.Direction Directions) {
        if (!IsLocalPlayer) return;
        if (AudioCuePlayer == null) {
            var ourMainCamera = GetMainCamera();
            AudioCuePlayer = ourMainCamera.gameObject.AddComponent<PedestrianNavigationAudioCues>();
        }

        AudioCuePlayer.SetNewNavigationInstructions(Directions);
    }


    public new static VR_Participant GetJoinTypeObject() {
        foreach (var pic in FindObjectsOfType<VR_Participant>())
            if (pic.IsLocalPlayer)
                return pic;
        return null;
    }


    public override void OnNetworkSpawn() //ToDo Turning on and off different elements could be done neater...
    {
        if (IsLocalPlayer) {
            MyLineRenderer = GetComponentsInChildren<XRRayInteractor>().ToArray();
                
        }
        else {
            foreach (var a in GetComponentsInChildren<SkinnedMeshRenderer>()) {
                a.enabled = true; // should happen twice to activate the hand
            }

            foreach (var a in GetComponentsInChildren<XRHandTrackingEvents>()) {
                a.enabled = false;
            }

            foreach (var a in GetComponentsInChildren<XRHandSkeletonDriver>()) {
                a.enabled = false;
            }

            foreach (var a in GetComponentsInChildren<XRHandMeshController>()) {
                a.enabled = false;
            }

            foreach (var a in GetComponentsInChildren<Camera>()) {
                a.enabled = false;
            }

            foreach (var a in GetComponentsInChildren<AudioListener>()) {
                a.enabled = false;
            }

            foreach (var a in GetComponentsInChildren<TrackedPoseDriver>()) {
                a.enabled = false;
            }
            foreach (var a in GetComponentsInChildren<XRPokeInteractor>()) {
                a.transform.gameObject.SetActive(false);
            }
            foreach (var a in GetComponentsInChildren<XRDirectInteractor>()) {
                a.transform.gameObject.SetActive(false);
            }
            foreach (var a in GetComponentsInChildren<XRRayInteractor>()) {
                a.transform.gameObject.SetActive(false);
            }

            foreach (var componentsInChild in GetComponentsInChildren<EventSystem>()) {
                componentsInChild.enabled = false;
            }
        }

        if (IsServer) {
            ConnectionAndSpawning.Singleton.ServerStateChange += ChangeRendering;
            m_participantOrder.Value = ConnectionAndSpawning.Singleton.GetParticipantOrderClientId(OwnerClientId);
            UpdateOffsetRemoteClientRPC(offsetPositon, offsetRotation, LastRot);
            GetComponentInChildren<ParticipantOrderReplayComponent>().SetParticipantOrder(m_participantOrder.Value);
            //  var cam =  GetMainCamera();
            // var boxCollider = cam.gameObject.AddComponent<BoxCollider>();
            //  var rigidbody = cam.gameObject.AddComponent<Rigidbody>();
            // rigidbody.isKinematic = true;
        }
        else {
            foreach (var a in GetComponentsInChildren<ReplayTransform>()) {
                a.enabled = false; // should happen twice to activate the hand
            }
        }
    }

    public XRRayInteractor[] MyLineRenderer;

    private void ChangeRendering(ActionState state) {
        
        switch (state) {
            case ActionState.DEFAULT:

                break;
            case ActionState.WAITINGROOM:
                
                if (mySpawnType.Value == SpawnType.PEDESTRIAN) {
                    SetUpZEDSpaceReferenceClientRPC();
                    SetPedestrianOpenXRRepresentaion(true);
                }
                SetInternalRayInteractor(true);
                break;
            case ActionState.LOADINGSCENARIO:

                break;
            case ActionState.LOADINGVISUALS:

                break;
            case ActionState.READY:
                
                if (mySpawnType.Value == SpawnType.PEDESTRIAN) {
                    SetPedestrianOpenXRRepresentaion(false);
                    SetUpZEDSpaceReferenceClientRPC();
                }
                m_callibDisplay.StopDisplay(); // Just to make sure its off!
                SetInternalRayInteractor(false);
                break;
            case ActionState.DRIVE:
                SetInternalRayInteractor(false);
                break;
            case ActionState.QUESTIONS:
                if (mySpawnType.Value == SpawnType.PEDESTRIAN) {
                    SetPedestrianOpenXRRepresentaion(true);
                }

                SetInternalRayInteractor(true);
                break;
            case ActionState.POSTQUESTIONS:
                if (mySpawnType.Value == SpawnType.PEDESTRIAN) {
                    SetPedestrianOpenXRRepresentaion(true);
                }

                SetInternalRayInteractor(true);
                
                break;
            case ActionState.RERUN:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }
    }

    [ClientRpc]
    private void SetUpZEDSpaceReferenceClientRPC() {
        var ZedSpaceReference = FindObjectOfType<ExperimentSpaceReference>();
        ZedSpaceReference.LoadSetup();
    }

    private void SetPedestrianOpenXRRepresentaion(bool val) {
        SetPedestrianOpenXRRepresentaionClientRPC(val);
        i_setPedestrianOpenXRRepresentaion(val);
    }

    [ClientRpc]
    private void SetPedestrianOpenXRRepresentaionClientRPC(bool val) {
        i_setPedestrianOpenXRRepresentaion(val);
    }
    private void i_setPedestrianOpenXRRepresentaion(bool val) {
        foreach (var smr in GetComponentsInChildren<SkinnedMeshRenderer>()) {
            smr.enabled = val;
        }
    }

    
    
    private void SetInternalRayInteractor(bool val) {
        SetInternalRayInteractorClientRPC(val);
        i_SetInternalRayInteractor(val);
    }

    [ClientRpc]
    private void SetInternalRayInteractorClientRPC(bool val) {
        i_SetInternalRayInteractor(val);
    }


    private void i_SetInternalRayInteractor(bool val) {
        foreach (XRRayInteractor lineRenderer in MyLineRenderer) {
            lineRenderer.enabled = val;
        }
    }
    
    
    
    public void Start() {
        m_callibDisplay = GetComponentInChildren<CalibrationTimerDisplay>();
    }

    public override void GoForPostQuestion() {
        if (!IsLocalPlayer) return;
        Debug.Log("Waiting for picture upload to finish!");
        StartCoroutine(AwaitFinishingPictureUpload());
    }

    private IEnumerator AwaitFinishingPictureUpload() {
        yield return new WaitUntil(() =>
            FinishedImageSending
        );
        PostQuestionServerRPC(OwnerClientId);
    }

    [ServerRpc]
    public void PostQuestionServerRPC(ulong clientID) {
        ConnectionAndSpawning.Singleton.FinishedQuestionair(clientID);
    }


    public override void SetParticipantOrder(ParticipantOrder _ParticipantOrder) {
        m_participantOrder.Value = _ParticipantOrder;
    }

    public override ParticipantOrder GetParticipantOrder() {
        return m_participantOrder.Value;
    }

    public override void SetSpawnType(SpawnType _spawnType) {
        mySpawnType.Value = _spawnType;
    }

    public override void AssignFollowTransform(Interactable_Object MyInteractableObject, ulong targetClient) {
        if (IsServer) {
            NetworkedInteractableObject = MyInteractableObject;
            switch (mySpawnType.Value) {
                case SpawnType.NONE:
                    break;
                case SpawnType.CAR:
                    NetworkObject.TrySetParent(MyInteractableObject.NetworkObject, false);
                    break;
                case SpawnType.PEDESTRIAN:
                    //NetworkObject.TrySetParent(MyInteractableObject.NetworkObject, false);
                    var pnac = GetComponent<PedestrianNavigationAudioCues>();
                    if (pnac == null) {
                        gameObject.AddComponent<PedestrianNavigationAudioCues>();
                    }

                    //ToDo We might still have to move you to the active screen (lighting and ReRun considerations... )
                    break;
                case SpawnType.PASSENGER:
                    break;
                case SpawnType.ROBOT:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            NetworkObjectReference reference = MyInteractableObject.GetComponent<NetworkObject>();
            Debug.Log($"Object network refeerence{reference} with targetClient{targetClient}");
            AssignInteractable_ClientRPC(reference, targetClient);
        }
    }

    public override Interactable_Object GetFollowTransform() {
        return NetworkedInteractableObject;
    }

    public override Transform GetMainCamera() {
        if (MyCamera == null) MyCamera = GetComponentInChildren<Camera>().transform;
        return MyCamera;
    }

    IEnumerator ClientSideCallibrationAwait(NetworkObjectReference MyInteractable, ulong targetClient) {
        yield return new WaitUntil(() => mySpawnType.Value != SpawnType.NONE);

        Debug.Log(
            $"Spawn{mySpawnType.Value} : My Interactable {MyInteractable.NetworkObjectId} targetClient:{targetClient}, OwnerClientId:{OwnerClientId}");
        var conf = new ConfigFileLoading();
        switch (mySpawnType.Value) {
            case SpawnType.CAR:
                if (MyInteractable.TryGet(out var targetObject)) {
                    if (targetClient == OwnerClientId) {
                        conf.Init(OffsetFileName);
                        if (conf.FileAvalible()) {
                            conf.LoadLocalOffset(out var localPosition, out var localRotation);
                            transform.localPosition = localPosition;
                            transform.localRotation = localRotation;
                        }

                        Debug.Log($"Found the car,{targetObject} and now trying to get the vehiclecontroler");
                        NetworkedInteractableObject = targetObject.transform.GetComponent<NetworkVehicleController>();
                    }
                    else {
                        Debug.LogWarning(
                            $" Some how  these are not the same?targetClient:{targetClient}, OwnerClientId:{OwnerClientId}");
                    }
                }
                else {
                    Debug.LogError(
                        "Did not manage to get my Car assigned interactions will not work. Maybe try calling this RPC later.");
                }

                break;
            case SpawnType.PEDESTRIAN:
                conf.Init(OffsetFileName);
                if (conf.FileAvalible()) {
                    conf.LoadLocalOffset(out var localPosition, out var localRotation);

                    Debug.Log(
                        $"FindObjectOfType<ExperimentSpaceReference>(){FindObjectOfType<ExperimentSpaceReference>()}");
                    var esr = FindObjectOfType<ExperimentSpaceReference>();
                    var space = esr.GetCallibrationPoint();
                    transform.position = space.TransformPoint(localPosition);
                    transform.forward = space.TransformDirection(localRotation * Vector3.forward);
                    esr.SetBoundaries(GetMainCamera());
                }

                if (MyInteractable.TryGet(out var targetObject2)) {
                    NetworkedInteractableObject = targetObject2.transform.GetComponent<Mocopie_Interactable>();
                }

                break;
            default:
                break;
        }
    }

    [ClientRpc]
    private void AssignInteractable_ClientRPC(NetworkObjectReference MyInteractable, ulong targetClient) {
        if (OwnerClientId != targetClient) return;
        else {
            StartCoroutine(ClientSideCallibrationAwait(MyInteractable, targetClient));
        }
    }

    public override void De_AssignFollowTransform(ulong targetClient, NetworkObject netobj) {
        if (IsServer) {
            NetworkObject.TryRemoveParent(false);
            NetworkedInteractableObject = null;
            De_AssignFollowTransformClientRPC(targetClient);
        }
    }

    [ClientRpc]
    private void De_AssignFollowTransformClientRPC(ulong targetClient) {
        NetworkedInteractableObject = null;
        DontDestroyOnLoad(gameObject);
        Debug.Log("De_assign Interactable ClientRPC");
    }

    private Action<bool> finishedCalibration_ServerSideReference;

    public override void CalibrateClient(Action<bool> finishedCalibration) {
        if (mySpawnType.Value is SpawnType.PEDESTRIAN or SpawnType.CAR) {
            finishedCalibration_ServerSideReference = finishedCalibration;
            CalibrateClientRPC();
        }
    }


    private QN_Display qnmanager;

    public override void StartQuestionair(QNDataStorageServer m_QNDataStorageServer) {
        qnmanager = Instantiate(QuestionairPrefab).GetComponentInChildren<QN_Display>();
        qnmanager.GetComponent<NetworkObject>().SpawnWithOwnership(OwnerClientId, true);
        string referenceTransformPath = ""; //TODO this is not Implemented
        QN_Display.FollowType tmp = QN_Display.FollowType.MainCamera;
        Vector3 Offset = Vector3.zero;
        bool KeepUpdating = false;
        switch (mySpawnType.Value) {
            case SpawnType.CAR:
                tmp = QN_Display.FollowType.Interactable;
                Offset = new Vector3(-0.38f, 1.14f, 0.6f);
                KeepUpdating = true;
                break;
            case SpawnType.NONE:
                break;
            case SpawnType.PEDESTRIAN:
            case SpawnType.PASSENGER:
                tmp = QN_Display.FollowType.MainCamera;
                Offset = new Vector3(0f, 0f, 0.5f);
                break;
            case SpawnType.ROBOT:
                break;
            default:
                break;
        }


        Debug.Log($"Spawning a questionnaire for Participant{m_participantOrder}");
        qnmanager.StartQuestionair(m_QNDataStorageServer, m_participantOrder.Value, tmp, Offset, KeepUpdating,
            referenceTransformPath, this);

        foreach (var screenShot in FindObjectsOfType<QnCaptureScreenShot>()) {
            if (screenShot.ContainsPO(ConnectionAndSpawning.Singleton.ParticipantOrder)) {
                if (screenShot.triggered) {
                    qnmanager.AddImage(screenShot.GetTexture());
                    InitiateImageTransfere(screenShot.GetTexture().EncodeToJPG(50));
                    break;
                }
            }
        }

        m_QNDataStorageServer.RegisterQNSCreen(m_participantOrder.Value, qnmanager);
    }

    private Transform LeftHand;
    private Transform RightHand;

    private IEnumerator OverTimeCalibration(Transform VrCamera, Transform ZedOrignReference, float maxtime = 10) {
        isCalibrationRunning = true;
        int runs = 0;
        const int MaxRuns = 250;
        m_callibDisplay.StartDispaly();
        m_callibDisplay.updateMessage("Hold still!");
        yield return new WaitForSeconds(2);


        //  Transform HandModelL= transform.Find("Camera Offset/Left Hand Tracking/L_Wrist/L_Palm");
        //  Transform HandModelR = transform.Find("Camera Offset/Right Hand Tracking/R_Wrist/R_Palm");
        Transform HandModelL = transform.GetComponent<SeatCalibration>().HandModelL;
        Transform HandModelR = transform.GetComponent<SeatCalibration>().HandModelR;

        // var interact = FindObjectOfType<SkeletonNetworkScript>();    // We making a bunch of assumptions here, kinda ugly! 


        Debug.Log(
            $"ZedOrignReference{ZedOrignReference}, VrCamera{VrCamera}, HandModelL{HandModelL}, HandModelR{HandModelR}");
        Debug.DrawRay(ZedOrignReference.position, -Vector3.up * ZedOrignReference.position.y, Color.magenta, 60);
        Debug.DrawRay(HandModelL.position, Vector3.up, Color.cyan, 60);
        Debug.DrawRay(HandModelR.position, Vector3.up, Color.cyan, 60);


        while (runs < MaxRuns) {
            Vector3 A = HandModelL.position;
            Vector3 B = HandModelR.position;
            Vector3 AtoB = B - A;
            Vector3 midPoint = (A + (AtoB * 0.5f));
            Vector3 transformDifference = ZedOrignReference.position - midPoint;
            Vector3 artificalForward = midPoint - VrCamera.position;

            float angle = Vector3.SignedAngle(new Vector3(artificalForward.x, 0, artificalForward.z),
                new Vector3(ZedOrignReference.forward.x, 0, ZedOrignReference.forward.z),
                Vector3.up);


            Debug.Log($"Angle:{angle}");
            Debug.DrawLine(A, B, Color.green, 10);
            Debug.DrawRay(VrCamera.position, transformDifference, Color.blue, 10);
            Debug.DrawRay(midPoint, artificalForward, Color.red, 10);

            SetNewRotationOffset(Quaternion.Euler(0, angle * 0.9f, 0));
            SetNewPositionOffset(transformDifference * 0.9f);

            maxtime -= Time.deltaTime;
            runs++;
            if (maxtime < 0) {
                break;
            }

            m_callibDisplay.updateMessage((MaxRuns - runs).ToString());
            yield return new WaitForEndOfFrame();
        }

        m_callibDisplay.StopDisplay();
        FinishedCalibration(ZedOrignReference);
        isCalibrationRunning = false;
    }

    private void FindInteractable() {
        if (NetworkedInteractableObject == null) {
            foreach (Interactable_Object io in Interactable_Object.Instances) {
                if (io.m_participantOrder.Value == m_participantOrder.Value) {
                    NetworkedInteractableObject = io;
                    break;
                }
            }
        }
    }

    private bool SkeletonSet = false;
    private int skeletonID;
    private bool isCalibrationRunning = false;

    [ClientRpc]
    public void CalibrateClientRPC(ClientRpcParams clientRpcParams = default) {
        if (!IsLocalPlayer) return;

        switch (mySpawnType.Value) {
            case SpawnType.CAR:
                if (NetworkedInteractableObject == null) {
                    FindInteractable();
                }

                var steering = NetworkedInteractableObject.transform.Find("SteeringCenter");
                var cam = GetMainCamera();
                var calib = GetComponent<SeatCalibration>();
                Debug.Log($"Calib{calib}, SteeringCenterObject:{steering.name}, and VrCamera{cam}");
                calib.StartCalibration(
                    steering,
                    cam,
                    this,
                    m_callibDisplay);
                Debug.Log("Calibrated ClientRPC");
                break;
            case SpawnType.PEDESTRIAN:
                var mainCamera = GetMainCamera();
                Debug.Log($"VrCamera Local Position {mainCamera.localPosition}");
                Debug.Log($"trying to Get Calibrate :{m_participantOrder}");

                if (isCalibrationRunning == false) {
                    StartCoroutine(OverTimeCalibration(mainCamera,
                        FindObjectOfType<ExperimentSpaceReference>().GetCallibrationPoint(), 10));
                }
                else {
                    Debug.Log("Callibration already running!");
                }


                break;
        }
    }

    public bool ButtonPush() {
        if (lastValue && ButtonPushed.Value == false) {
            lastValue = ButtonPushed.Value;
            return true;
        }

        lastValue = ButtonPushed.Value;
        return false;
    }


    public void SetNewRotationOffset(Quaternion offset) {
        transform.rotation *= offset;
    }

    public void SetNewPositionOffset(Vector3 positionOffset) {
        transform.position += positionOffset;
    }

    public void FinishedCalibration(Transform relativeTransform) {
        var conf = new ConfigFileLoading();
        conf.Init(OffsetFileName);
        Vector3 localPosToStore = relativeTransform.InverseTransformPoint(transform.position);

        Quaternion LocalRotation = Quaternion.Inverse(relativeTransform.rotation) * transform.rotation;

        conf.StoreLocalOffset(localPosToStore, LocalRotation);

        //  ShareOffsetServerRPC(offsetPositon, offsetRotation, LastRot);
        FinishedCalibrationServerRPC(true);
    }

    [ServerRpc]
    private void FinishedCalibrationServerRPC(bool val) {
        if (finishedCalibration_ServerSideReference != null) {
            finishedCalibration_ServerSideReference.Invoke(val);
        }
        else {
            Debug.LogWarning($"The Action to notify the server that calibration is finished was never defined");
        }
    }

    [ServerRpc]
    public void ShareOffsetServerRPC(Vector3 offsetPositon, Quaternion offsetRotation, Quaternion InitRotation) {
        UpdateOffsetRemoteClientRPC(offsetPositon, offsetRotation, InitRotation);
        this.offsetPositon = offsetPositon;
        this.offsetRotation = offsetRotation;
        LastRot = InitRotation;
        // Debug.Log(offsetPositon.ToString()+offsetRotation.ToString());
    }

    [ClientRpc]
    public void UpdateOffsetRemoteClientRPC(Vector3 _offsetPositon, Quaternion _offsetRotation,
        Quaternion InitRotation) {
        if (IsLocalPlayer) return;

        offsetPositon = _offsetPositon;
        offsetRotation = _offsetRotation;
        LastRot = InitRotation;
        init = true;
        Debug.Log("Updated Callibrated offsets based on remote poisiton");
    }

    public Transform GetMyInteractable() {
        return NetworkedInteractableObject.transform;
    }

    public bool DeleteCallibrationFile() {
        var conf = new ConfigFileLoading();
        conf.Init(OffsetFileName);
        return conf.DeleteFile();
    }

    public void NewScenario() {
        FinishedImageSending = true;
    }

    public void InitiateImageTransfere(byte[] Image) {
        if (!IsLocalPlayer) return;
        if (IsServer) return;
        FinishedImageSending = false;
        StartCoroutine(SendImageData(m_participantOrder.Value, Image));
    }


//https://answers.unity.com/questions/1113376/unet-send-big-amount-of-data-over-network-how-to-s.html
    private IEnumerator SendImageData(ParticipantOrder po, byte[] ImageArray) {
        var CurrentDataIndex = 0;
        var TotalBufferSize = ImageArray.Length;
        while (CurrentDataIndex < TotalBufferSize - 1) {
            //determine the remaining amount of bytes, still need to be sent.
            var bufferSize = QNDataStorageServer.ByteArraySize;
            var remaining = TotalBufferSize - CurrentDataIndex;
            if (remaining < bufferSize) bufferSize = remaining;

            var buffer = new byte[bufferSize];
            Array.Copy(ImageArray, CurrentDataIndex, buffer, 0, bufferSize);

            var tmp = new BasicByteArraySender { DataSendArray = buffer };

            var writer = new FastBufferWriter(bufferSize + 8, Allocator.TempJob);
            writer.WriteNetworkSerializable(tmp);
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(
                QNDataStorageServer.imageDataPrefix + po,
                NetworkManager.ServerClientId,
                writer);

            CurrentDataIndex += bufferSize;


            yield return null;
            writer.Dispose();
        }

        Debug.Log("Finished Sending Picture!");
        FinishedImageSending = true;
        FinishPictureSendingServerRPC(po, TotalBufferSize);
    }


    [ServerRpc]
    public void FinishPictureSendingServerRPC(ParticipantOrder po, int length) {
        if (!IsServer) return;

        ConnectionAndSpawning.Singleton.GetQnStorageServer().NewRemoteImage(po, length);
        Debug.Log("Finished Picture storing");
    }
}