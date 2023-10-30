//#define debug

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class QN_Display : NetworkBehaviour {


    public enum FollowType {
        NetworkObject,
        MainCamera,
        Interactable
    }

    public ParticipantOrder m_participantOrder;


 
    public GameObject ButtonPrefab;

    public enum QNStates {
        IDLE,
        WAITINGFORQUESTION,
        LOADINGQUESTION,
        RESPONSEWAIT,
        FINISH
    }

    public QNStates m_interalState = QNStates.IDLE;

    private string m_condition;
    // private InputAction selectAction;


    private Text QustionField;
    private selectionBarAnimation sba;
    private readonly List<RectTransform> AnswerFields = new();

    private LayerMask m_RaycastCollidableLayers;

    private string m_LanguageSelect;

    private RectTransform BackButton;
    private Text CountDisplay;
    private NetworkedQuestionnaireQuestion currentActiveQustion;

    private int _answerCount;
    private int _totalCount;

#if debug
    public List<TextAsset> QNFiles;
#endif

    public  Transform ParentPosition;
    [FormerlySerializedAs("up")] public float xOffset;
    [FormerlySerializedAs("forward")] public float yOffset;
    [FormerlySerializedAs("left")] public float zOffset;
    public bool KeepUpdating;

    public void ChangeLanguage(string lang) {
        m_LanguageSelect = lang;
    }

    public void setRelativePosition(Transform t, float up_, float forward_, float left_,bool KeepUpdating_) {
     
        ParentPosition = t;
        xOffset = up_;
        yOffset = forward_;
        zOffset = left_;
        KeepUpdating = KeepUpdating_;
        UpdatePosition();
    }

    private void Start() {
       
        QustionField = transform.Find("QuestionField").GetComponent<Text>();
        sba = GetComponentInChildren<selectionBarAnimation>();
        BackButton = transform.Find("BackButton").GetComponent<RectTransform>();
        CountDisplay = transform.Find("CountDisplay").GetComponent<Text>();

#if DEBUGQN
        startAskingTheQuestionairs(FindObjectOfType<LocalVRPlayer>().transform, QNFiles.ToArray(), "Test");
        changeLanguage("English");
        setRelativePosition(FindObjectOfType<LocalVRPlayer>().transform, 1, 2);
#endif
        
    }

    public GameObject ScenarioImageHolder;
    private Transform newImageHolder;

    public void AddImage(Texture2D CaptureScenarioImage) {
        if (ScenarioImageHolder == null) {
            Debug.LogError("This is not good. Could not show a picture even-though I was supposed to.!");
            return;
        }

        if (CaptureScenarioImage == null || CaptureScenarioImage.height <= 1 || CaptureScenarioImage.width <= 0) {
            Debug.LogWarning("Was supposed to show a picture but did not get anything usable");
            return;
        }

        if (newImageHolder == null) newImageHolder = Instantiate(ScenarioImageHolder, transform).transform;

        newImageHolder.gameObject.SetActive(true);
        Debug.Log("Set an image for the image screen!");
        CaptureScenarioImage.Apply();
        const float width = 300;
        var factor = width / (CaptureScenarioImage.width - 10);
        if (newImageHolder.GetChild(0).GetComponent<RawImage>() != null) {
            newImageHolder.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
                factor * CaptureScenarioImage.height);
            newImageHolder.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
                factor * CaptureScenarioImage.width);
            newImageHolder.GetChild(0).GetComponent<RawImage>().texture = CaptureScenarioImage;
            newImageHolder.transform.localPosition = new Vector3(0, factor * CaptureScenarioImage.height + 80, 0f);
        }
        else {
            Debug.Log("Was trying to show a screenshot but well didnt know were really?");
        }
    }


    private Transform positioingRef;

    private VR_Participant m_MyLocalClient;
    // Update is called once per frame
    public void startAskingTheQuestionairs(ParticipantOrder po) {
        if (m_interalState == QNStates.IDLE) {
            
            Debug.Log("Starting To Ask Questions");
            ChangeLanguage(ConnectionAndSpawning.Singleton.lang);
            m_condition = ConnectionAndSpawning.Singleton.GetScenarioManager().GetComponent<ScenarioManager>().conditionName;

            
            m_interalState = QNStates.WAITINGFORQUESTION;
            m_participantOrder = po;
            m_MyLocalClient=VR_Participant.GetJoinTypeObject();//ToDO Need to test!!
            Debug.Log("REquesting new questions from the server!");
            SendQNAnswerServerRPC(-1, 0, m_LanguageSelect);

            gameObject.SetActive(true);
        }
        else {
            Debug.LogError("I should really only once start the Questionnaire.");
        }
    }

    private void UpdatePosition() {
        if (ParentPosition == null) return;
        transform.rotation = ParentPosition.rotation;
        transform.position = ParentPosition.position +
                             ParentPosition.rotation * new Vector3(xOffset, yOffset, zOffset);
    }
    private void Update() {
        if (IsServer) {
            if (Input.GetKeyUp(KeyCode.Q)&& !Input.GetKey(KeyCode.LeftShift)) m_interalState = QNStates.FINISH;
        }

        if (IsServer && !IsHost) return;
        
        if (KeepUpdating) {
            UpdatePosition();
        }

        switch (m_interalState) {
            case QNStates.IDLE:
                //Nothing is happening just waiting for something to happen.
                break;

            case QNStates.WAITINGFORQUESTION:

                if (HasNewQuestion()) {
                    currentActiveQustion = GetNewQuestion();

                    if (currentActiveQustion.reply == replyType.NEWQUESTION)
                        m_interalState = QNStates.LOADINGQUESTION;
                    else if (currentActiveQustion.reply == replyType.FINISHED) m_interalState = QNStates.FINISH;
                }

                break;
            case QNStates.LOADINGQUESTION:

                updateCountDisaply();
                foreach (var r in AnswerFields) Destroy(r.gameObject);

                AnswerFields.Clear();

                QustionField.text = SetText(currentActiveQustion.QuestionText);

                if (currentActiveQustion.QnImagePath.Length > 0) {
                    try {
                        AddImage(Resources.Load<Texture2D>(currentActiveQustion.QnImagePath));
                    }
                    catch (Exception e) {
                        Debug.LogWarning(
                            "I tried to add an image but did not find the image in the referenced path (or something similar). The path was: " +
                            currentActiveQustion.QnImagePath + e);
                        if (newImageHolder != null) newImageHolder.gameObject.SetActive(false);
                    }
                }
                else {
                    if (newImageHolder != null) newImageHolder.gameObject.SetActive(false);
                }

                var i = 0;

                foreach (var a in currentActiveQustion.Answers.Keys) {
                    var rcb = Instantiate(ButtonPrefab, transform).transform
                        .GetComponentInChildren<rayCastButton>();
                    rcb.initButton(SetText(currentActiveQustion.Answers[a]), a);
                    var rtrans = rcb.transform.parent.GetComponentInParent<RectTransform>();
                    AnswerFields.Add(rtrans);
                    var tempVector = new Vector2(rtrans.anchoredPosition.x,
                        -i * (165 / currentActiveQustion.Answers.Count) + 55);
                    rtrans.anchoredPosition = tempVector;
                    i++;
                }

                m_interalState = QNStates.RESPONSEWAIT;
                break;


            case QNStates.RESPONSEWAIT:
                var results = new List<RaycastResult>();
                RaycastHit hit;
                if (Camera.main == null) {
                    Debug.Log("This is interesting unloading");
                    return;
                }

                var ray = Camera.main.ScreenPointToRay(new Vector2(Camera.main.pixelWidth / 2f,
                    Camera.main.pixelHeight / 2f));
                LayerMask layerMask = 1 << 5;
                if (Physics.Raycast(ray, out hit, Mathf.Infinity, layerMask, QueryTriggerInteraction.UseGlobal)) {
                    rayCastButton rcb = null;
                    var objectHit = hit.transform;
                    var onTarget = false;

                    if (hit.transform == transform) {
                        sba.updatePosition(transform.worldToLocalMatrix * (hit.point - transform.position));
                    }
                    else {
                        rcb = hit.transform.GetComponent<rayCastButton>();
                        if (rcb != null) {
                            onTarget = true;
                            sba.updatePosition(transform.worldToLocalMatrix * (hit.point - transform.position));
                            if (m_MyLocalClient.ButtonPush()) {
                                var AnswerIndex = rcb.activateNextQuestions();
                                SendQNAnswer(currentActiveQustion.ID, AnswerIndex, m_LanguageSelect);
                                _answerCount++;
                                m_interalState = QNStates.WAITINGFORQUESTION;
                            }
                        }
                        else {
                            if (hit.transform.GetComponent<RectTransform>() == BackButton) {
                                sba.updatePosition(transform.worldToLocalMatrix * (hit.point - transform.position));
                                if (m_MyLocalClient.ButtonPush()) {
                                    _answerCount--;
                                    if (_answerCount < 0) _answerCount = 0;
                                    SendQNAnswer(-1, -1, m_LanguageSelect);
                                    m_interalState = QNStates.WAITINGFORQUESTION;
                                }
                            }
                        }
                    }
                }

                break;
            case QNStates.FINISH:

                if (m_MyLocalClient != null && m_MyLocalClient.IsLocalPlayer) {
                    m_MyLocalClient.GoForPostQuestion();


                    foreach (var r in AnswerFields) Destroy(r.gameObject);

                    switch (m_LanguageSelect) {
                        case "Hebrew":
                            QustionField.text = StringExtension.RTLText("המתן בבקשה");
                            break;

                        case "English":
                        default:
                            QustionField.text = "Please Wait!";
                            break;
                    }

                    m_interalState = QNStates.IDLE;
                }
                else {
                    Debug.LogError("Did not get my local player dont know who to report back to.");
                }

                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private string SetText(string text) {
        return m_LanguageSelect.Contains("Hebrew") ? StringExtension.RTLText(text) : text;
    }

    public void SetTotalQNCount(int outval) {
        _totalCount = outval;
    }

    private void updateCountDisaply() {
        CountDisplay.text = 1 + _answerCount + " / " + _totalCount;
    }
    
    #region NetworkConnectivity;
    
    
    private QNDataStorageServer m_QNDataStorageServer = null;

    public delegate bool QuestionSelected();
        
    public void StartQuestionair(QNDataStorageServer in_QNDataStorageServer,
        ParticipantOrder po, 
        FollowType followType,
        Vector3 Offset,
        bool KeepUpdating,
        string referenceTransformPath){
        if (IsServer)
        {
            m_QNDataStorageServer = in_QNDataStorageServer;
            Debug.Log($"Questionnaire Activation for Participant{po} (serverside)");
            m_participantOrder = po;
            StartQuestionairClientRPC(po,followType,Offset,KeepUpdating,referenceTransformPath);
        }
    }

    [ClientRpc]
    public void StartQuestionairClientRPC(
        ParticipantOrder po,
        FollowType followType,
        Vector3 Offset,
        bool KeepUpdating_,
        string referenceTransformPath)
    {
        if (ConnectionAndSpawning.Singleton.ParticipantOrder==po) {
            Transform followObj = null;
            
          
            switch (followType) {
                case FollowType.NetworkObject:
                    followObj = null;
                    Debug.LogWarning("This is not implemented yet.");
                    break;
                case FollowType.MainCamera:
                    followObj = FindObjectsOfType<Client_Object>().Where(x => x.IsLocalPlayer).First().GetMainCamera();
                    break;
                case FollowType.Interactable:
                    followObj = FindObjectsOfType<Client_Object>().Where(x => x.IsLocalPlayer).First().GetFollowTransform().transform;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(followType), followType, null);
            }

            transform.localScale *= 0.1f;
            setRelativePosition(followObj.transform, Offset.x, Offset.y, Offset.z,KeepUpdating);
            KeepUpdating = KeepUpdating_;
            Debug.Log($"Questionnaire Activation for Participant{po} (ClientSide)");
            startAskingTheQuestionairs(po);
            
        }
        else {
            gameObject.SetActive(false);
            //TODO: we could start deleting objects here!
        }
    }


    public void SendQNAnswer(int id, int answerIndex, string lang)
    {
        if (!IsLocalPlayer) return;
        HasNewData = false;
        SendQNAnswerServerRPC(id, answerIndex, lang);
    }

    public bool HasNewData;


    [ServerRpc]
    public void SendQNAnswerServerRPC(int id, int answerIndex, string lang)
    {
        if (IsServer && m_QNDataStorageServer != null)
        {
            Debug.Log($"Asking for new Data with id:{id}, answerIndex:{answerIndex} and lang:{lang} for participant{m_participantOrder} ");
            
            m_QNDataStorageServer.NewDatapointfromClient(m_participantOrder, id, answerIndex, lang);
        }
    }

    [ClientRpc]
    public void SendNewQuestionClientRPC(NetworkedQuestionnaireQuestion newq)
    {
        if (!IsOwner) return;
        Debug.Log("Got new data and updated varaible" + HasNewData);
        if (HasNewData)
            Debug.LogWarning("I still had a question ready to go. Losing data here. this should not happen.");
        else
            HasNewData = true;

         lastQuestion = newq;
    }

    NetworkedQuestionnaireQuestion lastQuestion;
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

        Debug.LogError("I did have a new question yet I was asked for one quitting the QN early!");
        return new NetworkedQuestionnaireQuestion { reply = replyType.FINISHED };
    }

    [ClientRpc]
    public void SetTotalQNCountClientRpc(int outval)
    {
        if (!IsLocalPlayer) return;

        FindObjectOfType<QN_Display>()?.SetTotalQNCount(outval);
    }


    #endregion
    
}


/*
        QuestionnaireQuestion temp = new QuestionnaireQuestion
        {
            ID = 1,
            Behavior = "TurnSignals",
            SA_Level = "Prediction",

        };

        temp.QuestionText = new Dictionary<LanguageSelect, string>();
        temp.QuestionText.Add("English", "Test question?");
        temp.QuestionText.Add("German", "Test Frage?");

        temp.Answers = new List<ObjAnswer>();
        temp.Answers.Add(new ObjAnswer {index = 1});
        temp.Answers[0].AnswerText = new Dictionary<LanguageSelect, string>();
        temp.Answers[0].AnswerText.Add("English", "Test Answer 1");
        temp.Answers[0].AnswerText.Add("German", "Test Antwort 1");
        temp.Answers[0].nextQuestionIndexQueue = new List<int>{2};

        temp.Answers.Add(new ObjAnswer {index = 2});
        temp.Answers[1].AnswerText = new Dictionary<LanguageSelect, string>();
        temp.Answers[1].AnswerText.Add("English", "Test Answer 2");
        temp.Answers[1].AnswerText.Add("German", "Test Antwort 2");
        temp.Answers[1].nextQuestionIndexQueue = new List<int>{2};

        QuestionnaireQuestion temp2 = new QuestionnaireQuestion
        {
            ID = 2,
            Behavior = "TurnSignals",
            SA_Level = "Prediction",

        };

        temp2.QuestionText = new Dictionary<LanguageSelect, string>();
        temp2.QuestionText.Add("English", "Second test question?");
        temp2.QuestionText.Add("German", "Zweite Test Frage?");

        temp2.Answers = new List<ObjAnswer>();
        temp2.Answers.Add(new ObjAnswer {index = 1});
        temp2.Answers[0].AnswerText = new Dictionary<LanguageSelect, string>();
        temp2.Answers[0].AnswerText.Add("English", "Test Answer 3");
        temp2.Answers[0].AnswerText.Add("German", "Test Antwort 3");
        temp2.Answers[0].nextQuestionIndexQueue = new List<int>{};

        temp2.Answers.Add(new ObjAnswer {index = 2});
        temp2.Answers[1].AnswerText = new Dictionary<LanguageSelect, string>();
        temp2.Answers[1].AnswerText.Add("English", "Test Answer 4");
        temp2.Answers[1].AnswerText.Add("German", "Test Antwort 4");
        temp2.Answers[1].nextQuestionIndexQueue = new List<int>{};

         Debug.Log("SerializeObject");
       Debug.Log( JsonConvert.SerializeObject(new List<QuestionnaireQuestion>{temp,temp2}));
        */