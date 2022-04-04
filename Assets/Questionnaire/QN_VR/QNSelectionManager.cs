//#define debug

using System.IO;
using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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
        WAITINGFORQUESTION,
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

    private string m_LanguageSelect;


    NetworkedQuestionnaireQuestion currentActiveQustion;

   

#if debug
    public List<TextAsset> QNFiles;
#endif

    Transform ParentPosition;
    float up, forward;

    public void ChangeLanguage(string lang)
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
        sba = GetComponentInChildren<selectionBarAnimation>();


#if DEBUGQN
        startAskingTheQuestionairs(FindObjectOfType<LocalVRPlayer>().transform, QNFiles.ToArray(), "Test");
        changeLanguage("English");
        setRelativePosition(FindObjectOfType<LocalVRPlayer>().transform, 1, 2);
#endif
    }

 

    private Transform positioingRef;

    // Update is called once per frame
    public void startAskingTheQuestionairs(Transform mylocalclient,  string Condition,
        string lang)
    {
        if (m_interalState == QNStates.IDLE)
        {
          
            ChangeLanguage(lang);
            m_MyLocalClient = mylocalclient.GetComponent<ParticipantInputCapture>();

            m_condition = Condition;
         
            m_interalState = QNStates.WAITINGFORQUESTION;
            m_MyLocalClient.SendQNAnswerServerRPC(-1, 0,m_LanguageSelect);
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

        if (ParentPosition != null) {
            transform.rotation = ParentPosition.rotation;
            transform.position = ParentPosition.position + ParentPosition.rotation* new  Vector3(0, up, forward);
        }

        switch (m_interalState)
        {
            case QNStates.IDLE:
                //Nothing is happening just waiting for something to happen.
                break;

            case QNStates.WAITINGFORQUESTION:

                if(m_MyLocalClient.HasNewQuestion())
            {
                currentActiveQustion = m_MyLocalClient.GetNewQuestion();

                if (currentActiveQustion.reply == replyType.NEWQUESTION)
                {
                    m_interalState = QNStates.LOADINGQUESTION;
                }
                else if (currentActiveQustion.reply == replyType.FINISHED)
                {
                    m_interalState = QNStates.FINISH;
                }
            }
               
                break;
            case QNStates.LOADINGQUESTION:
               

                foreach (RectTransform r in AnswerFields)
                {
                    Destroy(r.gameObject);
                }

                AnswerFields.Clear();

                QustionField.text = SetText(currentActiveQustion.QuestionText);
                int i = 0;
                
                foreach (int a in currentActiveQustion.Answers.Keys)
                {
                    rayCastButton rcb = Instantiate(ButtonPrefab, this.transform).transform
                        .GetComponentInChildren<rayCastButton>();
                    rcb.initButton(SetText(currentActiveQustion.Answers[a]), a);
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
                LayerMask layerMask = 1 << 5;
                if (Physics.Raycast(ray, out hit, Mathf.Infinity, layerMask, QueryTriggerInteraction.UseGlobal))
                {
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


                    if (m_MyLocalClient.ButtonPush() && onTarget) {

                        int AnswerIndex = rcb.activateNextQuestions();


                        m_MyLocalClient.SendQNAnswer(currentActiveQustion.ID, AnswerIndex,m_LanguageSelect);
                        
                        //m_QNLogger.AddNewDataPoint(currentActiveQustion, AnswerIndex, m_LanguageSelect);
                        //  Debug.Log("To Question: " + currentActiveQustion.QuestionText["English"] +
                       // "We answered: " + currentActiveQustion.Answers[AnswerIndex].AnswerText["English"]);

                       // foreach (int nextQ in currentActiveQustion.Answers[AnswerIndex].nextQuestionIndexQueue)
                      //  {
                     //       nextQuestionsToAskQueue.Enqueue(nextQ);
                     //   }

                        m_interalState = QNStates.WAITINGFORQUESTION;
                    }

                }

                break;
            case QNStates.FINISH:

                if (m_MyLocalClient != null && m_MyLocalClient.IsLocalPlayer)
                {
                   /* m_QNLogger.DumpData(out string data);


                   
                    LongStringMessage message = new LongStringMessage();
                    message.message = data;
                    FastBufferWriter writer = new FastBufferWriter(message.GetSize(), Allocator.Temp);
                    Debug.Log("The message is" + message.GetSize() + " While the buffer has" + writer.Capacity);
                    writer.WriteNetworkSerializable(message);
                    NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(QNLogger.qnMessageName,
                       NetworkManager.Singleton.ServerClientId, writer, NetworkDelivery.Reliable);
                       
                        writer.Dispose();
*/
                    m_MyLocalClient.PostQuestionServerRPC(m_MyLocalClient.OwnerClientId);
                    m_interalState = QNStates.IDLE;
                   
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

    private string SetText(string text)
    {
        return m_LanguageSelect.Contains("Hebrew") ? StringExtension.Reverse(text) : text;
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
