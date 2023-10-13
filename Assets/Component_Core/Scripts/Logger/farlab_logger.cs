

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

public class farlab_logger : MonoBehaviour {
    public const char sep = ';'; //Separator for data values.
    public const char supSep = '_'; //Separator for values within one cell.


    public float UpdatedFreqeuncy = 1f / 25f;


    private Transform CarA;
    private Rigidbody carARigidbody;
    private Transform CarB;
    private Rigidbody carBRigidbody;

    private readonly ConcurrentQueue<string> databuffer = new(); //Data buffer queue
    private bool doneSending; //Boolean to check whether data is sent once the application is stopped
    private bool isSending; //Boolean to control the thread send
    private StreamWriter logStream; //List to contain all the different log files
    private float NextUpdate;
    private string path; //Location of the log files

    private Transform PlayerHeadA;

    private Transform PlayerHeadB;
    //Global variables
//#if UNITY_EDITOR || UNITY_STANDALONE_WIN

    private Transform PlayerVRCenterA;
    private Transform PlayerVRCenterB;
    private bool RECORDING;
    private double ScenarioStartTime;
    private Thread send; //Independent thread for writing and sending data from databuffer
    private NetworkVehicleController VehicleA;
    private NetworkVehicleController VehicleB;

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

            var velxA = new LogVariable("CarVelocityXA",
                delegate { return carARigidbody != null ? carARigidbody.velocity.x.ToString("F4") : " "; });
            var velyA = new LogVariable("Car VelocityYA",
                delegate { return carARigidbody != null ? carARigidbody.velocity.y.ToString("F4") : " "; });
            var velzA = new LogVariable("Car VelocityZA",
                delegate { return carARigidbody != null ? carARigidbody.velocity.z.ToString("F4") : " "; });


            var spxA = new LogVariable("Car PositionXA",
                delegate { return CarA != null ? CarA.position.x.ToString("F4") : " "; });
            var spyA = new LogVariable("Car PositionYA",
                delegate { return CarA != null ? CarA.position.y.ToString("F4") : " "; });
            var spzA = new LogVariable("Car PositionZA",
                delegate { return CarA != null ? CarA.position.z.ToString("F4") : " "; });
            var RotxA = new LogVariable("Car RotationXA",
                delegate { return CarA != null ? CarA.rotation.eulerAngles.x.ToString("F4") : " "; });
            var RotyA = new LogVariable("Car RotationYA",
                delegate { return CarA != null ? CarA.rotation.eulerAngles.y.ToString("F4") : " "; });
            var RotzA = new LogVariable("Car RotationZA",
                delegate { return CarA != null ? CarA.rotation.eulerAngles.z.ToString("F4") : " "; });

            var spxB = new LogVariable("Car PositionXB",
                delegate { return CarB != null ? CarB.position.x.ToString("F4") : " "; });
            var spyB = new LogVariable("Car PositionYB",
                delegate { return CarB != null ? CarB.position.y.ToString("F4") : " "; });
            var spzB = new LogVariable("Car PositionZB",
                delegate { return CarB != null ? CarB.position.z.ToString("F4") : " "; });
            var RotxB = new LogVariable("Car RotationXB",
                delegate { return CarB != null ? CarB.rotation.eulerAngles.x.ToString("F4") : " "; });
            var RotyB = new LogVariable("Car RotationYB",
                delegate { return CarB != null ? CarB.rotation.eulerAngles.y.ToString("F4") : " "; });
            var RotzB = new LogVariable("Car RotationZB",
                delegate { return CarB != null ? CarB.rotation.eulerAngles.z.ToString("F4") : " "; });

            var velxB = new LogVariable("Car VelocityXB",
                delegate { return carBRigidbody != null ? carBRigidbody.velocity.x.ToString("F4") : " "; });
            var velyB = new LogVariable("Car VelocityYB",
                delegate { return carBRigidbody != null ? carBRigidbody.velocity.y.ToString("F4") : " "; });
            var velzB = new LogVariable("Car VelocityZB",
                delegate { return carBRigidbody != null ? carBRigidbody.velocity.z.ToString("F4") : " "; });

            var AccelA = new LogVariable("AccelA",
                delegate {
                    return SteeringWheelManager.Singleton != null
                        ? SteeringWheelManager.Singleton.GetAccelInput(ParticipantOrder.A).ToString("F4")
                        : "";
                });
            var AccelB = new LogVariable("AccelB",
                delegate {
                    return SteeringWheelManager.Singleton != null
                        ? SteeringWheelManager.Singleton.GetAccelInput(ParticipantOrder.B).ToString("F4")
                        : "";
                });

            var SteerA = new LogVariable("SteerA",
                delegate {
                    return SteeringWheelManager.Singleton != null
                        ? SteeringWheelManager.Singleton.GetSteerInput(ParticipantOrder.A).ToString("F4")
                        : "";
                });
            var SteerB = new LogVariable("SteerB",
                delegate {
                    return SteeringWheelManager.Singleton != null
                        ? SteeringWheelManager.Singleton.GetSteerInput(ParticipantOrder.B).ToString("F4")
                        : "";
                });

            var ButtonA = new LogVariable("ButtonA",
                delegate {
                    return SteeringWheelManager.Singleton != null
                        ? SteeringWheelManager.Singleton.GetHornInput(ParticipantOrder.A).ToString()
                        : "";
                });
            var ButtonB = new LogVariable("ButtonB",
                delegate {
                    return SteeringWheelManager.Singleton != null
                        ? SteeringWheelManager.Singleton.GetHornInput(ParticipantOrder.B).ToString()
                        : "";
                });


            var IndicatorsA = new LogVariable("IndicatorsA",
                delegate {
                    return VehicleA != null
                        ? VehicleA.GetIndicatorString()
                        : "";
                });
            var IndicatorsB = new LogVariable("IndicatorsB",
                delegate {
                    return VehicleB != null
                        ? VehicleB.GetIndicatorString()
                        : "";
                });

            //Pedestrian Location
            //Head location
            //VectorHandDump


            var HeadPosXA = new LogVariable("HeadPosXA",
                delegate { return PlayerVRCenterA != null ? PlayerVRCenterA.position.x.ToString("F4") : " "; });
            var HeadPosYA = new LogVariable("HeadPosYA",
                delegate { return PlayerVRCenterA != null ? PlayerVRCenterA.position.y.ToString("F4") : " "; });
            var HeadPosZA = new LogVariable("HeadPosZA",
                delegate { return PlayerVRCenterA != null ? PlayerVRCenterA.position.z.ToString("F4") : " "; });
            var HeadrotXA = new LogVariable("HeadrotXA",
                delegate {
                    return PlayerVRCenterA != null ? PlayerVRCenterA.rotation.eulerAngles.x.ToString("F4") : " ";
                });
            var HeadrotYA = new LogVariable("HeadrotYA",
                delegate {
                    return PlayerVRCenterA != null ? PlayerVRCenterA.rotation.eulerAngles.y.ToString("F4") : " ";
                });
            var HeadrotZA = new LogVariable("HeadrotZA",
                delegate {
                    return PlayerVRCenterA != null ? PlayerVRCenterA.rotation.eulerAngles.z.ToString("F4") : " ";
                });


            var HeadPosXB = new LogVariable("HeadPosXB",
                delegate { return PlayerVRCenterB != null ? PlayerVRCenterB.position.x.ToString("F4") : " "; });
            var HeadPosYB = new LogVariable("HeadPosYB",
                delegate { return PlayerVRCenterB != null ? PlayerVRCenterB.position.y.ToString("F4") : " "; });
            var HeadPosZB = new LogVariable("HeadPosZB",
                delegate { return PlayerVRCenterB != null ? PlayerVRCenterB.position.z.ToString("F4") : " "; });

            var HeadrotXB = new LogVariable("HeadrotXB",
                delegate {
                    return PlayerVRCenterB != null ? PlayerVRCenterB.rotation.eulerAngles.x.ToString("F4") : "";
                });
            var HeadrotYB = new LogVariable("HeadrotYB",
                delegate {
                    return PlayerVRCenterB != null ? PlayerVRCenterB.rotation.eulerAngles.y.ToString("F4") : "";
                });
            var HeadrotZB = new LogVariable("HeadrotZB",
                delegate {
                    return PlayerVRCenterB != null ? PlayerVRCenterB.rotation.eulerAngles.z.ToString("F4") : "";
                });


            var VRPosXA = new LogVariable("VRPosXA",
                delegate { return PlayerHeadA != null ? PlayerHeadA.position.x.ToString("F4") : " "; });
            var VRPosYA = new LogVariable("VRPosYA",
                delegate { return PlayerHeadA != null ? PlayerHeadA.position.y.ToString("F4") : " "; });
            var VRPosZA = new LogVariable("VRPosZA",
                delegate { return PlayerHeadA != null ? PlayerHeadA.position.z.ToString("F4") : " "; });
            var VRrotXA = new LogVariable("VRRotXA",
                delegate { return PlayerHeadA != null ? PlayerHeadA.rotation.eulerAngles.x.ToString("F4") : " "; });
            var VRrotYA = new LogVariable("VRRotYA",
                delegate { return PlayerHeadA != null ? PlayerHeadA.rotation.eulerAngles.y.ToString("F4") : " "; });
            var VRrotZA = new LogVariable("VRRotZA",
                delegate { return PlayerHeadA != null ? PlayerHeadA.rotation.eulerAngles.z.ToString("F4") : " "; });


            var VRPosXB = new LogVariable("VRPosXB",
                delegate { return PlayerHeadB != null ? PlayerHeadB.position.x.ToString("F4") : " "; });
            var VRPosYB = new LogVariable("VRPosYB",
                delegate { return PlayerHeadB != null ? PlayerHeadB.position.y.ToString("F4") : " "; });
            var VRPosZB = new LogVariable("VRPosZB",
                delegate { return PlayerHeadB != null ? PlayerHeadB.position.z.ToString("F4") : " "; });
            var VRrotXB = new LogVariable("VRRotXB",
                delegate { return PlayerHeadB != null ? PlayerHeadB.rotation.eulerAngles.x.ToString("F4") : " "; });
            var VRrotYB = new LogVariable("VRRotYB",
                delegate { return PlayerHeadB != null ? PlayerHeadB.rotation.eulerAngles.y.ToString("F4") : " "; });
            var VRrotZB = new LogVariable("VRRotZB",
                delegate { return PlayerHeadB != null ? PlayerHeadB.rotation.eulerAngles.z.ToString("F4") : " "; });


            var time = new LogVariable("GameTime", delegate { return Time.time.ToString("F2"); });
            var realTime = new LogVariable("ScenarioTime",
                delegate { return (Time.time - ScenarioStartTime).ToString("F4"); });
            var Framerate = new LogVariable("FrameRate ",
                delegate { return (1.0f / Time.smoothDeltaTime).ToString(); });
            var FramerateNew = new LogVariable("FrameRate-XRDevice",
                delegate { return (1.0f / XRDevice.refreshRate).ToString(); });

            var frame = new LogVariable("Frame Number", delegate { return Time.frameCount.ToString(); });
*/

#pragma warning restore 0219
        }
    }

    private void Start() {
        doneSending = true;
    }

    /*
    // Update is called once per frame
    private void Update() {
        
        if (!RECORDING) return;

        if (PlayerVRCenterA == null)
            PlayerVRCenterA = ConnectionAndSpawning.Singleton.GetMainClientObject(ParticipantOrder.A);

        if (PlayerVRCenterB == null)
            PlayerVRCenterB = ConnectionAndSpawning.Singleton.GetMainClientObject(ParticipantOrder.B);


        if (PlayerHeadA == null)
            PlayerHeadA = ConnectionAndSpawning.Singleton.GetClientMainCameraObject(ParticipantOrder.A);

        if (PlayerHeadB == null)
            PlayerHeadB = ConnectionAndSpawning.Singleton.GetClientMainCameraObject(ParticipantOrder.B);


        if (CarA == null) {
            CarA = ConnectionAndSpawning.Singleton.GetInteractableObjects_For_Participants(ParticipantOrder.A).First()
                .transform;
        
    }
        else {
            if (carARigidbody == null) carARigidbody = CarA.GetComponent<Rigidbody>();

            if (VehicleA == null) VehicleA = CarA.GetComponent<NetworkVehicleController>();
        }

        if (CarB == null) {
            CarB = ConnectionAndSpawning.Singleton.GetInteractableObjects_For_Participants(ParticipantOrder.B).First()
                .transform;
        }
        else {
            if (carBRigidbody == null) carBRigidbody = CarB.GetComponent<Rigidbody>();

            if (VehicleB == null) VehicleB = CarB.GetComponent<NetworkVehicleController>();
        }
    }
    */

    private void LateUpdate() {
        if (!RECORDING) return;
        if (NextUpdate < Time.time) {
            NextUpdate = Time.time + UpdatedFreqeuncy;
            EnqueueData(LogVariable.GetVals());
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

    public void StartRecording(RerunManager activeManager, string ScenarioName, string sessionName) {
        var folderpath = activeManager.GetCurrentFolderPath() + "/csv/";
        Directory.CreateDirectory(folderpath);
        path = folderpath
               + "CSV"
               + "Scenario-" + ScenarioName + '_'
               + "Session-" + sessionName + '_'
               + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".csv";
        InitLogs();

        EnqueueData(LogVariable.GetHeader());

        doneSending = false;
        isSending = true;
        send = new Thread(ContinuousDataSend);
        send.Start();
        Debug.Log("Started Recording");
        ScenarioStartTime = Time.time;
        RECORDING = true;
    }

    public IEnumerator StopRecording() {
        isSending = false;
        Debug.Log("Stopping Recording");


        yield return new WaitUntil(() => doneSending);


        PlayerVRCenterA = null;
        PlayerVRCenterB = null;
        CarA = null;
        CarB = null;
        carARigidbody = null;
        carBRigidbody = null;
        VehicleA = null;
        VehicleB = null;
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

        doneSending = true;
        Debug.Log(dat);
    }

    public bool isRecording() {
        return RECORDING;
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
    public class LogVariable {
        private static readonly List<LogVariable> allVars = new();
        private string name;
        private readonly Func<string> updateMethod;
        private string val;


        public LogVariable(string header_name, string value = "-") {
            name = header_name;
            val = value;
            updateMethod = null;
            allVars.Add(this);
            allVars.Sort(CompareByName);
        }

        public LogVariable(string header_name, Func<string> updtMethod) {
            name = header_name;
            updateMethod = updtMethod;

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
}