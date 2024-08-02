using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class TestScreenClient : Client_Object {
    private const string OffsetFileName = "TestScreenClientOffset";
    private const float k_LookMultiplier = 500;

    public GameObject QuestionairPrefab;

    public float walkingSpeed = 1.5f;
    public Transform m_Camera;
    private readonly NetworkVariable<ActionState> m_ActionState = new();
    private readonly NetworkVariable<ParticipantOrder> m_participantOrder = new();
    private readonly NetworkVariable<SpawnType> m_spawnType = new();

    private CharacterController m_characterController;
    private Interactable_Object m_InteractableObject;
    private NetworkVehicleController m_VehicleController;

    private QN_Display qnmanager;

    // Start is called before the first frame update
    private void Start() {
        m_characterController = GetComponent<CharacterController>();
    }

    // Update is called once per frame
    private void Update() {
        if (IsServer) m_ActionState.Value = ConnectionAndSpawning.Singleton.ServerState;
        if (!IsLocalPlayer) return;


        switch (m_spawnType.Value) {
            case SpawnType.NONE:
                break;
            case SpawnType.CAR:
                if (m_ActionState.Value == ActionState.DRIVE) {
                    float Throttle = Input.GetKey(KeyCode.UpArrow) ? 1 :
                        Input.GetKey(KeyCode.DownArrow) ? -1 : 0;
                    float steering = Input.GetKey(KeyCode.LeftArrow) ? -1 :
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
                    var Motion = Vector3.zero;
                    Motion.z = Input.GetKey(KeyCode.W) ? 1 : Input.GetKey(KeyCode.S) ? -1 : 0;
                    Motion.x = Input.GetKey(KeyCode.A) ? -1 : Input.GetKey(KeyCode.D) ? 1 : 0;


                    var mouseX = Input.GetAxis("Mouse X");
                    var mouseY = -Input.GetAxis("Mouse Y");

                    if (!m_characterController.isGrounded) Motion.y = -m_characterController.stepOffset * 2f;

                    // Apply the rotation
                    transform.rotation *= Quaternion.Euler(0, mouseX * k_LookMultiplier * Time.deltaTime, 0);
                    m_characterController.Move(transform.rotation * (Motion * Time.deltaTime * walkingSpeed));
                    m_Camera.localRotation *= Quaternion.Euler(mouseY * k_LookMultiplier * Time.deltaTime, 0, 0);
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
        if (IsServer && m_spawnType.Value == SpawnType.CAR)
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

    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();
        if (!IsLocalPlayer) {
            DisableNonLocalobjects();
        }
        else {
            m_ActionState.OnValueChanged += ActionStateUpdate;
            GetMainCamera();
        }

        if (IsServer) SetupButtons();
    }

    private void SetupButtons() {
        var researcher_UI = FindObjectOfType<Researcher_UI>();
        researcher_UI.CreateButton("Calibrate", Calibrate, OwnerClientId);
    }

    private void ActionStateUpdate(ActionState previousvalue, ActionState newvalue) {
        if (newvalue == ActionState.DRIVE && m_spawnType.Value == SpawnType.PEDESTRIAN)
            Cursor.lockState = CursorLockMode.Locked;
        else
            Cursor.lockState = CursorLockMode.None;
    }

    private void DisableNonLocalobjects() {
        GetComponentInChildren<Camera>().enabled = false;
        GetComponentInChildren<AudioListener>().enabled = false;
    }

    public override ParticipantOrder GetParticipantOrder() {
        return m_participantOrder.Value;
    }

    public override void SetParticipantOrder(ParticipantOrder _ParticipantOrder) {
        m_participantOrder.Value = _ParticipantOrder;
    }

    public override void SetSpawnType(SpawnType _spawnType) {
        m_spawnType.Value = _spawnType;
        if (IsServer)
            switch (m_spawnType.Value) {
                case SpawnType.NONE:
                    break;
                case SpawnType.CAR:
                    GetComponent<CharacterController>().enabled = false;

                    break;
                case SpawnType.PEDESTRIAN:
                    GetComponent<CharacterController>().enabled = true;
                    break;
                case SpawnType.PASSENGER:
                    break;
                case SpawnType.ROBOT:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
    }

    public override void AssignFollowTransform(Interactable_Object MyInteractableObject, ulong targetClient) {
        if (IsServer) {
            m_InteractableObject = MyInteractableObject;
            NetworkObject.TrySetParent(m_InteractableObject.NetworkObject, false);
            AssignInteractable_ClientRPC(m_InteractableObject.GetComponent<NetworkObject>(), targetClient);
            if (m_spawnType.Value == SpawnType.CAR) {
                m_VehicleController = m_InteractableObject.GetComponent<NetworkVehicleController>();
                m_VehicleController.VehicleMode =
                    NetworkVehicleController.VehicleOpperationMode.REMOTEKEYBOARD;
            }
        }
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
            }

            m_InteractableObject = targetObject.transform.GetComponent<Interactable_Object>();
            if (m_spawnType.Value == SpawnType.CAR)
                m_VehicleController = m_InteractableObject.GetComponent<NetworkVehicleController>();
        }
        else {
            Debug.LogError(
                "Did not manage to get my Car assigned interactions will not work. Maybe try calling this RPC later.");
        }
    }

    public override Interactable_Object GetFollowTransform() {
        return m_InteractableObject;
    }

    public override void De_AssignFollowTransform(ulong targetClient, NetworkObject netobj) {
        if (IsServer) {
            NetworkObject.TryRemoveParent(false);
            m_InteractableObject = null;
            De_AssignFollowTransformClientRPC(targetClient);
            DontDestroyOnLoad(gameObject);
        }
    }

    [ClientRpc]
    private void De_AssignFollowTransformClientRPC(ulong targetClient) {
        //ToDo: currently we just deassigned everything but NetworkInteractable object and _transform could turn into lists etc...
        m_InteractableObject = null;

        DontDestroyOnLoad(gameObject);
        Debug.Log("De_assign Interactable ClientRPC");
    }


    public override Transform GetMainCamera() {
        if (m_Camera == null) m_Camera = GetComponentInChildren<Camera>().transform;
        return m_Camera;
    }

    public void Calibrate(Action<bool> finishedCalibration) {
        if (!IsLocalPlayer) return;
        if (m_InteractableObject != null) {
            transform.position += m_InteractableObject.GetCameraPositionObject().position - GetMainCamera().position;
            transform.rotation = m_InteractableObject.GetCameraPositionObject().rotation;
            var conf = new ConfigFileLoading();
            conf.Init(OffsetFileName);
            conf.StoreLocalOffset(transform.localPosition, transform.localRotation);
            finishedCalibration.Invoke(true);
        }
        else {
            finishedCalibration.Invoke(false);
            Debug.LogWarning("Couldnt calibrate Test screen.");
        }
    }


    public override void StartQuestionair(QNDataStorageServer m_QNDataStorageServer) {
        qnmanager = Instantiate(QuestionairPrefab).GetComponent<QN_Display>();
        qnmanager.GetComponent<NetworkObject>().SpawnWithOwnership(OwnerClientId, true);
        var referenceTransformPath = ""; //TODO this is not Implemented
        var tmp = QN_Display.FollowType.MainCamera;
        var Offset = Vector3.zero;
        var KeepUpdating = false;
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
        }

        Debug.Log($"SpawnType {m_spawnType.Value}  FollowType:{tmp} KeepUpdating:{KeepUpdating}");

        qnmanager.StartQuestionair(m_QNDataStorageServer, m_participantOrder.Value, tmp, Offset, KeepUpdating,
            referenceTransformPath, this);

        m_QNDataStorageServer.RegisterQNScreen(m_participantOrder.Value, qnmanager);

        Debug.Log("Setup Questionnaire serverside, ready for Questions");
    }

    public override void GoForPostQuestion() {
        if (!IsLocalPlayer) return;
        PostQuestionServerRPC(OwnerClientId);
    }

    public override void SetNewNavigationInstruction(
        Dictionary<ParticipantOrder, NavigationScreen.Direction> Directions) { }

    [ServerRpc]
    public void PostQuestionServerRPC(ulong clientID) {
        ConnectionAndSpawning.Singleton.FinishedQuestionair(clientID);
    }
}