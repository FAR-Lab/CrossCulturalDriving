using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

public class TestScreenClient : Client_Object {

    public GameObject QuestionairPrefab;
    private QN_Display qnmanager;
    
    private const string OffsetFileName = "TestScreenClientOffset";
    private NetworkVariable<SpawnType> m_spawnType;
    private NetworkVariable<ParticipantOrder> m_participantOrder;
    private Interactable_Object m_InteractableObject;

    private float m_MaxFlySpeed=1.4f;
    private Vector3 m_Motion;
    [FormerlySerializedAs("MyCamera")] public Transform m_Camera;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
        Vector3 Motion = Vector3.zero;
        switch (m_spawnType.Value) {
            case SpawnType.NONE:
                break;
            case SpawnType.CAR:
                break;
            case SpawnType.PEDESTRIAN:
                 Motion.z = Input.GetKey(KeyCode.W) == true ? 1 : Input.GetKey(KeyCode.S) ? -1 : 0;
                 Motion.x = Input.GetKey(KeyCode.A) == true ? -1 : Input.GetKey(KeyCode.D) ? 1 : 0;

                 Vector3.Lerp(m_Motion, Motion, 0.5f);
                 transform.Translate(m_Motion*Time.deltaTime);
                break;
            case SpawnType.PASSENGER:
                break;
            case SpawnType.ROBOT:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        
    }

    public override ParticipantOrder GetParticipantOrder() {
        return m_participantOrder.Value;
    }

    public void SetParticipantOrder(ParticipantOrder po) {
        m_participantOrder.Value = po;
    }
    public override void SetSpawnType(SpawnType _spawnType) {
        m_spawnType.Value = _spawnType;
        if (IsServer) {
            switch (m_spawnType.Value) {
                case SpawnType.NONE:
                    break;
                case SpawnType.CAR:
                    GetComponent<CapsuleCollider>().enabled = false;
                    
                    break;
                case SpawnType.PEDESTRIAN:
                    GetComponent<CapsuleCollider>().enabled = true;
                    break;
                case SpawnType.PASSENGER:
                    break;
                case SpawnType.ROBOT:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public override void AssignFollowTransform(Interactable_Object MyInteractableObject, ulong targetClient) {
        if (IsServer)
        {
            m_InteractableObject = MyInteractableObject;
            NetworkObject.TrySetParent(m_InteractableObject.NetworkObject, false);
            AssignInteractable_ClientRPC(m_InteractableObject.GetComponent<NetworkObject>(), targetClient);
            if (m_spawnType.Value == SpawnType.CAR) {
                m_InteractableObject.GetComponent<NetworkVehicleController>().VehicleMode =
                    NetworkVehicleController.VehicleOpperationMode.KEYBOARD;
            }
        }
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
            }
            m_InteractableObject = targetObject.transform.GetComponent<Interactable_Object>();
        }
        else
        {
            Debug.LogError(
                "Did not manage to get my Car assigned interactions will not work. Maybe try calling this RPC later.");
        }
    }

    public override Interactable_Object GetFollowTransform() {
       return m_InteractableObject;
    }

    public override void De_AssignFollowTransform(ulong targetClient, NetworkObject netobj)
    {
        if (IsServer)
        {
            NetworkObject.TryRemoveParent(false);
            m_InteractableObject = null;
            De_AssignFollowTransformClientRPC(targetClient);
            DontDestroyOnLoad(gameObject);
        }
    }

    [ClientRpc]
    private void De_AssignFollowTransformClientRPC(ulong targetClient)
    {
            //ToDo: currently we just deassigned everything but NetworkInteractable object and _transform could turn into lists etc...
            m_InteractableObject = null;

            DontDestroyOnLoad(gameObject);
            Debug.Log("De_assign Interactable ClientRPC");
        

    }

    
    public override Transform GetMainCamera() {
        if (m_Camera == null) {
            m_Camera = GetComponentInChildren<Camera>().transform;
        }
       return m_Camera;
    }

    public override void CalibrateClient(ClientRpcParams clientRpcParams) {
        if (m_InteractableObject != null) {
            transform.position += m_InteractableObject.GetCameraPositionObject().position-GetMainCamera().position;
            transform.rotation = m_InteractableObject.GetCameraPositionObject().rotation;
            var conf = new ConfigFileLoading();
            conf.Init(OffsetFileName);
            conf.StoreLocalOffset(transform.localPosition, transform.localRotation);
        }
        Debug.Log("Not Sure yet how to CalibrateClient for the threeScreen");
    }

    
    public override void StartQuestionair(QNDataStorageServer m_QNDataStorageServer) {
        qnmanager = Instantiate(QuestionairPrefab).GetComponent<QN_Display>();
        qnmanager.GetComponent<NetworkObject>().SpawnWithOwnership(OwnerClientId,true);
        string referenceTransformPath="";//TODO this is not Implemented
        QN_Display.FollowType tmp = QN_Display.FollowType.MainCamera;
        Vector3 Offset = Vector3.zero;
        bool KeepUpdating = false;
        switch (m_spawnType.Value) {
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


       
        qnmanager.StartQuestionair(m_QNDataStorageServer,m_participantOrder.Value,tmp,Offset,KeepUpdating,referenceTransformPath);
        
        m_QNDataStorageServer.RegisterQNSCreen(m_participantOrder.Value, qnmanager);
    }
}
