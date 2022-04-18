using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Threading;
using System;
using System.Collections.Concurrent;
using Rerun;


public class farlab_logger : MonoBehaviour
{
    //Global variables
//#if UNITY_EDITOR || UNITY_STANDALONE_WIN

    private Transform PLayerHeadA;
    private Transform PLayerHeadB;
    private Transform CarA;
    private Transform CarB;
    private Rigidbody carARigidbody;
    private Rigidbody carBRigidbody;
    private NetworkVehicleController VehicleA;
    private NetworkVehicleController VehicleB;
    private double ScenarioStartTime = 0.0d;

    public const char sep = ';'; //Separator for data values.
    public const char supSep = '_'; //Separator for values within one cell.

    /**The LogVariable class is a blueprint for the variable to be logged.
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
     **/
    public class LogVariable
    {
        private string name;
        private string val;
        private Func<string> updateMethod;
        private static List<LogVariable> allVars = new List<LogVariable>();


        public LogVariable(string header_name, string value = "-")
        {
            this.name = header_name;
            this.val = value;
            this.updateMethod = null;
            allVars.Add(this);
            allVars.Sort(CompareByName);
        }

        public LogVariable(string header_name, Func<string> updtMethod)
        {
            this.name = header_name;
            this.updateMethod = updtMethod;

            allVars.Add(this);
            allVars.Sort(CompareByName);
        }

        private static int CompareByName(LogVariable x, LogVariable y)
        {
            int a = x.GetName().CompareTo(y.GetName());
            if (a == 0)
                a = x.GetName().CompareTo(y.GetName());
            return a;
        }

        public static string GetHeader()
        {
            string uni = "";
            foreach (LogVariable var in allVars)
            {
                uni += var.GetName() + sep;
            }

            return uni.TrimEnd(';') + "\n\r";
        }

        public static string GetVals()
        {
            string uni = "";

            foreach (LogVariable var in allVars)
            {
                uni += var.GetVal() + sep;
            }

            return uni.TrimEnd(';') + "\n\r";
        }


        public void SetVal(string value)
        {
            this.val = value;
        }

        public void updateVal()
        {
            this.val = this.updateMethod();
        }

        public void SetName(string name)
        {
            this.name = name;
        }


        public string GetName()
        {
            return this.name;
        }

        public string GetVal()
        {
            if (updateMethod != null)
                updateVal();
            return this.val;
        }
    }

    private ConcurrentQueue<string> databuffer = new ConcurrentQueue<string>(); //Data buffer queue
    private string path; //Location of the log files
    private StreamWriter logStream; //List to contain all the different log files
    private Thread send; //Independent thread for writing and sending data from databuffer
    private bool isSending; //Boolean to control the thread send
    private bool doneSending; //Boolean to check whether data is sent once the application is stopped
    private bool RECORDING;

    public float UpdatedFreqeuncy = 1f / 15f;
    private float NextUpdate;


    private static farlab_logger _instance;

    public static farlab_logger Instance
    {
        get { return _instance; }
    }

    // Use this for initialization
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
        }


        if (enabled)
        {
#pragma warning disable 0219


            LogVariable LeftHandA = new LogVariable("LeftHandA",
                delegate()
                {
                    return (HandDataStreamRecorder.Singleton != null)
                        ? HandDataStreamRecorder.Singleton.GetLatestState(ParticipantOrder.A, OVRPlugin.Hand.HandLeft)
                        : " ";
                });
            LogVariable RightHandA = new LogVariable("RightHandA",
                delegate()
                {
                    return (HandDataStreamRecorder.Singleton != null)
                        ? HandDataStreamRecorder.Singleton.GetLatestState(ParticipantOrder.A, OVRPlugin.Hand.HandRight)
                        : " ";
                });


            LogVariable LeftHandB = new LogVariable("LeftHandB",
                delegate()
                {
                    return (HandDataStreamRecorder.Singleton != null)
                        ? HandDataStreamRecorder.Singleton.GetLatestState(ParticipantOrder.B, OVRPlugin.Hand.HandLeft)
                        : " ";
                });
            LogVariable RightHandB = new LogVariable("RightHandB",
                delegate()
                {
                    return (HandDataStreamRecorder.Singleton != null)
                        ? HandDataStreamRecorder.Singleton.GetLatestState(ParticipantOrder.B, OVRPlugin.Hand.HandRight)
                        : " ";
                });

            LogVariable velxA = new LogVariable("CarVelocityXA",
                delegate() { return carARigidbody != null ? carARigidbody.velocity.x.ToString("F4") : " "; });
            LogVariable velyA = new LogVariable("Car VelocityYA",
                delegate() { return carARigidbody != null ? carARigidbody.velocity.y.ToString("F4") : " "; });
            LogVariable velzA = new LogVariable("Car VelocityZA",
                delegate() { return carARigidbody != null ? carARigidbody.velocity.z.ToString("F4") : " "; });

            LogVariable velxB = new LogVariable("Car VelocityXB",
                delegate() { return carARigidbody != null ? carARigidbody.velocity.x.ToString("F4") : " "; });
            LogVariable velyB = new LogVariable("Car VelocityYB",
                delegate() { return carARigidbody != null ? carARigidbody.velocity.y.ToString("F4") : " "; });
            LogVariable velzB = new LogVariable("Car VelocityZB",
                delegate() { return carARigidbody != null ? carARigidbody.velocity.z.ToString("F4") : " "; });


            LogVariable spxA = new LogVariable("Car PositionXA",
                delegate() { return CarA != null ? CarA.position.x.ToString("F4") : " "; });
            LogVariable spyA = new LogVariable("Car PositionYA",
                delegate() { return CarA != null ? CarA.position.y.ToString("F4") : " "; });
            LogVariable spzA = new LogVariable("Car PositionZA",
                delegate() { return CarA != null ? CarA.position.z.ToString("F4") : " "; });
            LogVariable RotxA = new LogVariable("Car RotationXA",
                delegate() { return CarA != null ? CarA.rotation.eulerAngles.x.ToString("F4") : " "; });
            LogVariable RotyA = new LogVariable("Car RotationYA",
                delegate() { return CarA != null ? CarA.rotation.eulerAngles.y.ToString("F4") : " "; });
            LogVariable RotzA = new LogVariable("Car RotationZA",
                delegate() { return CarA != null ? CarA.rotation.eulerAngles.z.ToString("F4") : " "; });

            LogVariable spxB = new LogVariable("Car PositionXB",
                delegate() { return CarA != null ? CarA.position.x.ToString("F4") : " "; });
            LogVariable spyB = new LogVariable("Car PositionYB",
                delegate() { return CarA != null ? CarA.position.y.ToString("F4") : " "; });
            LogVariable spzB = new LogVariable("Car PositionZB",
                delegate() { return CarA != null ? CarA.position.z.ToString("F4") : " "; });
            LogVariable RotxB = new LogVariable("Car RotationXB",
                delegate() { return CarA != null ? CarA.rotation.eulerAngles.x.ToString("F4") : " "; });
            LogVariable RotyB = new LogVariable("Car RotationYB",
                delegate() { return CarA != null ? CarA.rotation.eulerAngles.y.ToString("F4") : " "; });
            LogVariable RotzB = new LogVariable("Car RotationZB",
                delegate() { return CarA != null ? CarA.rotation.eulerAngles.z.ToString("F4") : " "; });


            LogVariable AccelA = new LogVariable("AccelA",
                delegate()
                {
                    return (SteeringWheelManager.Singleton != null)
                        ? SteeringWheelManager.Singleton.GetAccelInput(ParticipantOrder.A).ToString("F4")
                        : "";
                });
            LogVariable AccelB = new LogVariable("AccelB",
                delegate()
                {
                    return (SteeringWheelManager.Singleton != null)
                        ? SteeringWheelManager.Singleton.GetAccelInput(ParticipantOrder.B).ToString("F4")
                        : "";
                });

            LogVariable SteerA = new LogVariable("SteerA",
                delegate()
                {
                    return (SteeringWheelManager.Singleton != null)
                        ? SteeringWheelManager.Singleton.GetSteerInput(ParticipantOrder.A).ToString("F4")
                        : "";
                });
            LogVariable SteerB = new LogVariable("SteerB",
                delegate()
                {
                    return (SteeringWheelManager.Singleton != null)
                        ? SteeringWheelManager.Singleton.GetSteerInput(ParticipantOrder.B).ToString("F4")
                        : "";
                });

            LogVariable ButtonA = new LogVariable("ButtonA",
                delegate()
                {
                    return (SteeringWheelManager.Singleton != null)
                        ? SteeringWheelManager.Singleton.GetButtonInput(ParticipantOrder.A).ToString()
                        : "";
                });
            LogVariable ButtonB = new LogVariable("ButtonB",
                delegate()
                {
                    return (SteeringWheelManager.Singleton != null)
                        ? SteeringWheelManager.Singleton.GetButtonInput(ParticipantOrder.B).ToString()
                        : "";
                });


            LogVariable IndicatorsA = new LogVariable("IndicatorsA",
                delegate()
                {
                    return VehicleA != null
                        ? VehicleA.GetIndicatorString()
                        : "";
                });
            LogVariable IndicatorsB = new LogVariable("IndicatorsB",
                delegate()
                {
                    return VehicleB != null
                        ? VehicleB.GetIndicatorString()
                        : "";
                });

            //Pedestrian Location
            //Head location
            //VectorHandDump


            LogVariable HeadPosXA = new LogVariable("HeadPosXA",
                delegate() { return PLayerHeadA != null ? PLayerHeadA.position.x.ToString("F4") : " "; });
            LogVariable HeadPosYA = new LogVariable("HeadPosYA",
                delegate() { return PLayerHeadA != null ? PLayerHeadA.position.y.ToString("F4") : " "; });
            LogVariable HeadPosZA = new LogVariable("HeadPosZA",
                delegate() { return PLayerHeadA != null ? PLayerHeadA.position.z.ToString("F4") : " "; });
            LogVariable HeadrotXA = new LogVariable("HeadrotXA",
                delegate() { return PLayerHeadA != null ? PLayerHeadA.rotation.eulerAngles.x.ToString("F4") : " "; });
            LogVariable HeadrotYA = new LogVariable("HeadrotYA",
                delegate() { return PLayerHeadA != null ? PLayerHeadA.rotation.eulerAngles.y.ToString("F4") : " "; });
            LogVariable HeadrotZA = new LogVariable("HeadrotZA",
                delegate() { return PLayerHeadA != null ? PLayerHeadA.rotation.eulerAngles.z.ToString("F4") : " "; });


            LogVariable HeadPosXB = new LogVariable("HeadPosXB",
                delegate() { return PLayerHeadB != null ? PLayerHeadB.position.x.ToString("F4") : " "; });
            LogVariable HeadPosYB = new LogVariable("HeadPosYB",
                delegate() { return PLayerHeadB != null ? PLayerHeadB.position.y.ToString("F4") : " "; });
            LogVariable HeadPosZB = new LogVariable("HeadPosZB",
                delegate() { return PLayerHeadB != null ? PLayerHeadB.position.z.ToString("F4") : " "; });

            LogVariable HeadrotXB = new LogVariable("HeadrotXB",
                delegate() { return PLayerHeadB != null ? PLayerHeadB.rotation.eulerAngles.x.ToString("F4") : ""; });
            LogVariable HeadrotYB = new LogVariable("HeadrotYB",
                delegate() { return PLayerHeadB != null ? PLayerHeadB.rotation.eulerAngles.y.ToString("F4") : ""; });
            LogVariable HeadrotZB = new LogVariable("HeadrotZB",
                delegate() { return PLayerHeadB != null ? PLayerHeadB.rotation.eulerAngles.z.ToString("F4") : ""; });


            LogVariable time = new LogVariable("GameTime", delegate() { return Time.time.ToString("F2"); });
            LogVariable realTime = new LogVariable("ScenarioTime",
                delegate() { return (Time.time - ScenarioStartTime).ToString("F4"); });
            LogVariable Framerate = new LogVariable("FrameRate",
                delegate() { return  (1.0f/Time.smoothDeltaTime).ToString(); });

            LogVariable frame = new LogVariable("Frame Number", delegate() { return Time.frameCount.ToString(); });


#pragma warning restore 0219
        }
    }

    private void Start()
    {
        doneSending = true;
    }

    public bool ReadyToRecord()
    {
        Debug.Log(RECORDING.ToString()+doneSending.ToString());
        if (RECORDING)
        {
          
            StartCoroutine(StopRecording());
            return false;
        }

        if (!doneSending)
        {
            return false;
        }

        return true;
    }

    public void StartRecording(RerunManager activeManager,string ScenarioName, string sessionName)
    {
        string folderpath = activeManager.GetCurrentFolderPath() + "/csv/";
        System.IO.Directory.CreateDirectory(folderpath);
         path = folderpath
                      + "CSV"
                      + "Scenario-" + ScenarioName+ '_'
                      + "Session-" + sessionName + '_'
                      + System.DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".csv";
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

    public IEnumerator StopRecording()
    {
        isSending = false;
        Debug.Log("Stopping Recording");


        yield return new WaitUntil(() => doneSending);


        PLayerHeadA = null;
        PLayerHeadB = null;
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

    // Update is called once per frame
    void Update()
    {
        if (!RECORDING)
        {
            return;
        }

        if (PLayerHeadA == null)
        {
            PLayerHeadA = ConnectionAndSpawing.Singleton.GetMainClientObject(ParticipantOrder.A);
        }

        if (PLayerHeadB == null)
        {
            PLayerHeadB = ConnectionAndSpawing.Singleton.GetMainClientObject(ParticipantOrder.B);
        }

        if (CarA == null)
        {
            CarA = ConnectionAndSpawing.Singleton.GetClientObject(ParticipantOrder.A,
                ConnectionAndSpawing.ParticipantObjectSpawnType.CAR);
        }
        else
        {
            if (carARigidbody == null)
            {
                carARigidbody = CarA.GetComponent<Rigidbody>();
            }

            if (VehicleA == null)
            {
                VehicleA = CarA.GetComponent<NetworkVehicleController>();
            }
        }

        if (CarB == null)
        {
            CarB = ConnectionAndSpawing.Singleton.GetClientObject(ParticipantOrder.B,
                ConnectionAndSpawing.ParticipantObjectSpawnType.CAR);
        }
        else
        {
            if (carBRigidbody == null)
            {
                carBRigidbody = CarB.GetComponent<Rigidbody>();
            }

            if (VehicleB == null)
            {
                VehicleB = CarB.GetComponent<NetworkVehicleController>();
            }
        }
    }

    private void LateUpdate()
    {
        if (!RECORDING) return;
        if (NextUpdate < Time.time)
        {
            NextUpdate = Time.time + UpdatedFreqeuncy;
            EnqueueData(LogVariable.GetVals());
        }
    }


    void OnApplicationQuit()
    {
        if (isRecording())
        {
            StartCoroutine(StopRecording());
        }
    }

    void EnqueueData(string data)
    {
        databuffer.Enqueue(data);
    }

    void InitLogs()
    {
        logStream = File.AppendText(path);
    }

    void CloseLogs()
    {
        logStream.Close();
    }


    void Flush()
    {
        logStream.Flush();
    }

    //Function to send the first element of the buffer queue
    void DataSend(string data)
    {
        //check whether queue is empty
        try
        {
            logStream.Write(data);
        }
        catch (Exception e)
        {
            Debug.Log(e);
            //Debug.Log(data);
        }
    }


    void ContinuousDataSend()
    {
        string dat = "";
        int count = 0;
        while (isSending)
        {
            while (databuffer.TryDequeue(out dat))
            {
                count += 1;
                DataSend(dat);
            }

            Thread.Sleep(100);
        }

        while (databuffer.TryDequeue(out dat))
        {
            DataSend(dat);
        }

        doneSending = true;
        Debug.Log(dat);
    }

    public bool isRecording()
    {
        return RECORDING;
    }

}
