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
    private NetworkVariable<SpawnType> m_spawnType=new NetworkVariable<SpawnType>();
    private NetworkVariable<ParticipantOrder> m_participantOrder=new NetworkVariable<ParticipantOrder>();
    private NetworkVariable<ActionState> m_ActionState=new NetworkVariable<ActionState>();
    private Interactable_Object m_InteractableObject;
    private NetworkVehicleController m_VehicleController;

    private float m_MaxFlySpeed=1.4f;
    private Vector3 m_Motion;
    [FormerlySerializedAs("MyCamera")] public Transform m_Camera;
    private Vector2 m_CamRotation = Vector2.zero;
    private const float k_LookMultiplier = 600;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update() {
        if (IsServer) {
            m_ActionState.Value = ConnectionAndSpawning.Singleton.ServerState;
        }
        if (!IsLocalPlayer) return;
        
        Vector3 Motion = Vector3.zero;
        switch (m_spawnType.Value) {
            case SpawnType.NONE:
                break;
            case SpawnType.CAR:
                if (m_ActionState.Value == ActionState.DRIVE) {
                    float Throttle = Input.GetKey(KeyCode.UpArrow) == true ? 1 :
                        Input.GetKey(KeyCode.DownArrow) ? -1 : 0;
                    float steering = Input.GetKey(KeyCode.LeftArrow) == true ? -1 :
                        Input.GetKey(KeyCode.RightArrow) ? 1 : 0;
                    NewDataForTheCarServerRPC(steering, Throttle,
                        Input.GetKey(KeyCode.J),
                        Input.GetKey(KeyCode.L),
                        Input.GetKey(KeyCode.K)
                    );
                }

                break;
            case SpawnType.PEDESTRIAN:
                if (m_ActionState.Value == ActionState.DRIVE) {
                    Motion.z = Input.GetKey(KeyCode.W) == true ? 1 : Input.GetKey(KeyCode.S) ? -1 : 0;
                    Motion.x = Input.GetKey(KeyCode.A) == true ? -1 : Input.GetKey(KeyCode.D) ? 1 : 0;

                    m_Motion = Vector3.Lerp(m_Motion, Motion, 0.05f);
                    transform.Translate(m_Motion * Time.deltaTime);
                    
                    float mouseX = Input.GetAxis("Mouse X");
                    float mouseY = -Input.GetAxis("Mouse Y");

                

                    // Apply the rotation
                    transform.rotation *= Quaternion.Euler(0,   mouseX* k_LookMultiplier * Time.deltaTime, 0);
                    
                    m_Camera.localRotation *= Quaternion.Euler(  mouseY* k_LookMultiplier * Time.deltaTime, 0, 0);

                }

                break;
            case SpawnType.PASSENGER:
                break;
            case SpawnType.ROBOT:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        
    }

    [ServerRpc]
    public void NewDataForTheCarServerRPC(float i_steering, 
        float i_throttle,
        bool i_left,
        bool i_right,
        bool i_honk) {
        if (IsServer && m_spawnType.Value == SpawnType.CAR) {
//            Debug.Log($"Sending New Data To the Car" +
      //                $"i_steering{i_steering}," +
      ///                $"i_throttle{i_throttle}," +
          //            $"i_left{i_left}." +
        ///              $"i_right{i_right}," +
             //         $"i_honk{i_honk}" );
            m_VehicleController.NewDataToCome(
                i_steering,
                i_throttle,
                i_left,
                i_right,
                i_honk
            );
        }
        
        
    }

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        if (!IsLocalPlayer) {
            DisableNonLocalobjects();
        }
        else {
            m_ActionState.OnValueChanged += ActionStateUpdate;
            GetMainCamera();
        }
        
    }

    private void ActionStateUpdate(ActionState previousvalue, ActionState newvalue) {
        if (newvalue == ActionState.DRIVE && m_spawnType.Value==SpawnType.PEDESTRIAN) {
            Cursor.lockState = CursorLockMode.Locked; 
        }
        else {
            Cursor.lockState = CursorLockMode.None;
        }
    }

    private void DisableNonLocalobjects() {
        GetComponentInChildren<Camera>().enabled = false;
        GetComponentInChildren<AudioListener>().enabled = false;

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
                m_VehicleController=m_InteractableObject.GetComponent<NetworkVehicleController>();
                m_VehicleController.VehicleMode =
                    NetworkVehicleController.VehicleOpperationMode.REMOTEKEYBOARD;
                
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
            if (m_spawnType.Value == SpawnType.CAR) {
                m_VehicleController = m_InteractableObject.GetComponent<NetworkVehicleController>();
            }
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
