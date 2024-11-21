using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class Researcher_UI : MonoBehaviour {
    public Transform PedIndicator;


    private readonly Dictionary<GameObject, SceneField> _buttonToScene = new();

    [NonSerialized] private readonly Dictionary<ParticipantOrder, List<Transform>> _spawnedButtons = new();

    private Dictionary<SpawnType, Sprite> _spawnTypeToSprite;


    private Dictionary<string, Dictionary<ParticipantOrder, float[]>> dict;

    private void Awake() {
        DontDestroyOnLoad(this);
    }

    private void Start() {
        ConnectionAndSpawning.Singleton.ServerStateChange += UpdateScenarioButton;
        ConnectionAndSpawning.Singleton.ServerStateChange += ToggleScenarioButtonVisibility;

        NetworkManager.Singleton.OnServerStarted += InitializeCanvas;
        NetworkManager.Singleton.OnClientDisconnectCallback += DeleteButton;
        _spawnTypeToSprite = Resources.Load<SO_SpawnTypeToSprite>("ScriptableObjects/SO_SpawnTypeToSprite")
            .EnumToValueDictionary;


        var path = ScenarioManager.PedestrianSpawnPointLocationPathJson;
        path = path.Replace(".json", "");
        Debug.Log(path);

        var text = Resources.Load<TextAsset>(path);

        dict = JsonConvert
            .DeserializeObject<Dictionary<string, Dictionary<ParticipantOrder, float[]>>>(text.ToString());
        if (dict != null) PedIndicator = FindObjectOfType<PedestrianWalkingTarget>().transform;
    }

    private void Update() {
        UpdateInformationText();
    }

    private void InitializeCanvas() {
        CreateScenarioButtons();
    }

    # region Information Text

    private void UpdateInformationText() {
        if (!NetworkManager.Singleton.IsServer) return;
        serverInformation.text =
            $"Server: {ConnectionAndSpawning.Singleton.ParticipantOrder} {NetworkManager.Singleton.ConnectedClients.Count} " +
            $"{(ConnectionAndSpawning.Singleton.participants.GetParticipantCount() - 1).ToString()}\n{ConnectionAndSpawning.Singleton.ServerState} {Time.timeScale}";
    }

    # endregion

    # region Prefabs

    public GameObject buttonTemplate_NoIcon;
    public GameObject buttonTemplate_HasIcon;
    public GameObject Seperator;

    # endregion

    # region Fields

    public GameObject scenarioButtons;
    public GameObject driverCalibrationButtons;
    public TextMeshProUGUI serverInformation;

    # endregion

    # region Scenario Buttons

    private void CreateScenarioButtons() {
        foreach (var sceneField in ConnectionAndSpawning.Singleton.IncludedScenes) {
            if (!ConnectionAndSpawning.Singleton.VisitedScenes.ContainsKey(sceneField))
                ConnectionAndSpawning.Singleton.VisitedScenes.Add(sceneField, false);
            if (dict.ContainsKey(sceneField.SceneName)) {
                var button2 = Instantiate(buttonTemplate_NoIcon);
                button2.transform.SetParent(scenarioButtons.transform);
                button2.name = "PreLoad:" + sceneField.SceneName;
                button2.GetComponentInChildren<TextMeshProUGUI>().text = button2.name;
                button2.GetComponentInChildren<Button>().onClick.AddListener(() => {
                    OnPreLoadScenarioButtonPressed(sceneField);
                });
            }

            var button = Instantiate(buttonTemplate_NoIcon);
            button.transform.SetParent(scenarioButtons.transform);
            button.name = sceneField.SceneName;

            button.GetComponentInChildren<TextMeshProUGUI>().text = sceneField.SceneName;
            button.GetComponentInChildren<Button>().onClick.AddListener(() => { OnScenarioButtonPressed(sceneField); });
            _buttonToScene.Add(button, sceneField);

            Debug.Log($"adding more buttons{button.name} {button.transform.parent.name}");
            var sep = Instantiate(Seperator);
            sep.transform.SetParent(scenarioButtons.transform);
        }
    }


    private void OnPreLoadScenarioButtonPressed(SceneField sceneField) {
        var spaceRef = FindObjectOfType<ExperimentSpaceReference>();
        var tmp = dict[sceneField.SceneName][ParticipantOrder.B];
        var pos = new Vector3(tmp[0], tmp[1], tmp[2]);

        pos = spaceRef.transform.TransformPoint(pos);
        pos.y = spaceRef.transform.position.y + 0.05f;
        PedIndicator.position = pos;
        PedIndicator.forward = Quaternion.Euler(0, tmp[3], 0) * spaceRef.transform.forward;
    }

    private void OnScenarioButtonPressed(SceneField sceneField) {
        ConnectionAndSpawning.Singleton.VisitedScenes[sceneField] = true;
        ConnectionAndSpawning.Singleton.SwitchToLoading(sceneField.SceneName);
    }

    private void UpdateScenarioButton(ActionState actionState) {
        if (actionState == ActionState.LOADINGSCENARIO)
            foreach (var button in _buttonToScene.Keys)
                if (ConnectionAndSpawning.Singleton.VisitedScenes[_buttonToScene[button]])
                    button.GetComponentInChildren<TextMeshProUGUI>().color = Color.red;
    }

    private void ToggleScenarioButtonVisibility(ActionState actionState) {
        if (actionState == ActionState.LOADINGSCENARIO)
            scenarioButtons.SetActive(false);
        else if (actionState == ActionState.WAITINGROOM) scenarioButtons.SetActive(true);
    }

    # endregion

    # region Calibration Buttons

    public void CreateButton(string text, Action<Action<bool>> onPress, ulong clientID,
        Action<Transform, bool> onFinished = null) {
        Debug.Log("Create Button Called");
        var success = ConnectionAndSpawning.Singleton.participants.GetOrder(clientID, out var po);
        if (!success) return;
        Debug.Log($"Creating button for {po}");
        var button = Instantiate(buttonTemplate_HasIcon, driverCalibrationButtons.transform, true);
        button.name = text + $" {po}";
        button.GetComponentInChildren<TextMeshProUGUI>().text = button.name;
        ConnectionAndSpawning.Singleton.participants.GetSpawnType(po, out var spawnType);
        button.transform.Find("Icon").GetComponent<Image>().sprite = _spawnTypeToSprite[spawnType];

        onFinished ??= (b, s) => {
            b.GetComponentInChildren<TextMeshProUGUI>().color =
                s ? new Color(0, 0.5f, 0, 1) : new Color(1, 0, 0, 1);
        };
        button.GetComponentInChildren<Button>().onClick.AddListener(() => {
            onPress.Invoke(s => onFinished(button.transform, s));
        });
        if (_spawnedButtons.TryGetValue(po, out var buttons))
            buttons.Add(button.transform);
        else
            _spawnedButtons.Add(po, new List<Transform> { button.transform });
    }

    public void DeleteButton(ulong clientID) {
        var success = ConnectionAndSpawning.Singleton.participants.GetOrder(clientID, out var po);
        if (!success) return;

        Debug.Log("Deleting Buttons");

        if (_spawnedButtons.ContainsKey(po)) {
            _spawnedButtons[po].ForEach(b => Destroy(b.gameObject));
            _spawnedButtons.Remove(po);
        }
        else {
            Debug.LogWarning($"Could not find calibration button for po:{po}");
        }
    }

    # endregion
}