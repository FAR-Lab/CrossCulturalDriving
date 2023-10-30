using System;
using System.Collections;
using UltimateReplay;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.Hands;

public class VR_Participant : Client_Object
{
    private const string OffsetFileName = "offset";

    //public NetworkVariable<NavigationScreen.Direction> CurrentDirection = new(); //ToDo shopuld be moved to the Navigation Screen
    public bool FinishedImageSending { get; private set; }

    //public Transform FollowTransform;
    public bool FollowRotation;
    public bool FollowLocation;
    public Transform MyCamera;

    public NetworkVariable<bool> ButtonPushed; // This is only active during QN time

    public Interactable_Object NetworkedInteractableObject;
    private bool init;


    private Quaternion LastRot = Quaternion.identity;


    private bool lastValue;
    public ParticipantOrder m_participantOrder = ParticipantOrder.None;

    private NetworkVariable<SpawnType> mySpawnType;
    private Vector3 offsetPositon = Vector3.zero;


    private Quaternion offsetRotation = Quaternion.identity;

   
    public GameObject QuestionairPrefab;

    // OK so I know this is not an elegant solution. We are feeding the image data through the playerobject. Really not great.
    // maybe we would want to use reliable message 

    
    
    private void Awake()
    {
        mySpawnType = new NetworkVariable<SpawnType>();
    }


    private void Update()
    {
        if (IsLocalPlayer)
        

            if (IsServer)
                ButtonPushed.Value = SteeringWheelManager.Singleton.GetButtonInput(m_participantOrder);
    }


    private void LateUpdate()
    {
    //    if (NetworkManager.Singleton.IsServer) return;

    }


 

    public static VR_Participant GetJoinTypeObject()
    {
        foreach (var pic in FindObjectsOfType<VR_Participant>())
            if (pic.IsLocalPlayer)
                return pic;
        return null;
    }

  


    public override void OnNetworkSpawn() //ToDo Turning on and off different elements could be done neater...
    {
        if (IsLocalPlayer)
        {
            m_participantOrder = ConnectionAndSpawning.Singleton.ParticipantOrder;
        }
        else
        {
            foreach (var a in GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                a.enabled = true; // should happen twice to activate the hand
            }

            foreach (var a in GetComponentsInChildren<XRHandTrackingEvents>())
            {
                a.enabled = false; // should happen twice to activate the hand
            }

            foreach (var a in GetComponentsInChildren<XRHandSkeletonDriver>())
            {
                a.enabled = false; // should happen twice to activate the hand
            }

            foreach (var a in GetComponentsInChildren<XRHandMeshController>())
            {
                a.enabled = false; // should happen twice to activate the hand
            }

            foreach (var a in GetComponentsInChildren<Camera>())
            {
                a.enabled = false; // should happen twice to activate the hand
            }

            foreach (var a in GetComponentsInChildren<AudioListener>())
            {
                a.enabled = false; // should happen twice to activate the hand
            }

            foreach (var a in GetComponentsInChildren<TrackedPoseDriver>())
            {
                a.enabled = false; // should happen twice to activate the hand
            }
        }

        if (IsServer)
        {
            m_participantOrder = ConnectionAndSpawning.Singleton.GetParticipantOrderClientId(OwnerClientId);
            UpdateOffsetRemoteClientRPC(offsetPositon, offsetRotation, LastRot);
            GetComponent<ParticipantOrderReplayComponent>().SetParticipantOrder(m_participantOrder);
        }
        else
        {
            foreach (var a in GetComponentsInChildren<ReplayTransform>())
            {
                a.enabled = false; // should happen twice to activate the hand
            }
        }
    }


    [ClientRpc]
    public void UpdateTrafficLightsClientRPC(TrafficLightSupervisor.trafficLightStatus msg)
    {
        if (!IsLocalPlayer || IsServer) return;
        foreach (var tmp in FindObjectsOfType<TrafficLightController>()) tmp.UpdatedTrafficlight(msg);
    }

    public void GoForPostQuestion()
    {
        if (!IsLocalPlayer) return;
        Debug.Log("Waiting for picture upload to finish!");
        StartCoroutine(AwaitFinishingPictureUpload());
    }

    private IEnumerator AwaitFinishingPictureUpload()
    {
        yield return new WaitUntil(() =>
            FinishedImageSending
        );
        PostQuestionServerRPC(OwnerClientId);
    }

    [ServerRpc]
    public void PostQuestionServerRPC(ulong clientID)
    {
        ConnectionAndSpawning.Singleton.FinishedQuestionair(clientID);
    }




    public override ParticipantOrder GetParticipantOrder()
    {
        return m_participantOrder;
    }

    public override void SetSpawnType(SpawnType _spawnType)
    {
        mySpawnType.Value = _spawnType;
    }

    public override void AssignFollowTransform(Interactable_Object MyInteractableObject, ulong targetClient)
    {
        if (IsServer) {
           NetworkedInteractableObject = MyInteractableObject;
            
            NetworkObject.TrySetParent(MyInteractableObject.NetworkObject, false);
          
            AssignInteractable_ClientRPC(MyInteractableObject.GetComponent<NetworkObject>(), targetClient);
        }
    }

    public override Interactable_Object GetFollowTransform() {
        return NetworkedInteractableObject;
    }

    public override Transform GetMainCamera()
    {
        if (MyCamera == null) MyCamera = transform.GetChild(0).Find("CenterEyeAnchor");
          return MyCamera;
    }

    [ClientRpc]
    private void AssignInteractable_ClientRPC(NetworkObjectReference MyInteractable, ulong targetClient)
    {
        Debug.Log(
            $"MyInteractable{MyInteractable.NetworkObjectId} targetClient:{targetClient}, OwnerClientId:{OwnerClientId}");
        if (MyInteractable.TryGet(out var targetObject))
        {
            if (targetClient == OwnerClientId)
            {

                var conf = new ConfigFileLoading();
                conf.Init(OffsetFileName);
                if (conf.FileAvalible())
                {
                    conf.LoadLocalOffset(out var localPosition, out var localRotation);
                    transform.localPosition = localPosition;
                    transform.localRotation = localRotation;
                }
            

            NetworkedInteractableObject = targetObject.transform.GetComponent<Interactable_Object>();
                
            }
        }
        else
        {
            Debug.LogError(
                "Did not manage to get my Car assigned interactions will not work. Maybe try calling this RPC later.");
        }
    }

    public override void De_AssignFollowTransform(ulong targetClient, NetworkObject netobj)
    {
        if (IsServer)
        {
            NetworkObject.TryRemoveParent(false);
            NetworkedInteractableObject = null;
            De_AssignFollowTransformClientRPC(targetClient);
        }
    }

    [ClientRpc]
    private void De_AssignFollowTransformClientRPC(ulong targetClient)
    {
        //ToDo: currently we just deassigned everything but NetworkInteractable object and _transform could turn into lists etc...
        NetworkedInteractableObject = null;
     
        DontDestroyOnLoad(gameObject);
        Debug.Log("De_assign Interactable ClientRPC");
        
    }

    public override void CalibrateClient(ClientRpcParams clientRpcParams)
    {
        CalibrateClientRPC(clientRpcParams);
    }

    private QN_Display qnmanager;
    public override void StartQuestionair(QNDataStorageServer m_QNDataStorageServer) {
         qnmanager = Instantiate(QuestionairPrefab).GetComponent<QN_Display>();
         qnmanager.GetComponent<NetworkObject>().SpawnWithOwnership(OwnerClientId,true);
         string referenceTransformPath="";//TODO this is not Implemented
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
                
                 Offset = new Vector3(0f, 0f, 0.16f);
                 break;
             case SpawnType.ROBOT:
                 break;
             default:
                 break;
         }


         Debug.Log($"Spawning a questionnaire for Participant{m_participantOrder}");
         qnmanager.StartQuestionair(m_QNDataStorageServer,m_participantOrder,tmp,Offset,KeepUpdating,referenceTransformPath);

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

    
    [ClientRpc]
    public void CalibrateClientRPC(ClientRpcParams clientRpcParams = default)
    {
        if (!IsLocalPlayer) return;

        switch (mySpawnType.Value)
        {
            case SpawnType.CAR:
                var steering = NetworkedInteractableObject.transform.Find("SteeringCenter");
                var cam = GetMainCamera();
                var calib = GetComponent<SeatCalibration>();
                Debug.Log($"Calib{calib}, SteeringCenterObject:{steering.name}, and Camera{cam}");
                calib.StartCalibration(
                    steering,
                    cam,
                    this);
                Debug.Log("Calibrated ClientRPC");
                break;
            case SpawnType.PEDESTRIAN:
                var tmp = GetMainCamera();
                tmp.GetComponent<TrackedPoseDriver>().trackingType = TrackedPoseDriver.TrackingType.RotationOnly;


               
                
                if (ConnectionAndSpawning.Singleton.GetScenarioManager()
                    .GetSpawnPose(m_participantOrder, out Pose pose))
                {
                    Quaternion q = Quaternion.FromToRotation(tmp.forward, pose.forward);
                    SetNewRotationOffset(Quaternion.Euler(0,q.eulerAngles.y,0));
                }
                
                
                SetNewPositionOffset(transform.parent.position-tmp.position);
                FinishedCalibration();
                break;
        }
    }

    public bool ButtonPush()
    {
        if (lastValue && ButtonPushed.Value == false)
        {
            lastValue = ButtonPushed.Value;
            return true;
        }

        lastValue = ButtonPushed.Value;
        return false;
    }



    public void SetNewRotationOffset(Quaternion offset)
    {
       // offsetRotation *= offset;
       transform.rotation *= offset;
    }

    public void SetNewPositionOffset(Vector3 positionOffset)
    {
       // offsetPositon += positionOffset;
        transform.position += positionOffset;
    }

    public void FinishedCalibration()
    {
        var conf = new ConfigFileLoading();
        conf.Init(OffsetFileName);
        conf.StoreLocalOffset(transform.localPosition, transform.localRotation);
     
        //  ShareOffsetServerRPC(offsetPositon, offsetRotation, LastRot);
    }

    [ServerRpc]
    public void ShareOffsetServerRPC(Vector3 offsetPositon, Quaternion offsetRotation, Quaternion InitRotation)
    {
        UpdateOffsetRemoteClientRPC(offsetPositon, offsetRotation, InitRotation);
        this.offsetPositon = offsetPositon;
        this.offsetRotation = offsetRotation;
        LastRot = InitRotation;
        // Debug.Log(offsetPositon.ToString()+offsetRotation.ToString());
    }

    [ClientRpc]
    public void UpdateOffsetRemoteClientRPC(Vector3 _offsetPositon, Quaternion _offsetRotation,
        Quaternion InitRotation)
    {
        if (IsLocalPlayer) return;

        offsetPositon = _offsetPositon;
        offsetRotation = _offsetRotation;
        LastRot = InitRotation;
        init = true;
        Debug.Log("Updated Callibrated offsets based on remote poisiton");
    }

    public Transform GetMyCar()
    {
        return NetworkedInteractableObject.transform;
    }

    public bool DeleteCallibrationFile()
    {
        var conf = new ConfigFileLoading();
        conf.Init(OffsetFileName);
        return conf.DeleteFile();
    }

    public void NewScenario()
    {
        FinishedImageSending = true;
    }

    public void InitiateImageTransfere(byte[] Image)
    {
        if (!IsLocalPlayer) return;
        if (IsServer) return;
        FinishedImageSending = false;
        StartCoroutine(SendImageData(m_participantOrder, Image));
    }


//https://answers.unity.com/questions/1113376/unet-send-big-amount-of-data-over-network-how-to-s.html
    private IEnumerator SendImageData(ParticipantOrder po, byte[] ImageArray)
    {
        var CurrentDataIndex = 0;
        var TotalBufferSize = ImageArray.Length;
        var DebugCounter = 0;
        while (CurrentDataIndex < TotalBufferSize - 1)
        {
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
    public void FinishPictureSendingServerRPC(ParticipantOrder po, int length)
    {
        if (!IsServer) return;

        ConnectionAndSpawning.Singleton.GetQnStorageServer().NewRemoteImage(po, length);
        Debug.Log("Finished Picture storing");
    }

}