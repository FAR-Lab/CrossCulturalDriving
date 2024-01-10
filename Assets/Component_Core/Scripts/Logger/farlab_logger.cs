using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Rerun;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Hands;
using Object = UnityEngine.Object;


// ToDo: Remove Frequent nullPointer references
// ToDo: Find a more performaned(memory efficinet) way to store the data.(preserve the 4byte Float accuracy)
// ToDo: One such solution would be to dump frames to JSON.

public class farlab_logger : MonoBehaviour {
    public const char sep = ';'; //Separator for data values.
    public const char supSep = '_'; //Separator for values within one cell.
    public const string Fpres = "F6";

    public float UpdatedFreqeuncy = 1f / 25f;


    public static HumanBodyBones[] BonesToTrack = {
        HumanBodyBones.Hips,
        HumanBodyBones.LeftUpperLeg,
        HumanBodyBones.RightUpperLeg,
        HumanBodyBones.LeftLowerLeg,
        HumanBodyBones.RightLowerLeg,
        HumanBodyBones.LeftFoot,
        HumanBodyBones.RightFoot,
        HumanBodyBones.Spine,
        HumanBodyBones.Chest,
        HumanBodyBones.Neck,
        HumanBodyBones.Head,
        HumanBodyBones.LeftShoulder,
        HumanBodyBones.RightShoulder,
        HumanBodyBones.LeftUpperArm,
        HumanBodyBones.RightUpperArm,
        HumanBodyBones.LeftLowerArm,
        HumanBodyBones.RightLowerArm,
        HumanBodyBones.LeftHand,
        HumanBodyBones.RightHand,
        HumanBodyBones.LeftToes,
        HumanBodyBones.RightToes,
        HumanBodyBones.LeftEye,
        HumanBodyBones.RightEye,
        HumanBodyBones.Jaw,
        HumanBodyBones.LeftThumbProximal,
        HumanBodyBones.LeftThumbIntermediate,
        HumanBodyBones.LeftThumbDistal,
        HumanBodyBones.LeftIndexProximal,
        HumanBodyBones.LeftIndexIntermediate,
        HumanBodyBones.LeftIndexDistal,
        HumanBodyBones.LeftMiddleProximal,
        HumanBodyBones.LeftMiddleIntermediate,
        HumanBodyBones.LeftMiddleDistal,
        HumanBodyBones.LeftRingProximal,
        HumanBodyBones.LeftRingIntermediate,
        HumanBodyBones.LeftRingDistal,
        HumanBodyBones.LeftLittleProximal,
        HumanBodyBones.LeftLittleIntermediate,
        HumanBodyBones.LeftLittleDistal,
        HumanBodyBones.RightThumbProximal,
        HumanBodyBones.RightThumbIntermediate,
        HumanBodyBones.RightThumbDistal,
        HumanBodyBones.RightIndexProximal,
        HumanBodyBones.RightIndexIntermediate,
        HumanBodyBones.RightIndexDistal,
        HumanBodyBones.RightMiddleProximal,
        HumanBodyBones.RightMiddleIntermediate,
        HumanBodyBones.RightMiddleDistal,
        HumanBodyBones.RightRingProximal,
        HumanBodyBones.RightRingIntermediate,
        HumanBodyBones.RightRingDistal,
        HumanBodyBones.RightLittleProximal,
        HumanBodyBones.RightLittleIntermediate,
        HumanBodyBones.RightLittleDistal,
        HumanBodyBones.UpperChest
        //  , HumanBodyBones.LastBone
    };

    //   private Transform CarA;


    /*
     private Rigidbody carARigidbody;
     private Transform CarB;
     private Rigidbody carBRigidbody;
     private Transform PlayerHeadA;
     private Transform PlayerHeadB;

     private Transform PlayerVRCenterA;
     private Transform PlayerVRCenterB;
     private NetworkVehicleController VehicleA;
     private NetworkVehicleController VehicleB;
 */
    private readonly ConcurrentQueue<string> databuffer = new(); //Data buffer queue
    private bool doneSending; //Boolean to check whether data is sent once the application is stopped
    private bool isSending; //Boolean to control the thread send
    private StreamWriter logStream; //List to contain all the different log files
    private float NextUpdate;
    private string path; //Location of the log files


    private bool RECORDING;
    private double ScenarioStartTime;
    private Thread send; //Independent thread for writing and sending data from databuffer
    public static farlab_logger Instance { get; private set; }

    // Use this for initialization
    private void Awake() {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;


        if (enabled) {
#pragma warning disable 0219
/*
            var LeftHandA = new LogVariable("LeftHandA",
                delegate {
                    return HandDataStreamRecorder.Singleton != null
                        ? HandDataStreamRecorder.Singleton.GetLatestState(ParticipantOrder.A, OVRPlugin.Hand.HandLeft)
                        : " ";
                });

            var RightHandA = new LogVariable("RightHandA",
                delegate {
                    return HandDataStreamRecorder.Singleton != null
                        ? HandDataStreamRecorder.Singleton.GetLatestState(ParticipantOrder.A, OVRPlugin.Hand.HandRight)
                        : " ";
                });


            var LeftHandB = new LogVariable("LeftHandB",
                delegate {
                    return HandDataStreamRecorder.Singleton != null
                        ? HandDataStreamRecorder.Singleton.GetLatestState(ParticipantOrder.B, OVRPlugin.Hand.HandLeft)
                        : " ";
                });
            var RightHandB = new LogVariable("RightHandB",
                delegate {
                    return HandDataStreamRecorder.Singleton != null
                        ? HandDataStreamRecorder.Singleton.GetLatestState(ParticipantOrder.B, OVRPlugin.Hand.HandRight)
                        : " ";
                });


*/
#pragma warning restore 0219
        }
    }

    private void Start() {
        doneSending = true;
    }


    // Update is called once per frame
    private void Update() {
        if (!RECORDING) return;
    }


    private void LateUpdate() {
        if (!RECORDING) return;
        if (NextUpdate < Time.time) {
            NextUpdate = Time.time + UpdatedFreqeuncy;


            var outVal = "";
            foreach (var item in logItems) outVal += item.Serialize() + sep;
            EnqueueData(outVal.TrimEnd(';') + "\n\r");
        }
    }


    private void OnApplicationQuit() {
        if (isRecording()) StartCoroutine(StopRecording());
    }

    public bool ReadyToRecord() {
        Debug.Log(RECORDING + doneSending.ToString());
        if (RECORDING) {
            StartCoroutine(StopRecording());
            return false;
        }

        if (!doneSending) return false;

        return true;
    }

    private List<LogItem> logItems;

    public void StartRecording(RerunManager activeManager, string ScenarioName, string sessionName) {
        var folderpath = activeManager.GetCurrentFolderPath() + "/csv/";
        Directory.CreateDirectory(folderpath);
        path = folderpath
               + "CSV"
               + "Scenario-" + ScenarioName + '_'
               + "Session-" + sessionName + '_'
               + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".csv";


        InitLogs();
        logItems = new List<LogItem>();

        logItems.Add(new LogItem(null,
            (refobj) => Time.time.ToString(Fpres),
            "GameTime"
        ));
        logItems.Add(new LogItem(null,
            (refobj) => (Time.time - ScenarioStartTime).ToString(Fpres),
            "ScenarioTime"
        ));
        logItems.Add(new LogItem(null,
            (refobj) => Time.smoothDeltaTime.ToString(Fpres),
            "FrameRate"
        ));
        logItems.Add(new LogItem(null,
            (refobj) => XRDevice.refreshRate.ToString(Fpres),
            "FrameRate-XRDevice"
        ));
        logItems.Add(new LogItem(null,
            (refobj) => Time.frameCount.ToString(),
            "Frame Number"
        ));


        foreach (var vr in FindObjectsOfType<VR_Participant>()) {
            logItems.Add(new LogItem(vr.GetMainCamera(),
                PositonLog,
                $"{vr.m_participantOrder} VR Pos"
            ));

            logItems.Add(new LogItem(vr.GetMainCamera(),
                OrientationLog,
                $"{vr.m_participantOrder} VR Rot"
            ));

            logItems.Add(new LogItem(vr.transform,
                PositonLog,
                $"{vr.m_participantOrder} XR Origin Pos"
            ));

            logItems.Add(new LogItem(vr.transform,
                OrientationLog,
                $"{vr.m_participantOrder} XR Origin  Rot"
            ));
        }


        foreach (var car in FindObjectsOfType<NetworkVehicleController>()) {
            logItems.Add(new LogItem(car.transform,
                PositonLog,
                $"{car.m_participantOrder.Value} car Pos"
            ));

            logItems.Add(new LogItem(car.transform,
                OrientationLog,
                $"{car.m_participantOrder.Value} car Rot"
            ));

            logItems.Add(new LogItem(car,
                (refobj) => ((NetworkVehicleController)refobj).ThrottleInput.ToString(Fpres),
                $"{car.m_participantOrder.Value} accel"
            ));
            logItems.Add(new LogItem(car,
                (refobj) => ((NetworkVehicleController)refobj).SteeringInput.ToString(Fpres),
                $"{car.m_participantOrder.Value} steering"
            ));

            logItems.Add(new LogItem(car.GetComponent<Rigidbody>(),
                (refobj) => ((Rigidbody)refobj).velocity.ToString(Fpres),
                $"{car.m_participantOrder.Value} velocity"
            ));

            logItems.Add(new LogItem(car,
                (refobj) => ((NetworkVehicleController)refobj).GetIndicatorString(),
                $"{car.m_participantOrder.Value} indicators"
            ));

            if (car.VehicleMode == NetworkVehicleController.VehicleOpperationMode.STEERINGWHEEL) {
                logItems.Add(new LogItem(car,
                    (refobj) => SteeringWheelManager.Singleton.GetButtonInput(car.m_participantOrder.Value).ToString(),
                    $"{car.m_participantOrder.Value} Horn Button"
                ));
            }
        }

        foreach (var moco in FindObjectsOfType<Mocopie_Interactable>()) {
            var avatar = moco.GetMocopiAvatar();
            logItems.Add(new LogItem(avatar.transform,
                PositonLog,
                $"{moco.m_participantOrder.Value} Avatar Pos"
            ));
            logItems.Add(new LogItem(avatar.transform,
                OrientationLog,
                $"{moco.m_participantOrder.Value} Avatar Rot"
            ));
            foreach (var bone in BonesToTrack) {
                Transform t_bone = avatar.Animator.GetBoneTransform(bone);
                if (t_bone != null) {
                    logItems.Add(new LogItem(t_bone,
                        PositonLog,
                        $"{moco.m_participantOrder.Value}  Bone Pos {bone}"
                    ));
                    logItems.Add(new LogItem(t_bone,
                        OrientationLog,
                        $"{moco.m_participantOrder.Value}  Bone Rot {bone}"
                    ));
                }
                else {
                    Debug.Log($"Skipping bone {bone}");
                }
            }
        }

        foreach (var hand in FindObjectsOfType<XRHandSkeletonDriver>()) {
            var vrParticipant = hand.transform.GetComponentInParent<VR_Participant>();
            Handedness l_handenness = hand.GetComponent<XRHandTrackingEvents>().handedness;
            logItems.Add(new LogItem(hand.rootTransform,
                PositonLog,
                $"{vrParticipant.m_participantOrder} Root Hand Pos {l_handenness}"
            ));
            logItems.Add(new LogItem(hand.rootTransform,
                OrientationLog,
                $"{vrParticipant.m_participantOrder} Root Hand Rot {l_handenness}"
            ));

            for (int i = 0; i < hand.jointTransformReferences.Count; i++) {
                var local = hand.jointTransformReferences[i];
                logItems.Add(new LogItem(local.jointTransform,
                    PositonLog,
                    $"{vrParticipant.m_participantOrder} Hand Bone Pos {local.xrHandJointID} {l_handenness}"
                ));
                logItems.Add(new LogItem(local.jointTransform,
                    OrientationLog,
                    $"{vrParticipant.m_participantOrder} Hand Bone Rot {local.xrHandJointID} {l_handenness}"
                ));
            }
        }


        var outVal = "";
        foreach (var item in logItems) outVal += item.GetJsonPropertyName() + sep;
        EnqueueData(outVal.TrimEnd(';') + "\n\r");


        doneSending = false;
        isSending = true;
        send = new Thread(ContinuousDataSend);
        send.Start();
        Debug.Log("Started Recording");
        ScenarioStartTime = Time.time;
        RECORDING = true;
    }

    private string PositonLog(object o) {
        return ((Transform)o).position.ToString(Fpres);
    }

    private string OrientationLog(object o) {
        return ((Transform)o).rotation.eulerAngles.ToString(Fpres);
    }


    public IEnumerator StopRecording() {
        isSending = false;
        Debug.Log("Stopping Recording");


        yield return new WaitUntil(() => doneSending);

        RECORDING = false;
        CloseLogs();
        Debug.Log("Stopped Recording");
    }

    private void EnqueueData(string data) {
        databuffer.Enqueue(data);
    }

    private void InitLogs() {
        logStream = File.AppendText(path);
    }

    private void CloseLogs() {
        logStream.Close();
    }


    private void Flush() {
        logStream.Flush();
    }

    //Function to send the first element of the buffer queue
    private void DataSend(string data) {
        //check whether queue is empty
        try {
            logStream.Write(data);
        }
        catch (Exception e) {
            Debug.Log(e);
            //Debug.Log(data);
        }
    }


    private void ContinuousDataSend() {
        var dat = "";
        var count = 0;
        while (isSending) {
            while (databuffer.TryDequeue(out dat)) {
                count += 1;
                DataSend(dat);
            }

            Thread.Sleep(100);
        }

        while (databuffer.TryDequeue(out dat)) DataSend(dat);
        EnqueueData("]");
        doneSending = true;
        Debug.Log(dat);
    }

    public bool isRecording() {
        return RECORDING;
    }


    public class LogItem {
        public object Reference { get; private set; }
        private Func<object, string> logProducer;
        private string jsonPropertyName;

        public LogItem(object reference, Func<object, string> logProducer, string jsonPropertyName) {
            this.Reference = reference;
            this.logProducer = logProducer;
            this.jsonPropertyName = jsonPropertyName;
        }

        public string Serialize() {
            return logProducer(Reference);
        }

        public string GetJsonPropertyName() {
            return jsonPropertyName;
        }
    }

    /**
     * The LogVariable class is a blueprint for the variable to be logged.
     * The attributes are :-
     * 1) dat_id - this is the dataset id for the variable
     * 2) name - this is the name of the variable, this will be the column header
     * for the variable values in the log file
     * 3) val - this is the value of the variable
     * 4) updateMethod - this is the method which is called to update the value of the variable. This method must return a string.
     *
     * To log a variable you need to provide three things :-
     * 1) Dataset ID
     * 2) Variable Name
     * 3) Update method or its value
     * Have a look at the examples in the Awake function.
     */
    /*
    public class LogVariable {
        private static readonly List<LogVariable> allVars = new();
        private string name;
        private readonly Func<string> updateMethod;
        private readonly Action referenceUpdateMethod;
        private string val;


        public LogVariable(string header_name, string value = "-") {
            name = header_name;
            val = value;
            updateMethod = null;
            allVars.Add(this);
            allVars.Sort(CompareByName);
        }

        public LogVariable(string header_name, Func<string> updtMethod,Action refUpdtMethod) {
            name = header_name;
            updateMethod = updtMethod;
            referenceUpdateMethod = refUpdtMethod;

            allVars.Add(this);
            allVars.Sort(CompareByName);
        }

        private static int CompareByName(LogVariable x, LogVariable y) {
            var a = x.GetName().CompareTo(y.GetName());
            if (a == 0)
                a = x.GetName().CompareTo(y.GetName());
            return a;
        }

        public static string GetHeader() {
            var uni = "";
            foreach (var var in allVars) uni += var.GetName() + sep;

            return uni.TrimEnd(';') + "\n\r";
        }

        public static string GetVals() {
            var uni = "";

            foreach (var var in allVars) uni += var.GetVal() + sep;

            return uni.TrimEnd(';') + "\n\r";
        }


        public void SetVal(string value) {
            val = value;
        }

        public void updateVal() {
            val = updateMethod();
        }

        public void SetName(string name) {
            this.name = name;
        }


        public string GetName() {
            return name;
        }

        public string GetVal() {
            if (updateMethod != null)
                updateVal();
            return val;
        }
    }
    */
}