//#define debug

using System.IO;
using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using Newtonsoft.Json;
using Unity.Collections;
using Unity.Netcode;

public class QNSelectionManager : MonoBehaviour
{
    public GameObject ButtonPrefab;

    public enum QNStates
    {
        IDLE,
        LOADINGSET,
        LOADINGQUESTION,
        RESPONSEWAIT,
        FINISH
    };

    public QNStates m_interalState = QNStates.IDLE;
    private string m_condition;
    private InputAction selectAction;


    Text QustionField;
    selectionBarAnimation sba;
    List<RectTransform> AnswerFields = new List<RectTransform>();

    private ParticipantInputCapture m_MyLocalClient;
    LayerMask m_RaycastCollidableLayers;

    private LanguageSelect m_LanguageSelect;

    public Dictionary<string, List<QuestionnaireQuestion>> QuestionariesToAsk =
        new Dictionary<string, List<QuestionnaireQuestion>>();

    private Dictionary<int, QuestionnaireQuestion> CurrentSetofQuestions;
    private Queue<int> nextQuestionsToAskQueue;
    QuestionnaireQuestion currentActiveQustion;

    private QNLogger m_QNLogger;

#if debug
    public List<TextAsset> QNFiles;
#endif

    Transform ParentPosition;
    float up, forward;

    public void ChangeLanguage(LanguageSelect lang)
    {
        m_LanguageSelect = lang;
    }

    public void setRelativePosition(Transform t, float up_, float forward_)
    {
        ParentPosition = t;
        up = up_;
        forward = forward_;
    }

    void Start()
    {
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
        QustionField = transform.Find("QuestionField").GetComponent<Text>();
        CurrentSetofQuestions = new Dictionary<int, QuestionnaireQuestion>();
        nextQuestionsToAskQueue = new Queue<int>();
        sba = GetComponentInChildren<selectionBarAnimation>();


#if debug
        startAskingTheQuestionairs(FindObjectOfType<LocalVRPlayer>().transform, QNFiles.ToArray(), "Test");
        changeLanguage("English");
        setRelativePosition(FindObjectOfType<LocalVRPlayer>().transform, 1, 2);
#endif
    }

    private void updateCursorPositoon(Transform currentHitTarget, RaycastResult rayRes)
    {
        Vector3 temp = Camera.main.transform.position
                       + Camera.main
                           .ScreenPointToRay(new Vector3(Camera.main.pixelWidth / 2, Camera.main.pixelHeight / 2, 0),
                               Camera.MonoOrStereoscopicEye.Mono).direction.normalized
                       * rayRes.distance;

        Debug.DrawLine(Camera.main.transform.position, temp, Color.red);
        temp -= transform.position;
        sba.updatePosition(transform.worldToLocalMatrix * temp);
    }

    // Update is called once per frame
    public void startAskingTheQuestionairs(Transform mylocalclient, TextAsset[] list, string Condition,
        LanguageSelect lang)
    {
        if (m_interalState == QNStates.IDLE)
        {
            m_QNLogger = new QNLogger();
            m_QNLogger.Init();
            ChangeLanguage(lang);
            m_MyLocalClient = mylocalclient.GetComponent<ParticipantInputCapture>();
            transform.parent = m_MyLocalClient.GetMyCar();
            m_condition = Condition;
            foreach (TextAsset s in list)
            {
                // Debug.Log(s);
                QuestionariesToAsk.Add(s.name, ReadString(s));
            }

            m_interalState = QNStates.LOADINGSET;
        }
        else
        {
            Debug.LogError("I should really only once start the Questionnaire.");
        }
    }

    void Update()
    {
        if (Input.GetKeyUp(KeyCode.Q))
        {
            m_interalState = QNStates.FINISH;
        }


        switch (m_interalState)
        {
            case QNStates.IDLE:
                //Nothing is happening just waiting for something to happen.
                break;
            case QNStates.LOADINGSET:
                if (QuestionariesToAsk.Count <= 0)
                {
                    m_interalState = QNStates.FINISH;
                    break;
                }

                string nextKey = QuestionariesToAsk.Keys.First(); //Not sure that this will work

                CurrentSetofQuestions.Clear();
                foreach (QuestionnaireQuestion q in QuestionariesToAsk[nextKey])
                {
                    if (!CurrentSetofQuestions.ContainsKey(q.ID))
                    {
                        CurrentSetofQuestions.Add(q.ID, q);
                    }
                    else
                    {
                        Debug.LogError("This should not happen. We have duplicate question IDs. Please check: " +
                                       nextKey);
                    }
                }

                QuestionariesToAsk.Remove(nextKey);

                currentActiveQustion = null;
                nextQuestionsToAskQueue.Clear();
                nextQuestionsToAskQueue.Enqueue(1);
                m_interalState = QNStates.LOADINGQUESTION;
                break;
            case QNStates.LOADINGQUESTION:


                if (nextQuestionsToAskQueue.Count <= 0)
                {
                    m_interalState = QNStates.LOADINGSET;
                    break;
                }

                int NextQuestiuonID = nextQuestionsToAskQueue.Dequeue();

                foreach (RectTransform r in AnswerFields)
                {
                    Destroy(r.gameObject);
                }

                AnswerFields.Clear();
                currentActiveQustion = CurrentSetofQuestions[NextQuestiuonID];
                Debug.Log("Get lanauge: >" + m_LanguageSelect + "<But only have the following avalible:");
                foreach (string s in currentActiveQustion.QuestionText.Keys)
                {
                    Debug.Log(">" + s + "<");
                }

                QustionField.text = currentActiveQustion.QuestionText[m_LanguageSelect];
                int i = 0;
                foreach (ObjAnswer a in currentActiveQustion.Answers)
                {
                    rayCastButton rcb = Instantiate(ButtonPrefab, this.transform).transform
                        .GetComponentInChildren<rayCastButton>();
                    rcb.initButton(a.AnswerText[m_LanguageSelect], currentActiveQustion.Answers.IndexOf(a));
                    RectTransform rtrans = rcb.transform.parent.GetComponentInParent<RectTransform>();
                    AnswerFields.Add(rtrans);
                    Vector2 tempVector = new Vector2(rtrans.anchoredPosition.x,
                        (-i * (165 / (currentActiveQustion.Answers.Count))) + 55);
                    rtrans.anchoredPosition = tempVector;
                    i++;
                }

                m_interalState = QNStates.RESPONSEWAIT;
                break;


            case QNStates.RESPONSEWAIT:
                List<RaycastResult> results = new List<RaycastResult>();
                RaycastHit hit;
                if (Camera.main == null)
                {
                    Debug.Log("This is interesting unloading");
                    return;
                }

                Ray ray = Camera.main.ScreenPointToRay(new Vector2(Camera.main.pixelWidth / 2f,
                    Camera.main.pixelHeight / 2f));
                const int layerMask = 1 << 5;
                if (Physics.Raycast(ray, out hit,35f,layerMask)) {
                    rayCastButton rcb = null;
                    Transform objectHit = hit.transform;
                    bool onTarget = false;

                    if (hit.transform == transform)
                    {
                        sba.updatePosition(transform.worldToLocalMatrix * (hit.point - transform.position));

                    }
                    else
                    {
                        rcb = hit.transform.GetComponent<rayCastButton>();
                        if (rcb != null)
                        {
                            onTarget = true;
                            sba.updatePosition(transform.worldToLocalMatrix * (hit.point - transform.position));
                        }

                    }

                    Debug.Log(layerMask.ToString()+"  "+hit.transform.name);

                    if (m_MyLocalClient.ButtonPush() && onTarget)
                    {
                        int AnswerIndex = rcb.activateNextQuestions();


                        m_QNLogger.AddNewDataPoint(currentActiveQustion, AnswerIndex, m_LanguageSelect);
                        //  Debug.Log("To Quesstion: " + currentActiveQustion.QuestionText["English"] +
                        //"We answered: " + currentActiveQustion.Answers[AnswerIndex].AnswerText["English"]);

                        foreach (int nextQ in currentActiveQustion.Answers[AnswerIndex].nextQuestionIndexQueue)
                        {
                            nextQuestionsToAskQueue.Enqueue(nextQ);
                        }

                        m_interalState = QNStates.LOADINGQUESTION;
                    }
                    //  sba.setPercentageSelection(Mathf.Clamp01(totalTime / targetTime));
                }

                break;
            case QNStates.FINISH:

                if (m_MyLocalClient != null && m_MyLocalClient.IsLocalPlayer)
                {
                    m_QNLogger.DumpData(out string data);

                    byte[] tmp = Encoding.Unicode.GetBytes(data);

                    Debug.Log(tmp.Length + "charcount");
                    FastBufferWriter writer = new FastBufferWriter(tmp.Length, Allocator.Temp);
                    writer.WriteBytesSafe(tmp);
                    NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(QNLogger.qnMessageName,
                        NetworkManager.Singleton.ServerClientId, writer, NetworkDelivery.Reliable);

                    m_MyLocalClient.PostQuestionServerRPC(m_MyLocalClient.OwnerClientId);
                    m_interalState = QNStates.IDLE;
                    writer.Dispose();
                }
                else
                {
                    Debug.LogError("Did not get my local player dont know who to report back to.");
                }

                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    List<QuestionnaireQuestion> ReadString(TextAsset asset)
    {
        Debug.Log("Trying to load text asset:" + asset.name);
        return JsonConvert.DeserializeObject<List<QuestionnaireQuestion>>(asset.text);
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
