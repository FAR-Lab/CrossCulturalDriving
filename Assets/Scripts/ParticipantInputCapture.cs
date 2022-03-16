/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using UltimateReplay;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class ParticipantInputCapture : NetworkBehaviour
{
    private StateManager _localStateManager;
    public bool ReadyForAssignment = false;

    private NetworkVehicleController NetworkedVehicle;

    public NetworkVariable<GpsController.Direction> CurrentDirection =
        new NetworkVariable<GpsController.Direction>(NetworkVariableReadPermission.Everyone);

    private GpsController m_GpsController;


    public Transform _transform;
    public string lang { private set; get; }
    private const string OffsetFileName = "offset";

    void Awake()
    {
        ReadyForAssignment = false;
    }


    public static ParticipantInputCapture GetMyPIC()
    {
        foreach (ParticipantInputCapture pic in FindObjectsOfType<ParticipantInputCapture>())
        {
            if (pic.IsLocalPlayer)
            {
                return pic;
            }
        }

        return null;
    }

    private void Start()
    {
    }

    private void NewGpsDirection(GpsController.Direction previousvalue, GpsController.Direction newvalue)
    {
        if (m_GpsController != null)
        {
            m_GpsController.SetDirection(newvalue);
        }
    }


    public override void OnNetworkSpawn()
    {
        if (IsClient && !IsLocalPlayer) return;
        if (IsLocalPlayer)
        {
            CurrentDirection.OnValueChanged += NewGpsDirection;
            _localStateManager = GetComponent<StateManager>();
          //ToDo delete
            // NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(
               // QNDataStorageServer.QNContentMessageName, AddQuestionsToTheQueue);

            ConfigFileLoading conf = new ConfigFileLoading();
            conf.Init(OffsetFileName);
            if (conf.FileAvalible())
            {
                conf.LoadLocalOffset(out offsetPositon, out offsetRotation);
            }

            m_participantOrder = ConnectionAndSpawing.Singleton.ParticipantOrder;
        }
        else if (IsServer)
        {
            m_participantOrder = ConnectionAndSpawing.Singleton.GetParticipantOrderClientId(OwnerClientId);
            UpdateOffsetRemoteClientRPC(offsetPositon, offsetRotation, LastRot);
            GetComponent<ParticipantOrderReplayComponent>().SetParticipantOrder(m_participantOrder);
        }

     
    }

    private ParticipantOrder m_participantOrder = ParticipantOrder.None;
    
    
    public NetworkVariable<bool> ButtonPushed; // This is only active during QN time 


    [ServerRpc]
    public void PostQuestionServerRPC(ulong clientID)
    {
        ConnectionAndSpawing.Singleton.FinishedQuestionair(clientID);
    }


    // [ClientRpc]
    //public void StartQuestionnaireClientRpc() {
    //      if (IsLocalPlayer) { FindObjectOfType<ScenarioManager>().RunQuestionairNow(transform); }
    // }


    [ClientRpc]
    public void SetGPSClientRpc(GpsController.Direction[] dir)
    {
        // GetComponentInChildren<GpsController>().SetDirection(dir[SceneStateManager.Instance.getParticipantID()]);
    }

    private void OnGUI()
    {
        if (IsLocalPlayer)
            GUI.Label(new Rect(200, 5, 150, 100), "Client State" + _localStateManager.GlobalState.Value);
    }

    public void AssignCarTransform(NetworkVehicleController MyCar, ulong targetClient)
    {
        if (IsServer)
        {
            NetworkedVehicle = MyCar;
            _transform = NetworkedVehicle.transform.Find("CameraPosition");
            AssignCarTransformClientRPC(MyCar.NetworkObject, targetClient);
        }
    }

    [ClientRpc]
    private void AssignCarTransformClientRPC(NetworkObjectReference MyCar, ulong targetClient)
    {
        if (MyCar.TryGet(out NetworkObject targetObject))
        {
            if (targetClient == OwnerClientId)
            {
                NetworkedVehicle = targetObject.transform.GetComponent<NetworkVehicleController>();

                Debug.Log("Tried to get a new car. Its my Car!");
            }

            _transform = NetworkedVehicle.transform.Find("CameraPosition");
        }
        else
        {
            Debug.LogWarning(
                "Did not manage to get my Car assigned interactions will not work. Maybe try calling this RPC later.");
        }
    }


    public void De_AssignCarTransform(ulong targetClient)
    {
        if (IsServer)
        {
            NetworkedVehicle = null;
            De_AssignCarTransformClientRPC(targetClient);
        }
    }

    [ClientRpc]
    private void De_AssignCarTransformClientRPC(ulong targetClient)
    {
        NetworkedVehicle = null;
        _transform = null;
        DontDestroyOnLoad(gameObject);
        Debug.Log("De_assign Car ClientRPC");
    }


    [ClientRpc]
    public void CalibrateClientRPC(ClientRpcParams clientRpcParams = default)
    {
        if (!IsLocalPlayer) return;
        GetComponent<SeatCalibration>().StartCalibration(
            NetworkedVehicle.transform.Find("SteeringCenter"),
            transform.Find("TrackingSpace").Find("CenterEyeAnchor"),
            this);
        Debug.Log("Calibrate ClientRPC");
    }

    void Update()
    {
        if (IsLocalPlayer)
        {
            if (m_GpsController == null && _transform != null)
            {
                m_GpsController = _transform.parent.GetComponentInChildren<GpsController>();
                if (m_GpsController != null)
                {
                    m_GpsController.SetDirection(CurrentDirection.Value);
                }
            }
        }

        if (IsServer)
        {
            ButtonPushed.Value = SteeringWheelManager.Singleton.GetButtonInput(m_participantOrder);
        }
    }


    private bool lastValue = false;

    public bool ButtonPush()
    {
        if (lastValue == true && ButtonPushed.Value == false)
        {
            lastValue = ButtonPushed.Value;
            Debug.Log("Button Got pushed!!");
            return true;
        }
        else
        {
            lastValue = ButtonPushed.Value;
            return false;
        }
    }


    private Quaternion offsetRotation = Quaternion.identity;
    private Vector3 offsetPositon = Vector3.zero;

    private Quaternion LastRot = Quaternion.identity;
    private bool init = false;


    private void LateUpdate()
    {
        if (_transform != null)
        {
            var transform1 = transform;
            var transform2 = _transform;
            transform1.rotation = transform2.rotation * offsetRotation;
            if (!init && IsLocalPlayer)
            {
                LastRot = transform1.rotation;
                init = true;
                ShareOffsetServerRPC(offsetPositon, offsetRotation, LastRot);
            }

            transform1.position = transform2.position +
                                  ((transform1.rotation * Quaternion.Inverse(LastRot)) * offsetPositon);

            //  if (IsServer) {
            //   Debug.Log("Updating relative positon on server with: "+offsetPositon.ToString()+" and "+offsetRotation.ToString() );
            //  }
        }
    }


    public void SetNewRotationOffset(Quaternion yawCorrection)
    {
        offsetRotation *= yawCorrection;
    }

    public void SetNewPositionOffset(Vector3 positionOffset)
    {
        offsetPositon += positionOffset;
    }

    public void FinishedCalibration()
    {
        ConfigFileLoading conf = new ConfigFileLoading();
        conf.Init(OffsetFileName);
        conf.StoreLocalOffset(offsetPositon, offsetRotation);
        ShareOffsetServerRPC(offsetPositon, offsetRotation, LastRot);
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
    public void UpdateOffsetRemoteClientRPC(Vector3 _offsetPositon, Quaternion _offsetRotation, Quaternion InitRotation)
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
        return NetworkedVehicle.transform;
    }

    public bool DeleteCallibrationFile()
    {
        ConfigFileLoading conf = new ConfigFileLoading();
        conf.Init(OffsetFileName);
        return conf.DeleteFile();
    }
    
     
    [ClientRpc]

    public void StartQuestionairClientRPC()
    {
        if (!IsLocalPlayer) return;
        FindObjectOfType<ScenarioManager>().RunQuestionairNow(transform);
    }
    
    
    /*
    [ClientRpc]
    public void AddQuestionsToTheQueueClientRPC(ulong senderclientid, FastBufferReader messagepayload)
    {
        if (!IsLocalPlayer) return;
        messagepayload.ReadNetworkSerializable<LongStringMessage>(out LongStringMessage todo);
        Debug.Log("Got new QN file");
        
        
    }*/
    
   
    public void SendQNAnswer(int id, int answerIndex,string lang)
    {
        if (!IsLocalPlayer) return;
        HasNewData = false;
        SendQNAnswerServerRPC(id, answerIndex, lang);

    }
    
    
[ServerRpc]
    public void SendQNAnswerServerRPC(int id, int answerIndex,string lang)
    {
        if (!IsServer) return;
        ConnectionAndSpawing.Singleton.QNNewDataPoint(m_participantOrder,id,answerIndex,lang);
        
    }
    
[ClientRpc]
    public void RecieveNewQuestionClientRPC(NetworkedQuestionnaireQuestion newq)
    {
        if (!IsLocalPlayer) return;
        Debug.Log("Got new data and updated varaible"+HasNewData);
        if (HasNewData)
        {
            Debug.LogWarning("I still had a question ready to go. Losing data here. this should not happen.");
            
        }
        else
        {
            HasNewData = true;
           
        }

        lastQuestion = newq;

    }
    
    private NetworkedQuestionnaireQuestion lastQuestion;
    private bool HasNewData = false;
    public bool HasNewQuestion()
    {
        return HasNewData;
    }

    public NetworkedQuestionnaireQuestion GetNewQuestion()
    {
        if (HasNewData)
        {
            HasNewData = false;
            return lastQuestion;

        }
        else
        {
            Debug.LogError("I did have a new question yet I was asked for one quitting the QN early!");
            return new NetworkedQuestionnaireQuestion{reply = replyType.FINISHED}; 
        }
    }
    
}