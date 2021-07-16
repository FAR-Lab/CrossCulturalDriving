using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System;
using UnityEngine;

public class QNLogger : MonoBehaviour
{
    private Queue<string> databuffer = new Queue<string>();
    private double epoch;
    public static char sep = ';'; //Separator for data values.
    private string path; //Location of the log files
    private Thread send; //Independent thread for writing and sending data from databuffer
    private StreamWriter sw;

    private bool isSending; //Boolean to control the thread send
    private bool doneSending; //Boolean to check whether data is sent once the application is stopped
    private static QNLogger _instance;
    public static QNLogger Instance { get { return _instance; } }
    public string qnName;
    private string participantID = "test";
    void Start()
    {
        epoch = (double)((System.DateTime.UtcNow - new System.DateTime(1970, 1, 1)).TotalSeconds - Time.time); //Epoch Time
        DateTime Now = System.DateTime.Now;
        path = @"C:\Users\ryanj\UnityProjects\" + Now.Year.ToString() + "-" + Now.Month.ToString() + "-" + Now.Day.ToString() + "-" + Now.Hour.ToString() + "-" + Now.Minute.ToString() + "-" + Now.Second.ToString() + "-"; //Log file path
        sw = File.AppendText(path + participantID + qnName + ".csv");
        doneSending = false;
        isSending = true;
        send = new Thread(ContinuousDataSend);
        send.Start();
        Debug.Log("Started Running");

    }
    void Update()
    {
        
    }

    public void EnqueEventLog(string log) {
        log += " - " + Time.time.ToString("F4") + " - " + Time.frameCount;
        databuffer.Enqueue(log);
    }

    void DataSend(string data, bool network) {
        //check whether queue is empty
        try {
            if (network) {
            } else if (!network) {
                sw.WriteLine(data);

            }
        } catch (Exception e) {
            //Debug.Log(e);
            //Debug.Log(data);
        }

    }
    private string AddEscapeSequenceInCsvField(string ValueToEscape) {
        if (ValueToEscape.Contains(",")) {
            return "\"" + ValueToEscape + "\"";
        } else {
            return ValueToEscape;
        }
    }

    void ContinuousDataSend() {
        string dat = "";
        string dat2 = "";
        int count = 0;
        while (isSending) {
            if (databuffer.Count != 0) {
                count += 1;
                dat = AddEscapeSequenceInCsvField(databuffer.Dequeue());
                dat2 += dat;
                DataSend(dat, false);
                if (count == 30) {
                    DataSend(dat2, true);
                    count = 0;
                    dat2 = "";
                    sw.Flush();
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

    void OnDestroy() {
        isSending = false; //Stop data sending from the independent thread

        /**Checking the status of data and buffer queue and sending
         * the remaining data 
         **/
        Debug.Log("About to shutdown");
        int i = 0;
        while (!doneSending) { i++; }
        sw.Flush();
        sw.Close();

        Debug.Log(" shutdown after " + i.ToString());

    }



}
