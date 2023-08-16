/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using System;
using System.Collections;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class ParticipantInputCapture : NetworkBehaviour {
    private const string OffsetFileName = "offset";
    public bool ReadyForAssignment;

    [SerializeField] private ConnectionAndSpawning.ParticipantObjectSpawnType mySpawnType;

    public NetworkVariable<GpsController.Direction> CurrentDirection = new();


    public Transform _transform;


    public NetworkVariable<bool> ButtonPushed; // This is only active during QN time 
    private StateManager _localStateManager;
    private bool HasNewData;
    private bool init;

    private NetworkedQuestionnaireQuestion lastQuestion;

    private Quaternion LastRot = Quaternion.identity;


    private bool lastValue;

    private GpsController m_GpsController;

    private ParticipantOrder m_participantOrder = ParticipantOrder.None;

    private NetworkVehicleController NetworkedVehicle;
    private Vector3 offsetPositon = Vector3.zero;


    private Quaternion offsetRotation = Quaternion.identity;
    public string lang { get; }


    // OK so I know this is not an elegant solution. We are feeding the image data through the playerobject. Really not great.
    // maybe we would want to use reliable message 


    public bool FinishedImageSending { get; private set; }

    private void Awake() {
        ReadyForAssignment = false;
    }

    private void Start() {
    }

    private void Update() {
        if (IsLocalPlayer)
            if (m_GpsController == null && _transform != null) {
                m_GpsController = _transform.parent.GetComponentInChildren<GpsController>();
                if (m_GpsController != null) m_GpsController.SetDirection(CurrentDirection.Value);
            }

        if (IsServer) ButtonPushed.Value = SteeringWheelManager.Singleton.GetButtonInput(m_participantOrder);
    }


    private void LateUpdate() {
        if (_transform != null) {
            var transform1 = transform;
            var transform2 = _transform;
            transform1.rotation = transform2.rotation * offsetRotation;
            if (!init && IsLocalPlayer) {
                LastRot = transform1.rotation;
                init = true;
                ShareOffsetServerRPC(offsetPositon, offsetRotation, LastRot);
            }

            transform1.position = transform2.position +
                                  transform1.rotation * Quaternion.Inverse(LastRot) * offsetPositon;

            //  if (IsServer) {
            //   Debug.Log("Updating relative positon on server with: "+offsetPositon.ToString()+" and "+offsetRotation.ToString() );
            //  }
        }
    }

    private void OnGUI() {
        if (IsLocalPlayer)
            GUI.Label(new Rect(200, 5, 150, 100), "Client State" + _localStateManager.GlobalState.Value);
    }

    public ParticipantOrder getMyOrder() {
        return m_participantOrder;
    }

    public void SetMySpawnType(ConnectionAndSpawning.ParticipantObjectSpawnType spawnType) {
        mySpawnType = spawnType;
    }

    public static ParticipantInputCapture GetMyPIC() {
        foreach (var pic in FindObjectsOfType<ParticipantInputCapture>())
            if (pic.IsLocalPlayer)
                return pic;

        return null;
    }

    private void NewGpsDirection(GpsController.Direction previousvalue, GpsController.Direction newvalue) {
        if (m_GpsController != null) m_GpsController.SetDirection(newvalue);
    }


    public override void OnNetworkSpawn() {
        if (IsClient && !IsLocalPlayer) return;
        if (IsLocalPlayer) {
            CurrentDirection.OnValueChanged += NewGpsDirection;
            _localStateManager = GetComponent<StateManager>();
            //ToDo delete
            // NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(
            // QNDataStorageServer.QNContentMessageName, AddQuestionsToTheQueue);

            var conf = new ConfigFileLoading();
            conf.Init(OffsetFileName);
            if (conf.FileAvalible()) conf.LoadLocalOffset(out offsetPositon, out offsetRotation);

            m_participantOrder = ConnectionAndSpawning.Singleton.ParticipantOrder;
        }
        else if (IsServer) {
            m_participantOrder = ConnectionAndSpawning.Singleton.GetParticipantOrderClientId(OwnerClientId);
            UpdateOffsetRemoteClientRPC(offsetPositon, offsetRotation, LastRot);
            GetComponent<ParticipantOrderReplayComponent>().SetParticipantOrder(m_participantOrder);
        }
    }


    [ClientRpc]
    public void UpdateTrafficLightsClientRPC(TrafficLightSupervisor.trafficLightStatus msg) {
        if (!IsLocalPlayer || IsServer) return;
        foreach (var tmp in FindObjectsOfType<TrafficLightController>()) tmp.UpdatedTrafficlight(msg);
    }

    public void GoForPostQuestion() {
        if (!IsLocalPlayer) return;
        Debug.Log("Waiting for picture upload to finish!");
        StartCoroutine(AwaidFinishingPictureUpload());
    }

    private IEnumerator AwaidFinishingPictureUpload() {
        yield return new WaitUntil(() =>
            FinishedImageSending
        );
        PostQuestionServerRPC(OwnerClientId);
    }

    [ServerRpc]
    public void PostQuestionServerRPC(ulong clientID) {
        ConnectionAndSpawning.Singleton.FinishedQuestionair(clientID);
    }


    // [ClientRpc]
    //public void StartQuestionnaireClientRpc() {
    //      if (IsLocalPlayer) { FindObjectOfType<ScenarioManager>().RunQuestionairNow(transform); }
    // }


    [ClientRpc]
    public void SetGPSClientRpc(GpsController.Direction[] dir) {
        // GetComponentInChildren<GpsController>().SetDirection(dir[SceneStateManager.Instance.getParticipantID()]);
    }

    public void AssignCarTransform(NetworkVehicleController MyCar, ulong targetClient) {
        if (IsServer) {
            NetworkedVehicle = MyCar;
            _transform = NetworkedVehicle.transform.Find("CameraPosition");
            AssignCarTransformClientRPC(MyCar.NetworkObject, targetClient);
        }
    }

    [ClientRpc]
    private void AssignCarTransformClientRPC(NetworkObjectReference MyCar, ulong targetClient) {
        if (MyCar.TryGet(out var targetObject)) {
            if (targetClient == OwnerClientId) {
                NetworkedVehicle = targetObject.transform.GetComponent<NetworkVehicleController>();

                Debug.Log("Tried to get a new car. Its my Car!");
            }

            _transform = NetworkedVehicle.transform.Find("CameraPosition");
        }
        else {
            Debug.LogWarning(
                "Did not manage to get my Car assigned interactions will not work. Maybe try calling this RPC later.");
        }
    }


    public void De_AssignCarTransform(ulong targetClient) {
        if (IsServer) {
            NetworkedVehicle = null;
            De_AssignCarTransformClientRPC(targetClient);
        }
    }

    [ClientRpc]
    private void De_AssignCarTransformClientRPC(ulong targetClient) {
        NetworkedVehicle = null;
        _transform = null;
        DontDestroyOnLoad(gameObject);
        Debug.Log("De_assign Car ClientRPC");
    }


    [ClientRpc]
    public void CalibrateClientRPC(ClientRpcParams clientRpcParams = default) {
        if (!IsLocalPlayer) return;
        GetComponent<SeatCalibration>().StartCalibration(
            NetworkedVehicle.transform.Find("SteeringCenter"),
            transform.Find("TrackingSpace").Find("CenterEyeAnchor"),
            this);
        Debug.Log("Calibrate ClientRPC");
    }

    public bool ButtonPush() {
        if (lastValue && ButtonPushed.Value == false) {
            lastValue = ButtonPushed.Value;
            Debug.Log("Button Got pushed!!");
            return true;
        }

        lastValue = ButtonPushed.Value;
        return false;
    }


    public void SetNewRotationOffset(Quaternion yawCorrection) {
        offsetRotation *= yawCorrection;
    }

    public void SetNewPositionOffset(Vector3 positionOffset) {
        offsetPositon += positionOffset;
    }

    public void FinishedCalibration() {
        var conf = new ConfigFileLoading();
        conf.Init(OffsetFileName);
        conf.StoreLocalOffset(offsetPositon, offsetRotation);
        ShareOffsetServerRPC(offsetPositon, offsetRotation, LastRot);
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

    public Transform GetMyCar() {
        return NetworkedVehicle.transform;
    }

    public bool DeleteCallibrationFile() {
        var conf = new ConfigFileLoading();
        conf.Init(OffsetFileName);
        return conf.DeleteFile();
    }


    [ClientRpc]
    public void StartQuestionairClientRPC() {
        if (!IsLocalPlayer) return;
        FindObjectOfType<ScenarioManager>().RunQuestionairNow(transform);
    }


    public void SendQNAnswer(int id, int answerIndex, string lang) {
        if (!IsLocalPlayer) return;
        HasNewData = false;
        SendQNAnswerServerRPC(id, answerIndex, lang);
    }


    [ServerRpc]
    public void SendQNAnswerServerRPC(int id, int answerIndex, string lang) {
        if (!IsServer) return;
        ConnectionAndSpawning.Singleton.QNNewDataPoint(m_participantOrder, id, answerIndex, lang);
    }

    [ClientRpc]
    public void RecieveNewQuestionClientRPC(NetworkedQuestionnaireQuestion newq) {
        if (!IsLocalPlayer) return;
        Debug.Log("Got new data and updated varaible" + HasNewData);
        if (HasNewData)
            Debug.LogWarning("I still had a question ready to go. Losing data here. this should not happen.");
        else
            HasNewData = true;

        lastQuestion = newq;
    }

    public bool HasNewQuestion() {
        return HasNewData;
    }

    public NetworkedQuestionnaireQuestion GetNewQuestion() {
        if (HasNewData) {
            HasNewData = false;
            return lastQuestion;
        }

        Debug.LogError("I did have a new question yet I was asked for one quitting the QN early!");
        return new NetworkedQuestionnaireQuestion { reply = replyType.FINISHED };
    }

    [ClientRpc]
    public void SetTotalQNCountClientRpc(int outval) {
        if (!IsLocalPlayer) return;

        FindObjectOfType<QNSelectionManager>()?.SetTotalQNCount(outval);
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