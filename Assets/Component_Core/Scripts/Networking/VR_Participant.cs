using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using UltimateReplay;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.Hands;
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
    public ParticipantOrder m_participantOrder = ParticipantOrder.None;

    private NetworkVariable<SpawnType> mySpawnType= new NetworkVariable<SpawnType>();
   
    private Vector3 offsetPositon = Vector3.zero;
    private Quaternion offsetRotation = Quaternion.identity;


    public GameObject QuestionairPrefab;

 
    private void Update() {
        if (IsServer) {
            ButtonPushed.Value = SteeringWheelManager.Singleton.GetHornButtonInput(m_participantOrder);
        }
    }

    public override void SetNewNavigationInstruction(Dictionary<ParticipantOrder, NavigationScreen.Direction> Directions) {
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
                var pnac = GetComponent<PedestrianNavigationAudioCues>();
                if (pnac == null) {
                    pnac = gameObject.AddComponent<PedestrianNavigationAudioCues>();
                }
                    
                pnac.SetNewNavigationInstructions(Directions, m_participantOrder);
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

    public static VR_Participant GetJoinTypeObject() {
        foreach (var pic in FindObjectsOfType<VR_Participant>())
            if (pic.IsLocalPlayer)
                return pic;
        return null;
    }


    public override void OnNetworkSpawn() //ToDo Turning on and off different elements could be done neater...
    {
        if (IsLocalPlayer) {
            m_participantOrder = ConnectionAndSpawning.Singleton.ParticipantOrder;
        }
        else {
            foreach (var a in GetComponentsInChildren<SkinnedMeshRenderer>()) {
                a.enabled = true; // should happen twice to activate the hand
            }

            foreach (var a in GetComponentsInChildren<XRHandTrackingEvents>()) {
                a.enabled = false; // should happen twice to activate the hand
            }

            foreach (var a in GetComponentsInChildren<XRHandSkeletonDriver>()) {
                a.enabled = false; // should happen twice to activate the hand
            }

            foreach (var a in GetComponentsInChildren<XRHandMeshController>()) {
                a.enabled = false; // should happen twice to activate the hand
            }

            foreach (var a in GetComponentsInChildren<Camera>()) {
                a.enabled = false; // should happen twice to activate the hand
            }

            foreach (var a in GetComponentsInChildren<AudioListener>()) {
                a.enabled = false; // should happen twice to activate the hand
            }

            foreach (var a in GetComponentsInChildren<TrackedPoseDriver>()) {
                a.enabled = false; // should happen twice to activate the hand
            }
        }

        if (IsServer) {
            m_participantOrder = ConnectionAndSpawning.Singleton.GetParticipantOrderClientId(OwnerClientId);
            UpdateOffsetRemoteClientRPC(offsetPositon, offsetRotation, LastRot);
            GetComponent<ParticipantOrderReplayComponent>().SetParticipantOrder(m_participantOrder);
        }
        else {
            foreach (var a in GetComponentsInChildren<ReplayTransform>()) {
                a.enabled = false; // should happen twice to activate the hand
            }
        }
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
        m_participantOrder = _ParticipantOrder;
    }

    public override ParticipantOrder GetParticipantOrder() {
        return m_participantOrder;
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
                    //ToDo We might still have to move you to the active sceen (lighting and ReRun considerations... )
                    break;
                case SpawnType.PASSENGER:
                    break;
                case SpawnType.ROBOT:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }


            AssignInteractable_ClientRPC(MyInteractableObject.GetComponent<NetworkObject>(), targetClient);
        }
    }

    public override Interactable_Object GetFollowTransform() {
        return NetworkedInteractableObject;
    }

    public override Transform GetMainCamera() {
        if (MyCamera == null) MyCamera = transform.GetChild(0).Find("CenterEyeAnchor");
        return MyCamera;
    }

    [ClientRpc]
    private void AssignInteractable_ClientRPC(NetworkObjectReference MyInteractable, ulong targetClient) {
        Debug.Log(
            $"MyInteractable{MyInteractable.NetworkObjectId} targetClient:{targetClient}, OwnerClientId:{OwnerClientId}");
        if (MyInteractable.TryGet(out var targetObject)) {
            if (targetClient == OwnerClientId) {
                var conf = new ConfigFileLoading();
                conf.Init(OffsetFileName);
                if (conf.FileAvalible()) {
                    conf.LoadLocalOffset(out var localPosition, out var localRotation);
                    transform.localPosition = localPosition;
                    transform.localRotation = localRotation;
                }
                NetworkedInteractableObject = targetObject.transform.GetComponent<Interactable_Object>();
            }
        }
        else {
            Debug.LogError(
                "Did not manage to get my Car assigned interactions will not work. Maybe try calling this RPC later.");
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

  
    public override void CalibrateClient() {
        if (mySpawnType.Value == SpawnType.PEDESTRIAN){//&& NetworkedInteractableObject.GetComponent<ZedAvatarInteractable>() != null) {
            
            
            // MakeSure the ZEDBodyTrackinManager is where its supposed to be
            //Wait a second for the network to update 
            // Send a callibration re quest to the Client  (ClientRPC) TO move the origin such that skeleton and VR align!
            
           // NetworkedInteractableObject.GetComponent<ZedAvatarInteractable>().WorldCalibration();
            CalibrateClientRPC();
            
        }else if (mySpawnType.Value == SpawnType.CAR) {

            CalibrateClientRPC();
        }
    }


    private QN_Display qnmanager;

    public override void StartQuestionair(QNDataStorageServer m_QNDataStorageServer) {
        if (!IsLocalPlayer) return;
        Debug.Log("we are going to spawn the questionnaire");
        qnmanager = Instantiate(QuestionairPrefab).GetComponent<QN_Display>();
        Debug.Log("we have finished spawning the questionnaire");
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
        qnmanager.StartQuestionair(m_QNDataStorageServer, m_participantOrder, tmp, Offset, KeepUpdating,
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

        m_QNDataStorageServer.RegisterQNSCreen(m_participantOrder, qnmanager);
    }

    private Transform LeftHand;
    private Transform RightHand;

    private IEnumerator OverTimeCalibration(Transform VrCamera,Transform ZedOrignReference, float maxtime = 10) {
        isCalibrationRunning = true;

        yield return new WaitForSeconds(1);
        
        int runs = 0;
        const int MaxRuns = 250;
        
        Transform HandModelL= transform.Find("Camera Offset/Left Hand Tracking/L_Wrist/L_Palm");
        Transform HandModelR = transform.Find("Camera Offset/Right Hand Tracking/R_Wrist/R_Palm");
     // var interact = FindObjectOfType<SkeletonNetworkScript>();    // We making a bunch of assumptions here, kinda ugly! 
        
        Debug.Log(ZedOrignReference.position);
        Debug.DrawRay(ZedOrignReference.position,-Vector3.up*ZedOrignReference.position.y,Color.magenta,60);
        Debug.DrawRay(HandModelL.position,Vector3.up,Color.cyan,60);
        Debug.DrawRay(HandModelR.position,Vector3.up,Color.cyan,60);

   
        while (runs < MaxRuns) {
            
            Vector3 A = HandModelL.position; 
            Vector3 B = HandModelR.position;
            Vector3 AtoB = B - A;
            Vector3 midPoint= (A + (AtoB * 0.5f));
            Vector3 transformDifference =  ZedOrignReference.position-midPoint;
            Vector3 artificalForward =   VrCamera.position - midPoint;

            float angle = Vector3.SignedAngle(new Vector3(artificalForward.x,0,artificalForward.z), 
                new Vector3(ZedOrignReference.forward.x,0,ZedOrignReference.forward.z),
                Vector3.up);
            
            
            Debug.Log($"Angle:{angle}");
            Debug.DrawLine(A,B,Color.green,10);
            Debug.DrawRay(VrCamera.position,transformDifference,Color.blue,10);
            Debug.DrawRay(midPoint,artificalForward,Color.red,10);
            
            SetNewRotationOffset(Quaternion.Euler(0, angle*0.9f, 0));
            SetNewPositionOffset(transformDifference*0.9f);
            
            maxtime -= Time.deltaTime;
            runs++;
            if (maxtime < 0) {
                break;
            }
            yield return new WaitForEndOfFrame();
        }
        FinishedCalibration();
        PedestrianCallibrationFinishedServerRPC();
        isCalibrationRunning = false;
    }

    [ServerRpc]
    private void PedestrianCallibrationFinishedServerRPC() {
        FindObjectOfType<ZedReferenceFinder>().InitiateInverseContinuesCalibration(GetMainCamera());
    }

    private bool SkeletonSet = false;
    private int skeletonID;
    private bool isCalibrationRunning = false;

    [ClientRpc]
    public void CalibrateClientRPC(ClientRpcParams clientRpcParams = default) {
        if (!IsLocalPlayer) return;

        switch (mySpawnType.Value) {
            case SpawnType.CAR:
                var steering = NetworkedInteractableObject.transform.Find("SteeringCenter");
                var cam = GetMainCamera();
                var calib = GetComponent<SeatCalibration>();
                Debug.Log($"Calib{calib}, SteeringCenterObject:{steering.name}, and VrCamera{cam}");
                calib.StartCalibration(
                    steering,
                    cam,
                    this);
                Debug.Log("Calibrated ClientRPC");
                break;
            case SpawnType.PEDESTRIAN:
                var mainCamera = GetMainCamera();
                Debug.Log($"VrCamera Local Position {mainCamera.localPosition}");
                Debug.Log($"trying to Get Calibrate :{m_participantOrder}");
                if (transform.parent != null &&
                    transform.parent == NetworkedInteractableObject.transform 
                    // && NetworkedInteractableObject.GetComponent<ZedAvatarInteractable>() != null
            ){
                    if (isCalibrationRunning == false) {
                      //  StartCoroutine(OverTimeCalibration(mainCamera, NetworkedInteractableObject.GetComponent<ZedAvatarInteractable>().GetCameraPositionObject(),10));
                    }
                    else {
                        Debug.Log("Callibration already running!");
                    }
                }
                else {
                    Debug.LogError("Pedestrian calibration failed!!");
                }
                /*
                else if (t != null) {
                    Quaternion q = Quaternion.FromToRotation(tmp.forward, t.forward);
                    SetNewRotationOffset(Quaternion.Euler(0, q.eulerAngles.y, 0));


                    FinishedCalibration();
                }
                else {
                    Debug.Log("Could not find GetCameraPositionObject :-(");
                    if (ConnectionAndSpawning.Singleton.GetScenarioManager()
                        .GetSpawnPose(m_participantOrder, out Pose pose)) {
                        Quaternion q = Quaternion.FromToRotation(tmp.forward, pose.forward);
                        SetNewRotationOffset(Quaternion.Euler(0, q.eulerAngles.y, 0));
                    }

                    SetNewPositionOffset(transform.parent.position - tmp.position);
                    FinishedCalibration();
                }*/
/*
                if (NetworkedInteractableObject.GetType() == typeof(ZEDMaster)) {
                    var zedMaster = NetworkedInteractableObject.GetComponent<ZEDMaster>();
                    LeftHand = transform.Find("VrCamera Offset/Left Hand Tracking/L_Wrist");
                    RightHand = transform.Find("VrCamera Offset/Right Hand Tracking/R_Wrist");
                    LeftHand.gameObject.GetOrAddComponent<ZombieHands>().Init(zedMaster.GetLeftHandRoot());
                    RightHand.gameObject.GetOrAddComponent<ZombieHands>().Init(zedMaster.GetRightHandRoot());
                }*/

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
        // offsetRotation *= offset;
        transform.rotation *= offset;
    }

    public void SetNewPositionOffset(Vector3 positionOffset) {
        // offsetPositon += positionOffset;
        transform.position += positionOffset;
    }

    public void FinishedCalibration() {
        var conf = new ConfigFileLoading();
        conf.Init(OffsetFileName);
        conf.StoreLocalOffset(transform.localPosition, transform.localRotation);

        //  ShareOffsetServerRPC(offsetPositon, offsetRotation, LastRot);
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
        StartCoroutine(SendImageData(m_participantOrder, Image));
    }


//https://answers.unity.com/questions/1113376/unet-send-big-amount-of-data-over-network-how-to-s.html
    private IEnumerator SendImageData(ParticipantOrder po, byte[] ImageArray) {
        var CurrentDataIndex = 0;
        var TotalBufferSize = ImageArray.Length;
        var DebugCounter = 0;
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