using System.Collections;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class QNSelectionManager : MonoBehaviour {
    // Some code to send data to the logger
    public GameObject ButtonPrefab;

    Text QustionField;


    private bool Questionloaded = false;
    public bool running;
    bool onTarget;
    rayCastButton lastHitButton;
    public float totalTime;

    public float targetTime;
    selectionBarAnimation sba;

    string _condition;

    int listPointer;
    string[] ToDolist = new string[20]; /// <summary>
                                        ///  TODO HARD CODED limmit of 20 questions
                                        /// </summary>

    string outputString = "";

    List<RectTransform> childList = new List<RectTransform>();
    Dictionary<int,QandASet> questionList = new Dictionary<int,QandASet>();

    LayerMask _RaycastCollidableLayers;

    GraphicRaycaster m_Raycaster;
    PointerEventData m_PointerEventData;
    EventSystem m_EventSystem;

    public string[] QNFiles;
    Dictionary<string, List<QandASet>> questionaries = new Dictionary<string, List<QandASet>>();
    private int targetIndex = -1;

    public struct QandASet {
        public int id;
        public string question;
        public List<OneAnswer> Answers;
    }

    public struct OneAnswer {
        public string Answer;
        public int NextQuestionID;
    }

    // Use this for initialization
    void Start() {

        foreach (string s in QNFiles) {
            questionaries.Add(s, ReadString(s));

        }

        QustionField = transform.Find("QuestionField").GetComponent<Text>();


        sba = GetComponentInChildren<selectionBarAnimation>();
        totalTime = 0;
        running = false;
        _RaycastCollidableLayers = LayerMask.GetMask("UI");
        m_Raycaster = GetComponent<GraphicRaycaster>();
        m_EventSystem = GetComponent<EventSystem>();

        startAskingTheQuestionairs(QNFiles, "Test");
    }

    private void updateCursorPositoon(Transform currentHitTarget, RaycastResult rayRes) {
        Vector3 temp = Camera.main.transform.position
                      + Camera.main.ScreenPointToRay(new Vector3(Camera.main.pixelWidth / 2, Camera.main.pixelHeight / 2, 0), Camera.MonoOrStereoscopicEye.Mono).direction.normalized
                      * rayRes.distance;
        // Debug.Log("Hit the Canvas" + temp);
        // Debug.DrawLine(Camera.main.transform.position, temp);
        temp -= transform.position;
        sba.updatePosition(transform.worldToLocalMatrix * temp);
        /// WTF Unity??? this should not be that hard!!
    }
    // Update is called once per frame
    public void startAskingTheQuestionairs(string[] list, string Condition) {

        if (!running) {
            _condition = Condition;
            list.CopyTo(ToDolist, 0);
            listPointer = 0;
            running = true;
            targetIndex = 0;
            Questionloaded = false;
            outputString = "";
        }
    }

    void Update() {
        if (running) {
            if (!Questionloaded) {
                if (targetIndex == 0) {
                    if (listPointer >= ToDolist.Length || ToDolist[listPointer] == null) {
                        Debug.Log("We are done here continue to the next conditon and stop displaying the questioniar.");
                        Debug.Log(outputString); //TODO: DATALOGGER
                        SceneStateManager.Instance.SetWaiting();
                        running = false;
                        transform.gameObject.SetActive(false);
                        return;
                    }

                    if (questionaries.ContainsKey(ToDolist[listPointer])) {
                        questionList.Clear();
                        foreach (QandASet q in questionaries[ToDolist[listPointer]]) {
                            Debug.Log("Our new questions are: " + q.question);
                            questionList.Add(q.id, q);
                        }
                        listPointer++;

                    } else {
                        Debug.LogError("This is bad");
                    }
                    targetIndex = 1;
                }

                foreach (RectTransform r in childList) {
                    Destroy(r.gameObject);
                }
                childList.Clear();
                foreach (int k in questionList.Keys) { Debug.Log("All the keys\t" + k); }
                
                if (questionList.ContainsKey(targetIndex)) {
                    QandASet temp = questionList[targetIndex];
                   // Debug.Log("the ammount of first answer we retaained" + temp.Answers.Count);
                   
                    QustionField.text = temp.question;
                    //Debug.Log(temp.question);
                    //Debug.Log(temp.Answers.Count);
                    int i = 0;
                    foreach (OneAnswer a in temp.Answers) {
                        rayCastButton rcb = GameObject.Instantiate(ButtonPrefab, this.transform).transform.GetComponentInChildren<rayCastButton>();
                        rcb.initButton(a.Answer, a.NextQuestionID);

                        RectTransform rtrans = rcb.transform.parent.GetComponentInParent<RectTransform>();
                        childList.Add(rtrans);
                        Vector2 tempVector= new Vector2(rtrans.anchoredPosition.x, ( -i * ( 135 /( temp.Answers.Count+1) ) ) + 50);
                        rtrans.anchoredPosition = tempVector;
                        
                        i++;
                    }
                    Questionloaded = true;
                } else {
                    Debug.LogError("Could not find the question you pointed me towards");
                }
            }






            //Set up the new Pointer Event
            m_PointerEventData = new PointerEventData(m_EventSystem);
            //Set the Pointer Event Position to that of the mouse position
            m_PointerEventData.position = new Vector2(Camera.main.pixelWidth / 2, Camera.main.pixelHeight / 2);

            //Create a list of Raycast Results
            List<RaycastResult> results = new List<RaycastResult>();

            //Raycast using the Graphics Raycaster and mouse click position
            m_Raycaster.Raycast(m_PointerEventData, results);
            //EventSystem.current.RaycastAll(m_PointerEventData, results);
            //For every result returned, output the name of the GameObject on the Canvas hit by the Ray
            bool success = false;

            foreach (RaycastResult result in results) {
                //Debug.Log(result.gameObject.name);
                if (result.gameObject.transform == transform) {
                    updateCursorPositoon(transform, result);
                }
                rayCastButton rcb = result.gameObject.transform.GetComponent<rayCastButton>();
                if (rcb == null) {
                    //  Debug.Log("Found something but not the right thing" + result.gameObject.name);
                    continue;
                } else {

                    success = true;
                    if (lastHitButton == rcb) {

                        if (!onTarget) {
                            //Debug.Log("Accquired Target");
                            onTarget = true;
                        } else {
                            // Debug.Log("On Target");
                            totalTime += Time.deltaTime;
                        }
                    } else {
                        //Debug.Log("Lost Target");
                        lastHitButton = rcb;
                        onTarget = false;
                        totalTime = 0;
                    }

                    updateCursorPositoon(result.gameObject.transform, result);
                    break; // we go out of the for each loop, we got what we came for!
                }
            }
            if (!success) {
                lastHitButton = null;
                onTarget = false;
                if (totalTime > 0) {
                    totalTime -= Time.deltaTime * 2;
                } else {
                    totalTime = 0;
                }
            }
            if (success && onTarget && totalTime >= targetTime) {
                outputString += lastHitButton.activateNextQuestions(out targetIndex);
                Debug.Log("new target index");
                totalTime = 0;
                lastHitButton = null;
                Questionloaded = false;
            }
            if (totalTime >= 0 && totalTime <= targetTime) {
                sba.setPercentageSelection(Mathf.Clamp01(totalTime / targetTime));
            } else {
                //Debug.Log("This should not happen");
            }

        }
    }



   /* public class ReadOnlyAttribute : PropertyAttribute { // Copies from https://answers.unity.com/questions/489942/how-to-make-a-readonly-property-in-inspector.html

    }

    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyDrawer : PropertyDrawer {
        public override float GetPropertyHeight(SerializedProperty property,
                                                GUIContent label) {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect position,
                                   SerializedProperty property,
                                   GUIContent label) {
            GUI.enabled = false;
            EditorGUI.PropertyField(position, property, label, true);
            GUI.enabled = true;
        }
    }*/

    //// This is For the file reading and interpretation
    List<QandASet> ReadString(string path_) {
        string path = "Assets/QN/" + path_ + ".txt";
        Debug.Log("Trying to open" + path);
        List<QandASet> output = new List<QandASet>();
        //Read the text from directly from the test.txt file
        StreamReader reader = new StreamReader(path);
        bool first = true;
        QandASet lastSet = new QandASet();
        lastSet.Answers = new List<OneAnswer>();

        string line;
        while (( line = reader.ReadLine() ) != null) {

            if (line.StartsWith("/")) {
                //Debug.Log(line);
                if (!first) {
                    Debug.Log(lastSet.Answers.Count + "   out last ansawer count");
                    output.Add(lastSet);
                    lastSet = new QandASet();
                    lastSet.Answers = new List<OneAnswer>();
                }
                first = false;
                string[] elems = line.Split('\t');

                int id = int.Parse(elems[0].TrimStart('/'));
                lastSet.id = id;
                lastSet.question = elems[1];
            } else if (line.StartsWith("\t")) {
                OneAnswer temp = new OneAnswer();

                string[] elems = line.TrimStart('\t').Split('\t');

                //foreach (string s in elems) { Debug.Log("loggin whole array: " + s); }
                temp.Answer = elems[0];

                for (int i = 1; i < elems.Length; i++) {
                    int candidate = -1;
                    if (int.TryParse(elems[i].TrimStart('/'), out candidate)) {
                        temp.NextQuestionID = candidate;
                        break;
                    } else {
                        temp.NextQuestionID = -1;
                    }
                }
                lastSet.Answers.Add(temp);
            } else {
                // Debug.Log("Fond an empty line or so");
            }


        }
        output.Add(lastSet);
        Debug.Log(output.Count);

        reader.Close();
        return output;
    }
}
