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
    
    private ConnectionAndSpawning CAS;
    private readonly Dictionary<GameObject, SceneField> _buttonToScene = new Dictionary<GameObject, SceneField>();
    private Dictionary<SpawnType, Sprite> _spawnTypeToSprite;
    void Awake()
    {
        DontDestroyOnLoad(this);
    }
    
    void Start()
    {
        CAS = ConnectionAndSpawning.Singleton;
        
        CAS.ServerStateChange += UpdateScenarioButton;
        CAS.ServerStateChange += ToggleScenarioButtonVisibility;
        
        NetworkManager.Singleton.OnServerStarted += InitializeCanvas;
        NetworkManager.Singleton.OnClientConnectedCallback += CreateButton;
        
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
        foreach (var sceneField in CAS.IncludedScenes)
        {
            if (!CAS.VisitedScenes.ContainsKey(sceneField))
            {
                CAS.VisitedScenes.Add(sceneField, false);
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
        CAS.VisitedScenes[sceneField] = true;
        CAS.SwitchToLoading(sceneField.SceneName);
    }

    private void UpdateScenarioButton(ActionState actionState)
    {
        if (actionState == ActionState.LOADINGSCENARIO)
        {
            foreach (var button in _buttonToScene.Keys)
            {
                if (CAS.VisitedScenes[_buttonToScene[button]])
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
        ParticipantOrder participant;
        bool success = CAS.participants.GetOrder(clientID, out participant);
        if (!success)
        {
            return;
        }
        Debug.Log($"Creating button for {participant}");
        GameObject button = Instantiate(buttonTemplate_HasIcon);
        button.transform.SetParent(driverCalibrationButtons.transform);
        button.name = $"Calibrate {participant}";
        button.GetComponentInChildren<TextMeshProUGUI>().text = $"Calibrate {participant}";
        SpawnType spawnType;
        CAS.participants.GetSpawnType(participant, out spawnType);
        button.transform.Find("Icon").GetComponent<Image>().sprite = _spawnTypeToSprite[spawnType];
        button.GetComponentInChildren<Button>().onClick.AddListener(() => { OnCalibrationButtonPressed(participant); });
        
    }

    private void OnCalibrationButtonPressed(ParticipantOrder participant)
    {
        var success = CAS.participants.GetClientID(participant, out var clientID);
        if (!success) return;

        var clientRpcParams = new ClientRpcParams {
            Send = new ClientRpcSendParams {
                TargetClientIds = new[] { clientID }
            }
        };
        CAS.Main_ParticipantObjects[participant].CalibrateClient(clientRpcParams);
    }
    
    # endregion
    
    # region Information Text
    private void UpdateInformationText()
    {
        if (!NetworkManager.Singleton.IsServer) return;
        serverInformation.text = $"Server: {CAS.ParticipantOrder} {NetworkManager.Singleton.ConnectedClients.Count} " +
                                 $"{(CAS.participants.GetParticipantCount() - 1).ToString()}\n{CAS.ServerState} {Time.timeScale}";
    }
    # endregion
}
