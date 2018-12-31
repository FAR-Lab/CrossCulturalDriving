using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System;

public class farlab_logger : MonoBehaviour {
    //Global variables
    private Transform player;


    public static char sep = ';'; //Separator for data values.

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
    public class LogVariable {
        private string dat_id;
        private string name;
        private string val;
        private Func<string> updateMethod;
        private static List<LogVariable> allVars = new List<LogVariable>();
        private static List<string> IDs = new List<string>();



        public LogVariable(string dataset_id, string header_name, string value = "-") {
            this.dat_id = dataset_id;
            this.name = header_name;
            this.val = value;
            this.updateMethod = null;
            if (!IDs.Contains(dataset_id) && !dataset_id.Equals("U"))
                IDs.Add(dataset_id);
            allVars.Add(this);
            allVars.Sort(CompareByDatId);

        }

        public LogVariable(string dataset_id, string header_name, Func<string> updtMethod) {
            this.dat_id = dataset_id;
            this.name = header_name;
            this.updateMethod = updtMethod;
            if (!IDs.Contains(dataset_id) && !dataset_id.Equals("U"))
                IDs.Add(dataset_id);
            allVars.Add(this);
            allVars.Sort(CompareByDatId);

        }

        private static int CompareByDatId(LogVariable x, LogVariable y) {
            int a = x.GetDatID().CompareTo(y.GetDatID());
            if (a == 0)
                a = x.GetName().CompareTo(y.GetName());
            return a;

        }

        public static List<string> GetHeader() {
            List<string> headers = new List<string>();
            string uni = "";
            string header = "";
            foreach (LogVariable var in allVars) {

                if (!var.GetDatID().Equals("U")) {
                    if (header.Equals(""))
                        header = var.GetDatID() + sep + var.GetName() + sep;
                    else {
                        if (header.Split(sep)[0].Equals(var.GetDatID()))
                            header += var.GetName() + sep;
                        else {
                            headers.Add(header);
                            header = var.GetDatID() + sep + var.GetName() + sep;
                        }
                    }
                } else
                    uni += var.GetName() + sep;

            }
            headers.Add(header);
            for (int i = 0; i < headers.Count; i++) {
                headers[i] = headers[i] + uni.TrimEnd(';') + '\n\r';
            }
            return headers;
        }

        public static List<string> GetVals() {
            List<string> vals = new List<string>();
            string uni = "";
            string v = "";

            foreach (LogVariable var in allVars) {
                if (!var.GetDatID().Equals("U")) {
                    if (v.Equals(""))
                        v = var.GetDatID() + sep + var.GetVal() + sep;
                    else {
                        if (v.Split(sep)[0].Equals(var.GetDatID()))
                            v += var.GetVal() + sep;
                        else {
                            vals.Add(v);
                            v = var.GetDatID() + sep + var.GetVal() + sep;
                            ;
                        }
                    }
                } else
                    uni += var.GetVal() + sep;
            }
            vals.Add(v);
            for (int i = 0; i < vals.Count; i++) {
                vals[i] = vals[i] + uni.TrimEnd(';') + '\n\r';
            }
            return vals;
        }

        public static List<string> GetIDs() {
            return IDs;
        }

        public void SetVal(string value) {
            this.val = value;
        }

        public void updateVal() {
            this.val = this.updateMethod();
        }

        public void SetName(string name) {
            this.name = name;
        }

        public void SetDatID(string dataset_id) {
            this.dat_id = dataset_id;
        }

        public string GetDatID() {
            return this.dat_id;
        }

        public string GetName() {
            return this.name;
        }

        public string GetVal() {
            if (updateMethod != null)
                updateVal();
            return this.val;
        }
    }

    private Queue<string> databuffer = new Queue<string>(); //Data buffer queue

    private string path; //Location of the log files

    private List<StreamWriter> logs; //List to contain all the different log files

    private Thread send; //Independent thread for writing and sending data from databuffer

    private bool isSending; //Boolean to control the thread send
    private bool doneSending; //Boolean to check whether data is sent once the application is stopped

    public string ip; public int port; //IP Address and port of the device to connect to and store logs

    TcpClient client; //To connect to the device via TCP

    NetworkStream stream; //To store the stream from the device


    // Use this for initialization
    private void Awake() {
        //Example of logging various different variables.
        foreach (VehicleInputControllerNetworked vi in FindObjectsOfType<VehicleInputControllerNetworked>()) {
            if (vi.isLocalPlayer) {
                player = vi.transform;
                break;
            }
        }

        
        LogVariable time = new LogVariable("U", "Time", delegate () { return Time.time.ToString(); });
        LogVariable vel = new LogVariable("D1", "Velocity", delegate () { return player != null ? player.transform.GetComponent<Rigidbody>().velocity.ToString() : " "; });
        LogVariable sp = new LogVariable("D1", "Position", delegate () { return player != null ? player.transform.position.ToString() : " "; });
        LogVariable dist = new LogVariable("D1", "Distance", delegate () { return player != null ? player.transform.position.magnitude.ToString() : " "; });
        LogVariable frame = new LogVariable("U", "Frame", delegate () { return Time.frameCount.ToString(); });
        LogVariable pos = new LogVariable("D2", "ActionState", delegate () { return SceneStateManager.Instance.ActionState.ToString(); });
        LogVariable timeScale = new LogVariable("D2", "timeScale", delegate () { return Time.timeScale.ToString(); });

    }
    void Start() {


        int epoch = (int)( System.DateTime.UtcNow - new System.DateTime(1970, 1, 1) ).TotalSeconds; //Epoch Time

        path = @"D:\Logs\" + epoch.ToString() + "-"; //Log file path

        //path_trig = @"C:\Users\Public\Documents\Unity Projects\Roll a Ball\Roll a Ball\Logs\" + epoch.ToString() + "trig.txt"; //Log file of triggers path;

        //port = 23456;
        //ip = "10.132.128.68";

        InitStream(ip, port);

        InitLogs();

        EnqueueData(LogVariable.GetHeader());

        doneSending = false;
        isSending = true;
        send = new Thread(ContinuousDataSend);
        send.Start();
        Debug.Log("Started Running");
    }

    // Update is called once per frame
    void Update() {
        if (player == null) {
            foreach (VehicleInputControllerNetworked vi in FindObjectsOfType<VehicleInputControllerNetworked>()) {
                if (vi.isLocalPlayer) {
                    player = vi.transform;
                    break;
                }
            }
        }
        }
    private void LateUpdate() {


        EnqueueData(LogVariable.GetVals());

    }
    void OnApplicationQuit() {
        isSending = false; //Stop data sending from the independent thread

        /**Checking the status of data and buffer queue and sending
         * the remaining data 
         **/
        Debug.Log("About to shutdown");
        int i = 0;
        while (!doneSending) { i++; }
        CloseStream();
        CloseLogs();
        Debug.Log(" shutdown after " + i.ToString());

        Debug.Log(GetFrame().ToString());
        Debug.Log(GetTime().ToString());
        Debug.Log(GetVelocity(player.gameObject).ToString());
        Debug.Log(GetPos(player.gameObject).ToString());

    }

    void EnqueueData(List<string> data) {
        foreach (string i in data) {
            databuffer.Enqueue(i);
        }
    }

    void InitLogs() {
        logs = new List<StreamWriter>();
        for (int i = 0; i < LogVariable.GetIDs().Count; i++) {
            StreamWriter f = File.AppendText(path + LogVariable.GetIDs()[i] + ".txt");
            logs.Add(f);
        }
    }

    void CloseLogs() {
        foreach (StreamWriter f in logs) {
            f.Close();
        }
    }

    void InitStream(string ip, int port) {
        try {
            /**Create a TcpClient.
             * Note, for this client to work you need to have a TcpServer 
             * connected to the same address as specified by the ip, port
             * combination.
             **/
            client = new TcpClient(ip, port);

            stream = client.GetStream(); // Client stream for reading and writing.
            Debug.Log(stream);
        }
        catch (ArgumentNullException e) {
            Debug.Log("ArgumentNullException: " + e.ToString());
        }
        catch (SocketException e) {
            Debug.Log("SocketException: " + e.ToString());
        }
    }

    void CloseStream() {
        if (stream != null) {
            stream.Close();
        }
        client.Close();
    }

    void Flush() {
        foreach (StreamWriter f in logs) {
            f.Flush();
        }
    }

    //Function to send the first element of the buffer queue
    void DataSend(string data, bool network) {
        //check whether queue is empty
        try {
            if (network) {
                byte[] msg = System.Text.Encoding.ASCII.GetBytes(data);
                stream.Write(msg, 0, msg.Length);
            } else if (!network) {
                int id_index = LogVariable.GetIDs().IndexOf(data.Split(sep)[0]);
                logs[id_index].Write(data);

            }
        }
        catch (Exception e) {
            Debug.Log(e);
            Debug.Log(data);
        }

    }
    //Getting the velocity of the object
    Vector3 GetVelocity(GameObject obj) {
        return obj.transform.GetComponent<Rigidbody>().velocity;
    }

    //Getting the position of the object
    Vector3 GetPos(GameObject obj) {
        return obj.transform.position;
    }


    bool GetState(GameObject obj) {
        return obj.activeSelf;
    }

    //Getting the elapsed time
    float GetTime() {
        return Time.time;
    }

    //Getting the frame count
    int GetFrame() {
        return Time.frameCount;
    }

    void ContinuousDataSend() {
        string dat = "";
        string dat2 = "";
        int count = 0;
        while (isSending) {
            if (databuffer.Count != 0) {
                count += 1;
                dat = databuffer.Dequeue();
                dat2 += dat;
                DataSend(dat, false);
                if (count == 20) {
                    DataSend(dat2, true);
                    count = 0;
                    dat2 = "";
                    Flush();
                }
            }
        }

        while (databuffer.Count != 0) {
            dat = databuffer.Dequeue();
            DataSend(dat, false);
            dat2 += dat;

        }
        DataSend(dat2, true);
        doneSending = true;
        Debug.Log(dat);
        Debug.Log(dat2);

    }
}
