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

   
   

    string outputString = "";

    List<RectTransform> childList = new List<RectTransform>();
    

    LayerMask _RaycastCollidableLayers;

    //GraphicRaycaster m_Raycaster;
    //PointerEventData m_PointerEventData;
    //EventSystem m_EventSystem;
    Queue<QandASet> ToDoQueue = new Queue<QandASet>();
    Queue<string> ToDolist = new Queue<string>();
    public string[] QNFiles;
    Dictionary<string, List<QandASet>> questionaries = new Dictionary<string, List<QandASet>>();
    Dictionary<int, QandASet> allCurrentQuestions = new Dictionary<int, QandASet>();
    //private int targetIndex = -1;
    //int listPointer;

    Transform ParentPosition;
    float up,forward;
    public struct QandASet {
        public int id;
        public string question;
        public List<OneAnswer> Answers;
    }

    public struct OneAnswer {
        public string Answer;
        public List<int> NextQuestionsIDs;
    }
    public void setRelativePosition(Transform t,  float up_,float forward_) {
        ParentPosition = t;
        up = up_;
        forward = forward_;
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
        //m_Raycaster = GetComponent<GraphicRaycaster>();
       // m_EventSystem = GetComponent<EventSystem>();
        string[] sarray = { "testfile" };
      startAskingTheQuestionairs(sarray, "Test"); //TODO
    }

    private void updateCursorPositoon(Transform currentHitTarget, RaycastResult rayRes) {
        
        Vector3 temp = Camera.main.transform.position
                      + Camera.main.ScreenPointToRay(new Vector3(Camera.main.pixelWidth / 2, Camera.main.pixelHeight / 2, 0), Camera.MonoOrStereoscopicEye.Mono).direction.normalized
                      * rayRes.distance;
        // Debug.Log("Hit the Canvas" + temp);
         Debug.DrawLine(Camera.main.transform.position, temp,Color.red);
        temp -= transform.position;
        sba.updatePosition(transform.worldToLocalMatrix * temp);
        /// WTF Unity??? this should not be that hard!!
    }
    // Update is called once per frame
    public void startAskingTheQuestionairs(string[] list, string Condition) {

        if (!running) {
            _condition = Condition;
            foreach (string s in list) {
                ToDolist.Enqueue(s);
                
                    }
            
            running = true;
            Questionloaded = false;
            outputString = "";
        }
    }

    void Update() {
        if (running) {
            transform.position = ParentPosition.position + ParentPosition.up*up+ParentPosition.forward*forward;
            if (!Questionloaded && ToDoQueue.Count<=0) {
                
                if (ToDolist.Count <= 0) {
                    Debug.Log("We are done here continue to the next conditon and stop displaying the questioniar.");
                    Debug.Log(outputString); //TODO: DATALOGGER
                    if (SceneStateManager.Instance != null) {
                        SceneStateManager.Instance.SetWaiting(); //TODO: DIsplay Wait Now Sign
                    }
                    running = false;
                    transform.gameObject.SetActive(false);
                    return;
                }
                string nextTodo = ToDolist.Dequeue();
                
                if (questionaries.ContainsKey(nextTodo)) {
                    allCurrentQuestions.Clear();
                    
                    foreach (QandASet q in questionaries[nextTodo]) {
                        Debug.Log("Our new questions are: " + q.question);
                        allCurrentQuestions.Add(q.id, q);
                    }
                    ToDoQueue.Enqueue(allCurrentQuestions[1]);// enque first question
                    Debug.Log(ToDoQueue.Count);
                } else {
                    Debug.LogError("Could not find the questionair in question => " + nextTodo);
                    return;
                }
            }

            if (!Questionloaded) {
                foreach (RectTransform r in childList) {
                    Destroy(r.gameObject);
                }
                childList.Clear();
                //foreach (int k in questionList.Keys) { Debug.Log("All the keys\t" + k); }


                QandASet temp = ToDoQueue.Dequeue();
                    // Debug.Log("the ammount of first answer we retaained" + temp.Answers.Count);

                    QustionField.text = temp.question;
                    Debug.Log(temp.question);
                    Debug.Log(temp.Answers.Count);
                    int i = 0;
                    foreach (OneAnswer a in temp.Answers) {
                        rayCastButton rcb = GameObject.Instantiate(ButtonPrefab, this.transform).transform.GetComponentInChildren<rayCastButton>();
                        rcb.initButton(a.Answer, a.NextQuestionsIDs);

                        RectTransform rtrans = rcb.transform.parent.GetComponentInParent<RectTransform>();
                        childList.Add(rtrans);
                        Vector2 tempVector = new Vector2(rtrans.anchoredPosition.x, ( -i * ( 165 / ( temp.Answers.Count  ) ) ) + 55);
                        rtrans.anchoredPosition = tempVector;
                        i++;
                    }
                    Questionloaded = true;
                }
            
            //Set up the new Pointer Event
          //  m_PointerEventData = new PointerEventData(m_EventSystem);
            //Set the Pointer Event Position to that of the mouse position
           // m_PointerEventData.position = new Vector2(Camera.main.pixelWidth / 2, Camera.main.pixelHeight / 2);

            //Create a list of Raycast Results
            List<RaycastResult> results = new List<RaycastResult>();

            //Raycast using the Graphics Raycaster and mouse click position
            //m_Raycaster.Raycast(m_PointerEventData, results);
            //EventSystem.current.RaycastAll(m_PointerEventData, results);
            //For every result returned, output the name of the GameObject on the Canvas hit by the Ray
           

            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(new Vector2(Camera.main.pixelWidth / 2f, Camera.main.pixelHeight / 2f));
            int layerMask = 1 << 5;
            if (Physics.Raycast(ray, out hit, layerMask)) {
                Debug.Log(hit.transform.name);
                Transform objectHit = hit.transform;
                Debug.DrawLine(Camera.main.transform.position, hit.point, Color.blue);
                bool success = false;
                if (hit.transform == transform) {
                    sba.updatePosition(transform.worldToLocalMatrix * ( hit.point - transform.position ));
                } else {
                    rayCastButton rcb = hit.transform.GetComponent<rayCastButton>();
                    if (rcb != null) {
                        success = true;
                        if (lastHitButton == rcb) {

                            if (!onTarget) {
                                //Debug.Log("Accquired Target");
                                onTarget = true;
                            } else {
                                // Debug.Log("On Target");
                                totalTime += Time.deltaTime * ( 1 / Time.timeScale );

                            }
                        } else {
                            lastHitButton = rcb;
                            onTarget = false;
                            totalTime = 0;

                        }

                        sba.updatePosition(transform.worldToLocalMatrix * ( hit.point - transform.position ));

                    }
                }
                if (!success) {
                    lastHitButton = null;
                    onTarget = false;
                    if (totalTime > 0) {
                        totalTime -= Time.deltaTime * 2 * ( 1 / Time.timeScale );
                    } else {
                        totalTime = 0;
                    }
                }
                if (success && onTarget && totalTime >= targetTime) {
                    List<int> temp;
                    outputString += lastHitButton.activateNextQuestions(out temp);
                    foreach (int i in temp) {
                        ToDoQueue.Enqueue(allCurrentQuestions[i]);
                    }
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
            /*
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
                            totalTime += Time.deltaTime*(1/Time.timeScale);
                        }
                    } else {
                        //Debug.Log("Lost Target");
                        lastHitButton = rcb;
                        onTarget = false;
                        totalTime = 0;
                    }

                    updateCursorPositoon(result.gameObject.transform, result);
                    break; // we get out of the for each loop, we got what we came for!
                }
            }
            if (!success) {
                lastHitButton = null;
                onTarget = false;
                if (totalTime > 0) {
                    totalTime -= Time.deltaTime * 2*(1/Time.timeScale);
                } else {
                    totalTime = 0;
                }
            }
            if (success && onTarget && totalTime >= targetTime) {
                List<int> temp;
                outputString += lastHitButton.activateNextQuestions(out temp);
                foreach (int i in temp) {
                    ToDoQueue.Enqueue(allCurrentQuestions[i]);
                }
                totalTime = 0;
                lastHitButton = null;
                Questionloaded = false;
            }
            if (totalTime >= 0 && totalTime <= targetTime) {
                sba.setPercentageSelection(Mathf.Clamp01(totalTime / targetTime));
            } else {
                //Debug.Log("This should not happen");
            }
            */
        }
    }


    //// This is For the file reading and interpretation
    List<QandASet> ReadString(string path_) {
        if (path_.Length <= 0) {
            return null;
        }
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

            if (line.StartsWith("/")) {// new Question
                //Debug.Log(line);
                if (!first) {
                    //Debug.Log(lastSet.Answers.Count + "   out last ansawer count");
                    output.Add(lastSet);
                    lastSet = new QandASet();
                    lastSet.Answers = new List<OneAnswer>();
                }
                first = false;
                string[] elems = line.Split('\t');
                //Debug.Log(elems[0]+" :from line: "+line);
                int id = 0;
                int.TryParse(elems[0].TrimStart('/'), out id);
                lastSet.id = id;
                lastSet.question = elems[1];
            } else if (line.StartsWith("\t")) {// New Answer    
                OneAnswer temp = new OneAnswer();

                string[] elems = line.TrimStart('\t').Split('\t');
                temp.Answer = elems[0];
                string[] PotentialFolllowIDs = elems[elems.Length - 1].Split(',');
                temp.NextQuestionsIDs = new List<int>();
                foreach(string s in PotentialFolllowIDs){
                    if (s.StartsWith("/")){
                        int candidate;
                        if (int.TryParse(s.TrimStart('/'), out candidate)) {
                            temp.NextQuestionsIDs.Add(candidate);
                        }
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
