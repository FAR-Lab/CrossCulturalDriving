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
using UnityEngine.Serialization;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Interaction.Toolkit;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

public class VR_Participant : Client_Object {
    private const string OffsetFileName = "offset";

    public NetworkVariable<bool> ButtonPushed; // This is only active during QN time

    public Interactable_Object NetworkedInteractableObject;
    public NetworkVariable<ParticipantOrder> m_participantOrder = new();

    public NetworkVariable<SpawnType> mySpawnType = new();


    public GameObject QuestionairPrefab;

    [FormerlySerializedAs("m_callibDisplay")]
    public CalibrationTimerDisplay m_calibDisplay;

    public XRRayInteractor[] MyLineRenderer;

    private PedestrianNavigationAudioCues AudioCuePlayer;

    // called after calibration finished
    private Action<bool> calibrationCallback;
    private bool init;
    private bool isCalibrationRunning;
    private Quaternion LastRot = Quaternion.identity;
    private bool lastValue;

    //public Transform FollowTransform;
    private Transform MyCamera;

    private Vector3 offsetPositon = Vector3.zero;
    private Quaternion offsetRotation = Quaternion.identity;


    private QN_Display qnmanager;
    private int skeletonID;

    private bool SkeletonSet = false;

    //public NetworkVariable<NavigationScreen.Direction> CurrentDirection = new(); //ToDo shopuld be moved to the Navigation Screen
    public bool FinishedImageSending { get; private set; }

    public override void StartQuestionair(QNDataStorageServer m_QNDataStorageServer) {
        qnmanager = Instantiate(QuestionairPrefab).GetComponentInChildren<QN_Display>();
        qnmanager.GetComponent<NetworkObject>().SpawnWithOwnership(OwnerClientId, true);
        var referenceTransformPath = ""; //TODO this is not Implemented
        var tmp = QN_Display.FollowType.MainCamera;
        var Offset = Vector3.zero;
        var KeepUpdating = false;
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
        }


        Debug.Log($"Spawning a questionnaire for Participant{m_participantOrder}");
        qnmanager.StartQuestionair(m_QNDataStorageServer, m_participantOrder.Value, tmp, Offset, KeepUpdating,
            referenceTransformPath, this);

        foreach (var screenShot in FindObjectsOfType<QnCaptureScreenShot>())
            if (screenShot.ContainsPO(ConnectionAndSpawning.Singleton.ParticipantOrder))
                if (screenShot.triggered) {
                    qnmanager.AddImage(screenShot.GetTexture());
                    InitiateImageTransfer(screenShot.GetTexture().EncodeToJPG(50));
                    break;
                }

        m_QNDataStorageServer.RegisterQNScreen(m_participantOrder.Value, qnmanager);
    }

    public bool ButtonPush() {
        if (lastValue && ButtonPushed.Value == false) {
            lastValue = ButtonPushed.Value;
            return true;
        }

        lastValue = ButtonPushed.Value;
        return false;
    }


    public void NewScenario() {
        FinishedImageSending = true;
    }

    public void InitiateImageTransfer(byte[] Image) {
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

    #region Setup

    public void Start() {
        m_calibDisplay = GetComponentInChildren<CalibrationTimerDisplay>();
    }

    private void Update() {
        if (IsServer) ButtonPushed.Value = SteeringWheelManager.Singleton.GetHornButtonInput(m_participantOrder.Value);
    }


    public override void SetNewNavigationInstruction(
        Dictionary<ParticipantOrder, NavigationScreen.Direction> Directions) {
        switch (mySpawnType.Value) {
            case SpawnType.NONE:
                break;
            case SpawnType.CAR:
                var nvc = NetworkedInteractableObject.GetComponent<NetworkVehicleController>();
                if (nvc != null) nvc.SetNewNavigationInstructions(Directions);

                break;
            case SpawnType.PEDESTRIAN:
                var oneDirection = Directions[m_participantOrder.Value];
                SetPedestrianNavigationInstructionsClientRPC(oneDirection);
                break;
            case SpawnType.PASSENGER:
                var nvc2 = NetworkedInteractableObject.GetComponent<NetworkVehicleController>();
                if (nvc2 != null) nvc2.SetNewNavigationInstructions(Directions);

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


    public static VR_Participant GetJoinTypeObject() {
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
            foreach (var a in
                     GetComponentsInChildren<SkinnedMeshRenderer>())
                a.enabled = true; // should happen twice to activate the hand

            foreach (var a in GetComponentsInChildren<XRHandTrackingEvents>()) a.enabled = false;

            foreach (var a in GetComponentsInChildren<XRHandSkeletonDriver>()) a.enabled = false;

            foreach (var a in GetComponentsInChildren<XRHandMeshController>()) a.enabled = false;

            foreach (var a in GetComponentsInChildren<Camera>()) a.enabled = false;

            foreach (var a in GetComponentsInChildren<AudioListener>()) a.enabled = false;

            foreach (var a in GetComponentsInChildren<TrackedPoseDriver>()) a.enabled = false;
            foreach (var a in GetComponentsInChildren<XRPokeInteractor>()) a.transform.gameObject.SetActive(false);
            foreach (var a in GetComponentsInChildren<XRDirectInteractor>()) a.transform.gameObject.SetActive(false);
            foreach (var a in GetComponentsInChildren<XRRayInteractor>()) a.transform.gameObject.SetActive(false);

            foreach (var componentsInChild in GetComponentsInChildren<EventSystem>()) componentsInChild.enabled = false;
        }

        if (IsServer) {
            ConnectionAndSpawning.Singleton.ServerStateChange += ChangeRendering;
            m_participantOrder.Value = ConnectionAndSpawning.Singleton.GetParticipantOrderClientId(OwnerClientId);
            UpdateOffsetRemoteClientRPC(offsetPositon, offsetRotation, LastRot);
            GetComponentInChildren<ParticipantOrderReplayComponent>().SetParticipantOrder(m_participantOrder.Value);
            StartCoroutine(SetupButtonsAwait());
        }
        else {
            foreach (var a in
                     GetComponentsInChildren<ReplayTransform>())
                a.enabled = false; // should happen twice to activate the hand
        }
    }

    private IEnumerator SetupButtonsAwait() {
        yield return new WaitUntil(() =>
            mySpawnType.Value != SpawnType.NONE
        );
        var researcher_UI = FindObjectOfType<Researcher_UI>();

        switch (mySpawnType.Value) {
            case SpawnType.PEDESTRIAN:
                researcher_UI.CreateButton("Calibrate Pos", CalibratePosition, OwnerClientId);
                researcher_UI.CreateButton("Calibrate Rot", CalibrateRotation, OwnerClientId);
                break;
            case SpawnType.CAR:
                researcher_UI.CreateButton("Calibrate Car", CalibrateCar, OwnerClientId);
                break;
        }
    }

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

                m_calibDisplay.StopDisplay(); // Just to make sure its off!
                SetInternalRayInteractor(false);
                break;
            case ActionState.DRIVE:
                SetInternalRayInteractor(false);
                break;
            case ActionState.QUESTIONS:
                if (mySpawnType.Value == SpawnType.PEDESTRIAN) SetPedestrianOpenXRRepresentaion(true);

                SetInternalRayInteractor(true);
                break;
            case ActionState.POSTQUESTIONS:
                if (mySpawnType.Value == SpawnType.PEDESTRIAN) SetPedestrianOpenXRRepresentaion(true);

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

    #endregion

    #region setters

    private void SetPedestrianOpenXRRepresentaion(bool val) {
        SetPedestrianOpenXRRepresentaionClientRPC(val);
        i_setPedestrianOpenXRRepresentaion(val);
    }

    [ClientRpc]
    private void SetPedestrianOpenXRRepresentaionClientRPC(bool val) {
        i_setPedestrianOpenXRRepresentaion(val);
    }

    private void i_setPedestrianOpenXRRepresentaion(bool val) {
        foreach (var smr in GetComponentsInChildren<SkinnedMeshRenderer>()) smr.enabled = val;
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
        foreach (var lineRenderer in MyLineRenderer) lineRenderer.enabled = val;
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
        if (!IsServer) return;
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
                if (pnac == null) gameObject.AddComponent<PedestrianNavigationAudioCues>();

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

    [ClientRpc]
    private void AssignInteractable_ClientRPC(NetworkObjectReference MyInteractable, ulong targetClient) {
        if (OwnerClientId != targetClient) return;
        StartCoroutine(ClientSideCalibrationAwait(MyInteractable, targetClient));
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

    public override Interactable_Object GetFollowTransform() {
        return NetworkedInteractableObject;
    }

    public override Transform GetMainCamera() {
        if (MyCamera == null) MyCamera = GetComponentInChildren<Camera>().transform;
        return MyCamera;
    }

    #endregion


    #region calibration

    private IEnumerator ClientSideCalibrationAwait(NetworkObjectReference MyInteractable, ulong targetClient) {
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

                    var esr = FindObjectOfType<ExperimentSpaceReference>();
                    Debug.Log(
                        $"FindObjectOfType<ExperimentSpaceReference>(){esr}");
                    var space = esr.GetCalibrationPoints().Item2;
                    transform.position = space.TransformPoint(localPosition);
                    transform.forward = space.TransformDirection(localRotation * Vector3.forward);
                    esr.SetBoundaries(GetMainCamera());
                }

                if (MyInteractable.TryGet(out var targetObject2))
                    NetworkedInteractableObject = targetObject2.transform.GetComponent<Mocopie_Interactable>();

                break;
        }
    }

    public void CalibrateCar(Action<bool> calibrationCallback) {
        this.calibrationCallback = calibrationCallback;
        CalibrateCarClientRPC();
    }

    [ClientRpc]
    private void CalibrateCarClientRPC() {
        if (NetworkedInteractableObject == null) FindInteractable();

        var steering = NetworkedInteractableObject.transform.Find("SteeringCenter");
        var cam = GetMainCamera();
        var calib = GetComponent<SeatCalibration>();
        Debug.Log($"Calib{calib}, SteeringCenterObject:{steering.name}, and VrCamera{cam}");
        calib.StartCalibration(
            steering,
            cam,
            this,
            m_calibDisplay);
    }

    public void CalibratePosition(Action<bool> calibrationCallback) {
        this.calibrationCallback = calibrationCallback;
        CalibratePositionClientRPC();
    }

    [ClientRpc]
    private void CalibratePositionClientRPC() {
        if (!IsLocalPlayer) return;
        var mainCamera = GetMainCamera();
        Debug.Log($"VrCamera Local Position {mainCamera.localPosition}");
        Debug.Log($"trying to Get Calibrate :{m_participantOrder}");

        var esr = FindObjectOfType<ExperimentSpaceReference>();
        var calibs = esr.GetCalibrationPoints();
        if (isCalibrationRunning == false)
            StartCoroutine(OverTimePositionCalibration(calibs.Item1));
        else
            Debug.Log("Calibration already running!");
    }

    private IEnumerator OverTimePositionCalibration(Transform originReference, float maxTime = 10) {
        isCalibrationRunning = true;
        m_calibDisplay.StartDisplay();
        m_calibDisplay.UpdateMessage("Hold still!");
        yield return new WaitForSeconds(2);

        var HandModelL = transform.GetComponent<SeatCalibration>().HandModelL;
        var HandModelR = transform.GetComponent<SeatCalibration>().HandModelR;

        Debug.Log(
            $"OriginReference{originReference}, HandModelL{HandModelL}, HandModelR{HandModelR}");
        Debug.DrawRay(originReference.position, -Vector3.up * originReference.position.y, Color.magenta, 60);
        Debug.DrawRay(HandModelL.position, Vector3.up, Color.cyan, 60);
        Debug.DrawRay(HandModelR.position, Vector3.up, Color.cyan, 60);

        const int MaxRuns = 250;
        for (var runs = 0; runs < MaxRuns; runs++) {
            var A = HandModelL.position;
            var B = HandModelR.position;
            var AtoB = B - A;
            var midPoint = A + AtoB * 0.5f;
            var transformDifference =
                originReference.position -
                midPoint;
            Debug.DrawLine(A, B, Color.green, 10);

            SetNewPositionOffset(transformDifference * 0.9f);

            maxTime -= Time.deltaTime;
            if (maxTime < 0) break;

            m_calibDisplay.UpdateMessage((MaxRuns - runs).ToString());
            yield return new WaitForEndOfFrame();
        }

        m_calibDisplay.StopDisplay();
        FinishedCalibration();
        isCalibrationRunning = false;
    }

    public void CalibrateRotation(Action<bool> calibrationCallback) {
        this.calibrationCallback = calibrationCallback;
        CalibrateRotationClientRPC();
    }

    [ClientRpc]
    private void CalibrateRotationClientRPC() {
        if (!IsLocalPlayer) return;
        var mainCamera = GetMainCamera();
        Debug.Log($"VrCamera Local Position {mainCamera.localPosition}");
        Debug.Log($"trying to Get Calibrate :{m_participantOrder}");

        var esr = FindObjectOfType<ExperimentSpaceReference>();
        var calibs = esr.GetCalibrationPoints();
        if (isCalibrationRunning == false)
            StartCoroutine(OverTimeRotationCalibration(calibs.Item1, calibs.Item2));
        else
            Debug.Log("Calibration already running!");
    }

    private IEnumerator OverTimeRotationCalibration(Transform pivot, Transform originReference, float maxTime = 10) {
        isCalibrationRunning = true;
        m_calibDisplay.StartDisplay();
        m_calibDisplay.UpdateMessage("Hold still!");
        yield return new WaitForSeconds(2);

        var HandModelL = transform.GetComponent<SeatCalibration>().HandModelL;
        var HandModelR = transform.GetComponent<SeatCalibration>().HandModelR;

        Debug.Log(
            $"OriginReference{originReference}, HandModelL{HandModelL}, HandModelR{HandModelR}");
        Debug.DrawRay(originReference.position, -Vector3.up * originReference.position.y, Color.magenta, 60);
        Debug.DrawRay(HandModelL.position, Vector3.up, Color.cyan, 60);
        Debug.DrawRay(HandModelR.position, Vector3.up, Color.cyan, 60);

        const int MaxRuns = 250;
        for (var runs = 0; runs < MaxRuns; runs++) {
            var A = HandModelL.position;
            var B = HandModelR.position;
            var AtoB = B - A;
            var midPoint = A + AtoB * 0.5f;
            var pivotPosition = pivot.position;
            var realDifference = midPoint - pivotPosition;
            var virtualDifference = originReference.position - pivotPosition;
            var angleDifference = Vector3.SignedAngle(new Vector3(realDifference.x, 0, realDifference.z),
                new Vector3(virtualDifference.x, 0, virtualDifference.z),
                Vector3.up);
            Debug.DrawLine(A, B, Color.green, 10);

            transform.RotateAround(pivotPosition, Vector3.up, angleDifference * 0.9f);

            maxTime -= Time.deltaTime;
            if (maxTime < 0) break;

            m_calibDisplay.UpdateMessage((MaxRuns - runs).ToString());
            yield return new WaitForEndOfFrame();
        }

        m_calibDisplay.StopDisplay();
        FinishedCalibration(originReference);
        isCalibrationRunning = false;
    }

    private IEnumerator CountDown(string message, int seconds) {
        for (var i = seconds; i >= 0; i--) {
            m_calibDisplay.UpdateMessage(message + i);
            yield return new WaitForSeconds(1);
        }
    }

    private void FindInteractable() {
        if (NetworkedInteractableObject == null)
            foreach (var io in Interactable_Object.Instances)
                if (io.m_participantOrder.Value == m_participantOrder.Value) {
                    NetworkedInteractableObject = io;
                    break;
                }
    }

    public void SetNewRotationOffset(Quaternion offset) {
        transform.rotation *= offset;
    }

    public void SetNewPositionOffset(Vector3 positionOffset) {
        transform.position += positionOffset;
    }

    public void FinishedCalibration() {
        FinishedCalibrationServerRPC(true);
    }

    public void FinishedCalibration(Transform relativeTransform) {
        var conf = new ConfigFileLoading();
        conf.Init(OffsetFileName);
        var localPosToStore = relativeTransform.InverseTransformPoint(transform.position);

        var localRotation = Quaternion.Inverse(relativeTransform.rotation) * transform.rotation;

        conf.StoreLocalOffset(localPosToStore, localRotation);

        //  ShareOffsetServerRPC(offsetPositon, offsetRotation, LastRot);
        FinishedCalibrationServerRPC(true);
    }

    [ServerRpc]
    private void FinishedCalibrationServerRPC(bool val) {
        if (calibrationCallback != null)
            calibrationCallback.Invoke(val);
        else
            Debug.LogWarning("The Action to notify the server that calibration is finished was never defined");
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

    public bool DeleteCalibrationFile() {
        var conf = new ConfigFileLoading();
        conf.Init(OffsetFileName);
        return conf.DeleteFile();
    }

    #endregion
}