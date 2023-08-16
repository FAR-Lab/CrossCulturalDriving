//#define debug

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class QNSelectionManager : MonoBehaviour {
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

    private VR_Participant m_MyLocalClient;
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

    private Transform ParentPosition;
    [FormerlySerializedAs("up")] public float xOffset;
    [FormerlySerializedAs("forward")] public float yOffset;
    [FormerlySerializedAs("left")] public float zOffset;


    public void ChangeLanguage(string lang) {
        m_LanguageSelect = lang;
    }

    public void setRelativePosition(Transform t, float up_, float forward_, float left_) {
        ParentPosition = t;
        xOffset = up_;
        yOffset = forward_;
        zOffset = left_;
    }

    private void Start() {
        /*
        selectAction = new InputAction("Select");
        selectAction.AddBinding("<Keyboard>/space");
        selectAction.AddBinding("<Joystick>/trigger");
        selectAction.AddBinding("<Joystick>/button2");
        selectAction.AddBinding("<Joystick>/button3");
        selectAction.AddBinding("<Joystick>/button4");
        selectAction.AddBinding("<Joystick>/button5");
        selectAction.AddBinding("<Joystick>/button6");
        selectAction.AddBinding("<Joystick>/button7");
        selectAction.AddBinding("<Joystick>/button8");
        selectAction.AddBinding("<Joystick>/button9");
        selectAction.AddBinding("<Joystick>/button10");
        selectAction.AddBinding("<Joystick>/button11");
        selectAction.AddBinding("<Joystick>/button12");
        selectAction.AddBinding("<Joystick>/button20");
        selectAction.AddBinding("<Joystick>/button21");
        selectAction.AddBinding("<Joystick>/button22");
        selectAction.AddBinding("<Joystick>/button23");
        selectAction.AddBinding("<Joystick>/button24");
        selectAction.AddBinding("<Joystick>/hat/down");
        selectAction.AddBinding("<Joystick>/hat/up");
        selectAction.AddBinding("<Joystick>/hat/left");
        selectAction.AddBinding("<Joystick>/hat/right");
        selectAction.Enable();
        */
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

    // Update is called once per frame
    public void startAskingTheQuestionairs(Transform mylocalclient, string Condition,
        string lang) {
        if (m_interalState == QNStates.IDLE) {
            ChangeLanguage(lang);
            m_MyLocalClient = mylocalclient.GetComponent<VR_Participant>();

            m_condition = Condition;

            m_interalState = QNStates.WAITINGFORQUESTION;
            m_MyLocalClient.SendQNAnswerServerRPC(-1, 0, m_LanguageSelect);
        }
        else {
            Debug.LogError("I should really only once start the Questionnaire.");
        }
    }

    private void Update() {
        if (Input.GetKeyUp(KeyCode.Q)) m_interalState = QNStates.FINISH;

        if (ParentPosition != null) {
            transform.rotation = ParentPosition.rotation;
            transform.position = ParentPosition.position +
                                 ParentPosition.rotation * new Vector3(xOffset, yOffset, zOffset);
        }

        switch (m_interalState) {
            case QNStates.IDLE:
                //Nothing is happening just waiting for something to happen.
                break;

            case QNStates.WAITINGFORQUESTION:

                if (m_MyLocalClient.HasNewQuestion()) {
                    currentActiveQustion = m_MyLocalClient.GetNewQuestion();

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
                                m_MyLocalClient.SendQNAnswer(currentActiveQustion.ID, AnswerIndex, m_LanguageSelect);
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
                                    m_MyLocalClient.SendQNAnswer(-1, -1, m_LanguageSelect);
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