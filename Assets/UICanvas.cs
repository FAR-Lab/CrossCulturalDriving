using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Burst;
using Unity.Netcode;
using UnityEngine.Serialization;
using UnityEngine.XR.Interaction.Toolkit.UI;

public class UICanvas : MonoBehaviour
{
    # region Prefabs
    public GameObject buttonTemplate_NoIcon;
    public GameObject buttonTemplate_HasIcon;
    # endregion
    
    # region Fields
    public GameObject scenarioButtons;
    public GameObject driverCalibrationButtons;
    public TextMeshProUGUI serverInformation;
    # endregion
    
    
    private readonly Dictionary<GameObject, SceneField> _buttonToScene = new Dictionary<GameObject, SceneField>();
    private Dictionary<SpawnType, Sprite> _spawnTypeToSprite;

    [NonSerialized]
    private Dictionary<ParticipantOrder, Transform> _spawnedButtons = new Dictionary<ParticipantOrder, Transform>();
    void Awake()
    {
        DontDestroyOnLoad(this);
    }
    
    void Start()
    {
       
        ConnectionAndSpawning.Singleton.ServerStateChange += UpdateScenarioButton;
        ConnectionAndSpawning.Singleton.ServerStateChange += ToggleScenarioButtonVisibility;
        
        NetworkManager.Singleton.OnServerStarted += InitializeCanvas;
        NetworkManager.Singleton.OnClientConnectedCallback += CreateButton;
        NetworkManager.Singleton.OnClientDisconnectCallback += DeleteButton;
        _spawnTypeToSprite = Resources.Load<SO_SpawnTypeToSprite>("SO/SO_SpawnTypeToSprite").EnumToValueDictionary;
    }

    void Update()
    {
        UpdateInformationText();
    }
    
    private void InitializeCanvas()
    {
        CreateScenarioButtons();
    }
    
    # region Scenario Buttons
    private void CreateScenarioButtons()
    {
        foreach (var sceneField in ConnectionAndSpawning.Singleton.IncludedScenes)
        {
            if (!ConnectionAndSpawning.Singleton.VisitedScenes.ContainsKey(sceneField))
            {
                ConnectionAndSpawning.Singleton.VisitedScenes.Add(sceneField, false);
            }
            GameObject button = Instantiate(buttonTemplate_NoIcon);
            button.transform.SetParent(scenarioButtons.transform);
            button.name = sceneField.SceneName;
            button.GetComponentInChildren<TextMeshProUGUI>().text = sceneField.SceneName;
            button.GetComponentInChildren<Button>().onClick.AddListener(() => { OnScenarioButtonPressed(sceneField); });
            _buttonToScene.Add(button, sceneField);

        }
    }

    private void OnScenarioButtonPressed(SceneField sceneField)
    {
        ConnectionAndSpawning.Singleton.VisitedScenes[sceneField] = true;
        ConnectionAndSpawning.Singleton.SwitchToLoading(sceneField.SceneName);
    }

    private void UpdateScenarioButton(ActionState actionState)
    {
        if (actionState == ActionState.LOADINGSCENARIO)
        {
            foreach (var button in _buttonToScene.Keys)
            {
                if (ConnectionAndSpawning.Singleton.VisitedScenes[_buttonToScene[button]])
                {
                    button.GetComponentInChildren<TextMeshProUGUI>().color = Color.red;
                }
            }
        }
    }

    private void ToggleScenarioButtonVisibility(ActionState actionState)
    {
        if (actionState == ActionState.LOADINGSCENARIO)
        {
            scenarioButtons.SetActive(false);
        }
        else if (actionState == ActionState.WAITINGROOM)
        {
            scenarioButtons.SetActive(true);
        }
    }
    
    # endregion
    
    # region Calibration Buttons
    private void CreateButton(ulong clientID)
    {
        ParticipantOrder po;
        bool success = ConnectionAndSpawning.Singleton.participants.GetOrder(clientID, out po);
        if (!success)
        {
            return;
        }
        Debug.Log($"Creating button for {po}");
        GameObject button = Instantiate(buttonTemplate_HasIcon);
        button.transform.SetParent(driverCalibrationButtons.transform);
        button.name = $"Calibrate {po}";
        button.GetComponentInChildren<TextMeshProUGUI>().text = $"Calibrate {po}";
        SpawnType spawnType;
        ConnectionAndSpawning.Singleton.participants.GetSpawnType(po, out spawnType);
        button.transform.Find("Icon").GetComponent<Image>().sprite = _spawnTypeToSprite[spawnType];
        button.GetComponentInChildren<Button>().onClick.AddListener(() => { OnCalibrationButtonPressed(po); });
        _spawnedButtons.Add(po,button.transform);
    }
    
    private void DeleteButton(ulong clientID)
    {
        ParticipantOrder po;
        bool success = ConnectionAndSpawning.Singleton.participants.GetOrder(clientID, out po);
        if (!success)
        {
            return;
        }

        if (_spawnedButtons.ContainsKey(po))
        {
            var tmp = _spawnedButtons[po];
            _spawnedButtons.Remove(po);
            Destroy(tmp.gameObject);
            
        }
        else
        {
            Debug.LogWarning($"Could not find calibration button for po:{po}");
        }
        
    }

    private void OnCalibrationButtonPressed(ParticipantOrder participant)
    {
        var success = ConnectionAndSpawning.Singleton.participants.GetClientID(participant, out var clientID);
        if (!success) return;

        var clientRpcParams = new ClientRpcParams {
            Send = new ClientRpcSendParams {
                TargetClientIds = new[] { clientID }
            }
        };
        ConnectionAndSpawning.Singleton.Main_ParticipantObjects[participant].CalibrateClient(clientRpcParams);
    }
    
    # endregion
    
    # region Information Text
    private void UpdateInformationText()
    {
        if (!NetworkManager.Singleton.IsServer) return;
        serverInformation.text = $"Server: {ConnectionAndSpawning.Singleton.ParticipantOrder} {NetworkManager.Singleton.ConnectedClients.Count} " +
                                 $"{(ConnectionAndSpawning.Singleton.participants.GetParticipantCount() - 1).ToString()}\n{ConnectionAndSpawning.Singleton.ServerState} {Time.timeScale}";
    }
    # endregion
}
